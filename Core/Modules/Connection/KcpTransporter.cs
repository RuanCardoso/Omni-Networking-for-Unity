using kcp2k;
using Omni.Core.Attributes;
using Omni.Core.Interfaces;
using Omni.Shared;
using Omni.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TriInspector;
using UnityEngine;

//// EXPERIMENTAL

// kcp implementation: https://github.com/MirrorNetworking/Mirror/blob/master/Assets/Mirror/Transports/KCP/KcpTransport.cs
// kcp2k forked from: https://github.com/MirrorNetworking/kcp2k - fork was ported to .net standard 2.1 [Span<T>, Memory<T>, ArrayPool<T>, etc..] thanks..
namespace Omni.Core.Modules.Connection
{
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Omni/Transporters/Kcp Transporter")]
    [DeclareBoxGroup("Basic")]
    [DeclareBoxGroup("Advanced")]
    internal class KcpTransporter : TransporterBehaviour, ITransporter
    {
        private const double PING_TIME_PRECISION = 0.025d;
        private const int MTU = Kcp.MTU_DEF;

        private readonly SimpleMovingAverage m_PingAvg = new(10);

        [GroupNext("Basic")]
        [Tooltip(
            "DualMode listens to IPv6 and IPv4 simultaneously. Disable if the platform only supports IPv4."
        )]
        [SerializeField]
        private bool m_DualMode = false;

        [Tooltip(
            "NoDelay is recommended to reduce latency. This also scales better without buffers getting full."
        )]
        [SerializeField]
        private bool m_NoDelay = true;

        [Tooltip(
            "KCP internal update interval. 100ms is KCP default, but a lower interval is recommended to minimize latency and to scale to more networked entities."
        )]
        [SerializeField]
        private uint m_Interval = 10;

        [Tooltip("KCP timeout in milliseconds. Note that KCP sends a ping automatically.")] [SerializeField]
        private int m_Timeout = 10000;

        [Tooltip(
            "Socket receive buffer size. Large buffer helps support more connections. Increase operating system socket buffer size limits if needed."
        )]
        [SerializeField]
        private int m_RecvBufferSize = 1024 * 1027 * 7;

        [Tooltip(
            "Socket send buffer size. Large buffer helps support more connections. Increase operating system socket buffer size limits if needed."
        )]
        [SerializeField]
        private int m_SendBufferSize = 1024 * 1027 * 7;

        [GroupNext("Advanced")]
        [Tooltip(
            "KCP fastresend parameter. Faster resend for the cost of higher bandwidth. 0 in normal mode, 2 in turbo mode."
        )]
        [SerializeField]
        private int m_FastResend = 2;

        [Tooltip(
            "KCP window size can be modified to support higher loads. This also increases max message size."
        )]
        [SerializeField]
        private uint m_ReceiveWindowSize = 4096; //Kcp.WND_RCV; 128 by default. sends a lot, so we need a lot more.

        [Tooltip("KCP window size can be modified to support higher loads.")] [SerializeField]
        private uint m_SendWindowSize = 4096; //Kcp.WND_SND; 32 by default. sends a lot, so we need a lot more.

        [Tooltip(
            "KCP will try to retransmit lost messages up to MaxRetransmit (aka dead_link) before disconnecting."
        )]
        [SerializeField]
        private uint
            m_MaxRetransmit = Kcp.DEADLINK * 2; // default prematurely disconnects a lot of people (#3022). use 2x.

        [Tooltip(
            "KCP congestion window. Restricts window size to reduce congestion. Results in only 2-3 MTU messages per Flush even on loopback. Best to keept his disabled."
        )]
        [SerializeField]
        [ReadOnly]
        private bool
            m_CongestionWindow =
                false; // KCP 'NoCongestionWindow' is false by default. here we negate it for ease of use.

        [Tooltip(
            "Enable to automatically set client & server send/recv buffers to OS limit. Avoids issues with too small buffers under heavy load, potentially dropping connections. Increase the OS limit if this is still too small."
        )]
        [SerializeField]
        private bool m_MaximizeSocketBuffers = true;

        private KcpServer kcpServer;
        private KcpClient kcpClient;

        private uint kcpClientConnectTime = 0;

        private bool isServer;
        private bool isRunning;

        private ITransporterReceive IManager;
        private readonly Dictionary<IPEndPoint, int> _peers = new();

        public void Initialize(ITransporterReceive IManager, bool isServer)
        {
            this.isServer = isServer;
            this.IManager = IManager;

            if (isRunning)
            {
                throw new InvalidOperationException("The Kcp Transporter has already been initialized.");
            }

            KcpConfig kcpConf = new KcpConfig(m_DualMode, m_RecvBufferSize, m_SendBufferSize, MTU, m_NoDelay,
                m_Interval, m_FastResend, m_CongestionWindow, m_SendWindowSize, m_ReceiveWindowSize, m_Timeout,
                m_MaxRetransmit);

            if (isServer)
            {
                kcpServer = new(
                    (connId) =>
                    {
                        var conn = kcpServer.connections[connId];
                        if (!_peers.TryAdd(conn.remoteEndPoint, connId))
                        {
                            NetworkLogger.__Log__(
                                $"Kcp: The peer: {conn.remoteEndPoint} is already connected!",
                                NetworkLogger.LogType.Error
                            );
                        }
                        else
                        {
                            IManager.Internal_OnServerPeerConnected(conn.remoteEndPoint,
                                new NativePeer(() => conn.time,
                                    () => throw new NotImplementedException(
                                        "[KCP] Individual ping not implemented! Use SNTP clock.")));
                        }
                    },
                    (connId, data, channel) =>
                    {
                        var conn = kcpServer.connections[connId];
                        IManager.Internal_OnDataReceived(data, GetDeliveryMode(channel), conn.remoteEndPoint, 0,
                            isServer, out byte msgType);

                        if (msgType == MessageType.KCP_PING_REQUEST_RESPONSE)
                        {
                            Send(data, conn.remoteEndPoint, DeliveryMode.ReliableOrdered, 0);
                        }
                    },
                    (connId) =>
                    {
                        var conn = kcpServer.connections[connId];
                        if (!_peers.Remove(conn.remoteEndPoint))
                        {
                            NetworkLogger.__Log__(
                                $"Kcp: The peer: {conn.remoteEndPoint} is already disconnected!",
                                NetworkLogger.LogType.Error
                            );
                        }
                        else
                        {
                            IManager.Internal_OnServerPeerDisconnected(conn.remoteEndPoint, "[Normally Disconnected]");
                        }
                    },
                    (connId, error, reason) =>
                    {
                        var conn = kcpServer.connections[connId];
                        IManager.Internal_OnServerPeerDisconnected(conn.remoteEndPoint, reason);

                        NetworkLogger.__Log__(
                            $"[KCP] OnServerError({connId}, {error}, {reason}",
                            NetworkLogger.LogType.Error
                        );
                    },
                    kcpConf
                );
            }
            else
            {
                kcpClient = new(
                    () =>
                    {
                        kcpClientConnectTime = kcpClient.time;
                        IManager.Internal_OnClientConnected(kcpClient.remoteEndPoint,
                            new NativePeer(() => (kcpClient.time - kcpClientConnectTime) / 1000d,
                                () => (int)Math.Round(m_PingAvg.Average * 1000d, 0)));

                        SendPingRequest();
                    },
                    (data, channel) =>
                    {
                        IManager.Internal_OnDataReceived(data, GetDeliveryMode(channel), kcpClient.remoteEndPoint, 0,
                            isServer, out byte msgType);

                        if (msgType == MessageType.KCP_PING_REQUEST_RESPONSE)
                        {
                            uint time = BitConverter.ToUInt32(data[1..5]); // 1: Skip MessageType
                            double halfRtt = (kcpClient.time - time) / 2d / 1000d;
                            m_PingAvg.Add(NetworkHelper.MinMax(halfRtt, PING_TIME_PRECISION));
                        }
                    },
                    () => IManager.Internal_OnClientDisconnected(kcpClient.remoteEndPoint, "Disconnected!"),
                    (error, reason) =>
                    {
                        IManager.Internal_OnClientDisconnected(kcpClient.remoteEndPoint, reason);
                        NetworkLogger.__Log__(
                            $"[KCP] OnServerError({error}, {reason}",
                            NetworkLogger.LogType.Error
                        );
                    },
                    kcpConf
                );
            }

            isRunning = true;
        }

        [ClientOnly]
        private async void SendPingRequest()
        {
            while (Application.isPlaying)
            {
                if (kcpClient.connected && NetworkManager.IsClientActive)
                {
                    using var pingRequest = NetworkManager.Pool.Rent();
                    pingRequest.Write(MessageType.KCP_PING_REQUEST_RESPONSE);
                    pingRequest.Write(kcpClient.time);
                    pingRequest.SuppressTracking();
                    Send(pingRequest.BufferAsSpan, default, DeliveryMode.ReliableOrdered, 0);
                }

                await UniTask.Delay(1000);
            }
        }

        private void Awake()
        {
            ITransporter = this;
        }

        // before the world because has priority.
        private void Update()
        {
            if (isRunning)
            {
                if (isServer)
                {
                    kcpServer.TickIncoming();
                }
                else
                {
                    kcpClient.TickIncoming();
                }
            }
        }

        private void LateUpdate()
        {
            if (isRunning)
            {
                if (isServer)
                {
                    kcpServer.TickOutgoing();
                }
                else
                {
                    kcpClient.TickOutgoing();
                }
            }
        }

        public void Listen(int port)
        {
            ThrowAnErrorIfNotInitialized();
            if (!NetworkHelper.IsPortAvailable(port, ProtocolType.Udp, m_DualMode))
            {
                if (isServer)
                {
                    NetworkLogger.__Log__(
                        "Kcp: Server is already initialized in another instance, only the client will be initialized.",
                        NetworkLogger.LogType.Warning
                    );

                    return;
                }

                port = NetworkHelper.GetAvailablePort(port, m_DualMode);
            }

            if (isServer && isRunning)
            {
                kcpServer.Start((ushort)port);
                IManager.Internal_OnServerInitialized();
            }
        }

        public void Connect(string address, int port)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                throw new Exception("The Connect() is not available for server.");
            }

            kcpClient.Connect(address, (ushort)port);
        }

        public void Disconnect(NetworkPeer peer)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                int connId = _peers[peer.EndPoint];
                kcpServer.connections[connId].Disconnect();
            }
            else
            {
                kcpClient.Disconnect();
            }
        }

        public void SendP2P(ReadOnlySpan<byte> data, IPEndPoint target)
        {
            throw new NotImplementedException();
        }

        // Span to array is very fast!
        public void Send(ReadOnlySpan<byte> data, IPEndPoint target, DeliveryMode deliveryMode, byte channel)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                if (_peers.TryGetValue(target, out int peer))
                {
                    byte[] dataArray = data.ToArray();
                    kcpServer.Send(peer, dataArray, GetKcpChannel(deliveryMode));
                }
            }
            else
            {
                byte[] dataArray = data.ToArray();
                kcpClient.Send(dataArray, GetKcpChannel(deliveryMode));
            }
        }

        public void Stop()
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                kcpServer.Stop();
            }
            else
            {
                kcpClient.Disconnect();
            }
        }

        private KcpChannel GetKcpChannel(DeliveryMode deliveryMode)
        {
            return deliveryMode switch
            {
                DeliveryMode.Unreliable => KcpChannel.Unreliable,
                DeliveryMode.ReliableOrdered => KcpChannel.Reliable,
                _
                    => throw new NotSupportedException(
                        "Unknown delivery mode! this mode is not supported!"
                    ),
            };
        }

        private DeliveryMode GetDeliveryMode(KcpChannel deliveryMethod)
        {
            return deliveryMethod switch
            {
                KcpChannel.Unreliable => DeliveryMode.Unreliable,
                KcpChannel.Reliable => DeliveryMode.ReliableOrdered,
                _
                    => throw new NotSupportedException(
                        "Unknown delivery method! this mode is not supported!"
                    ),
            };
        }

        [Conditional("OMNI_DEBUG")]
        private void ThrowAnErrorIfNotInitialized()
        {
            if (!isRunning)
            {
                throw new Exception("The KcpTransporter is not initialized.");
            }
        }

        public void CopyTo(ITransporter ITransporter)
        {
            KcpTransporter kcpTransporter = ITransporter as KcpTransporter;
            if (kcpTransporter != null)
            {
                kcpTransporter.m_DualMode = m_DualMode;
                kcpTransporter.m_NoDelay = m_NoDelay;
                kcpTransporter.m_Interval = m_Interval;
                kcpTransporter.m_Timeout = m_Timeout;
                kcpTransporter.m_RecvBufferSize = m_RecvBufferSize;
                kcpTransporter.m_SendBufferSize = m_SendBufferSize;
                kcpTransporter.m_FastResend = m_FastResend;
                kcpTransporter.m_CongestionWindow = m_CongestionWindow;
                kcpTransporter.m_ReceiveWindowSize = m_ReceiveWindowSize;
                kcpTransporter.m_SendWindowSize = m_SendWindowSize;
                kcpTransporter.m_MaxRetransmit = m_MaxRetransmit;
                kcpTransporter.m_MaximizeSocketBuffers = m_MaximizeSocketBuffers;
            }
        }
    }
}