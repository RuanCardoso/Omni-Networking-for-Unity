using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Omni.Core.NetworkManager;

#pragma warning disable

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    [DefaultExecutionOrder(-3000)]
    public class ServerBehaviour : NetworkEventBase, IRpcMessage, IServiceBehaviour
    {
        /// <summary>
        /// Gets the identifier of the associated <see cref="IRpcMessage"/>.
        /// </summary>
        /// <value>The identifier of the associated <see cref="IRpcMessage"/> as an integer.</value>
        public int IdentityId => m_Id;

        private NetworkEventServer remote;
        private readonly RpcHandler<DataBuffer, NetworkPeer, int, Null, Null> rpcHandler = new();

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NetworkEventServer"/> instance used to invoke messages on the client from the server.
        /// </summary>
        public NetworkEventServer Server
        {
            get
            {
                if (remote != null)
                    return remote;

                NetworkLogger.PrintHyperlink();
                throw new NullReferenceException(
                    "This property(Remote) is intended for server-side use only. It appears to be accessed from the client side. Or Call Awake() and Start() base first or initialize manually."
                );
            }
            internal set => remote = value;
        }

        /// <summary>
        /// The `Awake` method is virtual, allowing it to be overridden in derived classes
        /// for additional startup logic. If overridden, it is essential to call the base class's
        /// `Awake` method to ensure proper initialization. Not doing so may result in incomplete
        /// initialization and unpredictable behavior.
        /// </summary>
        public virtual void Awake()
        {
            InitAwake();
        }

        private void InitAwake()
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

        /// <summary>
        /// The `Start` method is virtual, allowing it to be overridden in derived classes
        /// for additional startup logic. If overridden, it is essential to call the base class's
        /// `Start` method to ensure proper initialization. Not doing so may result in incomplete
        /// initialization and unpredictable behavior.
        /// </summary>
        public virtual void Start()
        {
            ___InjectServices___();
            InitStart();
        }

        private void InitStart()
        {
            if (m_UnregisterOnLoad)
            {
                RegisterMatchmakingEvents();
                StartCoroutine(Internal_OnServerStart());

                OnStart();
                Service.UpdateReference(m_ServiceName);
            }

            m_UnregisterOnLoad = !NetworkHelper.IsDontDestroyOnLoad(gameObject);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Internal_Awake()
        {
            InitAwake();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Internal_Start()
        {
            InitStart();
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
        protected virtual void OnServerStart()
        {
        }

        protected void InitializeBehaviour()
        {
            rpcHandler.FindEvents<ServerAttribute>(this, m_BindingFlags);
            NetworkManager.ServerSide.AddEventBehaviour(m_Id, this);
            Server = new NetworkEventServer(this, m_BindingFlags);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            NetworkManager.ServerSide.OnMessage += OnMessage;
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
            NetworkManager.ServerSide.OnMessage -= OnMessage;

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

        protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
        }

        /// <summary>
        /// Called when the server has finished initialization.
        /// </summary>
        protected virtual void OnServerInitialized()
        {
        }

        /// <summary>
        /// Called when a new peer has successfully connected to the server.
        /// </summary>
        /// <param name="peer">The network peer that connected.</param>
        protected virtual void OnServerPeerConnected(NetworkPeer peer, Phase phase)
        {
        }

        /// <summary>
        /// Called when a peer has disconnected from the server.
        /// </summary>
        /// <param name="peer">The network peer that disconnected.</param>
        protected virtual void OnServerPeerDisconnected(NetworkPeer peer, Phase phase)
        {
        }

        /// <summary>
        /// Called when a player fails to leave a group on the server.
        /// </summary>
        /// <param name="peer">The network peer of the player.</param>
        /// <param name="reason">The reason for the failure to leave the group.</param>
        protected virtual void OnPlayerFailedLeaveGroup(NetworkPeer peer, string reason)
        {
        }

        /// <summary>
        /// Called when a player fails to join a group on the server.
        /// </summary>
        /// <param name="peer">The network peer of the player.</param>
        /// <param name="reason">The reason for the failure to join the group.</param>
        protected virtual void OnPlayerFailedJoinGroup(NetworkPeer peer, string reason)
        {
        }

        /// <summary>
        /// Called when a custom message is received on the server.
        /// </summary>
        /// <param name="msgId">The ID of the received message.</param>
        /// <param name="buffer">The buffer containing the message data.</param>
        /// <param name="peer">The network peer that sent the message.</param>
        /// <param name="seqChannel">The sequence channel through which the message was received.</param>
        protected virtual void OnMessage(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            buffer.SeekToBegin();
            TryCallServerRpc(msgId, buffer, peer, seqChannel); // Global Invoke
        }

        private void TryCallServerRpc(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            if (rpcHandler.Exists(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        rpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        rpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        rpcHandler.Rpc(msgId, buffer, peer);
                        break;
                    case 3:
                        rpcHandler.Rpc(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        rpcHandler.Rpc(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        rpcHandler.Rpc(msgId, buffer, peer, seqChannel, default, default);
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
        protected virtual void OnPlayerJoinedGroup(DataBuffer buffer, NetworkGroup group, NetworkPeer peer)
        {
        }

        /// <summary>
        /// Called when a player has left a group.
        /// </summary>
        /// <param name="group">The network group that the player left.</param>
        /// <param name="peer">The network peer representing the player who left the group.</param>
        /// <param name="reason">The reason for the player leaving the group.</param>
        protected virtual void OnPlayerLeftGroup(NetworkGroup group, NetworkPeer peer, Phase phase, string reason)
        {
        }

        public void OnRpcInvoked(byte methodId, DataBuffer buffer, NetworkPeer peer, bool isServer, int seqChannel)
        {
            rpcHandler.ThrowNoMethodFound(methodId);
            TryCallServerRpc(methodId, buffer, peer, seqChannel);
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.