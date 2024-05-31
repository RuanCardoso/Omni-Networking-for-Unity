using Omni.Core.Interfaces;
using Omni.Core.Modules.Matchmaking;
using Omni.Shared;
using UnityEngine;

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
        ) => NetworkManager.Client.GlobalInvoke(msgId, buffer, deliveryMode, sequenceChannel);

        public void Invoke(
            byte msgId,
            NetworkBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        ) =>
            NetworkManager.Client.Invoke(
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
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            byte sequenceChannel = 0
        ) =>
            NetworkManager.Server.GlobalInvoke(
                msgId,
                peerId,
                buffer,
                target,
                deliveryMode,
                groupId,
                sequenceChannel
            );

        public void Invoke(
            byte msgId,
            int peerId,
            NetworkBuffer buffer = null,
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            byte sequenceChannel = 0
        )
        {
            NetworkManager.Server.Invoke(
                msgId,
                peerId,
                m_NetworkMessage.IdentityId,
                buffer,
                target,
                deliveryMode,
                groupId,
                sequenceChannel
            );
        }
    }

    [DefaultExecutionOrder(-3000)]
    public class NetworkEventBehaviour : MonoBehaviour, INetworkMessage
    {
        [SerializeField]
        private int m_Id;

        public int IdentityId => m_Id;
        public NbClient Local { get; private set; }
        public NbServer Remote { get; private set; }

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
        }

        protected void InitializeBehaviour()
        {
            clientEventBehaviour.FindEvents<ClientAttribute>(this);
            serverEventBehaviour.FindEvents<ServerAttribute>(this);

            NetworkManager.Client.AddEventBehaviour(m_Id, this);
            NetworkManager.Server.AddEventBehaviour(m_Id, this);

            Local = new NbClient(this);
            Remote = new NbServer(this);
        }

        protected void RegisterEvents()
        {
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnClientDisconnected += OnClientDisconnected;
            NetworkManager.Client.OnMessage += OnClientMessage;

            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            NetworkManager.Server.OnMessage += OnServerMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            NetworkManager.Matchmaking.Client.OnJoinedGroup += OnJoinedGroup;
            NetworkManager.Matchmaking.Client.OnLeftGroup += OnLeftGroup;

            NetworkManager.Matchmaking.Server.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
            NetworkManager.Matchmaking.Server.OnPlayerLeftGroup += OnPlayerLeftGroup;
        }

        #region Client
        protected virtual void OnClientConnected() { }

        protected virtual void OnClientDisconnected(string reason) { }

        protected virtual void OnClientMessage(byte msgId, NetworkBuffer buffer, int seqChannel)
        {
            TryClientLocate(msgId, buffer, seqChannel); // Global Invoke
            buffer.PrepareForReading();
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

        protected virtual void OnServerMessage(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            TryServerLocate(msgId, buffer, peer, seqChannel); // Global Invoke
            buffer.PrepareForReading();
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
                m_Id = NetworkHelper.GenerateUniqueId();
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateUniqueId();
            }
        }
    }

    [DefaultExecutionOrder(-3000)]
    public class ClientEventBehaviour : MonoBehaviour, INetworkMessage
    {
        [SerializeField]
        private int m_Id;

        public int IdentityId => m_Id;
        public NbClient Local { get; private set; }

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
        }

        protected void InitializeBehaviour()
        {
            eventBehaviour.FindEvents<ClientAttribute>(this);
            NetworkManager.Client.AddEventBehaviour(m_Id, this);
            Local = new NbClient(this);
        }

        protected void RegisterEvents()
        {
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnClientDisconnected += OnClientDisconnected;
            NetworkManager.Client.OnMessage += OnMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            NetworkManager.Matchmaking.Client.OnJoinedGroup += OnJoinedGroup;
            NetworkManager.Matchmaking.Client.OnLeftGroup += OnLeftGroup;
        }

        protected virtual void OnClientConnected() { }

        protected virtual void OnClientDisconnected(string reason) { }

        protected virtual void OnMessage(byte msgId, NetworkBuffer buffer, int seqChannel)
        {
            TryClientLocate(msgId, buffer, seqChannel); // Global Invoke
            buffer.PrepareForReading();
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
                m_Id = NetworkHelper.GenerateUniqueId();
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateUniqueId();
            }
        }
    }

    [DefaultExecutionOrder(-3000)]
    public class ServerEventBehaviour : MonoBehaviour, INetworkMessage
    {
        [SerializeField]
        private int m_Id;

        public int IdentityId => m_Id;
        public NbServer Remote { get; private set; }

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
        }

        protected void InitializeBehaviour()
        {
            eventBehaviour.FindEvents<ServerAttribute>(this);
            NetworkManager.Server.AddEventBehaviour(m_Id, this);
            Remote = new NbServer(this);
        }

        protected void RegisterEvents()
        {
            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            NetworkManager.Server.OnMessage += OnMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            NetworkManager.Matchmaking.Server.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
            NetworkManager.Matchmaking.Server.OnPlayerLeftGroup += OnPlayerLeftGroup;
        }

        protected virtual void OnServerInitialized() { }

        protected virtual void OnServerPeerConnected(NetworkPeer peer) { }

        protected virtual void OnServerPeerDisconnected(NetworkPeer peer) { }

        protected virtual void OnMessage(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            TryServerLocate(msgId, buffer, peer, seqChannel); // Global Invoke
            buffer.PrepareForReading();
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
                m_Id = NetworkHelper.GenerateUniqueId();
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateUniqueId();
            }
        }
    }
}
