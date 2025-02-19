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
using Omni.Inspector;
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

        [Tooltip("KCP timeout in milliseconds. Note that KCP sends a ping automatically.")]
        [SerializeField]
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

        [Tooltip("KCP window size can be modified to support higher loads.")]
        [SerializeField]
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

        private ITransporterReceive transporter;
        private readonly Dictionary<IPEndPoint, int> _peers = new();

        public void Initialize(ITransporterReceive transporter, bool isServer)
        {
            this.isServer = isServer;
            this.transporter = transporter;

            if (isRunning)
            {
                throw new InvalidOperationException("[KcpTransporter] Cannot initialize - Instance is already running. Call Stop() before reinitializing.");
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
                                $"[KcpTransporter] Connection rejected - Peer {conn.remoteEndPoint} is already registered",
                                NetworkLogger.LogType.Error
                            );
                        }
                        else
                        {
                            transporter.Internal_OnServerPeerConnected(conn.remoteEndPoint,
                                new NativePeer(() => conn.time,
                                    () => throw new NotImplementedException(
                                        "[KcpTransporter] Individual ping measurement not supported - Use SNTP clock for time synchronization")));
                        }
                    },
                    (connId, data, channel) =>
                    {
                        var conn = kcpServer.connections[connId];
                        transporter.Internal_OnDataReceived(data, GetDeliveryMode(channel), conn.remoteEndPoint, 0,
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
                                $"[KcpTransporter] Failed to remove peer {conn.remoteEndPoint} - Peer is already disconnected or not in active connections",
                                NetworkLogger.LogType.Error
                            );
                        }
                        else
                        {
                            transporter.Internal_OnServerPeerDisconnected(conn.remoteEndPoint, "[KcpTransporter] Peer disconnected normally");
                        }
                    },
                    (connId, error, reason) =>
                    {
                        var conn = kcpServer.connections[connId];
                        transporter.Internal_OnServerPeerDisconnected(conn.remoteEndPoint, reason);

                        NetworkLogger.__Log__(
                            $"[KcpTransporter] Server error occurred - Connection ID: {connId}, Error: {error}, Reason: {reason}",
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
                        transporter.Internal_OnClientConnected(kcpClient.remoteEndPoint,
                            new NativePeer(() => (kcpClient.time - kcpClientConnectTime) / 1000d,
                                () => (int)Math.Round(m_PingAvg.Average * 1000d, 0)));

                        SendPingRequest();
                    },
                    (data, channel) =>
                    {
                        transporter.Internal_OnDataReceived(data, GetDeliveryMode(channel), kcpClient.remoteEndPoint, 0,
                            isServer, out byte msgType);

                        if (msgType == MessageType.KCP_PING_REQUEST_RESPONSE)
                        {
                            uint time = BitConverter.ToUInt32(data[1..5]); // 1: Skip MessageType
                            double halfRtt = (kcpClient.time - time) / 2d / 1000d;
                            m_PingAvg.Add(NetworkHelper.MinMax(halfRtt, PING_TIME_PRECISION));
                        }
                    },
                    () => transporter.Internal_OnClientDisconnected(kcpClient.remoteEndPoint, "[KcpTransporter] Client disconnected normally"),
                    (error, reason) =>
                    {
                        transporter.Internal_OnClientDisconnected(kcpClient.remoteEndPoint, reason);
                        NetworkLogger.__Log__(
                             $"[KcpTransporter] Client error occurred - Error: {error}, Reason: {reason}",
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
            if (isServer)
            {
                if (!NetworkHelper.IsPortAvailable(port, ProtocolType.Udp, m_DualMode))
                {
                    NetworkLogger.__Log__(
                        $"[KcpTransporter] Port {port} is already in use by another server instance - Operating in client-only mode",
                        NetworkLogger.LogType.Log
                    );

                    return;
                }
            }

            if (isServer && isRunning)
            {
                kcpServer.Start((ushort)port);
                transporter.Internal_OnServerInitialized();
            }
        }

        public void Connect(string address, int port)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                throw new InvalidOperationException("[KcpTransporter] Connect() method is not available on server instances - This operation is client-only");
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
        public void Send(ReadOnlySpan<byte> data, IPEndPoint target, DeliveryMode deliveryMode, byte sequenceChannel)
        {
            ThrowAnErrorIfNotInitialized();

            if (sequenceChannel > 0)
            {
                NetworkLogger.__Log__(
                    $"[KcpTransporter] Sequence channel {sequenceChannel} is not supported - Channel will be ignored",
                    NetworkLogger.LogType.Warning
                );
            }

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
            if (deliveryMode != DeliveryMode.Unreliable && deliveryMode != DeliveryMode.ReliableOrdered)
            {
                NetworkLogger.__Log__(
                    $"[KcpTransporter] Unsupported delivery mode '{deliveryMode}' - Falling back to ReliableOrdered",
                    NetworkLogger.LogType.Warning
                );
            }

            return deliveryMode switch
            {
                DeliveryMode.Unreliable => KcpChannel.Unreliable,
                DeliveryMode.ReliableOrdered => KcpChannel.Reliable,
                _ => KcpChannel.Reliable
            };
        }

        private DeliveryMode GetDeliveryMode(KcpChannel deliveryMethod)
        {
            return deliveryMethod switch
            {
                KcpChannel.Unreliable => DeliveryMode.Unreliable,
                KcpChannel.Reliable => DeliveryMode.ReliableOrdered,
                _ => DeliveryMode.ReliableOrdered
            };
        }

        [Conditional("OMNI_DEBUG")]
        private void ThrowAnErrorIfNotInitialized()
        {
            if (!isRunning)
            {
                throw new Exception("[KcpTransporter] Operation failed - Transporter is not initialized. Call Initialize() before performing any network operations.");
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