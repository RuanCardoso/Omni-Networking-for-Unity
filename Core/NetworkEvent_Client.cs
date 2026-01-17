using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.ComponentModel;
using Omni.Threading.Tasks;
using UnityEngine;
using static Omni.Core.NetworkManager;

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
        private readonly __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> m_RpcHandler = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> __ServerRpcHandler => throw new NotImplementedException("This property is not implemented for the ClientBehaviour class.");

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> __ClientRpcHandler => m_RpcHandler;

        /// <summary>
        /// Defines the default <see cref="NetworkGroup"/> associated with this object.  
        /// Used whenever no group is explicitly specified in a network operation  
        /// (e.g., RPCs, etc).
        /// </summary>
        public NetworkGroup DefaultGroup
        {
            get
            {
                throw new NotImplementedException("This property is not implemented for the ClientBehaviour class.");
            }
            set
            {
                throw new NotImplementedException("This property is not implemented for the ClientBehaviour class.");
            }
        }

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
                allowUnregisterService = false;
                gameObject.SetActive(false);
                Destroy(gameObject, 1f);
                return;
            }

            InitializeServiceLocator();
            InitializeBehaviour();
            RegisterSystemEvents();
            OnAwake();
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
            Internal_OnClientStart();

            OnStart();
            Service.UpdateReference(m_ServiceName);
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
            m_RpcHandler.RegisterRpcMethodHandlers<ClientAttribute>(this);
            ClientSide.AddRpcMessage(m_Id, this);
            Client = new NetworkEventClient(this);
        }

        protected void RegisterSystemEvents()
        {
            NetworkManager.OnClientConnected += OnClientConnected;
            NetworkManager.OnClientDisconnected += OnClientDisconnected;
            NetworkManager.OnPeerSharedDataChanged += OnPeerSharedDataChanged;
            ClientSide.OnMessage += OnMessage;
        }

        protected void Unregister()
        {
            NetworkManager.OnClientConnected -= OnClientConnected;
            NetworkManager.OnClientDisconnected -= OnClientDisconnected;
            NetworkManager.OnPeerSharedDataChanged -= OnPeerSharedDataChanged;
            ClientSide.OnMessage -= OnMessage;

            if (allowUnregisterService)
                NetworkService.Unregister(m_ServiceName);

            OnStop();
        }

        protected virtual void OnDestroy()
        {
            try
            {
                Unregister();
            }
            catch
            {
                // avoid exceptions on (Editor Only)
            }
        }

        protected virtual void OnPeerSharedDataChanged(NetworkPeer peer, string key)
        {
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
        }

        private void TryCallClientRpc(byte msgId, DataBuffer buffer, int seqChannel)
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
                        m_RpcHandler.Rpc(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        m_RpcHandler.Rpc(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        m_RpcHandler.Rpc(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        m_RpcHandler.Rpc(msgId, buffer, seqChannel, default, default, default);
                        break;
                }
            }
        }

        public void SetupRpcMessage(byte rpcId, NetworkGroup group, bool _, byte networkVariableId)
        {
            if (rpcId != NetworkConstants.k_NetworkVariableRpcId)
            {
                m_RpcHandler.GetRpcParameters(rpcId, out var deliveryMode, out var __, out var sequenceChannel);
                SetupRpcMessage(rpcId, deliveryMode, __, NetworkGroup.None, sequenceChannel, _);
            }
            else
            {
                if (m_NetworkVariables.TryGetValue(networkVariableId, out NetworkVariableField field))
                {
                    SetupRpcMessage(rpcId, field.DeliveryMode, default, NetworkGroup.None, field.SequenceChannel, _);
                }
            }
        }

        public void SetupRpcMessage(byte rpcId, DeliveryMode deliveryMode, Target target, NetworkGroup group, byte seqChannel, bool _)
        {
            NetworkManager.ClientSide.SetDeliveryMode(deliveryMode);
            NetworkManager.ClientSide.SetSequenceChannel(seqChannel);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnRpcReceived(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool _, int seqChannel)
        {
            try
            {
                m_RpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                TryCallClientRpc(rpcId, buffer, seqChannel);
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