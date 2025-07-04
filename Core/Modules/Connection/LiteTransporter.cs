using LiteNetLib;
using Omni.Core.Attributes;
using Omni.Core.Interfaces;
using Omni.Shared;
using Omni.Threading.Tasks;
using OpenNat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Omni.Inspector;
using UnityEngine;
using LiteNetLib.Layers;
using Omni.Core.Cryptography;

namespace Omni.Core.Modules.Connection
{
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Omni/Transporters/Lite Transporter")]
    [DeclareBoxGroup("Settings")]
    [DeclareBoxGroup("Lag Simulator [Debug only!]")]
    internal class LiteTransporter : TransporterBehaviour, ITransporter
    {
        internal enum NetworkSimulationMode
        {
            None,
            ClientOnly,
            ServerOnly,
            Both
        }

        public class LiteSecurityLayer : PacketLayerBase
        {
            private const int ChecksumSize = 32; // 32 bytes for SHA256 hash
            public LiteSecurityLayer() : base(ChecksumSize) { }

            public override void ProcessInboundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int length)
            {
                if (length < NetConstants.HeaderSize + ChecksumSize)
                {
                    NetworkLogger.__Log__("[LiteTransporter] Packet too short for checksum", NetworkLogger.LogType.Error);
                    length = 0;
                    return;
                }

                int offset = length - ChecksumSize;
                // Get the checksum from the end of the packet
                ReadOnlySpan<byte> checksum = data.AsSpan(offset, ChecksumSize);
                // Calculate the checksum for the data without the checksum itself
                if (!HmacGenerator.Validate(data, 0, offset, NetworkManager.ProductionKey, checksum))
                {
                    NetworkLogger.__Log__(
                        $"[LiteTransporter] Invalid checksum detected - Packet discarded (Checksum: {BitConverter.ToString(checksum.ToArray())})",
                        NetworkLogger.LogType.Error
                    );

                    length = 0; // Invalid checksum, discard the packet;
                    return;
                }

                length -= ChecksumSize;
            }

            public override void ProcessOutBoundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int offset, ref int length)
            {
                // Get the end of the packet
                Span<byte> dataSpan = data.AsSpan(offset + length, ChecksumSize);
                // Calculate the checksum for the data
                ReadOnlySpan<byte> checksum = HmacGenerator.Compute(data, offset, length, NetworkManager.ProductionKey);
                // Copy the checksum to the end of the packet
                checksum.CopyTo(dataSpan);
                length += ChecksumSize;
            }
        }

        private EventBasedNetListener _listener;
        private NetManager _manager;

        private bool isServer;
        private bool isRunning;

        [SerializeField]
        [GroupNext("Settings")]
        [Tooltip(
            "Specifies the version of the game. The server rejects older or different versions."
        )]
        [LabelWidth(140)]
        private string m_VersionName = "1.0.0.0";

        [SerializeField]
        [Tooltip(
            "Specifies the time limit (in milliseconds) without receiving any message before the server disconnects the client."
        )]
        [Range(1000, 10000), LabelWidth(140)]
        private int m_disconnectTimeout = 3000;

        [SerializeField]
        [Tooltip(
            "Specifies the interval (in milliseconds) at which ping messages are sent to the server."
        )]
        [Range(1000, 5000), LabelWidth(140)]
        private int m_pingInterval = 1000;

        [SerializeField]
        [Tooltip("Specifies the maximum number of connections allowed at the same time.")]
        [Min(1), LabelWidth(140)]
        private int m_MaxConnections = 256;

        [SerializeField]
        [Tooltip("Max events that will be processed per frame.")]
        [LabelWidth(140)]
        [Min(0)]
        private int m_MaxEventsPerFrame = 0; // 0 - No limit

        [SerializeField]
        [Range(1, 64), LabelWidth(140)]
        private byte m_ChannelsCount = 3;

        [SerializeField]
        [Tooltip("Specifies whether IPv6 is enabled. Note: Not all platforms may support this.")]
        [LabelText("IPv6 Enabled")]
        [LabelWidth(140)]
        private bool m_IPv6Enabled = false;

        [SerializeField]
        [Tooltip("Specifies whether port forwarding is enabled with PMP or UPnP protocols.")]
        [LabelWidth(140)]
        private bool m_UsePortForwarding = false;

        [SerializeField]
        [Tooltip(
            "Specifies whether to use native sockets for networking operations. Note: Enabling this option may enhance performance but could be platform-dependent."
        )]
        [LabelWidth(140)]
        private bool m_useNativeSockets = false;

        [SerializeField]
        [Tooltip(
            "Specifies whether to use a safe MTU (Maximum Transmission Unit) size for networking operations. Using a safe MTU can reduce the risk of packet loss and ensure smoother data transmission."
        )]
        [LabelWidth(140)]
        private bool m_useSafeMtu = false;

        [SerializeField]
        [LabelWidth(140)]
        private bool m_ManualMode = false;

        [SerializeField]
        [LabelWidth(140), HideIf("m_ManualMode")]
        [Range(1, 100)]
        private int m_UpdateTime = 1;

        [SerializeField]
        [LabelWidth(140)]
        private bool m_UseSecurityLayer = false;

        [GroupNext("Lag Simulator [Debug only!]")]
        [SerializeField]
        [LabelWidth(130)]
        private NetworkSimulationMode m_SimulationMode = NetworkSimulationMode.None;

        [SerializeField]
        [DisableIf("m_SimulationMode", NetworkSimulationMode.None)]
        [Min(0), LabelWidth(130)]
        private int m_MinLatency = 60;

        [SerializeField]
        [DisableIf("m_SimulationMode", NetworkSimulationMode.None)]
        [Min(0), LabelWidth(130)]
        private int m_MaxLatency = 60;

        [SerializeField]
        [DisableIf("m_SimulationMode", NetworkSimulationMode.None)]
        [Range(0, 100), LabelWidth(130)]
        private int m_LossPercent = 0;

        private ITransporterReceive transporter;
        private NetPeer localPeer;
        private readonly Dictionary<IPEndPoint, NetPeer> _peers = new();

        public bool IsServer => isServer;
        public bool IsRunning => isRunning;

        public int MaxConnections
        {
            get => m_MaxConnections;
            set => m_MaxConnections = value;
        }

        public int DisconnectTimeout
        {
            get => m_disconnectTimeout;
            set => m_disconnectTimeout = value;
        }

        public int PingInterval
        {
            get => m_pingInterval;
            set => m_pingInterval = value;
        }

        public bool IPv6Enabled
        {
            get => m_IPv6Enabled;
            set => m_IPv6Enabled = value;
        }

        public bool UseNativeSockets
        {
            get => m_useNativeSockets;
            set => m_useNativeSockets = value;
        }

        public bool UseSafeMtu
        {
            get => m_useSafeMtu;
            set => m_useSafeMtu = value;
        }

        public void Initialize(ITransporterReceive transporter, bool isServer)
        {
#if !OMNI_DEBUG // Lag Simulator [Debug only!] - Disabled in Release(Production)
            m_SimulationMode = NetworkSimulationMode.None;
#endif
            this.isServer = isServer;
            this.transporter = transporter;

            if (isRunning)
            {
                throw new InvalidOperationException("[LiteTransporter] Cannot initialize: Instance is already running. Call Stop() before reinitializing.");
            }

            bool simulateLag = isServer
                ? m_SimulationMode == NetworkSimulationMode.ServerOnly || m_SimulationMode == NetworkSimulationMode.Both
                : m_SimulationMode == NetworkSimulationMode.ClientOnly || m_SimulationMode == NetworkSimulationMode.Both;

            _listener = new EventBasedNetListener();
            _manager = m_UseSecurityLayer ? new(_listener, new LiteSecurityLayer()) : new(_listener)
            {
                AutoRecycle = false,
                EnableStatistics = false,
                ReuseAddress = false,
                DisconnectTimeout = m_disconnectTimeout,
                IPv6Enabled = m_IPv6Enabled,
                PingInterval = m_pingInterval,
                UseNativeSockets =
                    m_useNativeSockets, // Experimental feature! mostly for servers. Only for Windows/Linux
                UseSafeMtu = m_useSafeMtu,
                ChannelsCount = m_ChannelsCount,
                SimulateLatency = simulateLag,
                SimulatePacketLoss = simulateLag,
                SimulationMinLatency = m_MinLatency,
                SimulationMaxLatency = m_MaxLatency,
                SimulationPacketLossChance = m_LossPercent,
                UnconnectedMessagesEnabled = false, // P2P
                UpdateTime = m_UpdateTime,
            };

            if (isServer)
            {
                _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
                _listener.PeerConnectedEvent += OnPeerConnectedEvent;
                _listener.PeerDisconnectedEvent += OnPeerDisconnectedEvent;
            }
            else
            {
                _listener.PeerConnectedEvent += peer =>
                {
                    if (peer.ConnectionState == ConnectionState.Connected)
                    {
                        localPeer ??= peer;
                        transporter.Internal_OnClientConnected(peer, new NativePeer());
                    }
                };

                _listener.PeerDisconnectedEvent += (peer, info) =>
                {
                    transporter.Internal_OnClientDisconnected(localPeer,
                        $"code: {info.SocketErrorCode} | reason: {info.Reason}");
                };

                _listener.NetworkReceiveUnconnectedEvent += OnP2PMessage;
            }

            _listener.NetworkReceiveEvent += OnReceiveEvent;
            isRunning = true;
        }

        [ClientOnly]
        private void OnP2PMessage(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            ReadOnlySpan<byte> data = reader.GetRemainingBytesSpan();
            transporter.Internal_OnP2PDataReceived(data, remoteEndPoint);
            reader.Recycle(); // Avoid memory leaks - auto recycle is disabled.
        }

        private void OnReceiveEvent(NetPeer peer, NetPacketReader reader, byte seqChannel, DeliveryMethod deliveryMode)
        {
            ReadOnlySpan<byte> data = reader.GetRemainingBytesSpan();
            DeliveryMode mode = GetDeliveryMode(deliveryMode);

            transporter.Internal_OnDataReceived(data, mode, peer, seqChannel, isServer, out _);
            reader.Recycle(); // Avoid memory leaks - auto recycle is disabled.
        }

        [ServerOnly]
        private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo info)
        {
            if (!_peers.Remove(peer))
            {
                NetworkLogger.__Log__(
                    $"[LiteTransporter] Failed to remove peer {peer} - Peer is already disconnected or not found in active connections",
                    NetworkLogger.LogType.Error
                );
            }
            else
            {
                transporter.Internal_OnServerPeerDisconnected(peer, info.Reason.ToString());
            }
        }

        [ServerOnly]
        private void OnPeerConnectedEvent(NetPeer peer)
        {
            if (!_peers.TryAdd(peer, peer))
            {
                NetworkLogger.__Log__(
                    $"[LiteTransporter] Duplicate connection attempt - Peer {peer} is already registered in the active connections",
                    NetworkLogger.LogType.Error
                );
            }
            else
            {
                transporter.Internal_OnServerPeerConnected(peer, new NativePeer());
            }
        }

        [ServerOnly]
        private void OnConnectionRequestEvent(ConnectionRequest request)
        {
            if (_manager.ConnectedPeersCount < m_MaxConnections)
            {
                if (request.AcceptIfKey(m_VersionName) == null)
                {
                    NetworkLogger.__Log__(
                        $"[LiteTransporter] Version mismatch detected - Connection rejected (Client version differs from server version: {m_VersionName})",
                        NetworkLogger.LogType.Error
                    );
                }
            }
            else
            {
                request.Reject();
                NetworkLogger.__Log__(
                    $"[LiteTransporter] Connection rejected - Server at maximum capacity ({m_MaxConnections} connections)",
                    NetworkLogger.LogType.Warning
                );
            }
        }

        private void Awake()
        {
            ITransporter = this;
        }

        private void Update()
        {
            if (isRunning)
            {
                _manager.PollEvents(m_MaxEventsPerFrame); // Rec
            }
        }

        float elapsed = 0f;
        private void LateUpdate()
        {
            if (isRunning && m_ManualMode)
            {
                elapsed += Time.deltaTime * 1000f;
                int ms = Mathf.FloorToInt(elapsed);
                if (ms > 0)
                {
                    _manager.ManualUpdate(ms);
                    elapsed -= ms;
                }
            }
        }

        public void Connect(string address, int port)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                throw new InvalidOperationException("Connect() method is not available on server instances. This operation is client-only.");
            }

            localPeer = _manager.Connect(address, port, m_VersionName);
        }

        public void Disconnect(NetworkPeer peer)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                _manager.DisconnectPeer(_peers[peer.EndPoint]);
            }
            else
            {
                localPeer.Disconnect();
            }
        }

        public void Listen(int port)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                if (!NetworkHelper.IsPortAvailable(port, ProtocolType.Udp, m_IPv6Enabled))
                {
                    NetworkLogger.__Log__(
                        $"[LiteTransporter] Port {port} is already in use by another server instance. This instance will operate in client-only mode.",
                        NetworkLogger.LogType.Log
                    );

                    return;
                }
            }

            if (m_UsePortForwarding)
            {
                TryPortForwarding(port);
            }

            if (_manager.Start(IPAddress.Any, IPAddress.IPv6Any, port, m_ManualMode))
            {
                if (isServer && isRunning)
                {
                    transporter.Internal_OnServerInitialized();
                }

#if UNITY_SERVER
                if (_manager.UseNativeSockets)
                {
                    NetworkLogger.__Log__(
                        $"[LiteTransporter] Native socket optimization enabled - This experimental feature may improve server performance on Windows/Linux platforms",
                        NetworkLogger.LogType.Warning
                    );
                }
#endif
            }
        }

        private async void TryPortForwarding(int port)
        {
            if (!isServer)
            {
                // Wait 500ms before attempting to open the port.
                // Avoid opening server and client ports simultaneously, which will cause an error.
                await UniTask.Delay(500);
            }

            if (await NetworkHelper.OpenPortAsync(port, Protocol.Udp))
            {
                NetworkLogger.Print($"[Port Forwarding] Successfully mapped UDP port {port} using UPnP/PMP protocol",
                    NetworkLogger.LogType.Log);
            }
            else
            {
                NetworkLogger.Print($"[Port Forwarding] Failed to map UDP port {port} using UPnP/PMP protocol - Check if your router supports port forwarding",
                    NetworkLogger.LogType.Error);
            }
        }

        public void SendP2P(ReadOnlySpan<byte> data, IPEndPoint target)
        {
            ThrowAnErrorIfNotInitialized();
            _manager.SendUnconnectedMessage(data, target);
        }

        public void Send(ReadOnlySpan<byte> data, IPEndPoint target, DeliveryMode deliveryMode, byte sequenceChannel)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                if (_peers.TryGetValue(target, out NetPeer peer))
                {
                    if (peer.ConnectionState == ConnectionState.Connected)
                    {
                        peer.Send(data, sequenceChannel, GetDeliveryMethod(deliveryMode));
                    }
                }
            }
            else
            {
                if (localPeer.ConnectionState == ConnectionState.Connected)
                {
                    localPeer.Send(data, sequenceChannel, GetDeliveryMethod(deliveryMode));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DeliveryMethod GetDeliveryMethod(DeliveryMode deliveryMode)
        {
            return deliveryMode switch
            {
                DeliveryMode.ReliableOrdered => DeliveryMethod.ReliableOrdered,
                DeliveryMode.ReliableSequenced => DeliveryMethod.ReliableSequenced,
                DeliveryMode.ReliableUnordered => DeliveryMethod.ReliableUnordered,
                DeliveryMode.Unreliable => DeliveryMethod.Unreliable,
                DeliveryMode.Sequenced => DeliveryMethod.Sequenced,
                _ => throw new NotImplementedException(
                     $"[LiteTransporter] Unsupported delivery mode '{deliveryMode}'"),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DeliveryMode GetDeliveryMode(DeliveryMethod deliveryMethod) // my*
        {
            return deliveryMethod switch
            {
                DeliveryMethod.ReliableOrdered => DeliveryMode.ReliableOrdered,
                DeliveryMethod.ReliableSequenced => DeliveryMode.ReliableSequenced,
                DeliveryMethod.ReliableUnordered => DeliveryMode.ReliableUnordered,
                DeliveryMethod.Unreliable => DeliveryMode.Unreliable,
                DeliveryMethod.Sequenced => DeliveryMode.Sequenced,
                _ => throw new NotImplementedException(
                    $"[LiteTransporter] Unsupported delivery method '{deliveryMethod}'"),
            };
        }

        public void Stop()
        {
            ThrowAnErrorIfNotInitialized();
            _manager.Stop(true);
        }

        [Conditional("OMNI_DEBUG")]
        private void ThrowAnErrorIfNotInitialized()
        {
            if (!isRunning)
            {
                throw new InvalidOperationException("[LiteTransporter] Operation failed - Transporter is not initialized. Call Initialize() before performing any network operations.");
            }
        }

        public void CopyTo(ITransporter ITransporter)
        {
            LiteTransporter liteTransporter = ITransporter as LiteTransporter;
            if (liteTransporter != null)
            {
                liteTransporter.m_VersionName = m_VersionName;
                liteTransporter.m_disconnectTimeout = m_disconnectTimeout;
                liteTransporter.m_IPv6Enabled = m_IPv6Enabled;
                liteTransporter.m_pingInterval = m_pingInterval;
                liteTransporter.m_MaxConnections = m_MaxConnections;
                liteTransporter.m_MaxEventsPerFrame = m_MaxEventsPerFrame;
                liteTransporter.m_useNativeSockets = m_useNativeSockets;
                liteTransporter.m_useSafeMtu = m_useSafeMtu;
                liteTransporter.m_ChannelsCount = m_ChannelsCount;
                liteTransporter.m_UsePortForwarding = m_UsePortForwarding;
                liteTransporter.m_UseSecurityLayer = m_UseSecurityLayer;
                liteTransporter.m_UpdateTime = m_UpdateTime;
                liteTransporter.m_ManualMode = m_ManualMode;

                // Lag properties
                liteTransporter.m_SimulationMode = m_SimulationMode;
                liteTransporter.m_MinLatency = m_MinLatency;
                liteTransporter.m_MaxLatency = m_MaxLatency;
                liteTransporter.m_LossPercent = m_LossPercent;
            }
        }

#if OMNI_DEBUG // Inspector Changes
        private void OnValidate()
        {
            LiteTransporter[] liteTransporters = GetComponentsInChildren<LiteTransporter>();
            foreach (LiteTransporter liteTransporter in liteTransporters)
            {
                if (liteTransporter.isRunning)
                {
                    bool simulateLag = liteTransporter.isServer
                        ? m_SimulationMode == NetworkSimulationMode.ServerOnly || m_SimulationMode == NetworkSimulationMode.Both
                        : m_SimulationMode == NetworkSimulationMode.ClientOnly || m_SimulationMode == NetworkSimulationMode.Both;

                    liteTransporter._manager.PingInterval = m_pingInterval;
                    liteTransporter._manager.SimulateLatency = simulateLag;
                    liteTransporter._manager.SimulatePacketLoss = simulateLag;
                    liteTransporter._manager.SimulationMinLatency = m_MinLatency;
                    liteTransporter._manager.SimulationMaxLatency = m_MaxLatency;
                    liteTransporter._manager.SimulationPacketLossChance = m_LossPercent;

                    // Debug purpose only: Adjusts the maximum events processed per frame to help determine the optimal value for your use case.
                    liteTransporter.m_MaxEventsPerFrame = m_MaxEventsPerFrame;
                }
            }
        }
#endif
    }
}