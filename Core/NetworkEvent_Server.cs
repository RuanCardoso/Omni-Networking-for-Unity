using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.ComponentModel;
using Omni.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Omni.Core.NetworkManager;
using Omni.Core.Web;
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
    /// The <c>ServerBehaviour</c> class represents core server functionality within the network infrastructure.
    /// It serves as a base class for implementing server-specific features and behaviors, offering methods
    /// and properties to manage server initialization, connections, and network event handling.
    /// <para>
    /// This class is optimized to minimize boilerplate and maximize performance through unconventional coding techniques.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="NetworkEventBase"/>, and implements the <see cref="Omni.Core.Interfaces.IRpcMessage"/>
    /// and <see cref="Omni.Core.Interfaces.IServiceBehaviour"/> interfaces.
    /// <para>
    /// Key responsibilities include setting up network events, managing peer connections, and handling
    /// data buffer processing for server-side operations.
    /// </para>
    /// </remarks>
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

        private HttpRouteManager _http;
        /// <summary>
        /// Gets the HTTP route manager that provides access to HTTP-based networking functionality.
        /// This property allows server implementations to handle RESTful endpoints, web hooks,
        /// and other HTTP-based communication.
        /// </summary>
        /// <remarks>
        /// Accessing this property automatically retrieves the HTTP service instance from the service locator.
        /// If the HTTP module is not enabled in the NetworkManager inspector, or if accessed too early in
        /// the initialization process, a NullReferenceException will be thrown with detailed instructions.
        /// </remarks>
        /// <exception cref="NullReferenceException">
        /// Thrown when the HTTP service is not available, either because the HTTP module is not enabled
        /// in the NetworkManager settings or because the property is accessed before service initialization.
        /// </exception>
        /// <value>
        /// The <see cref="HttpRouteManager"/> instance for handling HTTP routes and requests.
        /// </value>
        protected HttpRouteManager Http
        {
            get
            {
                if (_http == null)
                {
                    if (!NetworkService.TryGet(out _http))
                    {
                        throw new NullReferenceException(
                            "HTTP service not available. Make sure the HTTP module is enabled in the Network Manager inspector. " +
                            "If already enabled, this service may only be available after Start() has completed initialization. " +
                            "Try accessing this property from OnStart() or later in the execution lifecycle."
                        );
                    }
                }

                return _http;
            }
        }

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
        protected TransporterRouteManager.ServerRouteManager Router => NetworkManager._transporterRouteManager.Server;

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
        protected MatchServer Matchmaking => NetworkManager.Matchmaking.Server;

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

        /// <summary>
        /// Invoked when the server becomes active. This method functions similarly to Unity's Start(),
        /// but is specifically called when the server is up and running.
        /// </summary>
        protected virtual void OnServerStart()
        {
        }

        protected void InitializeBehaviour()
        {
            ___RegisterNetworkVariables___();
            // Registers notifications for changes in the collection, enabling automatic updates when the collection is modified.
            ___NotifyCollectionChange___();
            rpcHandler.RegisterRpcMethodHandlers<ServerAttribute>(this);
            ServerSide.AddRpcMessage(m_Id, this);
            Server = new NetworkEventServer(this);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            ServerSide.OnMessage += OnMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            if (MatchmakingModuleEnabled)
            {
                NetworkManager.Matchmaking.Server.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
                NetworkManager.Matchmaking.Server.OnPlayerLeftGroup += OnPlayerLeftGroup;

                NetworkManager.Matchmaking.Server.OnPlayerFailedJoinGroup += OnPlayerFailedJoinGroup;
                NetworkManager.Matchmaking.Server.OnPlayerFailedLeaveGroup += OnPlayerFailedLeaveGroup;
            }
        }

        protected void Unregister()
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            NetworkManager.OnServerInitialized -= OnServerInitialized;
            NetworkManager.OnServerPeerConnected -= OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
            ServerSide.OnMessage -= OnMessage;

            if (MatchmakingModuleEnabled)
            {
                NetworkManager.Matchmaking.Server.OnPlayerJoinedGroup -= OnPlayerJoinedGroup;
                NetworkManager.Matchmaking.Server.OnPlayerLeftGroup -= OnPlayerLeftGroup;

                NetworkManager.Matchmaking.Server.OnPlayerFailedJoinGroup -= OnPlayerFailedJoinGroup;
                NetworkManager.Matchmaking.Server.OnPlayerFailedLeaveGroup -= OnPlayerFailedLeaveGroup;
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

        public void OnRpcInvoked(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool _, int seqChannel)
        {
            try
            {
                bool isClientAuthority = false;

                if (rpcId == NetworkConstants.NETWORK_VARIABLE_RPC_ID)
                {
                    byte id = buffer.BufferAsSpan[0];
                    if (m_NetworkVariables.TryGetValue(id, out NetworkVariableField field))
                    {
                        isClientAuthority = field.IsClientAuthority;

                        if (!AllowNetworkVariablesFromClients && !isClientAuthority)
                        {
#if OMNI_DEBUG
                            NetworkLogger.__Log__(
                                $"[Security] Network Variable modification rejected: Client lacks permission to modify '{field.Name}' (ID: {id}). " +
                                $"This variable is not marked with 'ClientAuthority' option. " +
                                $"Client ID: {peer.Id}, Object: {GetType().Name}",
                                NetworkLogger.LogType.Error
                            );
#else
                            NetworkLogger.__Log__(
                                $"[Security] Client {peer.Id} disconnected: Unauthorized network variable modification attempt. " +
                                $"Attempted to modify '{field.Name}' (ID: {id}) without proper permissions. " +
                                $"To allow client modifications, either enable 'AllowNetworkVariablesFromClients' in NetworkManager " +
                                $"or mark the variable with 'ClientAuthority' option.",
                                NetworkLogger.LogType.Error
                            );

                            peer.Disconnect();
#endif
                            return;
                        }
                    }
                    else
                    {
                        NetworkLogger.__Log__($"The 'NetworkVariable' with ID '{id}' does not exist. " +
                            "Ensure the ID is valid and registered in the network variables collection.",
                            NetworkLogger.LogType.Error
                        );

                        return;
                    }
                }

                rpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                TryCallServerRpc(rpcId, buffer, peer, seqChannel);
            }
            catch (Exception ex)
            {
                string methodName = rpcHandler.GetRpcName(rpcId);
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