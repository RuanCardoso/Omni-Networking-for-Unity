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
        /// Gets the NTP client clock.
        /// </summary>
        public NtpClient Client { get; private set; }

        /// <summary>
        /// Gets the NTP server clock.
        /// </summary>
        public NtpServer Server { get; private set; }

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
            private const double PING_TIME_PRECISION = 0.01d;
            private const double PING_TICK_PRECISION = 0.01d * 1000d;

            /// <summary>
            /// Returns the synchronized time or ticks, which is the unity time plus the smoothed offset average.
            /// </summary>
            public double Time =>
                Math.Round(ClockTime + OffsetAvg.Average, UseTickTiming ? 0 : 2);

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
            public double Ping =>
                UseTickTiming
                    ? Math.Round(MinMax(HalfRtt * TickSystem.MsPerTick, PING_TICK_PRECISION), 0)
                    : Math.Round(MinMax(HalfRtt, PING_TIME_PRECISION) * 1000d, 0);

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
            private IMovingAverage OffsetAvg { get; } // default EMA

            internal NtpClient()
            {
                RttAvg = new SimpleMovingAverage(NetworkClock.DEFAULT_RTT_WINDOW);
                OffsetAvg = UseTickTiming
                    ? new SimpleMovingAverage(NetworkClock.DEFAULT_TIME_WINDOW)
                    : new ExponentialMovingAverage(NetworkClock.DEFAULT_TIME_WINDOW);
            }

            internal NtpClient(NetworkClock clock)
            {
                RttAvg = new SimpleMovingAverage(clock.RttWindow);
                OffsetAvg = UseTickTiming
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
                using var message = Pool.Rent();
                message.Write(ClockTime);
                message.Write(DeltaTime);

                // Query the server.
                NetworkManager.Client.SendMessage(
                    MessageType.NtpQuery,
                    message,
                    DeliveryMode.Unreliable,
                    0
                );
            }

            // https://ntp.br/conteudo/ntp/#:~:text=NTP%20significa%20Network%20Time%20Protocol,de%20refer%C3%AAncias%20de%20tempo%20confi%C3%A1veis.
            // https://info.support.huawei.com/info-finder/encyclopedia/en/NTP.html
            internal void Evaluate(double a, double x, double y, float t)
            {
                double b = ClockTime;
                // y += t(deltaTime) - used to offset timing from one frame to the next frame and maintain accuracy.
                y += t;
                // Rtt(Round-Trip-Time) (Delay) = (b-a)-(y-x)
                double rtt = (b - a) - (y - x);
                RttAvg.Add(rtt);
                // Given that the round trip time is equal to the return time, the displacement between the server and the local clock can be calculated as follows:
                // Offset = ((T2-T1) + (T3-T4))/2
                double timeOffset = ((x - a) + (y - b)) / 2d;
                OffsetAvg.Add(timeOffset);
            }

            private double MinMax(double value, double min)
            {
                return value < min ? 0 : value - min;
            }
        }

        public class NtpServer
        {
            private int yInstantAccuracy;
            public double Time => IsServerActive ? ClockTime : 0d;

            internal NtpServer()
            {
                yInstantAccuracy = NetworkClock.DEFAULT_ACCURACY;
            }

            internal NtpServer(NetworkClock clock)
            {
                yInstantAccuracy = clock.Accuracy;
            }

            // https://techhub.hpe.com/eginfolib/networking/docs/switches/5820x-5800/5998-7395r_nmm_cg/content/441755722.htm
            internal void SendNtpResponse(double time, NetworkPeer peer, float t)
            {
                double a = time + t; // client time + delta time
                double x = ClockTime; // server time

                using var message = Pool.Rent();
                message.Write(a);
                message.Write(x);

                // A method is used to obtain a small delay to obtain the instant Y.
                SendWithYInstant(peer, message);
            }

            private void SendWithYInstant(NetworkPeer peer, DataBuffer message)
            {
                double y = int.MaxValue; // server time
                float t = 0; // delta time

                // <- small delay with for loop to obtain the instant Y to best precision!
                for (int i = 0; i < yInstantAccuracy; i++)
                {
                    y = ClockTime;
                    t = DeltaTime;
                }

                message.Write(y);
                message.Write(t);
                // Send NTP response
                NetworkManager.Server.SendMessage(
                    MessageType.NtpQuery,
                    peer,
                    message,
                    target: Target.Self,
                    deliveryMode: DeliveryMode.Unreliable,
                    sequenceChannel: 0
                );
            }
        }
    }
}
