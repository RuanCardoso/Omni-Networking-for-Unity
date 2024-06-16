using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Omni.Core.Attributes;
using Omni.Core.Cryptography;
using Omni.Core.Interfaces;
using Omni.Core.Modules.Connection;
using Omni.Core.Modules.Matchmaking;
using Omni.Core.Modules.Ntp;
using Omni.Core.Modules.UConsole;
using Omni.Shared;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    enum ScriptingBackend
    {
        IL2CPP,
        Mono
    }

    public enum Status
    {
        /// <summary>
        /// Indicates the initial phase of an event.
        /// Typically used to signal the start of a process.
        /// </summary>
        Begin,

        /// <summary>
        /// Represents the intermediate phase of an event.
        /// This status is used when the main actions or operations are being performed.
        /// </summary>
        Normal,

        /// <summary>
        /// Marks the final phase of an event.
        /// It signifies the completion and cleanup of the process.
        /// </summary>
        End
    }

    [Flags]
    public enum CacheMode
    {
        None = 0,
        New = 1,
        Overwrite = 2,
        Global = 4,
        Group = 8,
        AutoDestroy = 16,
    }

    public enum Module
    {
        Console,
        Connection,
        Matchmaking,
        NtpClock,
        TickSystem
    }

    /// <summary>
    /// Specifies the target recipients for a network message.
    /// </summary>
    public enum Target
    {
        /// <summary>
        /// Sends the message to the current client itself. If the peer ID is 0 (server), the message is not executed.
        /// </summary>
        Self,

        /// <summary>
        /// Broadcasts the message to all connected players.
        /// </summary>
        All,

        /// <summary>
        /// Sends the message to all players except the sender.
        /// </summary>
        AllExceptSelf,

        /// <summary>
        /// Sends the message to all players who are members of the same groups as the sender.
        /// </summary>
        GroupMembers,

        /// <summary>
        /// Sends the message to all players(except the sender) who are members of the same groups as the sender.
        /// </summary>
        GroupMembersExceptSelf,

        /// <summary>
        /// Sends the message to all players who are not members of any groups.
        /// </summary>
        NonGroupMembers
    }

    /// <summary>
    /// Specifies the delivery mode for network packets within a communication protocol.
    /// </summary>
    public enum DeliveryMode : byte
    {
        /// <summary>
        /// Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
        /// </summary>
        Unreliable = 4,

        /// <summary>
        /// Reliable. Packets won't be dropped, won't be duplicated, can arrive without order.
        /// </summary>
        ReliableUnordered = 0,

        /// <summary>
        /// Unreliable. Packets can be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        Sequenced = 1,

        /// <summary>
        /// Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        ReliableOrdered = 2,

        /// <summary>
        /// Reliable only last packet. Packets can be dropped (except the last one), won't be duplicated, will arrive in order.
        /// Cannot be fragmented
        /// </summary>
        ReliableSequenced = 3
    }

    internal class MessageType // not a enum to avoid casting
    {
        internal const byte HttpPostResponseAsync = 245;
        internal const byte HttpPostFetchAsync = 246;
        internal const byte HttpGetResponseAsync = 247;
        internal const byte HttpGetFetchAsync = 248;
        internal const byte NtpQuery = 249;
        internal const byte BeginHandshake = 250;
        internal const byte EndHandshake = 251;
        internal const byte LocalInvoke = 252;
        internal const byte GlobalInvoke = 253;
        internal const byte LeaveGroup = 254;
        internal const byte JoinGroup = 255;
    }

    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class NetworkManager : MonoBehaviour, ITransporterReceive
    {
        private static Stopwatch _stopwatch = new Stopwatch();
        public static bool UseTickTiming { get; private set; } = false;
        internal static float DeltaTime =>
            UseTickTiming ? (float)TickSystem.DeltaTick : UnityEngine.Time.deltaTime;

        public static double ClockTime =>
            UseTickTiming ? TickSystem.ElapsedTicks : _stopwatch.Elapsed.TotalSeconds; // does not depend on frame rate.

        public static int MainThreadId { get; private set; }
        public static IObjectPooling<DataBuffer> Pool { get; } = new DataBufferPool();

        public static event Action OnServerInitialized;
        public static event Action<NetworkPeer, Status> OnServerPeerConnected;
        public static event Action<NetworkPeer, Status> OnServerPeerDisconnected;
        public static event Action OnClientConnected;
        public static event Action<string> OnClientDisconnected;

        private static event Action<byte, DataBuffer, NetworkPeer, int> OnServerCustomMessage;
        private static event Action<byte, DataBuffer, int> OnClientCustomMessage;

        internal static event Action<string, DataBuffer> OnJoinedGroup; // for client
        internal static event Action<DataBuffer, NetworkGroup, NetworkPeer> OnPlayerJoinedGroup; // for server
        internal static event Action<NetworkPeer, string> OnPlayerFailedJoinGroup; // for server
        internal static event Action<string, string> OnLeftGroup; // for client
        internal static event Action<NetworkGroup, NetworkPeer, Status, string> OnPlayerLeftGroup; // for server
        internal static event Action<NetworkPeer, string> OnPlayerFailedLeaveGroup;

        static NetworkConsole _console;
        public static NetworkConsole Console
        {
            get
            {
                if (_console == null)
                {
                    throw new Exception(
                        "Console module not initialized. Please call NetworkManager.InitializeModule(Module.Console) at least once before accessing the Console."
                    );
                }

                return _console;
            }
            private set
            {
                if (_console != null)
                {
                    throw new Exception(
                        "Console module already initialized. Please call NetworkManager.InitializeModule(Module.Console) only once."
                    );
                }

                _console = value;
            }
        }

        static NetworkConnection _connection;
        private static NetworkConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    throw new Exception(
                        "Connection module not initialized. Please call NetworkManager.InitializeModule(Module.Connection) at least once before accessing the Connection."
                    );
                }

                return _connection;
            }
            set
            {
                if (_connection != null)
                {
                    throw new Exception(
                        "Connection module already initialized. Please call NetworkManager.InitializeModule(Module.Connection) only once."
                    );
                }

                _connection = value;
            }
        }

        static NetworkMatchmaking _matchmaking;
        public static NetworkMatchmaking Matchmaking
        {
            get
            {
                if (_matchmaking == null)
                {
                    throw new Exception(
                        "Matchmaking module not initialized. Please call NetworkManager.InitializeModule(Module.Matchmaking) at least once before accessing the Matchmaking."
                    );
                }

                return _matchmaking;
            }
            set
            {
                if (_matchmaking != null)
                {
                    throw new Exception(
                        "Matchmaking module already initialized. Please call NetworkManager.InitializeModule(Module.Matchmaking) only once."
                    );
                }

                _matchmaking = value;
            }
        }

        static SimpleNtp _ntpClock;
        public static SimpleNtp SNTP
        {
            get
            {
                if (_ntpClock == null)
                {
                    throw new Exception(
                        "NtpClock module not initialized. Please call NetworkManager.InitializeModule(Module.NtpClock) at least once before accessing the NtpClock."
                    );
                }

                return _ntpClock;
            }
            set
            {
                if (_ntpClock != null)
                {
                    throw new Exception(
                        "NtpClock module already initialized. Please call NetworkManager.InitializeModule(Module.NtpClock) only once."
                    );
                }

                _ntpClock = value;
            }
        }

        static NetworkTickSystem _tickSystem;
        public static NetworkTickSystem TickSystem
        {
            get
            {
                if (_tickSystem == null)
                {
                    throw new Exception(
                        "TickSystem module not initialized. Please ensure that NetworkManager.InitializeModule(Module.TickSystem) is called at least once before accessing the TickSystem. If you are a client, wait until the connection and authentication processes are completed before accessing the TickSystem."
                    );
                }

                return _tickSystem;
            }
            set
            {
                if (_tickSystem != null)
                {
                    throw new Exception(
                        "TickSystem module already initialized. Please call NetworkManager.InitializeModule(Module.TickSystem) only once."
                    );
                }

                _tickSystem = value;
            }
        }

        /// <summary>
        /// Gets or sets the native peer.
        /// This property stores an instance of the NativePeer class, which represents a low-level peer in the network.
        /// It is used to manage native networking operations and interactions.
        /// </summary>
        static NativePeer LocalNativePeer { get; set; }
        static NetworkPeer _localPeer;

        /// <summary>
        /// Gets the local network peer.
        /// This property stores an instance of the NetworkPeer class, representing the local peer in the network.
        /// </summary>
        public static NetworkPeer LocalPeer
        {
            get
            {
                return _localPeer
                    ?? throw new Exception(
                        "Client(LocalPeer) is neither active, nor authenticated. Please verify using NetworkManager.IsClientActive."
                    );
            }
            private set => _localPeer = value;
        }

        /// <summary>
        /// Gets the local endpoint.
        /// This property stores an instance of the IPEndPoint class, representing the local network endpoint.
        /// It is used to specify the IP address and port number of the local peer.
        /// </summary>
        public static IPEndPoint LocalEndPoint { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the client is active.
        /// This property returns true if the client is currently active, authenticated and connected; otherwise, false.
        /// It is used to determine the client's connection status in the network.
        /// </summary>
        public static bool IsClientActive { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the server is active.
        /// This property returns true if the server is currently active; otherwise, false.
        /// It is used to determine the server's status in the network.
        /// </summary>
        public static bool IsServerActive { get; private set; }

        protected virtual void Awake()
        {
            if (_manager != null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!UseTickTiming)
            {
                _stopwatch.Start();
            }

            NetworkHelper.SaveComponent(this, "setup.cfg");
            _manager = this;
#if !UNITY_SERVER || UNITY_EDITOR
            if (m_MaxFpsOnClient > 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = m_MaxFpsOnClient;
            }
#else
            NetworkLogger.__Log__(
                $"MaxFpsOnClient is set to {m_MaxFpsOnClient}. This setting is ignored on server build.",
                NetworkLogger.LogType.Warning
            );
#endif
            AotHelper.EnsureDictionary<string, object>();
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
            DisableAutoStartIfHasHud();

            if (m_Connection)
            {
                HttpLite.Initialize();
                InitializeModule(Module.Connection);
            }

            if (m_Console)
            {
                InitializeModule(Module.Console);
            }

            if (m_NtpClock)
            {
                InitializeModule(Module.NtpClock);
            }

            if (m_Matchmaking)
            {
                InitializeModule(Module.Matchmaking);
            }

            if (m_TickSystem)
            {
                InitializeModule(Module.TickSystem);
            }
        }

        protected virtual void Start()
        {
#if OMNI_SERVER && !UNITY_EDITOR
            SkipDefaultUnityLog();
            ShowDefaultOmniLog();
#endif
        }

        private void Update()
        {
            if (m_TickSystem && _tickSystem != null)
            {
                TickSystem.OnTick();
            }

            UpdateFrameAndCpuMetrics();
        }

        private void UpdateFrameAndCpuMetrics()
        {
            deltaTime += UnityEngine.Time.unscaledDeltaTime;
            frameCount++;

            float rate = 1f;
            if (deltaTime >= rate)
            {
                Framerate = frameCount / deltaTime;
                CpuTimeMs = deltaTime / frameCount * 1000f;

                deltaTime -= rate;
                frameCount = 0;
            }
        }

        private void SkipDefaultUnityLog()
        {
            System.Console.Clear();
        }

        private void ShowDefaultOmniLog()
        {
            NetworkLogger.Log("Welcome to Omni Server Console.");
#if OMNI_DEBUG
            NetworkLogger.Log("You are in Debug Mode.");
#else
            NetworkLogger.Log("You are in Release Mode.");
#endif
            System.Console.Write("\n");
        }

        public static void InitializeModule(Module module)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            switch (module)
            {
                case Module.TickSystem:
                    {
                        if (IsServerActive && _tickSystem == null)
                        {
                            TickSystem = new NetworkTickSystem();
                            TickSystem.Initialize(Manager.m_TickRate);
                        }
                    }
                    break;
                case Module.NtpClock:
                    {
                        NetworkClock nClock = Manager.GetComponent<NetworkClock>();
                        if (nClock != null)
                        {
                            Manager.m_QueryInterval = nClock.QueryInterval;
                            UseTickTiming = nClock.UseTickTiming;
                        }

                        SNTP = new SimpleNtp();
                        SNTP.Initialize(nClock);
                    }
                    break;
                case Module.Console:
                    {
                        Console = new NetworkConsole();
                        Console.Initialize();
                    }
                    break;
                case Module.Connection:
                    {
                        if (
                            !Manager.TryGetComponent<TransporterBehaviour>(
                                out var currentTransporter
                            )
                        )
                        {
                            throw new Exception(
                                "No transporter found on NetworkManager. Please add one."
                            );
                        }

                        GameObject clientTransporter = new("Client Transporter");
                        GameObject serverTransporter = new("Server Transporter");

                        clientTransporter.hideFlags =
                            HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                        serverTransporter.hideFlags =
                            HideFlags.HideInHierarchy | HideFlags.HideInInspector;

                        clientTransporter.transform.parent = Manager.transform;
                        serverTransporter.transform.parent = Manager.transform;

                        clientTransporter.AddComponent(currentTransporter.GetType());
                        serverTransporter.AddComponent(currentTransporter.GetType());

                        Manager.m_ClientTransporter =
                            clientTransporter.GetComponent<TransporterBehaviour>();

                        Manager.m_ServerTransporter =
                            serverTransporter.GetComponent<TransporterBehaviour>();

                        currentTransporter.ITransporter.CopyTo(
                            Manager.m_ClientTransporter.ITransporter
                        );

                        currentTransporter.ITransporter.CopyTo(
                            Manager.m_ServerTransporter.ITransporter
                        );

                        // Don't destroy the manager!
                        DontDestroyOnLoad(Manager);

                        Connection = new NetworkConnection();
                        Connection.Server.StartTransporter(
                            Manager.m_ServerTransporter.ITransporter,
                            Manager
                        );

                        Connection.Client.StartTransporter(
                            Manager.m_ClientTransporter.ITransporter,
                            Manager
                        );

#if OMNI_RELEASE
                        Manager.m_AutoStartServer = true;
                        Manager.m_AutoStartClient = true;
#else
                        if (
                            ConnectAddress.ToLower() != "localhost"
                            && ConnectAddress != "127.0.0.1"
                            && ConnectAddress != Manager.PublicIPv4
                            && ConnectAddress != Manager.PublicIPv6
                        )
                        {
                            Manager.m_AutoStartServer = false;
                            NetworkLogger.__Log__(
                                "Server auto-start has been disabled as the client address is not a recognized localhost or public IPv4/IPv6 address. "
                                    + "Starting a server in this case does not make sense because the client cannot connect to it. But you can start it manually.",
                                NetworkLogger.LogType.Warning
                            );
                        }
#endif

                        if (Manager.m_AutoStartServer)
                        {
                            StartServer(Manager.m_ServerListenPort);
                        }

                        if (Manager.m_AutoStartClient)
                        {
                            Connect(
                                Manager.m_ConnectAddress,
                                Manager.m_ConnectPort,
                                Manager.m_ClientListenPort
                            );
                        }
                    }
                    break;
                case Module.Matchmaking:
                    {
                        Matchmaking = new NetworkMatchmaking();
                        Matchmaking.Initialize();
                    }
                    break;
            }
        }

        public static void StartServer(int port)
        {
            if (!IsServerActive)
            {
#if OMNI_DEBUG
                Server.GenerateRsaKeys();
                Connection.Server.Listen(port);
                NetworkHelper.SaveComponent(_manager, "setup.cfg");
#else
#if UNITY_EDITOR
                Server.GenerateRsaKeys();
                Connection.Server.Listen(port);
                NetworkHelper.SaveComponent(_manager, "setup.cfg");
#elif !UNITY_SERVER
                NetworkLogger.LogToFile("Server is not available in release mode on client build.");
#else
                Server.GenerateRsaKeys();
                Connection.Server.Listen(port);
                NetworkHelper.SaveComponent(_manager, "setup.cfg");
#endif
#endif
            }
            else
            {
                throw new Exception(
                    "Server is already initialized. Ensure to call StopServer() before calling StartServer()."
                );
            }
        }

        public static void DisconnectPeer(NetworkPeer peer)
        {
            if (IsServerActive)
            {
                Connection.Server.Disconnect(peer);
            }
            else
            {
                throw new Exception("Server is not initialized. Ensure to call StartServer().");
            }
        }

        public static void StopServer()
        {
            if (IsServerActive)
            {
                Connection.Server.Stop();
            }
            else
            {
                throw new Exception(
                    "Server is not initialized. Ensure to call StartServer() before calling StopServer()."
                );
            }
        }

        public static void Connect(string address, int port)
        {
            Connect(address, port, Manager.m_ClientListenPort);
        }

        public static void Connect(string address, int port, int listenPort)
        {
            if (!IsClientActive)
            {
#if !UNITY_SERVER || UNITY_EDITOR // Don't connect to the server in server build!
                Connection.Client.Listen(listenPort);
                Connection.Client.Connect(address, port);
#elif UNITY_SERVER && !UNITY_EDITOR
                NetworkLogger.__Log__("Debug: Client is not available in a server build.");
#endif
            }
            else
            {
                throw new Exception(
                    "Client is already initialized. Ensure to call StopClient() before calling Connect()."
                );
            }
        }

        public static void Disconnect()
        {
            if (IsClientActive)
            {
                Connection.Client.Disconnect(LocalPeer);
            }
            else
            {
                throw new Exception("Client is not initialized. Ensure to call Connect().");
            }
        }

        public static void StopClient()
        {
            if (IsClientActive)
            {
                Connection.Client.Stop();
            }
            else
            {
                throw new Exception(
                    "Client is not initialized. Ensure to call Connect() before calling StopClient()."
                );
            }
        }

        internal static void SendToClient(
            byte msgType,
            DataBuffer buffer,
            IPEndPoint fromPeer,
            Target target,
            DeliveryMode deliveryMode,
            int groupId,
            int cacheId,
            CacheMode cacheMode,
            byte sequenceChannel
        )
        {
            Manager.Internal_SendToClient(
                msgType,
                buffer.WrittenSpan,
                fromPeer,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }

        internal static void SendToServer(
            byte msgType,
            DataBuffer buffer,
            DeliveryMode deliveryMode,
            byte sequenceChannel
        )
        {
            Manager.Internal_SendToServer(
                msgType,
                buffer.WrittenSpan,
                deliveryMode,
                sequenceChannel
            );
        }

        protected virtual ReadOnlySpan<byte> PrepareClientMessageForSending(
            byte msgType,
            ReadOnlySpan<byte> message
        )
        {
            using DataBuffer header = Pool.Rent();
            header.FastWrite(msgType);
            header.Write(message);
            return header.WrittenSpan;
        }

        protected virtual ReadOnlySpan<byte> PrepareServerMessageForSending(
            byte msgType,
            ReadOnlySpan<byte> message
        )
        {
            using DataBuffer header = Pool.Rent();
            header.FastWrite(msgType);
            header.Write(message);
            return header.WrittenSpan;
        }

        protected virtual void Internal_SendToClient(
            byte msgType,
            ReadOnlySpan<byte> _data,
            IPEndPoint fromPeer,
            Target target,
            DeliveryMode deliveryMode,
            int groupId,
            int cacheId,
            CacheMode cacheMode,
            byte sequenceChannel
        )
        {
            NetworkHelper.EnsureRunningOnMainThread();
            void Send(ReadOnlySpan<byte> message, IPEndPoint peer)
            {
                Connection.Server.Send(message, peer, deliveryMode, sequenceChannel);
            }

            void CreateCache(ReadOnlySpan<byte> message, NetworkGroup _group)
            {
                if (cacheMode != CacheMode.None || cacheId != 0)
                {
                    if (
                        (cacheId != 0 && cacheMode == CacheMode.None)
                        || (cacheMode != CacheMode.None && cacheId == 0)
                    )
                    {
                        throw new Exception(
                            "Cache Error: Both cacheId and cacheMode must be set together."
                        );
                    }
                    else
                    {
                        if (PeersByIp.TryGetValue(fromPeer, out NetworkPeer owner))
                        {
                            if (
                                cacheMode == (CacheMode.Global | CacheMode.New)
                                || cacheMode
                                    == (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
                            )
                            {
                                Server.CACHES_APPEND_GLOBAL.Add(
                                    new NetworkCache(
                                        cacheId,
                                        cacheMode,
                                        message.ToArray(),
                                        owner,
                                        deliveryMode,
                                        target,
                                        sequenceChannel,
                                        destroyOnDisconnect: cacheMode.HasFlag(
                                            CacheMode.AutoDestroy
                                        )
                                    )
                                );
                            }
                            else if (
                                cacheMode == (CacheMode.Group | CacheMode.New)
                                || cacheMode
                                    == (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
                            )
                            {
                                if (_group != null)
                                {
                                    _group.CACHES_APPEND.Add(
                                        new NetworkCache(
                                            cacheId,
                                            cacheMode,
                                            message.ToArray(),
                                            owner,
                                            deliveryMode,
                                            target,
                                            sequenceChannel,
                                            destroyOnDisconnect: cacheMode.HasFlag(
                                                CacheMode.AutoDestroy
                                            )
                                        )
                                    );
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        "Cache Error: The specified group was not found. Please verify the existence of the group and ensure the groupId is correct.",
                                        NetworkLogger.LogType.Error
                                    );
                                }
                            }
                            else if (
                                cacheMode == (CacheMode.Global | CacheMode.Overwrite)
                                || cacheMode
                                    == (
                                        CacheMode.Global
                                        | CacheMode.Overwrite
                                        | CacheMode.AutoDestroy
                                    )
                            )
                            {
                                NetworkCache newCache = new NetworkCache(
                                    cacheId,
                                    cacheMode,
                                    message.ToArray(),
                                    owner,
                                    deliveryMode,
                                    target,
                                    sequenceChannel,
                                    destroyOnDisconnect: cacheMode.HasFlag(CacheMode.AutoDestroy)
                                );

                                if (Server.CACHES_OVERWRITE_GLOBAL.ContainsKey(cacheId))
                                {
                                    Server.CACHES_OVERWRITE_GLOBAL[cacheId] = newCache;
                                }
                                else
                                {
                                    Server.CACHES_OVERWRITE_GLOBAL.Add(cacheId, newCache);
                                }
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    "Cache Error: Unsupported cache mode set.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else if (
                            cacheMode == (CacheMode.Group | CacheMode.Overwrite)
                            || cacheMode
                                == (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
                        )
                        {
                            if (_group != null)
                            {
                                NetworkCache newCache = new NetworkCache(
                                    cacheId,
                                    cacheMode,
                                    message.ToArray(),
                                    owner,
                                    deliveryMode,
                                    target,
                                    sequenceChannel,
                                    destroyOnDisconnect: cacheMode.HasFlag(CacheMode.AutoDestroy)
                                );

                                if (_group.CACHES_OVERWRITE.ContainsKey(cacheId))
                                {
                                    _group.CACHES_OVERWRITE[cacheId] = newCache;
                                }
                                else
                                {
                                    _group.CACHES_OVERWRITE.Add(cacheId, newCache);
                                }
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    "Cache Error: The specified group was not found. Please verify the existence of the group and ensure the groupId is correct.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else
                        {
                            NetworkLogger.__Log__(
                                "Cache Error: Peer not found. ensure that the peer is connected.",
                                NetworkLogger.LogType.Error
                            );
                        }
                    }
                }
            }

            ReadOnlySpan<byte> message = PrepareServerMessageForSending(msgType, _data);
            bool cacheIsEnabled = cacheMode != CacheMode.None || cacheId != 0;

            if (IsServerActive)
            {
                if (!_allowZeroGroupForInternalMessages && !m_ZeroGroupMessage && groupId == 0)
                {
                    NetworkLogger.__Log__(
                        "Send: Access denied: Zero-group message not allowed. Join a group first or set 'AllowZeroGroupMessage' to true.",
                        NetworkLogger.LogType.Error
                    );

                    return;
                }

                if (target == Target.Self && groupId != 0 && !cacheIsEnabled)
                {
                    NetworkLogger.__Log__(
                        "Target.Self cannot be used with groups. Note that this is not a limitation, it just doesn't make sense.",
                        NetworkLogger.LogType.Warning
                    );
                }

                var peersById = PeersById;
                NetworkGroup _group = null;

                if (groupId != 0)
                {
                    if (Groups.TryGetValue(groupId, out _group))
                    {
                        if (!m_AcrossGroupMessage || !_group.AllowAcrossGroupMessage)
                        {
                            if (PeersByIp.TryGetValue(fromPeer, out var peer))
                            {
                                if (!_group._peersById.ContainsKey(peer.Id) && peer.Id != 0)
                                {
                                    NetworkLogger.__Log__(
                                        "Send: Access denied: Across-group message not allowed. Or set 'AllowAcrossGroupMessage' to true.",
                                        NetworkLogger.LogType.Error
                                    );

                                    return;
                                }
                            }
                        }

                        peersById = _group._peersById; // Filter: peers by group.
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            $"Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                            NetworkLogger.LogType.Error
                        );

                        return;
                    }
                }

                CreateCache(message, _group);

                switch (target)
                {
                    case Target.NonGroupMembers:
                        {
                            var peers = peersById.Values.Where(p => p.Groups.Count == 0);
                            foreach (var peer in peers)
                            {
                                if (peer.Id == Server.ServerPeer.Id)
                                    continue;

                                Send(message, peer.EndPoint);
                            }
                        }
                        break;
                    case Target.GroupMembersExceptSelf:
                    case Target.GroupMembers:
                        {
                            if (groupId != 0 && !cacheIsEnabled)
                            {
                                NetworkLogger.__Log__(
                                    "Send: Target.GroupMembers cannot be used with specified groups(groupId is not 0). Note that this is not a limitation, it just doesn't make sense.",
                                    NetworkLogger.LogType.Warning
                                );
                            }

                            if (PeersByIp.TryGetValue(fromPeer, out var sender))
                            {
                                if (sender.Groups.Count == 0)
                                {
                                    NetworkLogger.__Log__(
                                        "Send: You are not in any groups. Please join a group first.",
                                        NetworkLogger.LogType.Error
                                    );

                                    return;
                                }

                                foreach (var (_, group) in sender.Groups)
                                {
                                    foreach (var (_, peer) in group._peersById)
                                    {
                                        if (peer.Id == Server.ServerPeer.Id)
                                            continue;

                                        if (
                                            peer.EndPoint.Equals(fromPeer)
                                            && target == Target.GroupMembersExceptSelf
                                        )
                                            continue;

                                        Send(message, peer.EndPoint);
                                    }
                                }
                            }
                        }
                        break;
                    case Target.All:
                        {
                            foreach (var (_, peer) in peersById)
                            {
                                if (peer.Id == Server.ServerPeer.Id)
                                    continue;

                                Send(message, peer.EndPoint);
                            }
                        }
                        break;
                    case Target.AllExceptSelf:
                        {
                            foreach (var (_, peer) in peersById)
                            {
                                if (peer.Id == Server.ServerPeer.Id)
                                    continue;

                                if (peer.EndPoint.Equals(fromPeer))
                                    continue;

                                Send(message, peer.EndPoint);
                            }
                        }
                        break;
                    case Target.Self:
                        {
                            // group id doesn't make sense here, because peersById is not used for target.Self.
                            Send(message, fromPeer);
                        }
                        break;
                }

                _allowZeroGroupForInternalMessages = false;
            }
        }

        protected virtual void Internal_SendToServer(
            byte msgType,
            ReadOnlySpan<byte> data,
            DeliveryMode deliveryMode,
            byte sequenceChannel
        )
        {
            NetworkHelper.EnsureRunningOnMainThread();
            if (IsClientActive)
            {
                Connection.Client.Send(
                    PrepareClientMessageForSending(msgType, data),
                    LocalEndPoint,
                    deliveryMode,
                    sequenceChannel
                );
            }
            else
            {
                NetworkLogger.__Log__(
                    "Your are trying to send a message to the server while not connected. Please connect first.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        private float m_QueryInterval = NetworkClock.DEFAULT_QUERY_INTERVAL;

        private IEnumerator QueryNtp()
        {
            // The purpose of these calls before the while loop may be to ensure that the system clock is initially synchronized before entering the continuous query cycle.
            // This can be helpful to prevent situations where the system clock is not immediately synchronized when the application starts.
            //
            // Furthermore, the introduction of these initial pauses may serve as a startup measure to allow the system time to stabilize before initiating the repetitive querying of the NTP server.
            // This can be particularly useful if there are other startup or configuration operations that need to occur before the system is fully ready to synchronize the clock continuously.

            if (IsClientActive && m_NtpClock)
            {
                SNTP.Client.Query();
                yield return new WaitForSeconds(0.5f);
                SNTP.Client.Query();
                yield return new WaitForSeconds(0.5f);
                SNTP.Client.Query();
                yield return new WaitForSeconds(0.5f);

                while (IsClientActive && m_NtpClock)
                {
                    // Continuously query the NTP server to ensure that the system clock is continuously synchronized with the NTP server.
                    SNTP.Client.Query();
                    yield return new WaitForSeconds(m_QueryInterval);
                }
            }
        }

        public virtual void Internal_OnServerInitialized()
        {
            NetworkHelper.EnsureRunningOnMainThread();
            // Set the default peer, used when the server sends to nothing(peerId = 0).
            PeersByIp.Add(Server.ServerPeer.EndPoint, Server.ServerPeer);
            PeersById.Add(Server.ServerPeer.Id, Server.ServerPeer);

            IsServerActive = true;
            OnServerInitialized?.Invoke();
        }

        public virtual void Internal_OnClientConnected(IPEndPoint peer, NativePeer nativePeer)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            LocalEndPoint = peer;
            LocalNativePeer = nativePeer;
        }

        public virtual void Internal_OnClientDisconnected(IPEndPoint peer, string reason)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            IsClientActive = false;
            OnClientDisconnected?.Invoke(reason);
        }

        public virtual async void Internal_OnServerPeerConnected(
            IPEndPoint peer,
            NativePeer nativePeer
        )
        {
            NetworkHelper.EnsureRunningOnMainThread();
            NetworkPeer newPeer = new(peer, p_UniqueId++);
            newPeer._nativePeer = nativePeer;

            if (!PeersByIp.TryAdd(peer, newPeer))
            {
                NetworkLogger.__Log__(
                    $"Connection Error: Failed to add peer '{peer}' because it already exists.",
                    NetworkLogger.LogType.Error
                );
            }
            else
            {
                if (!PeersById.TryAdd(newPeer.Id, newPeer))
                {
                    NetworkLogger.__Log__(
                        $"Connection Error: Failed to add peer by ID '{newPeer.Id}' because it already exists.",
                        NetworkLogger.LogType.Error
                    );
                }
                else
                {
                    newPeer.IsConnected = true;
                    using var message = Pool.Rent();
                    message.FastWrite(newPeer.Id);
                    // Write the server's RSA public key to the buffer
                    message.Write(Server.RsaPublicKey);

                    SendToClient(
                        MessageType.BeginHandshake,
                        message,
                        peer,
                        Target.Self,
                        DeliveryMode.ReliableOrdered,
                        0,
                        0,
                        CacheMode.None,
                        0
                    );

                    NetworkLogger.__Log__(
                        $"Connection Info: Peer '{peer}' added to the server successfully."
                    );

                    OnServerPeerConnected?.Invoke(newPeer, Status.Begin);
                }
            }
        }

        public virtual void Internal_OnServerPeerDisconnected(IPEndPoint peer, string reason)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            OnServerPeerDisconnected?.Invoke(PeersByIp[peer], Status.Begin);
            if (!PeersByIp.Remove(peer, out NetworkPeer currentPeer))
            {
                NetworkLogger.__Log__(
                    $"Disconnection Error: Failed to remove peer '{peer}' from the server.",
                    NetworkLogger.LogType.Error
                );
            }
            else
            {
                if (!PeersById.Remove(currentPeer.Id, out NetworkPeer _))
                {
                    NetworkLogger.__Log__(
                        $"Disconnection Error: Failed to remove peer by ID '{currentPeer.Id}' from the server.",
                        NetworkLogger.LogType.Error
                    );
                }
                else
                {
                    foreach (var (_, group) in currentPeer.Groups)
                    {
                        if (group._peersById.Remove(currentPeer.Id, out _))
                        {
                            NetworkLogger.__Log__(
                                $"Disconnection Info: Peer '{peer}' removed from group '{group.Identifier}'."
                            );

                            // Dereferencing to allow for GC(Garbage Collector).
                            // All resources should be released at this point.
                            group.DestroyAllCaches(currentPeer);

                            OnPlayerLeftGroup?.Invoke(
                                group,
                                currentPeer,
                                Status.End,
                                "Leave event called by disconnect event."
                            );

                            if (group.DestroyWhenEmpty)
                            {
                                Server.DestroyGroup(group);
                            }
                        }
                        else
                        {
                            NetworkLogger.__Log__(
                                $"Disconnection Error: Failed to remove peer '{peer}' from group '{group.Identifier}'.",
                                NetworkLogger.LogType.Error
                            );
                        }
                    }

                    NetworkLogger.__Log__(
                        $"Disconnection Info: Peer '{peer}' removed from the server. Reason: {reason}."
                    );

                    OnServerPeerDisconnected?.Invoke(currentPeer, Status.Normal);

                    // Dereferencing to allow for GC(Garbage Collector).
                    currentPeer.ClearGroups();
                    currentPeer.ClearData();

                    // All resources should be released at this point.
                    Server.DestroyAllCaches(currentPeer);
                    currentPeer.IsConnected = false;

                    // Finished disconnection
                    OnServerPeerDisconnected?.Invoke(currentPeer, Status.End);
                }
            }
        }

        public virtual void Internal_OnDataReceived(
            ReadOnlySpan<byte> _data,
            DeliveryMode deliveryMethod,
            IPEndPoint _peer,
            byte sequenceChannel,
            bool isServer
        )
        {
            NetworkHelper.EnsureRunningOnMainThread();
            if (PeersByIp.TryGetValue(_peer, out NetworkPeer peer) || !isServer)
            {
                using DataBuffer message = Pool.Rent();
                message.Write(_data);
                message.ResetWrittenCount();

                byte msgType = message.FastRead<byte>(); // Note: On Message event
                message._reworkStart = message.WrittenCount; // Skip header
                message._reworkEnd = _data.Length; // Slice -> [Header..Length]

                if (!isServer)
                {
                    if (
                        msgType != MessageType.BeginHandshake
                        && msgType != MessageType.EndHandshake
                    )
                    {
                        if (!IsClientActive)
                        {
                            throw new Exception(
                                "The client received a message while not yet authenticated. Wait until the handshake is completed."
                            );
                        }
                    }
                }

                void ResetReadPosition()
                {
                    message._reworkStart = message.WrittenCount; // Skip header
                }

                switch (msgType)
                {
                    case MessageType.NtpQuery:
                        {
                            if (isServer)
                            {
                                double time = message.FastRead<double>();
                                float t = message.FastRead<float>();
                                ResetReadPosition();
                                SNTP.Server.SendNtpResponse(time, peer, t);
                            }
                            else
                            {
                                double a = message.FastRead<double>();
                                double x = message.FastRead<double>();
                                double y = message.FastRead<double>();
                                float t = message.FastRead<float>();
                                ResetReadPosition();
                                SNTP.Client.Evaluate(a, x, y, t);
                            }
                        }
                        break;
                    case MessageType.BeginHandshake:
                        {
                            if (!isServer)
                            {
                                // Client side!

                                int localPeerId = message.FastRead<int>();
                                string rsaServerPublicKey = message.ReadString();
                                ResetReadPosition();

                                // Initialize the local peer
                                LocalPeer = new NetworkPeer(LocalEndPoint, localPeerId);
                                LocalPeer._nativePeer = LocalNativePeer;
                                IsClientActive = true; // true: to allow send the aes key to the server.

                                // Generate AES Key and send it to the server(Encrypted by RSA public key).
                                Client.RsaServerPublicKey = rsaServerPublicKey;
                                byte[] aesKey = AesCryptography.GenerateKey();
                                LocalPeer.AesKey = aesKey;

                                // Crypt the AES Key with the server's RSA public key
                                byte[] cryptedAesKey = RsaCryptography.Encrypt(
                                    aesKey,
                                    Client.RsaServerPublicKey
                                );

                                // Send the AES Key to the server
                                using DataBuffer authMessage = Pool.Rent();
                                authMessage.ToBinary(cryptedAesKey);
                                SendToServer(
                                    MessageType.BeginHandshake,
                                    authMessage,
                                    DeliveryMode.ReliableOrdered,
                                    0
                                );

                                IsClientActive = false; // Waiting for server's authorization response.
                            }
                            else
                            {
                                // Server side!

                                byte[] aesKey = message.FromBinary<byte[]>();
                                ResetReadPosition();

                                // Decrypt the AES Key with the server's RSA private key
                                peer.AesKey = RsaCryptography.Decrypt(aesKey, Server.RsaPrivateKey);

                                // Send Ok to the client!
                                SendToClient(
                                    MessageType.EndHandshake,
                                    DataBuffer.Empty,
                                    _peer,
                                    Target.Self,
                                    DeliveryMode.ReliableOrdered,
                                    0,
                                    0,
                                    CacheMode.None,
                                    0
                                );
                            }
                        }
                        break;
                    case MessageType.EndHandshake:
                        {
                            if (!isServer)
                            {
                                if (_tickSystem == null)
                                {
                                    TickSystem = new NetworkTickSystem();
                                    TickSystem.Initialize(m_TickRate);
                                }
                                else
                                {
                                    print("Inicializado já");
                                }

                                // Connection end & authorized.
                                LocalPeer.IsConnected = true;
                                IsClientActive = true;
                                StartCoroutine(QueryNtp());
                                OnClientConnected?.Invoke();

                                // Send Ok to the server!
                                SendToServer(
                                    MessageType.EndHandshake,
                                    DataBuffer.Empty,
                                    DeliveryMode.ReliableOrdered,
                                    0
                                );
                            }
                            else
                            {
                                OnServerPeerConnected?.Invoke(peer, Status.Normal);
                                OnServerPeerConnected?.Invoke(peer, Status.End);
                            }
                        }
                        break;
                    case MessageType.LocalInvoke:
                        {
                            int identityId = message.FastRead<int>();
                            byte instanceId = message.FastRead<byte>();
                            byte invokeId = message.FastRead<byte>();

                            ResetReadPosition();

                            var key = (identityId, instanceId);
                            var eventBehavious = isServer
                                ? Server.LocalEventBehaviours
                                : Client.LocalEventBehaviours;

                            if (eventBehavious.TryGetValue(key, out INetworkMessage behaviour))
                            {
                                behaviour.Internal_OnMessage(
                                    invokeId,
                                    message,
                                    peer, // peer is null on client.
                                    isServer,
                                    sequenceChannel
                                );
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Invoke Error: Failed to find global event behaviour with Id: [{identityId}] and instance Id: [{instanceId}] on the {(isServer ? "Server" : "Client")} side. "
                                        + $"This function exists on the {(!isServer ? "Server" : "Client")} side, but is missing on the {(!isServer ? "Client" : "Server")} side. "
                                        + "Ensure it is registered first or ignore it if intended.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        break;
                    case MessageType.GlobalInvoke:
                        {
                            int identityId = message.FastRead<int>();
                            byte invokeId = message.FastRead<byte>();

                            ResetReadPosition();

                            var eventBehavious = isServer
                                ? Server.GlobalEventBehaviours
                                : Client.GlobalEventBehaviours;

                            if (
                                eventBehavious.TryGetValue(
                                    identityId,
                                    out INetworkMessage behaviour
                                )
                            )
                            {
                                behaviour.Internal_OnMessage(
                                    invokeId,
                                    message,
                                    peer, // peer is null on client.
                                    isServer,
                                    sequenceChannel
                                );
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Invoke Error: Failed to find global event behaviour with Id: [{identityId}] on the {(isServer ? "Server" : "Client")} side. "
                                        + $"This function exists on the {(!isServer ? "Server" : "Client")} side, but is missing on the {(!isServer ? "Client" : "Server")} side. "
                                        + "Ensure it is registered first or ignore it if intended.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        break;
                    case MessageType.LeaveGroup:
                        {
                            string groupName = message.FastReadString();
                            string reason = message.FastReadString();

                            ResetReadPosition();

                            if (isServer)
                            {
                                Server.LeaveGroup(groupName, reason, peer);
                            }
                            else
                            {
                                OnLeftGroup?.Invoke(groupName, reason);
                            }
                        }
                        break;
                    case MessageType.JoinGroup:
                        {
                            string groupName = message.FastReadString();

                            ResetReadPosition();

                            if (isServer)
                            {
                                if (string.IsNullOrEmpty(groupName))
                                {
                                    NetworkLogger.__Log__(
                                        "JoinGroup: Group name cannot be null or empty.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                if (groupName.Length > 256)
                                {
                                    NetworkLogger.__Log__(
                                        "JoinGroup: Group name cannot be longer than 256 characters.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                Server.JoinGroup(groupName, message, peer, false);
                            }
                            else
                            {
                                OnJoinedGroup?.Invoke(groupName, message);
                            }
                        }
                        break;
                    default:
                        {
                            if (isServer)
                            {
                                ResetReadPosition();
                                OnServerCustomMessage?.Invoke(
                                    msgType,
                                    message,
                                    peer,
                                    sequenceChannel
                                );
                            }
                            else
                            {
                                ResetReadPosition();
                                OnClientCustomMessage?.Invoke(msgType, message, sequenceChannel);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Converts an object to JSON format.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to be converted.</param>
        /// <param name="settings">Optional settings for JSON serialization (default is null).</param>
        /// <returns>A string representing the JSON serialization of the object.</returns>
        public static string ToJson<T>(T obj, JsonSerializerSettings settings = null)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }

        /// <summary>
        /// Converts an object to binary format using MemoryPackSerializer.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to be converted.</param>
        /// <param name="settings">Optional settings for serialization (default is null).</param>
        /// <returns>A byte array representing the binary serialization of the object.</returns>
        public static byte[] ToBinary<T>(T obj, MemoryPackSerializerOptions settings = null)
        {
            return MemoryPackSerializer.Serialize(obj, settings);
        }

        /// <summary>
        /// Deserializes an object from JSON format.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="settings">Optional settings for JSON deserialization (default is null).</param>
        /// <returns>The deserialized object.</returns>
        public static T FromJson<T>(string json, JsonSerializerSettings settings = null)
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Deserializes an object from binary format using MemoryPackSerializer.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="data">The byte array containing the binary data to deserialize.</param>
        /// <param name="settings">Optional settings for deserialization (default is null).</param>
        /// <returns>The deserialized object.</returns>
        public static T FromBinary<T>(byte[] data, MemoryPackSerializerOptions settings = null)
        {
            return MemoryPackSerializer.Deserialize<T>(data, settings);
        }

        public static List<byte[]> Split(ReadOnlySpan<byte> data, int blockSize = 128)
        {
            if (data.Length <= blockSize)
            {
                throw new Exception("The specified data must be longer than block size");
            }

            int x = blockSize;
            if (!((x != 0) && ((x & (x - 1)) == 0)))
            {
                throw new Exception("Block size must be a power of 2.");
            }

            int offset = 0;
            List<byte[]> blocks = new();
            while (offset < data.Length)
            {
                int end = Math.Min(data.Length - offset, blockSize);
                ReadOnlySpan<byte> block = data.Slice(offset, end);
                offset += block.Length;

                // Add the block to the final result list.
                blocks.Add(block.ToArray());
            }

            // Return the splitted data in order.
            return blocks;
        }
    }

    public partial class NetworkManager
    {
        private int frameCount = 0;
        private float deltaTime = 0f;

        private static int p_UniqueId = 1; // 0 - is reserved for server
        private static bool _allowZeroGroupForInternalMessages = false;

        private static NetworkManager _manager;
        private static NetworkManager Manager
        {
            get
            {
                if (_manager == null)
                {
                    throw new Exception(
                        "Network Manager not initialized. Please add it to the scene."
                    );
                }

                return _manager;
            }
            set => _manager = value;
        }

        private static Dictionary<int, NetworkGroup> Groups { get; } = new();
        private static Dictionary<IPEndPoint, NetworkPeer> PeersByIp { get; } = new();
        private static Dictionary<int, NetworkPeer> PeersById { get; } = new();

        [SerializeField]
        [Label("Public IPv4")]
        [ReadOnly]
        private string PublicIPv4 = "127.0.0.1";

        [SerializeField]
        [Label("Public IPv6")]
        [ReadOnly]
        private string PublicIPv6 = "127.0.0.1";

        [Header("Scripting Backend")]
        [SerializeField]
#if OMNI_DEBUG
        [ReadOnly]
#endif
        [Label("Client Backend")]
        private ScriptingBackend m_ClientScriptingBackend = ScriptingBackend.Mono;

        [SerializeField]
#if OMNI_DEBUG
        [ReadOnly]
#endif
        [Label("Server Backend")]
        private ScriptingBackend m_ServerScriptingBackend = ScriptingBackend.Mono;

        [ReadOnly]
        [SerializeField]
        [Header("Modules")]
        private bool m_Connection = true;

        [SerializeField]
        private bool m_Matchmaking = false;

        [SerializeField]
        private bool m_TickSystem = false;

        [SerializeField]
        [Label("Server Console")]
        private bool m_Console = false;

        [SerializeField]
        [Label("Server Clock(Ntp)")]
        private bool m_NtpClock = false;

        [ReadOnly]
        [SerializeField]
        [Header("Transporters")]
        private TransporterBehaviour m_ServerTransporter;

        [ReadOnly]
        [SerializeField]
        private TransporterBehaviour m_ClientTransporter;

        [SerializeField]
        [Header("Listen")]
        [Label("Server Port")]
        private int m_ServerListenPort = 7777;

        [SerializeField]
        [Label("Client Port")]
        private int m_ClientListenPort = 7778;

        [Header("Connection")]
        [SerializeField]
        [Label("Host Address")]
        private string m_ConnectAddress = "127.0.0.1";

        [SerializeField]
        [Label("Port")]
        private int m_ConnectPort = 7777;

        [SerializeField]
        [Header("Misc")]
        [Min(1)]
        private int m_TickRate = 15;

        [SerializeField]
        [Min(1)]
        private int m_MaxFpsOnClient = 60;

        [SerializeField]
        [Label("Allow Across-Group Message")]
        private bool m_AcrossGroupMessage = false;

        [SerializeField]
        [Label("Allow Zero-Group Message")]
        private bool m_ZeroGroupMessage = true;

        [Header("Misc +")]
        [SerializeField]
#if OMNI_RELEASE
        [ReadOnly]
#endif
        private bool m_AutoStartClient = true;

        [SerializeField]
#if OMNI_RELEASE
        [ReadOnly]
#endif
        private bool m_AutoStartServer = true;

        [SerializeField]
        private bool m_RunInBackground = true;

        public static string ConnectAddress => Manager.m_ConnectAddress;

        internal static bool MatchmakingModuleEnabled => Manager.m_Matchmaking;
        internal static bool TickSystemModuleEnabled => Manager.m_TickSystem;

        public static int ServerListenPort => Manager.m_ServerListenPort;
        public static int ClientListenPort => Manager.m_ClientListenPort;
        public static int ConnectPort => Manager.m_ConnectPort;

        public static float Framerate { get; private set; }
        public static float CpuTimeMs { get; private set; }

        public virtual void Reset()
        {
            OnValidate();
        }

        public virtual void OnValidate()
        {
#if OMNI_DEBUG
            m_ClientScriptingBackend = ScriptingBackend.Mono;
            m_ServerScriptingBackend = ScriptingBackend.Mono;
#endif
            m_Connection = true;
            if (!Application.isPlaying)
            {
                if (m_ClientTransporter != null || m_ServerTransporter != null)
                {
                    m_ClientTransporter = m_ServerTransporter = null;
                    throw new Exception("Transporter cannot be set. Is automatically initialized.");
                }

                GetExternalIp();
                SetScriptingBackend();
                StripComponents();
            }

            Application.runInBackground = m_RunInBackground;
            m_ConnectAddress = m_ConnectAddress.Trim();
            DisableAutoStartIfHasHud();
        }

        [ContextMenu("Strip Components")]
        private void StripComponents()
        {
            // Strip the components.
            var serverObject = transform.GetChild(0);
            var clientObject = transform.GetChild(1);

#if OMNI_RELEASE
            UnityEngine.Debug.Log("Stripping components... Ready to build!");
            name = "Network Manager";

#if UNITY_SERVER
            clientObject.tag = "EditorOnly";
            serverObject.tag = "Untagged";
#else
            serverObject.tag = "EditorOnly";
            clientObject.tag = "Untagged";
#endif
#elif OMNI_DEBUG
            clientObject.tag = "Untagged";
            serverObject.tag = "Untagged";
#endif
        }

        [ContextMenu("Set Scripting Backend")]
        private void SetScriptingBackend()
        {
            ScriptingBackend[] scriptingBackends =
            {
                m_ServerScriptingBackend,
                m_ClientScriptingBackend
            };

            using StreamWriter writer = new("ScriptingBackend.txt");
            writer.Write(ToJson(scriptingBackends));
        }

        [ContextMenu("Get External IP")]
        private void ForceGetExternalIp()
        {
            PlayerPrefs.DeleteKey("IPLastReceiveDate");
            GetExternalIp();
        }

        [Conditional("UNITY_EDITOR")]
        private async void GetExternalIp()
        {
            string lastDateTime = PlayerPrefs.GetString(
                "IPLastReceiveDate",
                DateTime.UnixEpoch.ToString()
            );

            int minutes = 15;
            TimeSpan timeLeft = DateTime.Now - DateTime.Parse(lastDateTime);
            // Check if the last call was successful or if an {minutes} time has passed since the last call to avoid spamming.
            if (timeLeft.TotalMinutes >= minutes)
            {
                PublicIPv4 = (await NetworkHelper.GetExternalIp(useIPv6: false)).ToString();
                PublicIPv6 = (await NetworkHelper.GetExternalIp(useIPv6: true)).ToString();

                // Update the player preference with the current timestamp.
                PlayerPrefs.SetString("IPLastReceiveDate", DateTime.Now.ToString());
            }
            else
            {
#if OMNI_DEBUG
                timeLeft = TimeSpan.FromMinutes(minutes) - timeLeft;
                NetworkLogger.Log(
                    $"You should wait {minutes} minutes before you can get the external IP again. Go to the context menu and click \"Get External IP\" to force it. Remaining time: {timeLeft.Minutes:0} minutes and {timeLeft.Seconds} seconds.",
                    logType: NetworkLogger.LogType.Warning
                );
#endif
            }
        }

        private bool DisableAutoStartIfHasHud()
        {
            if (TryGetComponent<NetworkHud>(out _))
            {
                m_AutoStartClient = false;
                m_AutoStartServer = false;

                return true;
            }

            return false;
        }

        public virtual void OnApplicationQuit()
        {
            Connection.Server.Stop();
            Connection.Client.Stop();
        }
    }

    public class TransporterBehaviour : MonoBehaviour
    {
        private ITransporter _ITransporter;
        internal ITransporter ITransporter
        {
            get
            {
                if (_ITransporter == null)
                {
                    throw new NullReferenceException(
                        "This transporter is not initialized! Call Initialize() first."
                    );
                }

                return _ITransporter;
            }
            set => _ITransporter = value;
        }
    }
}
