using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using MemoryPack;
using Newtonsoft.Json;
using Omni.Core.Attributes;
using Omni.Core.Interfaces;
using Omni.Core.Modules.Connection;
using Omni.Core.Modules.Matchmaking;
using Omni.Core.Modules.UConsole;
using Omni.Shared;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    [Flags]
    public enum CacheMode
    {
        None = 0,
        New = 1,
        Overwrite = 2,
        Global = 4,
        Group = 8,
        DestroyOnDisconnect = 16,
    }

    public enum Module
    {
        Console,
        Connection,
        Matchmaking
    }

    public enum Target
    {
        Self,
        All,
        AllExceptSelf,
    }

    /// <summary>
    /// Sending method type
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
        internal const byte Handshake = 250;
        internal const byte GenerateUniqueId = 251;
        internal const byte LocalInvoke = 252;
        internal const byte GlobalInvoke = 253;
        internal const byte LeaveGroup = 254;
        internal const byte JoinGroup = 255;
    }

    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public partial class NetworkManager : MonoBehaviour, ITransporterReceive
    {
        public static int MainThreadId { get; private set; }
        public static IObjectPooling<NetworkBuffer> Pool { get; } = new NetworkBufferPool();

        public static event Action OnServerInitialized;
        public static event Action<NetworkPeer> OnServerPeerConnected;
        public static event Action<NetworkPeer> OnServerPeerDisconnected;
        public static event Action OnClientConnected;
        public static event Action<string> OnClientDisconnected;

        // private and internal usages, do not expose to the public.
        private static event Action<byte, NetworkBuffer, NetworkPeer, int> OnServerCustomMessage;
        private static event Action<byte, NetworkBuffer, int> OnClientCustomMessage;

        internal static event Action<string, NetworkBuffer> OnJoinedGroup; // for client
        internal static event Action<NetworkBuffer, NetworkGroup, NetworkPeer> OnPlayerJoinedGroup; // for server
        internal static event Action<NetworkPeer, string> OnPlayerFailedJoinGroup; // for server
        internal static event Action<string, string> OnLeftGroup; // for client
        internal static event Action<NetworkGroup, NetworkPeer, string> OnPlayerLeftGroup; // for server
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

        public static NetworkPeer LocalPeer { get; private set; }
        public static IPEndPoint LocalClientEndPoint { get; private set; }
        public static bool IsLocalClientConnected { get; private set; }
        public static bool IsServerActive { get; private set; }

        public virtual void Awake()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
            if (_manager != null)
            {
                gameObject.SetActive(false);
                return;
            }

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

            DisableAutoStartIfHasHud();
            if (m_Console)
            {
                InitializeModule(Module.Console);
            }

            if (m_Matchmaking)
            {
                InitializeModule(Module.Matchmaking);
            }

            if (m_Connection)
            {
                InitializeModule(Module.Connection);
            }
        }

        public static void InitializeModule(Module module)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            switch (module)
            {
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
#if OMNI_DEBUG
            Connection.Server.Listen(port);
#else
#if UNITY_EDITOR
            Connection.Server.Listen(port);
#elif !UNITY_SERVER
            NetworkLogger.LogToFile("Server is not available in release mode on client build.");
#else
            Connection.Server.Listen(port);
#endif
#endif
        }

        public static void Connect(string address, int port)
        {
            Connect(address, port, Manager.m_ClientListenPort);
        }

        public static void Connect(string address, int port, int listenPort)
        {
#if !UNITY_SERVER || UNITY_EDITOR // Don't connect to the server in server build!
            Connection.Client.Listen(listenPort);
            Connection.Client.Connect(address, port);
#elif UNITY_SERVER && !UNITY_EDITOR
            NetworkLogger.__Log__("Debug: Local client is not available in a server build.");
#endif
        }

        internal static void SendToClient(
            byte msgType,
            NetworkBuffer buffer,
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
            NetworkBuffer buffer,
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
            using NetworkBuffer header = Pool.Rent();
            header.FastWrite(msgType);
            header.Write(message);
            return header.WrittenSpan;
        }

        protected virtual ReadOnlySpan<byte> PrepareServerMessageForSending(
            byte msgType,
            ReadOnlySpan<byte> message
        )
        {
            using NetworkBuffer header = Pool.Rent();
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
                                    == (
                                        CacheMode.Global
                                        | CacheMode.New
                                        | CacheMode.DestroyOnDisconnect
                                    )
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
                                            CacheMode.DestroyOnDisconnect
                                        )
                                    )
                                );
                            }
                            else if (
                                cacheMode == (CacheMode.Group | CacheMode.New)
                                || cacheMode
                                    == (
                                        CacheMode.Group
                                        | CacheMode.New
                                        | CacheMode.DestroyOnDisconnect
                                    )
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
                                                CacheMode.DestroyOnDisconnect
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
                                        | CacheMode.DestroyOnDisconnect
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
                                    destroyOnDisconnect: cacheMode.HasFlag(
                                        CacheMode.DestroyOnDisconnect
                                    )
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
                                == (
                                    CacheMode.Group
                                    | CacheMode.Overwrite
                                    | CacheMode.DestroyOnDisconnect
                                )
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
                                    destroyOnDisconnect: cacheMode.HasFlag(
                                        CacheMode.DestroyOnDisconnect
                                    )
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

                if (target == Target.Self && groupId != 0)
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
            if (IsLocalClientConnected)
            {
                Connection.Client.Send(
                    PrepareClientMessageForSending(msgType, data),
                    LocalClientEndPoint,
                    deliveryMode,
                    sequenceChannel
                );
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

        public virtual void Internal_OnClientConnected(IPEndPoint peer)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            LocalClientEndPoint = peer;
            IsLocalClientConnected = true;
            OnClientConnected?.Invoke();
        }

        public virtual void Internal_OnClientDisconnected(IPEndPoint peer, string reason)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            LocalClientEndPoint = peer;
            IsLocalClientConnected = false;
            OnClientDisconnected?.Invoke(reason);
        }

        public virtual void Internal_OnServerPeerConnected(IPEndPoint peer)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            NetworkPeer newPeer = new(peer, p_UniqueId++);
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
                    // TODO: Implement handshake with AES & RSA.
                    using NetworkBuffer message = Pool.Rent();
                    message.FastWrite(newPeer.Id);
                    SendToClient(
                        MessageType.Handshake,
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

                    OnServerPeerConnected?.Invoke(newPeer);
                }
            }
        }

        public virtual void Internal_OnServerPeerDisconnected(IPEndPoint peer)
        {
            NetworkHelper.EnsureRunningOnMainThread();
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
                                $"Disconnection Info: Peer '{peer}' removed from group '{group.Name}'."
                            );

                            OnPlayerLeftGroup?.Invoke(
                                group,
                                currentPeer,
                                "Leave event called by disconnect event."
                            );

                            // Dereferencing to allow for GC(Garbage Collector).
                            // All resources should be released at this point.
                            group.DestroyAllCaches(currentPeer);
                        }
                        else
                        {
                            NetworkLogger.__Log__(
                                $"Disconnection Error: Failed to remove peer '{peer}' from group '{group.Name}'.",
                                NetworkLogger.LogType.Error
                            );
                        }
                    }

                    NetworkLogger.__Log__(
                        $"Disconnection Info: Peer '{peer}' removed from the server."
                    );

                    OnServerPeerDisconnected?.Invoke(currentPeer);

                    // Dereferencing to allow for GC(Garbage Collector).
                    currentPeer.ClearGroups();
                    currentPeer.ClearData();

                    // All resources should be released at this point.
                    Server.DestroyAllCaches(currentPeer);
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
                using NetworkBuffer message = Pool.Rent();
                message.Write(_data);
                message.ResetWrittenCount();

                byte msgType = message.FastRead<byte>(); // Note: On Message event
                message._reworkStart = message.WrittenCount; // Skip header
                message._reworkEnd = _data.Length; // Slice -> [Header..Length]

                switch (msgType)
                {
                    case MessageType.Handshake:
                        {
                            if (!isServer)
                            {
                                int localPeerId = message.FastRead<int>();
                                message._reworkStart = message.WrittenCount; // Skip header
                                LocalPeer = new NetworkPeer(LocalClientEndPoint, localPeerId);
                            }
                        }
                        break;
                    case MessageType.LocalInvoke:
                        {
                            int identityId = message.FastRead<int>();
                            byte instanceId = message.FastRead<byte>();
                            byte invokeId = message.FastRead<byte>();

                            // Skip header
                            message._reworkStart = message.WrittenCount;

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
                                    $"Invoke Error: Failed to find event behaviour with ID: {identityId}. Register it first or ignore it.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        break;
                    case MessageType.GlobalInvoke:
                        {
                            int identityId = message.FastRead<int>();
                            byte invokeId = message.FastRead<byte>();

                            // Skip header
                            message._reworkStart = message.WrittenCount;

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
                                    $"Invoke Error: Failed to find event behaviour with ID: {identityId}. Register it first or ignore it.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        break;
                    case MessageType.LeaveGroup:
                        {
                            string groupName = message.FastReadString();
                            string reason = message.FastReadString();

                            // Skip header
                            message._reworkStart = message.WrittenCount;

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

                            // Skip header
                            message._reworkStart = message.WrittenCount;

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
                                OnServerCustomMessage?.Invoke(
                                    msgType,
                                    message,
                                    peer,
                                    sequenceChannel
                                );
                            }
                            else
                            {
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
    }

    public partial class NetworkManager
    {
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

        [ReadOnly]
        // [SerializeField]
        [Header("Modules")]
        private bool m_Connection = true;

        // [SerializeField]
        private bool m_Console = true;

        // [SerializeField]
        private bool m_Matchmaking = true;

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
        [Min(0)]
        private int m_MaxFpsOnClient = 60;

        [SerializeField]
        [Label("Allow Across-Group Message")]
        private bool m_AcrossGroupMessage = false;

        [SerializeField]
        [Label("Allow Zero-Group Message")]
        private bool m_ZeroGroupMessage = true;

        [SerializeField]
        private bool m_AutoStartClient = true;

        [SerializeField]
        private bool m_AutoStartServer = true;

        public static string ConnectAddress => Manager.m_ConnectAddress;
        public static int ServerListenPort => Manager.m_ServerListenPort;
        public static int ClientListenPort => Manager.m_ClientListenPort;
        public static int ConnectPort => Manager.m_ConnectPort;

        public virtual void Reset()
        {
            DisableAutoStartIfHasHud();
        }

        public virtual void OnValidate()
        {
            m_Connection = true;
            if (!Application.isPlaying)
            {
                if (m_ClientTransporter != null || m_ServerTransporter != null)
                {
                    m_ClientTransporter = m_ServerTransporter = null;
                    throw new Exception("Transporter cannot be set. Is automatically initialized.");
                }
            }

            DisableAutoStartIfHasHud();
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
