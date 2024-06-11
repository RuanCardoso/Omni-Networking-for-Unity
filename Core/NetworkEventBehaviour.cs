using System.Collections;
using System.Runtime.CompilerServices;
using Omni.Core.Interfaces;
using Omni.Core.Modules.Matchmaking;
using UnityEngine;
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

        internal NbClient(INetworkMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
        }

        public void GlobalInvoke(
            byte msgId,
            NetworkBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        ) => Client.GlobalInvoke(msgId, buffer, deliveryMode, sequenceChannel);

        public void Invoke(
            byte msgId,
            NetworkBuffer buffer = null,
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

        internal NbServer(INetworkMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
        }

        public void GlobalInvoke(
            byte msgId,
            int peerId,
            NetworkBuffer buffer = null,
            Target target = Target.Self,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        ) =>
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

        public void Invoke(
            byte msgId,
            int peerId,
            NetworkBuffer buffer = null,
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
        [SerializeField]
        private int m_Id;

        private NbClient local;
        private NbServer remote;

        public int IdentityId => m_Id;
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

        private readonly EventBehaviour<NetworkBuffer, int, Null, Null, Null> clientEventBehaviour =
            new();

        private readonly EventBehaviour<
            NetworkBuffer,
            NetworkPeer,
            int,
            Null,
            Null
        > serverEventBehaviour = new();

        protected virtual void Awake()
        {
            InitializeBehaviour();
            RegisterEvents();
        }

        protected virtual void Start()
        {
            RegisterMatchmakingEvents();
            StartCoroutine(Internal_OnServerStart());
            StartCoroutine(Internal_OnClientStart());
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

        protected virtual void OnServerStart() { }

        protected virtual void OnClientStart() { }

        protected void InitializeBehaviour()
        {
            clientEventBehaviour.FindEvents<ClientAttribute>(this);
            serverEventBehaviour.FindEvents<ServerAttribute>(this);

            Client.AddEventBehaviour(m_Id, this);
            Server.AddEventBehaviour(m_Id, this);

            Local = new NbClient(this);
            Remote = new NbServer(this);
        }

        protected void RegisterEvents()
        {
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

        protected void ManualSync<T>(
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
            using NetworkBuffer message = CreateHeader(property, propertyId);
            Remote.Invoke(
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

        protected void AutoSync<T>(
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
            IPropertyMemberInfo propertyInfo = GetPropertyInfoWithCallerName<T>(callerName);
            IPropertyMemberInfo<T> propertyInfoGeneric = propertyInfo as IPropertyMemberInfo<T>;

            if (propertyInfo != null)
            {
                peer ??= Server.ServerPeer;
                using NetworkBuffer message = CreateHeader(
                    propertyInfoGeneric.GetFunc(),
                    propertyInfo.Id
                );

                Remote.Invoke(
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

        protected void ManualSyncFromClient<T>(
            T property,
            byte propertyId,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        )
        {
            using NetworkBuffer message = CreateHeader(property, propertyId);
            Local.Invoke(255, message, deliveryMode, sequenceChannel);
        }

        protected void AutoSyncFromClient<T>(
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0,
            [CallerMemberName] string callerName = ""
        )
        {
            IPropertyMemberInfo propertyInfo = GetPropertyInfoWithCallerName<T>(callerName);
            IPropertyMemberInfo<T> propertyInfoGeneric = propertyInfo as IPropertyMemberInfo<T>;

            if (propertyInfo != null)
            {
                using NetworkBuffer message = CreateHeader(
                    propertyInfoGeneric.GetFunc(),
                    propertyInfo.Id
                );

                Local.Invoke(255, message, deliveryMode, sequenceChannel);
            }
        }

        #region Client
        protected virtual void OnClientConnected() { }

        protected virtual void OnClientDisconnected(string reason) { }

        protected virtual void OnClientMessage(byte msgId, NetworkBuffer buffer, int seqChannel)
        {
            TryClientLocate(msgId, buffer, seqChannel); // Global Invoke
            buffer.ResetReadPosition();
        }

        private void TryClientLocate(byte msgId, NetworkBuffer buffer, int seqChannel)
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

        protected virtual void OnJoinedGroup(string groupName, NetworkBuffer buffer) { }

        protected virtual void OnLeftGroup(string groupName, string reason) { }
        #endregion

        #region Server
        protected virtual void OnServerInitialized() { }

        protected virtual void OnServerPeerConnected(NetworkPeer peer) { }

        protected virtual void OnServerPeerDisconnected(NetworkPeer peer) { }

        protected virtual void OnPlayerFailedLeaveGroup(NetworkPeer peer, string reason) { }

        protected virtual void OnPlayerFailedJoinGroup(NetworkPeer peer, string reason) { }

        protected virtual void OnServerMessage(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            TryServerLocate(msgId, buffer, peer, seqChannel); // Global Invoke
            buffer.ResetReadPosition();
        }

        private void TryServerLocate(
            byte msgId,
            NetworkBuffer buffer,
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
            NetworkBuffer buffer,
            NetworkGroup group,
            NetworkPeer peer
        ) { }

        protected virtual void OnPlayerLeftGroup(
            NetworkGroup group,
            NetworkPeer peer,
            string reason
        ) { }
        #endregion

        public void Internal_OnMessage(
            byte msgId,
            NetworkBuffer buffer,
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
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }
        }
    }

    [DefaultExecutionOrder(-3000)]
    public class ClientEventBehaviour : NetVarBehaviour, INetworkMessage
    {
        [SerializeField]
        private int m_Id;
        private NbClient local;

        public int IdentityId => m_Id;
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

        private readonly EventBehaviour<NetworkBuffer, int, Null, Null, Null> eventBehaviour =
            new();

        protected virtual void Awake()
        {
            InitializeBehaviour();
            RegisterEvents();
        }

        protected virtual void Start()
        {
            RegisterMatchmakingEvents();
            StartCoroutine(Internal_OnClientStart());
        }

        private IEnumerator Internal_OnClientStart()
        {
            yield return new WaitUntil(() => IsClientActive);
            OnClientStart();
        }

        protected virtual void OnClientStart() { }

        protected void InitializeBehaviour()
        {
            eventBehaviour.FindEvents<ClientAttribute>(this);
            Client.AddEventBehaviour(m_Id, this);
            Local = new NbClient(this);
        }

        protected void RegisterEvents()
        {
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

        protected void ManualSync<T>(
            T property,
            byte propertyId,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        )
        {
            using NetworkBuffer message = CreateHeader(property, propertyId);
            Local.Invoke(255, message, deliveryMode, sequenceChannel);
        }

        protected void AutoSync<T>(
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0,
            [CallerMemberName] string callerName = ""
        )
        {
            IPropertyMemberInfo propertyInfo = GetPropertyInfoWithCallerName<T>(callerName);
            IPropertyMemberInfo<T> propertyInfoGeneric = propertyInfo as IPropertyMemberInfo<T>;

            if (propertyInfo != null)
            {
                using NetworkBuffer message = CreateHeader(
                    propertyInfoGeneric.GetFunc(),
                    propertyInfo.Id
                );

                Local.Invoke(255, message, deliveryMode, sequenceChannel);
            }
        }

        protected virtual void OnClientConnected() { }

        protected virtual void OnClientDisconnected(string reason) { }

        protected virtual void OnMessage(byte msgId, NetworkBuffer buffer, int seqChannel)
        {
            TryClientLocate(msgId, buffer, seqChannel); // Global Invoke
            buffer.ResetReadPosition();
        }

        private void TryClientLocate(byte msgId, NetworkBuffer buffer, int seqChannel)
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

        protected virtual void OnJoinedGroup(string groupName, NetworkBuffer buffer) { }

        protected virtual void OnLeftGroup(string groupName, string reason) { }

        public void Internal_OnMessage(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            bool isServer,
            int seqChannel
        )
        {
            TryClientLocate(msgId, buffer, seqChannel);
        }

        protected virtual void Reset()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }
        }
    }

    [DefaultExecutionOrder(-3000)]
    public class ServerEventBehaviour : NetVarBehaviour, INetworkMessage
    {
        [SerializeField]
        private int m_Id;
        private NbServer remote;

        public int IdentityId => m_Id;
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

        private readonly EventBehaviour<
            NetworkBuffer,
            NetworkPeer,
            int,
            Null,
            Null
        > eventBehaviour = new();

        protected virtual void Awake()
        {
            InitializeBehaviour();
            RegisterEvents();
        }

        protected virtual void Start()
        {
            RegisterMatchmakingEvents();
            StartCoroutine(Internal_OnServerStart());
        }

        private IEnumerator Internal_OnServerStart()
        {
            yield return new WaitUntil(() => IsServerActive);
            OnServerStart();
        }

        protected virtual void OnServerStart() { }

        protected void InitializeBehaviour()
        {
            eventBehaviour.FindEvents<ServerAttribute>(this);
            Server.AddEventBehaviour(m_Id, this);
            Remote = new NbServer(this);
        }

        protected void RegisterEvents()
        {
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

        protected void ManualSync<T>(
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
            using NetworkBuffer message = CreateHeader(property, propertyId);
            Remote.Invoke(
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

        protected void AutoSync<T>(
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
            IPropertyMemberInfo propertyInfo = GetPropertyInfoWithCallerName<T>(callerName);
            IPropertyMemberInfo<T> propertyInfoGeneric = propertyInfo as IPropertyMemberInfo<T>;

            if (propertyInfo != null)
            {
                peer ??= Server.ServerPeer;
                using NetworkBuffer message = CreateHeader(
                    propertyInfoGeneric.GetFunc(),
                    propertyInfo.Id
                );

                Remote.Invoke(
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

        protected virtual void OnServerInitialized() { }

        protected virtual void OnServerPeerConnected(NetworkPeer peer) { }

        protected virtual void OnServerPeerDisconnected(NetworkPeer peer) { }

        protected virtual void OnPlayerFailedLeaveGroup(NetworkPeer peer, string reason) { }

        protected virtual void OnPlayerFailedJoinGroup(NetworkPeer peer, string reason) { }

        protected virtual void OnMessage(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            TryServerLocate(msgId, buffer, peer, seqChannel); // Global Invoke
            buffer.ResetReadPosition();
        }

        private void TryServerLocate(
            byte msgId,
            NetworkBuffer buffer,
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

        protected virtual void OnPlayerJoinedGroup(
            NetworkBuffer buffer,
            NetworkGroup group,
            NetworkPeer peer
        ) { }

        protected virtual void OnPlayerLeftGroup(
            NetworkGroup group,
            NetworkPeer peer,
            string reason
        ) { }

        public void Internal_OnMessage(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            bool isServer,
            int seqChannel
        )
        {
            TryServerLocate(msgId, buffer, peer, seqChannel);
        }

        protected virtual void Reset()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
            }
        }
    }
}
