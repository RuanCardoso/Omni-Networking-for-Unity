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
using TriInspector;
using UnityEngine;

namespace Omni.Core.Modules.Connection
{
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Omni/Transporters/Lite Transporter")]
    [DeclareBoxGroup("Settings")]
    [DeclareBoxGroup("Lag Simulator [Debug only!]")]
    internal class LiteTransporter : TransporterBehaviour, ITransporter
    {
        private EventBasedNetListener _listener;
        private NetManager _manager;

        private bool isServer;
        private bool isRunning;

        [GroupNext("Settings")]
        [Tooltip(
            "Specifies the version of the game. The server rejects older or different versions."
        )]
        [SerializeField]
        private string m_VersionName = "1.0.0.0";

        [SerializeField]
        [Tooltip(
            "Specifies the time limit (in milliseconds) without receiving any message before the server disconnects the client."
        )]
        [Range(1000, 10000)]
        private int m_disconnectTimeout = 3000;

        [SerializeField]
        [Tooltip(
            "Specifies the interval (in milliseconds) at which ping messages are sent to the server."
        )]
        [Range(1000, 5000)]
        private int m_pingInterval = 1000;

        [SerializeField] [Tooltip("Specifies the maximum number of connections allowed at the same time.")] [Min(1)]
        private int m_MaxConnections = 256;

        [SerializeField] [Tooltip("Max events that will be processed per frame.")] [Min(0)]
        private int m_MaxEventsPerFrame = 0;

        [SerializeField] [Range(1, 64)] private byte m_ChannelsCount = 3;

        [SerializeField]
        [Tooltip("Specifies whether IPv6 is enabled. Note: Not all platforms may support this.")]
        [LabelText("IPv6 Enabled")]
        private bool m_IPv6Enabled = false;

        [SerializeField] [Tooltip("Specifies whether port forwarding is enabled with PMP or UPnP protocols.")]
        private bool m_UsePortForwarding = false;

        [SerializeField]
        [Tooltip(
            "Specifies whether to use native sockets for networking operations. Note: Enabling this option may enhance performance but could be platform-dependent."
        )]
        private bool m_useNativeSockets = false;

        [SerializeField]
        [Tooltip(
            "Specifies whether to use a safe MTU (Maximum Transmission Unit) size for networking operations. Using a safe MTU can reduce the risk of packet loss and ensure smoother data transmission."
        )]
        private bool m_useSafeMtu = false;

        [GroupNext("Lag Simulator [Debug only!]")] [SerializeField]
        private bool m_SimulateLag = false;

        [SerializeField] [EnableIf("m_SimulateLag")] [Min(0)]
        private int m_MinLatency = 60;

        [SerializeField] [EnableIf("m_SimulateLag")] [Min(0)]
        private int m_MaxLatency = 60;

        [SerializeField] [EnableIf("m_SimulateLag")] [Range(0, 100)]
        private int m_LossPercent = 0;

        private ITransporterReceive IManager;
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

        public void Initialize(ITransporterReceive IManager, bool isServer)
        {
#if !OMNI_DEBUG // Lag Simulator [Debug only!] - Disabled in Release(Production)
            m_SimulateLag = false;
#endif
            this.isServer = isServer;
            this.IManager = IManager;

            if (isRunning)
            {
                throw new Exception("Lite Transporter is already initialized.");
            }

            _listener = new EventBasedNetListener();
            _manager = new NetManager(_listener)
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
                SimulateLatency = m_SimulateLag,
                SimulatePacketLoss = m_SimulateLag,
                SimulationMinLatency = m_MinLatency,
                SimulationMaxLatency = m_MaxLatency,
                SimulationPacketLossChance = m_LossPercent,
                UnconnectedMessagesEnabled = true, // P2P
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
                        IManager.Internal_OnClientConnected(peer,
                            new NativePeer(() => (peer.RemoteUtcTime - new DateTime(peer.ConnectTime)).TotalSeconds,
                                () => peer.Ping));
                    }
                };

                _listener.PeerDisconnectedEvent += (peer, info) =>
                {
                    IManager.Internal_OnClientDisconnected(localPeer,
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
            IManager.Internal_OnP2PDataReceived(data, remoteEndPoint);
            reader.Recycle(); // Avoid memory leaks - auto recycle is disabled.
        }

        private void OnReceiveEvent(NetPeer peer, NetPacketReader reader, byte seqChannel, DeliveryMethod deliveryMode)
        {
            ReadOnlySpan<byte> data = reader.GetRemainingBytesSpan();
            DeliveryMode mode = GetDeliveryMode(deliveryMode);

            IManager.Internal_OnDataReceived(data, mode, peer, seqChannel, isServer, out _);
            reader.Recycle(); // Avoid memory leaks - auto recycle is disabled.
        }

        [ServerOnly]
        private void OnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo info)
        {
            if (!_peers.Remove(peer))
            {
                NetworkLogger.__Log__(
                    $"Lite Transporter: The peer: {peer} is already disconnected.",
                    NetworkLogger.LogType.Error
                );
            }
            else
            {
                IManager.Internal_OnServerPeerDisconnected(peer, info.Reason.ToString());
            }
        }

        [ServerOnly]
        private void OnPeerConnectedEvent(NetPeer peer)
        {
            if (!_peers.TryAdd(peer, peer))
            {
                NetworkLogger.__Log__(
                    $"Lite Transporter: The peer: {peer} is already connected.",
                    NetworkLogger.LogType.Error
                );
            }
            else
            {
                IManager.Internal_OnServerPeerConnected(peer,
                    new NativePeer(() => (peer.RemoteUtcTime - new DateTime(peer.ConnectTime)).TotalSeconds,
                        () => peer.Ping));
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
                        "Lite Transporter: The connection was rejected! because the version is not the same.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
            else
            {
                request.Reject();
                NetworkLogger.__Log__(
                    "Lite Transporter: Max connections reached! The connection was rejected!",
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
                _manager.PollEvents(m_MaxEventsPerFrame);
            }
        }

        public void Connect(string address, int port)
        {
            ThrowAnErrorIfNotInitialized();
            if (isServer)
            {
                throw new Exception("The Connect() is not available for server.");
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
            if (!NetworkHelper.IsPortAvailable(port, ProtocolType.Udp, m_IPv6Enabled))
            {
                if (isServer)
                {
                    NetworkLogger.__Log__(
                        "Lite Transporter: Detected that the server is already initialized in another instance. Only the client will be initialized in this instance.",
                        NetworkLogger.LogType.Warning
                    );

                    return;
                }

                port = NetworkHelper.GetAvailablePort(port, m_IPv6Enabled);
            }

            if (m_UsePortForwarding)
            {
                TryPortForwarding(port);
            }

            if (_manager.Start(port))
            {
                if (isServer && isRunning)
                {
                    IManager.Internal_OnServerInitialized();
                }

#if UNITY_SERVER
                if (_manager.UseNativeSockets)
                {
                    NetworkLogger.__Log__(
                        "Lite Transporter: Native sockets are being used for networking operations.",
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
                NetworkLogger.Print("[Port Forwarding] Successfully opened UDP port forwarding on port " + port + ".",
                    NetworkLogger.LogType.Log);
            }
            else
            {
                NetworkLogger.Print("[Port Forwarding] Failed to open UDP port forwarding on port " + port + ".",
                    NetworkLogger.LogType.Error);
            }
        }

        public void SendP2P(ReadOnlySpan<byte> data, IPEndPoint target)
        {
            if (isRunning)
            {
                _manager.SendUnconnectedMessage(data, target);
            }
            else
            {
                throw new InvalidOperationException(
                    "Lite Transporter: The transporter is not initialized. Ensure that the transporter is properly initialized before attempting this operation.");
            }
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
                    $"The delivery mode '{deliveryMode}' is not recognized or implemented."),
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
                    $"The delivery mode '{deliveryMethod}' is not recognized or implemented."),
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
                throw new InvalidOperationException(
                    "Lite Transporter: The transporter is not initialized. Ensure that the transporter is properly initialized before attempting this operation.");
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

                // Lag properties
                liteTransporter.m_SimulateLag = m_SimulateLag;
                liteTransporter.m_MinLatency = m_MinLatency;
                liteTransporter.m_MaxLatency = m_MaxLatency;
                liteTransporter.m_LossPercent = m_LossPercent;
            }
        }

#if OMNI_DEBUG
        private void OnValidate()
        {
            LiteTransporter[] liteTransporters = GetComponentsInChildren<LiteTransporter>();
            foreach (LiteTransporter liteTransporter in liteTransporters)
            {
                if (liteTransporter.isRunning)
                {
                    liteTransporter._manager.PingInterval = m_pingInterval;
                    liteTransporter._manager.SimulateLatency = m_SimulateLag;
                    liteTransporter._manager.SimulatePacketLoss = m_SimulateLag;
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