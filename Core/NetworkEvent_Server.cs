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
        private readonly __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> m_RpcHandler = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> __ServerRpcHandler => m_RpcHandler;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> __ClientRpcHandler => throw new NotImplementedException("This property is not implemented for the ServerBehaviour class.");

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
        /// The <see cref="NetworkMatchmaking"/> instance for handling server-side matchmaking operations.
        /// </value>
        protected NetworkMatchmaking Matchmaking => NetworkManager.Matchmaking;

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
        protected NetworkPeer Peer
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
            m_RpcHandler.RegisterRpcMethodHandlers<ServerAttribute>(this);
            ServerSide.AddRpcMessage(m_Id, this);
            Server = new NetworkEventServer(this);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnServerInitialized += OnServerInitialized;
            NetworkManager.OnServerPeerConnected += Internal_OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            ServerSide.OnMessage += OnMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            if (MatchmakingModuleEnabled)
            {
                NetworkManager.Matchmaking.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
                NetworkManager.Matchmaking.OnPlayerLeftGroup += OnPlayerLeftGroup;

                NetworkManager.Matchmaking.OnPlayerFailedJoinGroup += OnPlayerFailedJoinGroup;
                NetworkManager.Matchmaking.OnPlayerFailedLeaveGroup += OnPlayerFailedLeaveGroup;
            }
        }

        protected void Unregister()
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            NetworkManager.OnServerInitialized -= OnServerInitialized;
            NetworkManager.OnServerPeerConnected -= Internal_OnServerPeerConnected;
            NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
            ServerSide.OnMessage -= OnMessage;

            if (MatchmakingModuleEnabled)
            {
                NetworkManager.Matchmaking.OnPlayerJoinedGroup -= OnPlayerJoinedGroup;
                NetworkManager.Matchmaking.OnPlayerLeftGroup -= OnPlayerLeftGroup;

                NetworkManager.Matchmaking.OnPlayerFailedJoinGroup -= OnPlayerFailedJoinGroup;
                NetworkManager.Matchmaking.OnPlayerFailedLeaveGroup -= OnPlayerFailedLeaveGroup;
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
        }

        private void TryCallServerRpc(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            if (m_RpcHandler.IsValid(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        m_RpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        m_RpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        m_RpcHandler.Rpc(msgId, buffer, peer);
                        break;
                    case 3:
                        m_RpcHandler.Rpc(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        m_RpcHandler.Rpc(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        m_RpcHandler.Rpc(msgId, buffer, peer, seqChannel, default, default);
                        break;
                }
            }
        }

        /// <summary>
        /// Called when a player has joined a group.
        /// </summary>
        /// <param name="group">The network group that the player joined.</param>
        /// <param name="peer">The network peer representing the player who joined the group.</param>
        protected virtual void OnPlayerJoinedGroup(NetworkGroup group, NetworkPeer peer)
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

        public void SetupRpcMessage(byte rpcId, NetworkGroup group, bool _, byte networkVariableId)
        {
            if (rpcId != NetworkConstants.k_NetworkVariableRpcId)
            {
                m_RpcHandler.GetRpcParameters(rpcId, out var deliveryMode, out var target, out var sequenceChannel);
                SetupRpcMessage(rpcId, deliveryMode, target, group, sequenceChannel, _);
            }
            else
            {
                if (m_NetworkVariables.TryGetValue(networkVariableId, out NetworkVariableField field))
                {
                    SetupRpcMessage(rpcId, field.DeliveryMode, field.Target, group, field.SequenceChannel, _);
                }
            }
        }

        public void SetupRpcMessage(byte rpcId, DeliveryMode deliveryMode, Target target, NetworkGroup group, byte seqChannel, bool _)
        {
            NetworkManager.ServerSide.SetDefaultNetworkConfiguration(deliveryMode, target, group, seqChannel);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnRpcReceived(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool _, int seqChannel)
        {
            try
            {
                bool isClientAuthority = false;

                if (rpcId == NetworkConstants.k_NetworkVariableRpcId)
                {
                    byte id = buffer.BufferAsSpan[0];
                    if (m_NetworkVariables.TryGetValue(id, out NetworkVariableField field))
                    {
                        isClientAuthority = field.IsClientAuthority;

                        if (!isClientAuthority)
                        {
#if OMNI_DEBUG
                            NetworkLogger.__Log__(
                                $"NetworkVariable modification rejected. " +
                                $"ClientId={peer.Id} tried to modify '{field.Name}' (Id={id}) without authority. " +
                                $"Object={GetType().Name}",
                                NetworkLogger.LogType.Error
                            );
#else
                            NetworkLogger.__Log__(
                                 $"Client {peer.Id} disconnected: tried to modify '{field.Name}' (Id={id}) without permission. " +
                                 "Enable 'IsClientAuthority' if this is intended.",
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

                m_RpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                TryCallServerRpc(rpcId, buffer, peer, seqChannel);
            }
            catch (Exception ex)
            {
                string methodName = m_RpcHandler.GetRpcName(rpcId);
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