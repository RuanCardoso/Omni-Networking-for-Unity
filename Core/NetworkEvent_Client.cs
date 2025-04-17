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
    /// The ClientBehaviour class is a specialized component within the Omni.Core namespace.
    /// It extends the NetworkEventBase class and implements the IInvokeMessage and IServiceBehaviour interfaces.
    /// This class is marked with a DefaultExecutionOrder attribute to ensure specific execution timing during the Unity lifecycle.
    /// </summary>
    /// <remarks>
    /// ClientBehaviour is fundamental for managing client-specific network events within a Unity environment.
    /// The class is designed with performance in mind, employing techniques to reduce boilerplate code,
    /// reflection, and source generation, which are critical for maintaining high execution speed.
    /// </remarks>
    [DefaultExecutionOrder(-3000)]
    public class ClientBehaviour : NetworkEventBase, IRpcMessage, IServiceBehaviour
    {
        /// <summary>
        /// Gets the identifier of the associated <see cref="IRpcMessage"/>.
        /// </summary>
        /// <value>The identifier of the associated <see cref="IRpcMessage"/> as an integer.</value>
        public int IdentityId => m_Id;

        private NetworkEventClient local;
        private readonly RpcHandler<DataBuffer, int, Null, Null, Null> rpcHandler = new();

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
        protected TransporterRouteManager.ClientRouteManager Router => NetworkManager._transporterRouteManager.Client;

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
        protected MatchClient Matchmaking => NetworkManager.Matchmaking.Client;

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
        protected NetworkPeer Peer
        {
            get
            {
                return NetworkManager.LocalPeer;
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

        private async void Internal_OnClientStart()
        {
            await UniTask.WaitUntil(() => IsClientActive);
            OnClientStart();
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
            rpcHandler.RegisterRpcMethodHandlers<ClientAttribute>(this);
            ClientSide.AddRpcMessage(m_Id, this);
            Client = new NetworkEventClient(this);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnClientDisconnected += OnClientDisconnected;
            NetworkManager.OnClientIdentitySpawned += OnClientIdentitySpawned;
            NetworkManager.OnPeerSharedDataChanged += OnPeerSharedDataChanged;
            ClientSide.OnMessage += OnMessage;
        }

        protected void RegisterMatchmakingEvents()
        {
            if (MatchmakingModuleEnabled)
            {
                NetworkManager.Matchmaking.Client.OnJoinedGroup += OnJoinedGroup;
                NetworkManager.Matchmaking.Client.OnLeftGroup += OnLeftGroup;
            }
        }

        protected void Unregister()
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            NetworkManager.OnClientConnected -= OnClientConnected;
            NetworkManager.OnClientDisconnected -= OnClientDisconnected;
            NetworkManager.OnClientIdentitySpawned -= OnClientIdentitySpawned;
            NetworkManager.OnPeerSharedDataChanged -= OnPeerSharedDataChanged;
            ClientSide.OnMessage -= OnMessage;

            if (MatchmakingModuleEnabled)
            {
                NetworkManager.Matchmaking.Client.OnJoinedGroup -= OnJoinedGroup;
                NetworkManager.Matchmaking.Client.OnLeftGroup -= OnLeftGroup;
            }

            NetworkService.Unregister(m_ServiceName);
            OnStop();
        }

        protected virtual void OnPeerSharedDataChanged(NetworkPeer peer, string key)
        {
        }

        protected virtual void OnClientIdentitySpawned(NetworkIdentity identity)
        {
        }

        protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
        }

        protected virtual void OnClientConnected()
        {
        }

        protected virtual void OnClientDisconnected(string reason)
        {
        }

        protected virtual void OnMessage(byte msgId, DataBuffer buffer, int seqChannel)
        {
            buffer.SeekToBegin();
            TryCallClientRpc(msgId, buffer, seqChannel); // Global Invoke
        }

        private void TryCallClientRpc(byte msgId, DataBuffer buffer, int seqChannel)
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
                        rpcHandler.Rpc(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        rpcHandler.Rpc(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        rpcHandler.Rpc(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        rpcHandler.Rpc(msgId, buffer, seqChannel, default, default, default);
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

        public void OnRpcInvoked(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool _, int seqChannel)
        {
            try
            {
                rpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                TryCallClientRpc(rpcId, buffer, seqChannel);
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