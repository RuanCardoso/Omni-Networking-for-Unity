using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Collections;
using System.ComponentModel;
using Omni.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Omni.Core.NetworkManager;
using static Omni.Core.Modules.Matchmaking.NetworkMatchmaking;

#pragma warning disable

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    /// <summary>
    /// A derivative of <see cref="NetworkEventBase"/> that serves as a dual-behavior system capable of handling
    /// both client and server operations in a networked environment. Designed to minimize boilerplate code.
    /// </summary>
    /// <remarks>
    /// This class is optimized for high performance and includes unconventional design patterns necessary
    /// for minimizing execution overhead.
    /// </remarks>
    [DefaultExecutionOrder(-3000)]
    public class DualBehaviour : NetworkEventBase, IRpcMessage, IServiceBehaviour
    {
        /// <summary>
        /// Gets the identifier of the associated <see cref="IRpcMessage"/>.
        /// </summary>
        /// <value>The identifier of the associated <see cref="IRpcMessage"/> as an integer.</value>
        public int IdentityId => m_Id;

        private readonly RpcHandler<DataBuffer, int, Null, Null, Null> clientRpcHandler = new();
        private readonly RpcHandler<DataBuffer, NetworkPeer, int, Null, Null> serverRpcHandler = new();

        private NetworkEventClient local;
        private NetworkEventServer remote;

        /// <summary>
        /// Gets the server-side routing manager that handles network message routing and delivery.
        /// Provides access to server-specific routing functionality for sending messages to connected clients
        /// using the underlying transport layer.
        /// </summary>
        /// <remarks>
        /// This property offers a convenient shorthand to access the server router without directly
        /// referencing the NetworkManager's transport system. Use this for managing server-to-client
        /// message routing, custom packet handling, and optimizing network traffic.
        /// </remarks>
        protected TransporterRouteManager.ServerRouteManager ServerRouter => NetworkManager._transporterRouteManager.Server;

        /// <summary>
        /// Gets the client-side routing manager that handles network message routing and delivery.
        /// Provides access to client-specific routing functionality for sending messages to the server
        /// using the underlying transport layer.
        /// </summary>
        /// <remarks>
        /// This property offers a convenient shorthand to access the client router without directly
        /// referencing the NetworkManager's transport system. Use this for managing client-to-server
        /// message routing, custom packet handling, and optimizing network traffic.
        /// </remarks>
        protected TransporterRouteManager.ClientRouteManager ClientRouter => NetworkManager._transporterRouteManager.Client;

        /// <summary>
        /// Gets the server-side matchmaking manager that handles player grouping, matchmaking, and lobby functionality.
        /// This property provides access to methods for creating, managing, and monitoring player groups and matches.
        /// </summary>
        /// <remarks>
        /// This property offers a convenient shorthand to access the server matchmaking system without directly
        /// referencing the NetworkManager's matchmaking module. Use this for implementing features such as
        /// game lobbies, team assignment, custom matchmaking rules, and player grouping logic.
        /// </remarks>
        /// <value>
        /// The <see cref="MatchServer"/> instance for handling server-side matchmaking operations.
        /// </value>
        protected MatchServer ServerMatchmaking => NetworkManager.Matchmaking.Server;

        /// <summary>
        /// Gets the client-side matchmaking manager that handles player grouping, matchmaking, and lobby functionality.
        /// This property provides access to methods for joining, leaving, and interacting with player groups and matches.
        /// </summary>
        /// <remarks>
        /// This property offers a convenient shorthand to access the client matchmaking system without directly
        /// referencing the NetworkManager's matchmaking module. Use this for implementing features such as
        /// joining game lobbies, requesting team assignments, and participating in matchmaking services.
        /// </remarks>
        /// <value>
        /// The <see cref="MatchClient"/> instance for handling client-side matchmaking operations.
        /// </value>
        protected MatchClient ClientMatchmaking => NetworkManager.Matchmaking.Client;

        /// <summary>
        /// Provides access to the <see cref="NetworkEventClient"/> instance, 
        /// allowing the client to send Remote Procedure Calls (RPCs) to the server.
        /// </summary>
        /// <remarks>
        /// This enables RPCs to be invoked from other objects, facilitating communication 
        /// from the client to the server in a networked environment.
        /// </remarks>
        public NetworkEventClient Client
        {
            get
            {
                if (local != null)
                    return local;

                NetworkLogger.PrintHyperlink();
                throw new NullReferenceException(
                    "The 'Client' property is null. Ensure this property is accessed only on the client-side. Verify that 'Awake()' and 'Start()' have been called or initialize the property manually before use."
                );
            }
            internal set => local = value;
        }

        /// <summary>
        /// Provides access to the <see cref="NetworkEventServer"/> instance, 
        /// enabling the server to send Remote Procedure Calls (RPCs) to clients.
        /// </summary>
        /// <remarks>
        /// This allows RPCs to be invoked from other objects, facilitating communication between
        /// the server and clients in a networked environment.
        /// </remarks>
        public NetworkEventServer Server
        {
            get
            {
                if (remote != null)
                    return remote;

                NetworkLogger.PrintHyperlink();
                throw new NullReferenceException(
                    "The 'Server' property is null. Ensure this property is accessed only on the server-side. Verify that 'Awake()' and 'Start()' have been called or initialize the property manually before use."
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
            InitStart();
        }

        private void InitStart()
        {
            ___InjectServices___();
            if (m_UnregisterOnLoad)
            {
                RegisterMatchmakingEvents();
                Internal_OnServerStart();
                Internal_OnClientStart();

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

        private async void Internal_OnServerStart()
        {
            await UniTask.WaitUntil(() => IsServerActive);
            OnServerStart();
        }

        private async void Internal_OnClientStart()
        {
            await UniTask.WaitUntil(() => IsClientActive);
            OnClientStart();
        }

        /// <summary>
        /// Invoked when the server becomes active. This method functions similarly to Unity's Start(),
        /// but is specifically called when the server is up and running.
        /// </summary>
        protected virtual void OnServerStart()
        {
        }

        /// <summary>
        /// Invoked when the client becomes active. This method functions similarly to Unity's Start(),
        /// but is specifically called when the client is up and running.
        /// </summary>
        protected virtual void OnClientStart()
        {
        }

        protected void InitializeBehaviour()
        {
            clientRpcHandler.FindAllRpcMethods<ClientAttribute>(this, m_BindingFlags);
            serverRpcHandler.FindAllRpcMethods<ServerAttribute>(this, m_BindingFlags);

            ClientSide.AddRpcMessage(m_Id, this);
            ServerSide.AddRpcMessage(m_Id, this);

            Client = new NetworkEventClient(this, m_BindingFlags);
            Server = new NetworkEventServer(this, m_BindingFlags);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnClientDisconnected += OnClientDisconnected;
            NetworkManager.OnClientIdentitySpawned += OnClientIdentitySpawned;
            NetworkManager.OnPeerSharedDataChanged += OnPeerSharedDataChanged;
            ClientSide.OnMessage += OnClientMessage;

            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            ServerSide.OnMessage += OnServerMessage;
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
            NetworkManager.OnClientIdentitySpawned -= OnClientIdentitySpawned;
            NetworkManager.OnPeerSharedDataChanged -= OnPeerSharedDataChanged;
            ClientSide.OnMessage -= OnClientMessage;

            NetworkManager.OnServerInitialized -= OnServerInitialized;
            NetworkManager.OnServerPeerConnected -= OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
            ServerSide.OnMessage -= OnServerMessage;

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

        protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
        }

        #region Client

        protected virtual void OnPeerSharedDataChanged(NetworkPeer peer, string key)
        {
        }

        protected virtual void OnClientIdentitySpawned(NetworkIdentity identity)
        {
        }

        protected virtual void OnClientConnected()
        {
        }

        protected virtual void OnClientDisconnected(string reason)
        {
        }

        protected virtual void OnClientMessage(byte msgId, DataBuffer buffer, int seqChannel)
        {
            buffer.SeekToBegin();
            TryCallClientRpc(msgId, buffer, seqChannel); // Global Invoke
        }

        private void TryCallClientRpc(byte msgId, DataBuffer buffer, int seqChannel)
        {
            if (clientRpcHandler.Exists(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        clientRpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        clientRpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel, default, default, default);
                        break;
                }
            }
        }

        protected virtual void OnJoinedGroup(string groupName, DataBuffer buffer)
        {
        }

        protected virtual void OnLeftGroup(string groupName, string reason)
        {
        }

        #endregion

        #region Server

        protected virtual void OnServerInitialized()
        {
        }

        protected virtual void OnServerPeerConnected(NetworkPeer peer, Phase phase)
        {
        }

        protected virtual void OnServerPeerDisconnected(NetworkPeer peer, Phase phase)
        {
        }

        protected virtual void OnPlayerFailedLeaveGroup(NetworkPeer peer, string reason)
        {
        }

        protected virtual void OnPlayerFailedJoinGroup(NetworkPeer peer, string reason)
        {
        }

        protected virtual void OnServerMessage(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            buffer.SeekToBegin();
            TryCallServerRpc(msgId, buffer, peer, seqChannel); // Global Invoke
        }

        private void TryCallServerRpc(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            if (serverRpcHandler.Exists(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        serverRpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        serverRpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        serverRpcHandler.Rpc(msgId, buffer, peer);
                        break;
                    case 3:
                        serverRpcHandler.Rpc(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        serverRpcHandler.Rpc(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        serverRpcHandler.Rpc(msgId, buffer, peer, seqChannel, default, default);
                        break;
                }
            }
        }

        protected virtual void OnPlayerJoinedGroup(DataBuffer buffer, NetworkGroup group, NetworkPeer peer)
        {
        }

        protected virtual void OnPlayerLeftGroup(NetworkGroup group, NetworkPeer peer, Phase phase, string reason)
        {
        }

        #endregion

        public void OnRpcInvoked(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool isServer, int seqChannel)
        {
            try
            {
                if (isServer)
                {
                    serverRpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                    TryCallServerRpc(rpcId, buffer, peer, seqChannel);
                }
                else
                {
                    clientRpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                    TryCallClientRpc(rpcId, buffer, seqChannel);
                }
            }
            catch (Exception ex)
            {
                string methodName = NetworkConstants.INVALID_RPC_NAME;
                if (isServer) methodName = serverRpcHandler.GetRpcName(rpcId);
                else methodName = clientRpcHandler.GetRpcName(rpcId);

                NetworkLogger.__Log__(
                    $"[RPC Error] An exception occurred while processing the RPC -> " +
                    $"Rpc Id: '{rpcId}', Rpc Name: '{methodName}' in Class: '{GetType().Name}' -> " +
                    $"Exception Details: {ex.Message}. ",
                    NetworkLogger.LogType.Error
                );

                NetworkLogger.PrintHyperlink(ex);
            }
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.