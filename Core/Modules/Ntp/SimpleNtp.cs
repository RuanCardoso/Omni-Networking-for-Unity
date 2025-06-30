using System;
using Omni.Shared;
using UnityEngine;
using static Omni.Core.NetworkManager;

#pragma warning disable

// does not depend on frame rate.
namespace Omni.Core.Modules.Ntp
{
    // https://pt.wikipedia.org/wiki/Network_Time_Protocol
    /// <summary>
    /// NTP stands for Network Time Protocol.
    /// It is the standard that allows synchronization of the clocks of network devices such as servers, workstations, routers, and other equipment using reliable time references.
    /// In addition to the communication protocol itself, NTP defines a set of algorithms used to query servers, calculate time differences, estimate errors, choose the best references, and adjust the local clock.
    /// </summary>
    public class SimpleNtp
    {
        internal const int k_DefaultPingWindow = 8;
        internal const int k_DefaultTimeWindow = 60;
        internal const int k_DefaultRttWindow = 20;
        internal const float k_DefaultQueryInterval = 1f;

        /// <summary>
        /// Gets the NTP client clock which provides synchronized time functionality.
        /// This component handles time offset calculations, round-trip time measurements, 
        /// and provides access to synchronization metrics like ping and time offset.
        /// Use this on the client side to access synchronized time values.
        /// </summary>
        public NtpClient Client { get; private set; }

        /// <summary>
        /// Gets the NTP server clock which manages the authoritative time source.
        /// This component responds to time synchronization requests from clients,
        /// maintaining the reference time that all clients synchronize with.
        /// Use this on the server side to access or reference the authoritative time.
        /// </summary>
        public NtpServer Server { get; private set; }

        /// <summary>
        /// Gets the current time value based on the timing system configuration.
        /// Returns ticks from TickSystem when UseTickTiming is true, otherwise returns Unity's unscaled time.
        /// </summary>
        public static double ElapsedTicks => TickSystem.ElapsedTicks;

        internal void Initialize()
        {
            Client = new NtpClient();
            Server = new NtpServer();
        }

        public class NtpClient
        {
            /// <summary>
            /// Gets the synchronized time value by combining the base elapsed ticks with the calculated time offset.
            /// This value represents the client's best estimate of the server's time, accounting for network latency
            /// and clock drift through the smoothed offset average.
            /// </summary>
            public long SyncedTime => (long)Math.Round((SimpleNtp.ElapsedTicks + OffsetAvg.Average));

            /// <summary>
            /// Returns the round-trip time (RTT) smoothed average.
            /// </summary>
            public double Rtt => RttAvg.Average;

            /// <summary>
            /// Returns the half round-trip time (RTT) smoothed average.
            /// </summary>
            public double HalfRtt => Rtt / 2d;

            /// <summary>
            /// Retrieves the ping time in milliseconds in real time.
            /// </summary>
            public int Ping => (int)Math.Round(PingAvg.Average * 1000.0);

            /// <summary>
            /// Retrieves the ping time in milliseconds based in tick time.
            /// </summary>
            public int Ping2 => (int)Math.Round(Rtt * TickSystem.MsPerTick);

            private ExponentialMovingAverage PingAvg { get; }
            private ExponentialMovingAverage RttAvg { get; }
            private ExponentialMovingAverage OffsetAvg { get; }

            internal NtpClient()
            {
                PingAvg = new ExponentialMovingAverage(k_DefaultPingWindow);
                RttAvg = new ExponentialMovingAverage(k_DefaultRttWindow);
                OffsetAvg = new ExponentialMovingAverage(k_DefaultTimeWindow);
            }

            // The Client reads its clock, which provides the time a(T1).
            // The Client sends Message 1 with the timestamp a(T1) to the server.
            // The Server receives Message 1 and reads its clock at that moment, which provides the timestamp x(T2). The Server stores a(T1) and x(T2) in variables.
            // After some time, the Server reads its clock again, which provides the timestamp y(T3).
            // The Server sends Message 2 with a(T1), x(T2), and y(T3) to the client.
            // The Client receives Message 2 and reads its clock at that moment, which provides the timestamp b(T4).
            internal void Query()
            {
                using var message = Pool.Rent(enableTracking: false);
                // Write the current client timestamp a(T1) to the message
                // This timestamp will be used as the reference point for calculating network latency and clock offset
                // Note: The actual time value includes any accumulated frame delays between send and receive
                message.Write(SimpleNtp.ElapsedTicks);
                message.Write(Time.realtimeSinceStartup);
                ClientSide.SetDeliveryMode(DeliveryMode.Unreliable);
                ClientSide.SendMessage(NetworkPacketType.k_NtpQuery, message);
            }

            // https://ntp.br/conteudo/ntp/#:~:text=NTP%20significa%20Network%20Time%20Protocol,de%20refer%C3%AAncias%20de%20tempo%20confi%C3%A1veis.
            // https://info.support.huawei.com/info-finder/encyclopedia/en/NTP.html
            internal void Evaluate(double a, double x, double y, float realtimeSinceStartup)
            {
                float now = Time.realtimeSinceStartup;
                float pingMs = now - realtimeSinceStartup;
                PingAvg.Add(pingMs);
                // T1 (a) = Client's timestamp when sending request
                // T2 (x) = Server's timestamp when receiving request  
                // T3 (y) = Server's timestamp when sending response
                // T4 (b) = Client's timestamp when receiving response
                double b = SimpleNtp.ElapsedTicks;
                // Calculate Round-Trip-Time (RTT) or network delay
                // RTT = (Client receive time - Client send time) - (Server send time - Server receive time)
                // This measures the total time for a message to travel from client to server and back,
                // minus the processing time on the server side
                double rtt = (b - a) - (y - x);
                RttAvg.Add(rtt);
                // The time offset between server and client clocks is calculated using the NTP algorithm:
                // Assuming symmetric network delay, the time offset is:
                // Offset = ((T2-T1) + (T3-T4))/2 = ((x-a) + (y-b))/2
                double timeOffset = ((x - a) + (y - b)) / 2d;
                OffsetAvg.Add(timeOffset);
            }
        }

        public class NtpServer
        {
            public double LocalTime => IsServerActive ? SimpleNtp.ElapsedTicks : 0d;

            // https://techhub.hpe.com/eginfolib/networking/docs/switches/5820x-5800/5998-7395r_nmm_cg/content/441755722.htm
            internal void SendResponse(double time, NetworkPeer peer, float realtimeSinceStartup)
            {
                double a = time; // client time
                double x = SimpleNtp.ElapsedTicks; // server time

                using var message = Pool.Rent(enableTracking: false);
                message.Write(a);
                message.Write(x);
                SendMessage(peer, message, realtimeSinceStartup);
            }

            private void SendMessage(NetworkPeer peer, DataBuffer message, float realtimeSinceStartup)
            {
                double y = SimpleNtp.ElapsedTicks;
                message.Write(y);
                message.Write(realtimeSinceStartup);
                ServerSide.SetDefaultNetworkConfiguration(DeliveryMode.Unreliable, Target.Self, null, 0);
                ServerSide.SendMessage(NetworkPacketType.k_NtpQuery, peer, message);
            }
        }
    }
}