using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Omni.Core.Interfaces;
using Omni.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    public class NbClient
    {
        private readonly INetworkMessage m_NetworkMessage;
        private readonly NetVarBehaviour m_NetVarBehaviour;

        internal NbClient(INetworkMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
            m_NetVarBehaviour = m_NetworkMessage as NetVarBehaviour;
        }

        /// <summary>
        /// Sends a manual 'NetVar' message to the server with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void ManualSync<T>(
            T property,
            byte propertyId,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        )
        {
            using DataBuffer message = m_NetVarBehaviour.CreateHeader(property, propertyId);
            Invoke(255, message, deliveryMode, sequenceChannel);
        }

        /// <summary>
        /// Automatically sends a 'NetVar' message to the server based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        /// <param name="callerName">The name of the calling member. This parameter is automatically supplied by the compiler.</param>
        public void AutoSync<T>(
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0,
            [CallerMemberName] string callerName = ""
        )
        {
            IPropertyInfo property = m_NetVarBehaviour.GetPropertyInfoWithCallerName<T>(callerName);
            IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

            if (property != null)
            {
                using DataBuffer message = m_NetVarBehaviour.CreateHeader(
                    propertyGeneric.Invoke(),
                    property.Id
                );

                Invoke(255, message, deliveryMode, sequenceChannel);
            }
        }

        /// <summary>
        /// Invokes a global message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void GlobalInvoke(
            byte msgId,
            DataBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        ) => Client.GlobalInvoke(msgId, buffer, deliveryMode, sequenceChannel);

        /// <summary>
        /// Invokes a message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void Invoke(
            byte msgId,
            DataBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        ) =>
            Client.Invoke(
                msgId,
                m_NetworkMessage.IdentityId,
                buffer,
                deliveryMode,
                sequenceChannel
            );
    }

    public class NbServer
    {
        private readonly INetworkMessage m_NetworkMessage;
        private readonly NetVarBehaviour m_NetVarBehaviour;

        internal NbServer(INetworkMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
            m_NetVarBehaviour = m_NetworkMessage as NetVarBehaviour;
        }

        /// <summary>
        /// Sends a manual 'NetVar' message to all(default) clients with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void ManualSync<T>(
            T property,
            byte propertyId,
            NetworkPeer peer = null,
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            peer ??= Server.ServerPeer;
            using DataBuffer message = m_NetVarBehaviour.CreateHeader(property, propertyId);
            Invoke(
                255,
                peer.Id,
                message,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }

        /// <summary>
        /// Automatically sends a 'NetVar' message to all(default) clients based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        /// <param name="callerName">The name of the calling member. This parameter is automatically supplied by the compiler
        public void AutoSync<T>(
            NetworkPeer peer = null,
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0,
            [CallerMemberName] string callerName = ""
        )
        {
            IPropertyInfo property = m_NetVarBehaviour.GetPropertyInfoWithCallerName<T>(callerName);
            IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

            if (property != null)
            {
                peer ??= Server.ServerPeer;
                using DataBuffer message = m_NetVarBehaviour.CreateHeader(
                    propertyGeneric.Invoke(),
                    property.Id
                );

                Invoke(
                    255,
                    peer.Id,
                    message,
                    target,
                    deliveryMode,
                    groupId,
                    cacheId,
                    cacheMode,
                    sequenceChannel
                );
            }
        }

        /// <summary>
        /// Invokes a global message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void GlobalInvoke(
            byte msgId,
            int peerId,
            DataBuffer buffer = null,
            Target target = Target.Self,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            Server.GlobalInvoke(
                msgId,
                peerId,
                buffer,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }

        /// <summary>
        /// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void Invoke(
            byte msgId,
            int peerId,
            DataBuffer buffer = null,
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            Server.Invoke(
                msgId,
                peerId,
                m_NetworkMessage.IdentityId,
                buffer,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }
    }

    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    [DefaultExecutionOrder(-3000)]
    public class NetworkEventBehaviour : NetVarBehaviour, INetworkMessage
    {
        [Header("Service Settings")]
        [SerializeField]
        private string m_ServiceName;

        [SerializeField]
        private int m_Id;

        private NbClient local;
        private NbServer remote;

        private bool m_UnregisterOnLoad = true;

        /// <summary>
        /// Gets the identifier of the associated <see cref="INetworkMessage"/>.
        /// </summary>
        /// <value>The identifier of the associated <see cref="INetworkMessage"/> as an integer.</value>
        public int IdentityId => m_Id;

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbClient"/> instance used to invoke messages on the server from the client.
        /// </summary>
        public NbClient Local
        {
            get
            {
                if (local == null)
                {
                    throw new NullReferenceException(
                        "The event behaviour has not been initialized. Call Awake() first or initialize manually."
                    );
                }

                return local;
            }
            private set => local = value;
        }

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbServer"/> instance used to invoke messages on the client from the server.
        /// </summary>
        public NbServer Remote
        {
            get
            {
                if (remote == null)
                {
                    throw new NullReferenceException(
                        "The event behaviour has not been initialized. Call Awake() first or initialize manually."
                    );
                }

                return remote;
            }
            private set => remote = value;
        }

        private readonly EventBehaviour<DataBuffer, int, Null, Null, Null> clientEventBehaviour =
            new();

        private readonly EventBehaviour<
            DataBuffer,
            NetworkPeer,
            int,
            Null,
            Null
        > serverEventBehaviour = new();

        protected virtual void Awake()
        {
            if (NetworkService.Exists(m_ServiceName))
            {
                m_UnregisterOnLoad = false;
                return;
            }

            if (m_UnregisterOnLoad)
            {
                InitializeServiceLocator();
                InitializeBehaviour();
                RegisterSystemEvents();
                OnAwake();
            }
        }

        protected virtual void Start()
        {
            if (m_UnregisterOnLoad)
            {
                RegisterMatchmakingEvents();
                StartCoroutine(Internal_OnServerStart());
                StartCoroutine(Internal_OnClientStart());
                OnStart();
            }

            m_UnregisterOnLoad = !NetworkHelper.IsDontDestroyOnLoad(gameObject);
        }

        protected void InitializeServiceLocator()
        {
            if (!NetworkService.TryRegister(this, m_ServiceName))
            {
                // Update the old reference to the new one.
                NetworkService.Update(this, m_ServiceName);
            }
        }

        private IEnumerator Internal_OnServerStart()
        {
            yield return new WaitUntil(() => IsServerActive);
            OnServerStart();
        }

        private IEnumerator Internal_OnClientStart()
        {
            yield return new WaitUntil(() => IsClientActive);
            OnClientStart();
        }

        /// <summary>
        /// Invoked when the server becomes active. This method functions similarly to Unity's Start(),
        /// but is specifically called when the server is up and running.
        /// </summary>
        protected virtual void OnServerStart() { }

        /// <summary>
        /// Invoked when the client becomes active. This method functions similarly to Unity's Start(),
        /// but is specifically called when the client is up and running.
        /// </summary>
        protected virtual void OnClientStart() { }

        protected virtual void OnAwake() { }

        protected virtual void OnStart() { }

        protected virtual void OnStop() { }

        protected void InitializeBehaviour()
        {
            clientEventBehaviour.FindEvents<ClientAttribute>(this);
            serverEventBehaviour.FindEvents<ServerAttribute>(this);

            Client.AddEventBehaviour(m_Id, this);
            Server.AddEventBehaviour(m_Id, this);

            Local = new NbClient(this);
            Remote = new NbServer(this);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnClientDisconnected += OnClientDisconnected;
            Client.OnMessage += OnClientMessage;

            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            Server.OnMessage += OnServerMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            if (MatchmakingModuleEnabled)
            {
                Matchmaking.Client.OnJoinedGroup += OnJoinedGroup;
                Matchmaking.Client.OnLeftGroup += OnLeftGroup;

                Matchmaking.Server.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
                Matchmaking.Server.OnPlayerLeftGroup += OnPlayerLeftGroup;

                Matchmaking.Server.OnPlayerFailedJoinGroup += OnPlayerFailedJoinGroup;
                Matchmaking.Server.OnPlayerFailedLeaveGroup += OnPlayerFailedLeaveGroup;
            }
        }

        protected void Unregister()
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            NetworkManager.OnClientConnected -= OnClientConnected;
            NetworkManager.OnClientDisconnected -= OnClientDisconnected;
            Client.OnMessage -= OnClientMessage;

            NetworkManager.OnServerInitialized -= OnServerInitialized;
            NetworkManager.OnServerPeerConnected -= OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
            Server.OnMessage -= OnServerMessage;

            if (MatchmakingModuleEnabled)
            {
                Matchmaking.Client.OnJoinedGroup -= OnJoinedGroup;
                Matchmaking.Client.OnLeftGroup -= OnLeftGroup;

                Matchmaking.Server.OnPlayerJoinedGroup -= OnPlayerJoinedGroup;
                Matchmaking.Server.OnPlayerLeftGroup -= OnPlayerLeftGroup;

                Matchmaking.Server.OnPlayerFailedJoinGroup -= OnPlayerFailedJoinGroup;
                Matchmaking.Server.OnPlayerFailedLeaveGroup -= OnPlayerFailedLeaveGroup;
            }

            NetworkService.Unregister(m_ServiceName);
            OnStop();
        }

        protected virtual void OnBeforeSceneLoad(Scene scene)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
        }

        #region Client
        protected virtual void OnClientConnected() { }

        protected virtual void OnClientDisconnected(string reason) { }

        protected virtual void OnClientMessage(byte msgId, DataBuffer buffer, int seqChannel)
        {
            buffer.SeekToBegin();
            TryClientLocate(msgId, buffer, seqChannel); // Global Invoke
        }

        private void TryClientLocate(byte msgId, DataBuffer buffer, int seqChannel)
        {
            if (clientEventBehaviour.TryGetLocate(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        clientEventBehaviour.Invoke(msgId);
                        break;
                    case 1:
                        clientEventBehaviour.Invoke(msgId, buffer);
                        break;
                    case 2:
                        clientEventBehaviour.Invoke(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        clientEventBehaviour.Invoke(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        clientEventBehaviour.Invoke(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        clientEventBehaviour.Invoke(
                            msgId,
                            buffer,
                            seqChannel,
                            default,
                            default,
                            default
                        );
                        break;
                }
            }
        }

        protected virtual void OnJoinedGroup(string groupName, DataBuffer buffer) { }

        protected virtual void OnLeftGroup(string groupName, string reason) { }
        #endregion

        #region Server
        protected virtual void OnServerInitialized() { }

        protected virtual void OnServerPeerConnected(NetworkPeer peer, Status status) { }

        protected virtual void OnServerPeerDisconnected(NetworkPeer peer, Status status) { }

        protected virtual void OnPlayerFailedLeaveGroup(NetworkPeer peer, string reason) { }

        protected virtual void OnPlayerFailedJoinGroup(NetworkPeer peer, string reason) { }

        protected virtual void OnServerMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            buffer.SeekToBegin();
            TryServerLocate(msgId, buffer, peer, seqChannel); // Global Invoke
        }

        private void TryServerLocate(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            if (serverEventBehaviour.TryGetLocate(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        serverEventBehaviour.Invoke(msgId);
                        break;
                    case 1:
                        serverEventBehaviour.Invoke(msgId, buffer);
                        break;
                    case 2:
                        serverEventBehaviour.Invoke(msgId, buffer, peer);
                        break;
                    case 3:
                        serverEventBehaviour.Invoke(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        serverEventBehaviour.Invoke(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        serverEventBehaviour.Invoke(
                            msgId,
                            buffer,
                            peer,
                            seqChannel,
                            default,
                            default
                        );
                        break;
                }
            }
        }

        protected virtual void OnPlayerJoinedGroup(
            DataBuffer buffer,
            NetworkGroup group,
            NetworkPeer peer
        ) { }

        protected virtual void OnPlayerLeftGroup(
            NetworkGroup group,
            NetworkPeer peer,
            Status status,
            string reason
        ) { }
        #endregion

        public void Internal_OnMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            bool isServer,
            int seqChannel
        )
        {
            if (isServer)
            {
                TryServerLocate(msgId, buffer, peer, seqChannel);
            }
            else
            {
                TryClientLocate(msgId, buffer, seqChannel);
            }
        }

        protected virtual void Reset()
        {
            OnValidate();
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }

            if (string.IsNullOrEmpty(m_ServiceName))
            {
                m_ServiceName = GetType().Name;
            }
        }
    }

    [DefaultExecutionOrder(-3000)]
    public class ClientEventBehaviour : NetVarBehaviour, INetworkMessage
    {
        [Header("Service Settings")]
        [SerializeField]
        private string m_ServiceName;

        [SerializeField]
        private int m_Id;
        private NbClient local;

        private bool m_UnregisterOnLoad = true;

        /// <summary>
        /// Gets the identifier of the associated <see cref="INetworkMessage"/>.
        /// </summary>
        /// <value>The identifier of the associated <see cref="INetworkMessage"/> as an integer.</value>
        public int IdentityId => m_Id;

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbClient"/> instance used to invoke messages on the server from the client.
        /// </summary>
        public NbClient Local
        {
            get
            {
                if (local == null)
                {
                    throw new System.NullReferenceException(
                        "The event behaviour has not been initialized. Call Awake() first or initialize manually."
                    );
                }

                return local;
            }
            private set => local = value;
        }

        private readonly EventBehaviour<DataBuffer, int, Null, Null, Null> eventBehaviour = new();

        protected virtual void Awake()
        {
            if (NetworkService.Exists(m_ServiceName))
            {
                m_UnregisterOnLoad = false;
                return;
            }

            if (m_UnregisterOnLoad)
            {
                InitializeServiceLocator();
                InitializeBehaviour();
                RegisterSystemEvents();
                OnAwake();
            }
        }

        protected virtual void Start()
        {
            if (m_UnregisterOnLoad)
            {
                RegisterMatchmakingEvents();
                StartCoroutine(Internal_OnClientStart());
                OnStart();
            }

            m_UnregisterOnLoad = !NetworkHelper.IsDontDestroyOnLoad(gameObject);
        }

        protected void InitializeServiceLocator()
        {
            if (!NetworkService.TryRegister(this, m_ServiceName))
            {
                // Update the old reference to the new one.
                NetworkService.Update(this, m_ServiceName);
            }
        }

        private IEnumerator Internal_OnClientStart()
        {
            yield return new WaitUntil(() => IsClientActive);
            OnClientStart();
        }

        /// <summary>
        /// Invoked when the client becomes active. This method functions similarly to Unity's Start(),
        /// but is specifically called when the client is up and running.
        /// </summary>
        protected virtual void OnClientStart() { }

        protected virtual void OnAwake() { }

        protected virtual void OnStart() { }

        protected virtual void OnStop() { }

        protected void InitializeBehaviour()
        {
            eventBehaviour.FindEvents<ClientAttribute>(this);
            Client.AddEventBehaviour(m_Id, this);
            Local = new NbClient(this);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnClientDisconnected += OnClientDisconnected;
            Client.OnMessage += OnMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            if (MatchmakingModuleEnabled)
            {
                Matchmaking.Client.OnJoinedGroup += OnJoinedGroup;
                Matchmaking.Client.OnLeftGroup += OnLeftGroup;
            }
        }

        protected void Unregister()
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            NetworkManager.OnClientConnected -= OnClientConnected;
            NetworkManager.OnClientDisconnected -= OnClientDisconnected;
            Client.OnMessage -= OnMessage;

            if (MatchmakingModuleEnabled)
            {
                Matchmaking.Client.OnJoinedGroup -= OnJoinedGroup;
                Matchmaking.Client.OnLeftGroup -= OnLeftGroup;
            }

            NetworkService.Unregister(m_ServiceName);
            OnStop();
        }

        protected virtual void OnBeforeSceneLoad(Scene scene)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
        }

        protected virtual void OnClientConnected() { }

        protected virtual void OnClientDisconnected(string reason) { }

        protected virtual void OnMessage(byte msgId, DataBuffer buffer, int seqChannel)
        {
            buffer.SeekToBegin();
            TryClientLocate(msgId, buffer, seqChannel); // Global Invoke
        }

        private void TryClientLocate(byte msgId, DataBuffer buffer, int seqChannel)
        {
            if (eventBehaviour.TryGetLocate(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        eventBehaviour.Invoke(msgId);
                        break;
                    case 1:
                        eventBehaviour.Invoke(msgId, buffer);
                        break;
                    case 2:
                        eventBehaviour.Invoke(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        eventBehaviour.Invoke(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        eventBehaviour.Invoke(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        eventBehaviour.Invoke(msgId, buffer, seqChannel, default, default, default);
                        break;
                }
            }
        }

        protected virtual void OnJoinedGroup(string groupName, DataBuffer buffer) { }

        protected virtual void OnLeftGroup(string groupName, string reason) { }

        public void Internal_OnMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            bool isServer,
            int seqChannel
        )
        {
            TryClientLocate(msgId, buffer, seqChannel);
        }

        protected virtual void Reset()
        {
            OnValidate();
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }

            if (string.IsNullOrEmpty(m_ServiceName))
            {
                m_ServiceName = GetType().Name;
            }
        }
    }

    [DefaultExecutionOrder(-3000)]
    public class ServerEventBehaviour : NetVarBehaviour, INetworkMessage
    {
        [Header("Service Settings")]
        [SerializeField]
        private string m_ServiceName;

        [SerializeField]
        private int m_Id;
        private NbServer remote;

        private bool m_UnregisterOnLoad = true;

        /// <summary>
        /// Gets the identifier of the associated <see cref="INetworkMessage"/>.
        /// </summary>
        /// <value>The identifier of the associated <see cref="INetworkMessage"/> as an integer.</value>
        public int IdentityId => m_Id;

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbServer"/> instance used to invoke messages on the client from the server.
        /// </summary>
        public NbServer Remote
        {
            get
            {
                if (remote == null)
                {
                    throw new System.NullReferenceException(
                        "The event behaviour has not been initialized. Call Awake() first or initialize manually."
                    );
                }

                return remote;
            }
            private set => remote = value;
        }

        private readonly EventBehaviour<DataBuffer, NetworkPeer, int, Null, Null> eventBehaviour =
            new();

        protected virtual void Awake()
        {
            if (NetworkService.Exists(m_ServiceName))
            {
                m_UnregisterOnLoad = false;
                return;
            }

            if (m_UnregisterOnLoad)
            {
                InitializeServiceLocator();
                InitializeBehaviour();
                RegisterSystemEvents();
                OnAwake();
            }
        }

        protected virtual void Start()
        {
            if (m_UnregisterOnLoad)
            {
                RegisterMatchmakingEvents();
                StartCoroutine(Internal_OnServerStart());
                OnStart();
            }

            m_UnregisterOnLoad = !NetworkHelper.IsDontDestroyOnLoad(gameObject);
        }

        protected void InitializeServiceLocator()
        {
            if (!NetworkService.TryRegister(this, m_ServiceName))
            {
                // Update the old reference to the new one.
                NetworkService.Update(this, m_ServiceName);
            }
        }

        private IEnumerator Internal_OnServerStart()
        {
            yield return new WaitUntil(() => IsServerActive);
            OnServerStart();
        }

        /// <summary>
        /// Invoked when the server becomes active. This method functions similarly to Unity's Start(),
        /// but is specifically called when the server is up and running.
        /// </summary>
        protected virtual void OnServerStart() { }

        protected virtual void OnAwake() { }

        protected virtual void OnStart() { }

        protected virtual void OnStop() { }

        protected void InitializeBehaviour()
        {
            eventBehaviour.FindEvents<ServerAttribute>(this);
            Server.AddEventBehaviour(m_Id, this);
            Remote = new NbServer(this);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            Server.OnMessage += OnMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            if (MatchmakingModuleEnabled)
            {
                Matchmaking.Server.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
                Matchmaking.Server.OnPlayerLeftGroup += OnPlayerLeftGroup;

                Matchmaking.Server.OnPlayerFailedJoinGroup += OnPlayerFailedJoinGroup;
                Matchmaking.Server.OnPlayerFailedLeaveGroup += OnPlayerFailedLeaveGroup;
            }
        }

        protected void Unregister()
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            NetworkManager.OnServerInitialized -= OnServerInitialized;
            NetworkManager.OnServerPeerConnected -= OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
            Server.OnMessage -= OnMessage;

            if (MatchmakingModuleEnabled)
            {
                Matchmaking.Server.OnPlayerJoinedGroup -= OnPlayerJoinedGroup;
                Matchmaking.Server.OnPlayerLeftGroup -= OnPlayerLeftGroup;

                Matchmaking.Server.OnPlayerFailedJoinGroup -= OnPlayerFailedJoinGroup;
                Matchmaking.Server.OnPlayerFailedLeaveGroup -= OnPlayerFailedLeaveGroup;
            }

            NetworkService.Unregister(m_ServiceName);
            OnStop();
        }

        protected virtual void OnBeforeSceneLoad(Scene scene)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
        }

        /// <summary>
        /// Called when the server has finished initialization.
        /// </summary>
        protected virtual void OnServerInitialized() { }

        /// <summary>
        /// Called when a new peer has successfully connected to the server.
        /// </summary>
        /// <param name="peer">The network peer that connected.</param>
        protected virtual void OnServerPeerConnected(NetworkPeer peer, Status status) { }

        /// <summary>
        /// Called when a peer has disconnected from the server.
        /// </summary>
        /// <param name="peer">The network peer that disconnected.</param>
        protected virtual void OnServerPeerDisconnected(NetworkPeer peer, Status status) { }

        /// <summary>
        /// Called when a player fails to leave a group on the server.
        /// </summary>
        /// <param name="peer">The network peer of the player.</param>
        /// <param name="reason">The reason for the failure to leave the group.</param>
        protected virtual void OnPlayerFailedLeaveGroup(NetworkPeer peer, string reason) { }

        /// <summary>
        /// Called when a player fails to join a group on the server.
        /// </summary>
        /// <param name="peer">The network peer of the player.</param>
        /// <param name="reason">The reason for the failure to join the group.</param>
        protected virtual void OnPlayerFailedJoinGroup(NetworkPeer peer, string reason) { }

        /// <summary>
        /// Called when a custom message is received on the server.
        /// </summary>
        /// <param name="msgId">The ID of the received message.</param>
        /// <param name="buffer">The buffer containing the message data.</param>
        /// <param name="peer">The network peer that sent the message.</param>
        /// <param name="seqChannel">The sequence channel through which the message was received.</param>
        protected virtual void OnMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            buffer.SeekToBegin();
            TryServerLocate(msgId, buffer, peer, seqChannel); // Global Invoke
        }

        private void TryServerLocate(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            if (eventBehaviour.TryGetLocate(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        eventBehaviour.Invoke(msgId);
                        break;
                    case 1:
                        eventBehaviour.Invoke(msgId, buffer);
                        break;
                    case 2:
                        eventBehaviour.Invoke(msgId, buffer, peer);
                        break;
                    case 3:
                        eventBehaviour.Invoke(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        eventBehaviour.Invoke(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        eventBehaviour.Invoke(msgId, buffer, peer, seqChannel, default, default);
                        break;
                }
            }
        }

        /// <summary>
        /// Called when a player has joined a group.
        /// </summary>
        /// <param name="buffer">The buffer containing additional data related to the player joining the group.</param>
        /// <param name="group">The network group that the player joined.</param>
        /// <param name="peer">The network peer representing the player who joined the group.</param>
        protected virtual void OnPlayerJoinedGroup(
            DataBuffer buffer,
            NetworkGroup group,
            NetworkPeer peer
        ) { }

        /// <summary>
        /// Called when a player has left a group.
        /// </summary>
        /// <param name="group">The network group that the player left.</param>
        /// <param name="peer">The network peer representing the player who left the group.</param>
        /// <param name="reason">The reason for the player leaving the group.</param>
        protected virtual void OnPlayerLeftGroup(
            NetworkGroup group,
            NetworkPeer peer,
            Status status,
            string reason
        ) { }

        public void Internal_OnMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            bool isServer,
            int seqChannel
        )
        {
            TryServerLocate(msgId, buffer, peer, seqChannel);
        }

        protected virtual void Reset()
        {
            OnValidate();
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }

            if (string.IsNullOrEmpty(m_ServiceName))
            {
                m_ServiceName = GetType().Name;
            }
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.
