using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using Omni.Core.Attributes;
using Omni.Core.Interfaces;
using Omni.Shared;
using UnityEngine;

namespace Omni.Core.Modules.Connection
{
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Omni/Transporters/Lite Transporter")]
    internal class LiteTransporter : TransporterBehaviour, ITransporter
    {
        private EventBasedNetListener _listener;
        private NetManager _manager;

        private bool isServer;
        private bool isRunning;

        [Header("Settings")]
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

        [SerializeField]
        [Tooltip("Specifies the maximum number of connections allowed at the same time.")]
        [Min(1)]
        private int m_MaxConnections = 256;

        [SerializeField]
        [Tooltip("Max events that will be processed per frame.")]
        [Min(0)]
        private int m_MaxEventsPerFrame = 0;

        [SerializeField]
        [Tooltip("Specifies whether IPv6 is enabled. Note: Not all platforms may support this.")]
        [Label("IPv6 Enabled")]
        private bool m_IPv6Enabled = false;

        [SerializeField]
        [Tooltip(
            "Specifies whether to use native sockets for networking operations. Note: Enabling this option may enhance performance but could be platform-dependent."
        )]
        private bool m_useNativeSockets = false;

        [SerializeField]
        [Tooltip(
            "Specifies whether to use a safe MTU (Maximum Transmission Unit) size for networking operations. Using a safe MTU can reduce the risk of packet loss and ensure smoother data transmission."
        )]
        private bool m_useSafeMtu = true;

        private ITransporterReceive IReceive;
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

        public void Initialize(ITransporterReceive IReceive, bool isServer)
        {
            this.isServer = isServer;
            this.IReceive = IReceive;

            if (isRunning)
            {
                throw new Exception("Transporter is already initialized!");
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
                UseNativeSockets = m_useNativeSockets, // Experimental feature mostly for servers. Only for Windows/Linux
                UseSafeMtu = m_useSafeMtu
            };

            if (isServer)
            {
                _listener.ConnectionRequestEvent += request =>
                {
                    if (_manager.ConnectedPeersCount < m_MaxConnections)
                    {
                        if (request.AcceptIfKey(m_VersionName) == null)
                        {
                            NetworkLogger.__Log__(
                                "Lite: The connection was rejected! because the version is not the same.",
                                NetworkLogger.LogType.Error
                            );
                        }
                    }
                    else
                    {
                        request.Reject();
                        NetworkLogger.__Log__(
                            "Lite: Max connections reached! The connection was rejected!",
                            NetworkLogger.LogType.Warning
                        );
                    }
                };

                _listener.PeerConnectedEvent += peer =>
                {
                    if (!_peers.TryAdd(peer, peer))
                    {
                        NetworkLogger.__Log__(
                            $"Lite: The peer: {peer} is already connected!",
                            NetworkLogger.LogType.Error
                        );
                    }
                    else
                    {
                        IReceive.Internal_OnServerPeerConnected(peer);
                    }
                };

                _listener.PeerDisconnectedEvent += (peer, info) =>
                {
                    if (!_peers.Remove(peer))
                    {
                        NetworkLogger.__Log__(
                            $"Lite: The peer: {peer} is already disconnected!",
                            NetworkLogger.LogType.Error
                        );
                    }
                    else
                    {
                        IReceive.Internal_OnServerPeerDisconnected(peer);
                    }
                };
            }
            else
            {
                _listener.PeerConnectedEvent += peer =>
                {
                    if (peer.ConnectionState == ConnectionState.Connected)
                    {
                        localPeer ??= peer;
                        IReceive.Internal_OnClientConnected(peer);
                    }
                };

                _listener.PeerDisconnectedEvent += (peer, info) =>
                {
                    IReceive.Internal_OnClientDisconnected(
                        localPeer,
                        $"code: {info.SocketErrorCode} | reason: {info.Reason}"
                    );
                };
            }

            _listener.NetworkReceiveEvent += (peer, reader, sequenceChannel, deliveryMethod) =>
            {
                IReceive.Internal_OnDataReceived(
                    reader.GetRemainingBytesSpan(),
                    GetDeliveryMode(deliveryMethod),
                    peer,
                    sequenceChannel,
                    isServer
                );

                reader.Recycle(); // Important! avoid memory leaks.
            };

            isRunning = true;
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
                throw new Exception("Connect() is not available for server!");
            }

            localPeer = _manager.Connect(address, port, m_VersionName);
        }

        public void Disconnect()
        {
            Stop();
        }

        public void Listen(int port)
        {
            ThrowAnErrorIfNotInitialized();
            if (!NetworkHelper.IsPortAvailable(port, ProtocolType.Udp, m_IPv6Enabled))
            {
                if (isServer)
                {
                    NetworkLogger.__Log__(
                        "Lite: Server is already initialized in another instance, only the client will be initialized.",
                        NetworkLogger.LogType.Warning
                    );

                    return;
                }

                port = NetworkHelper.GetAvailablePort(port, m_IPv6Enabled);
            }

            if (_manager.Start(port))
            {
                if (isServer && isRunning)
                {
                    IReceive.Internal_OnServerInitialized();
                }

#if UNITY_SERVER
                if (_manager.UseNativeSockets)
                {
                    NetworkLogger.__Log__(
                        "Lite: Using native sockets for networking operations.",
                        NetworkLogger.LogType.Warning
                    );
                }
#endif
            }
        }

        public void Send(
            ReadOnlySpan<byte> data,
            IPEndPoint target,
            DeliveryMode deliveryMode,
            byte sequenceChannel
        )
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

        private DeliveryMethod GetDeliveryMethod(DeliveryMode deliveryMode)
        {
            return deliveryMode switch
            {
                DeliveryMode.ReliableOrdered => DeliveryMethod.ReliableOrdered,
                DeliveryMode.ReliableSequenced => DeliveryMethod.ReliableSequenced,
                DeliveryMode.ReliableUnordered => DeliveryMethod.ReliableUnordered,
                DeliveryMode.Unreliable => DeliveryMethod.Unreliable,
                DeliveryMode.Sequenced => DeliveryMethod.Sequenced,
                _ => throw new NotImplementedException("Unknown delivery mode!"),
            };
        }

        private DeliveryMode GetDeliveryMode(DeliveryMethod deliveryMethod) // my*
        {
            return deliveryMethod switch
            {
                DeliveryMethod.ReliableOrdered => DeliveryMode.ReliableOrdered,
                DeliveryMethod.ReliableSequenced => DeliveryMode.ReliableSequenced,
                DeliveryMethod.ReliableUnordered => DeliveryMode.ReliableUnordered,
                DeliveryMethod.Unreliable => DeliveryMode.Unreliable,
                DeliveryMethod.Sequenced => DeliveryMode.Sequenced,
                _ => throw new NotImplementedException("Unknown delivery method!"),
            };
        }

        public void Stop()
        {
            ThrowAnErrorIfNotInitialized();
            _manager.Stop();
        }

        [Conditional("OMNI_DEBUG")]
        private void ThrowAnErrorIfNotInitialized()
        {
            if (!isRunning)
            {
                throw new Exception("Low Level: Transporter is not initialized!");
            }
        }

        public void CopyTo(ITransporter ITransporter)
        {
            LiteTransporter liteTransporter = ITransporter as LiteTransporter;
            liteTransporter.m_VersionName = m_VersionName;
            liteTransporter.m_disconnectTimeout = m_disconnectTimeout;
            liteTransporter.m_IPv6Enabled = m_IPv6Enabled;
            liteTransporter.m_pingInterval = m_pingInterval;
            liteTransporter.m_MaxConnections = m_MaxConnections;
            liteTransporter.m_MaxEventsPerFrame = m_MaxEventsPerFrame;
            liteTransporter.m_useNativeSockets = m_useNativeSockets;
            liteTransporter.m_useSafeMtu = m_useSafeMtu;
        }
    }
}
