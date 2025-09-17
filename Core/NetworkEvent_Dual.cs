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
using Omni.Core.Modules.Matchmaking;

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

        private readonly __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> m_ClientRpcHandler = new();
        private readonly __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> m_ServerRpcHandler = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> __ServerRpcHandler => m_ServerRpcHandler;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> __ClientRpcHandler => m_ClientRpcHandler;

        private NetworkEventClient local;
        private NetworkEventServer remote;

        /// <summary>
        /// Indicates whether this instance is running in host mode (both server and client active in the same process).
        /// Returns <c>true</c> if the application is acting simultaneously as server and client,
        /// which is typical for local multiplayer testing or single-player with network features.
        /// </summary>
        protected bool IsHost => IsServerActive && IsClientActive;

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
        /// The <see cref="NetworkMatchmaking"/> instance for handling server-side matchmaking operations.
        /// </value>
        protected NetworkMatchmaking Matchmaking => NetworkManager.Matchmaking;

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
                    "The 'Client' property is null. Access it only on the client side after initialization (Awake/Start)."
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
                    "The 'Server' property is null. Access it only on the server side after initialization (Awake/Start)."
                );
            }
            internal set => remote = value;
        }

        /// <summary>
        /// Gets the client's network peer instance that represents the client itself in the network.
        /// </summary>
        /// <value>
        /// The <see cref="NetworkPeer"/> instance that represents the local client in the network.
        /// </value>
        /// <remarks>
        /// This property provides convenient access to the client's peer object, which contains
        /// information about the client's network identity and connection. Use this property
        /// when you need to reference the client as a network entity in client-side operations.
        /// </remarks>
        protected NetworkPeer LocalPeer
        {
            get
            {
                return NetworkManager.LocalPeer;
            }
        }

        /// <summary>
        /// Gets the server's network peer instance that represents the server itself in the network.
        /// </summary>
        /// <value>
        /// The <see cref="NetworkPeer"/> instance that represents the server in the network.
        /// </value>
        /// <remarks>
        /// This property provides convenient access to the server's peer object, which contains
        /// information about the server's network identity and connection. Use this property
        /// when you need to reference the server as a network entity in server-side operations.
        /// </remarks>
        protected NetworkPeer ServerPeer
        {
            get
            {
                return NetworkManager.ServerSide.ServerPeer;
            }
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
            ___RegisterNetworkVariables___();
            // Registers notifications for changes in the collection, enabling automatic updates when the collection is modified.
            ___NotifyCollectionChange___();
            m_ClientRpcHandler.RegisterRpcMethodHandlers<ClientAttribute>(this);
            m_ServerRpcHandler.RegisterRpcMethodHandlers<ServerAttribute>(this);

            ClientSide.AddRpcMessage(m_Id, this);
            ServerSide.AddRpcMessage(m_Id, this);

            Client = new NetworkEventClient(this);
            Server = new NetworkEventServer(this);
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
            NetworkManager.OnServerPeerConnected += Internal_OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            ServerSide.OnMessage += OnServerMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            if (MatchmakingModuleEnabled)
            {
                Matchmaking.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
                Matchmaking.OnPlayerLeftGroup += OnPlayerLeftGroup;

                Matchmaking.OnPlayerFailedJoinGroup += OnPlayerFailedJoinGroup;
                Matchmaking.OnPlayerFailedLeaveGroup += OnPlayerFailedLeaveGroup;
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
            NetworkManager.OnServerPeerConnected -= Internal_OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
            ServerSide.OnMessage -= OnServerMessage;

            if (MatchmakingModuleEnabled)
            {
                Matchmaking.OnPlayerJoinedGroup -= OnPlayerJoinedGroup;
                Matchmaking.OnPlayerLeftGroup -= OnPlayerLeftGroup;

                Matchmaking.OnPlayerFailedJoinGroup -= OnPlayerFailedJoinGroup;
                Matchmaking.OnPlayerFailedLeaveGroup -= OnPlayerFailedLeaveGroup;
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
        }

        private void TryCallClientRpc(byte msgId, DataBuffer buffer, int seqChannel)
        {
            if (m_ClientRpcHandler.IsValid(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        m_ClientRpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        m_ClientRpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        m_ClientRpcHandler.Rpc(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        m_ClientRpcHandler.Rpc(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        m_ClientRpcHandler.Rpc(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        m_ClientRpcHandler.Rpc(msgId, buffer, seqChannel, default, default, default);
                        break;
                }
            }
        }

        #endregion

        #region Server

        protected virtual void OnServerInitialized()
        {
        }

        private void Internal_OnServerPeerConnected(NetworkPeer peer, Phase phase)
        {
            OnServerPeerConnected(peer, phase);
            if (phase == Phase.Ended)
            {
                // Synchronizes all network variables with the client to ensure that the client has 
                // the most up-to-date data from the server immediately after the spawning process.
                SyncNetworkState(peer);
            }
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
        #endregion

        protected virtual void OnServerMessage(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            buffer.SeekToBegin();
        }

        private void TryCallServerRpc(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            if (m_ServerRpcHandler.IsValid(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        m_ServerRpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        m_ServerRpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        m_ServerRpcHandler.Rpc(msgId, buffer, peer);
                        break;
                    case 3:
                        m_ServerRpcHandler.Rpc(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        m_ServerRpcHandler.Rpc(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        m_ServerRpcHandler.Rpc(msgId, buffer, peer, seqChannel, default, default);
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

        public void SetupRpcMessage(byte rpcId, NetworkGroup group, bool isServer, byte networkVariableId)
        {
            if (rpcId != NetworkConstants.k_NetworkVariableRpcId)
            {
                if (isServer)
                {
                    m_ServerRpcHandler.GetRpcParameters(rpcId, out var deliveryMode, out var target, out var sequenceChannel);
                    SetupRpcMessage(rpcId, deliveryMode, target, group, sequenceChannel, isServer);
                }
                else
                {
                    m_ClientRpcHandler.GetRpcParameters(rpcId, out var deliveryMode, out var __, out var sequenceChannel);
                    SetupRpcMessage(rpcId, deliveryMode, __, NetworkGroup.None, sequenceChannel, isServer);
                }
            }
            else
            {
                if (m_NetworkVariables.TryGetValue(networkVariableId, out NetworkVariableField field))
                {
                    if (isServer)
                    {
                        SetupRpcMessage(rpcId, field.DeliveryMode, field.Target, group, field.SequenceChannel, isServer);
                    }
                    else
                    {
                        SetupRpcMessage(rpcId, field.DeliveryMode, default, NetworkGroup.None, field.SequenceChannel, isServer);
                    }
                }
            }
        }

        public void SetupRpcMessage(byte rpcId, DeliveryMode deliveryMode, Target target, NetworkGroup group, byte seqChannel, bool isServer)
        {
            if (isServer)
            {
                NetworkManager.ServerSide.SetDefaultNetworkConfiguration(deliveryMode, target, group, seqChannel);
            }
            else
            {
                NetworkManager.ClientSide.SetDeliveryMode(deliveryMode);
                NetworkManager.ClientSide.SetSequenceChannel(seqChannel);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnRpcReceived(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool isServer, int seqChannel)
        {
            try
            {
                if (isServer)
                {
                    m_ServerRpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                    TryCallServerRpc(rpcId, buffer, peer, seqChannel);
                }
                else
                {
                    m_ClientRpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                    TryCallClientRpc(rpcId, buffer, seqChannel);
                }
            }
            catch (Exception ex)
            {
                string methodName = NetworkConstants.k_InvalidRpcName;
                if (isServer) methodName = m_ServerRpcHandler.GetRpcName(rpcId);
                else methodName = m_ClientRpcHandler.GetRpcName(rpcId);

                NetworkLogger.__Log__(
                    $"An exception occurred while processing the RPC -> " +
                    $"Rpc Id: '{rpcId}', Rpc Name: '{methodName}' in Class: '{GetType().Name}' -> " +
                    $"Exception Details: {ex.Message}. ",
                    NetworkLogger.LogType.Error
                );

                NetworkLogger.PrintHyperlink(ex);
#if OMNI_DEBUG
                throw;
#endif
            }
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.