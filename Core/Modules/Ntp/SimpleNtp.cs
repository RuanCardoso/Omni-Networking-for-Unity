using System;
using Omni.Shared;
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
        public static double Time => IsTickTimingEnabled ? TickSystem.ElapsedTicks : UnityEngine.Time.unscaledTimeAsDouble;

        /// <summary>
        /// Gets the time delta value based on the timing system configuration.
        /// Returns the tick delta from TickSystem when UseTickTiming is true, otherwise returns Unity's frame delta time.
        /// This provides a consistent way to measure time differences regardless of the underlying timing mechanism.
        /// </summary>
        public static double Delta => IsTickTimingEnabled ? TickSystem.DeltaTick : UnityEngine.Time.deltaTime;

        internal void Initialize(NetworkClock networkClock)
        {
            if (networkClock == null)
            {
                Client = new NtpClient();
                Server = new NtpServer();
            }
            else
            {
                Client = new NtpClient(networkClock);
                Server = new NtpServer(networkClock);
            }
        }

        public class NtpClient
        {
            private const double PING_TIME_PRECISION = 0.035d;
            private const double PING_TICK_PRECISION = 0.035d * 1000d;

            /// <summary>
            /// Returns the synchronized time or ticks, which is the unity time plus the smoothed offset average.
            /// </summary>
            public double Time => IsTickTimingEnabled ? (int)(SimpleNtp.Time + OffsetAvg.Average) : NetworkHelper.Truncate((SimpleNtp.Time + OffsetAvg.Average), 3);

            /// <summary>
            /// Returns the round-trip time (RTT) smoothed average.
            /// </summary>
            public double Rtt => RttAvg.Average;

            /// <summary>
            /// Returns the half round-trip time (RTT) smoothed average.
            /// </summary>
            public double HalfRtt => Rtt / 2d;

            /// <summary>
            /// Retrieves the ping time in milliseconds.
            /// </summary>
            public double Ping => (int)(NetworkHelper.MinMax(Rtt, IsTickTimingEnabled ? PING_TICK_PRECISION : PING_TIME_PRECISION) * (IsTickTimingEnabled ? TickSystem.MsPerTick : 1000d));

            /// <summary>
            /// Simple Moving Average (SMA) for RTT measurements to smooth out short-term fluctuations.
            /// </summary>
            private SimpleMovingAverage RttAvg { get; }

            /// <summary>
            /// The Exponential Moving Average (EMA) algorithm can play a valuable role in time synchronization systems like the Network Time Protocol (NTP).
            /// By applying EMA to the time data provided by NTP servers, abrupt variations in times can be smoothed out and temporal drift trends can be effectively detected.
            /// EMA's sensitivity to the most recent data allows for a quick response to changes in synchronization times, contributing to a faster and more accurate adaptation of the network devices' clocks.
            /// This smoothed approach not only improves stability in synchronization by mitigating temporal fluctuations but also provides a solid foundation for detecting and correcting temporal deviations.
            /// Thus, it helps maintain time accuracy in environments that heavily rely on time synchronization, such as NTP systems.
            /// </summary>
            private IMovingAverage OffsetAvg { get; }

            internal NtpClient()
            {
                RttAvg = new SimpleMovingAverage(NetworkClock.DEFAULT_RTT_WINDOW);
                OffsetAvg = IsTickTimingEnabled
                    ? new SimpleMovingAverage(NetworkClock.DEFAULT_TIME_WINDOW)
                    : new ExponentialMovingAverage(NetworkClock.DEFAULT_TIME_WINDOW);
            }

            internal NtpClient(NetworkClock clock)
            {
                RttAvg = new SimpleMovingAverage(clock.RttWindow);
                OffsetAvg = IsTickTimingEnabled
                    ? new SimpleMovingAverage(clock.TimeWindow)
                    : new ExponentialMovingAverage(clock.TimeWindow);
            }

            // The Client reads its clock, which provides the time a.
            // The Client sends Message 1 with the timestamp a to the server.
            // The Server receives Message 1 and reads its clock at that moment, which provides the timestamp x. The Server stores a and x in variables.
            // After some time, the Server reads its clock again, which provides the timestamp y.
            // The Server sends Message 2 with a, x, and y to the client.
            // The Client receives Message 2 and reads its clock at that moment, which provides the timestamp b.
            internal void Query()
            {
                using var message = Pool.Rent(enableTracking: false);
                message.Write(SimpleNtp.Time);
                NetworkManager.ClientSide.SendMessage(MessageType.NtpQuery, message, DeliveryMode.Unreliable, 0);
            }

            // https://ntp.br/conteudo/ntp/#:~:text=NTP%20significa%20Network%20Time%20Protocol,de%20refer%C3%AAncias%20de%20tempo%20confi%C3%A1veis.
            // https://info.support.huawei.com/info-finder/encyclopedia/en/NTP.html
            internal void Evaluate(double a, double x, double y)
            {
                double b = SimpleNtp.Time;
                // Rtt(Round-Trip-Time) (Delay) = (b-a)-(y-x)
                double rtt = (b - a) - (y - x);
                RttAvg.Add(rtt);
                // Given that the round trip time is equal to the return time, the displacement between the server and the local clock can be calculated as follows:
                // Offset = ((T2-T1) + (T3-T4))/2
                double timeOffset = ((x - a) + (y - b)) / 2d;
                OffsetAvg.Add(timeOffset);
            }
        }

        public class NtpServer
        {
            private int yInstantAccuracy;
            public double Time => IsServerActive ? SimpleNtp.Time : 0d;

            internal NtpServer()
            {
                yInstantAccuracy = NetworkClock.DEFAULT_ACCURACY;
            }

            internal NtpServer(NetworkClock clock)
            {
                yInstantAccuracy = clock.Accuracy;
            }

            // https://techhub.hpe.com/eginfolib/networking/docs/switches/5820x-5800/5998-7395r_nmm_cg/content/441755722.htm
            internal void SendNtpResponse(double time, NetworkPeer peer)
            {
                double a = time; // client time
                double x = SimpleNtp.Time; // server time

                using var message = Pool.Rent(enableTracking: false);
                message.Write(a);
                message.Write(x);
                TransmitNtpMessage(peer, message);
            }

            private void TransmitNtpMessage(NetworkPeer peer, DataBuffer message)
            {
                double y = SimpleNtp.Time;
                message.Write(y);
                NetworkManager.ServerSide.SendMessage(MessageType.NtpQuery, peer, message, target: Target.SelfOnly, deliveryMode: DeliveryMode.Unreliable, sequenceChannel: 0);
            }
        }
    }
}