using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Omni.Core.Interfaces;
using Omni.Shared;
using UnityEngine;

namespace Omni.Core
{
    public class NetworkBehaviour : NetVarBehaviour, INetworkMessage
    {
        // Hacky: DIRTY CODE!
        // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
        // Despite its appearance, this approach is essential to achieve high performance.
        // Avoid refactoring as these techniques are crucial for optimizing execution speed.
        // Works with il2cpp.

        public class NbClient
        {
            private readonly NetworkBehaviour m_NetworkBehaviour;

            internal NbClient(NetworkBehaviour networkBehaviour)
            {
                m_NetworkBehaviour = networkBehaviour;
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
                using DataBuffer message = m_NetworkBehaviour.CreateHeader(property, propertyId);
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
                IPropertyInfo property = m_NetworkBehaviour.GetPropertyInfoWithCallerName<T>(
                    callerName
                );

                IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

                if (property != null)
                {
                    using DataBuffer message = m_NetworkBehaviour.CreateHeader(
                        propertyGeneric.Invoke(),
                        property.Id
                    );

                    Invoke(255, message, deliveryMode, sequenceChannel);
                }
            }

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
                NetworkManager.Client.Invoke(
                    msgId,
                    m_NetworkBehaviour.IdentityId,
                    m_NetworkBehaviour.Id,
                    buffer,
                    deliveryMode,
                    sequenceChannel
                );
        }

        public class NbServer
        {
            private readonly NetworkBehaviour m_NetworkBehaviour;

            internal NbServer(NetworkBehaviour networkBehaviour)
            {
                m_NetworkBehaviour = networkBehaviour;
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
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                int cacheId = 0,
                CacheMode cacheMode = CacheMode.None,
                byte sequenceChannel = 0
            )
            {
                using DataBuffer message = m_NetworkBehaviour.CreateHeader(property, propertyId);
                Invoke(
                    255,
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
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                int cacheId = 0,
                CacheMode cacheMode = CacheMode.None,
                byte sequenceChannel = 0,
                [CallerMemberName] string callerName = ""
            )
            {
                IPropertyInfo propertyInfo = m_NetworkBehaviour.GetPropertyInfoWithCallerName<T>(
                    callerName
                );
                IPropertyInfo<T> propertyInfoGeneric = propertyInfo as IPropertyInfo<T>;

                if (propertyInfo != null)
                {
                    using DataBuffer message = m_NetworkBehaviour.CreateHeader(
                        propertyInfoGeneric.Invoke(),
                        propertyInfo.Id
                    );

                    Invoke(
                        255,
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
                DataBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                int cacheId = 0,
                CacheMode cacheMode = CacheMode.None,
                byte sequenceChannel = 0
            )
            {
                if (m_NetworkBehaviour.Identity.Owner == null)
                {
                    NetworkLogger.__Log__(
                        "Invocation Error: The 'Invoke' method is intended for server-side use only. It appears to be accessed from the client side. Please ensure that this method is called from the server.",
                        NetworkLogger.LogType.Error
                    );

                    return;
                }

                NetworkManager.Server.Invoke(
                    msgId,
                    m_NetworkBehaviour.Identity.Owner.Id,
                    m_NetworkBehaviour.IdentityId,
                    m_NetworkBehaviour.Id,
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

        private readonly EventBehaviour<DataBuffer, int, Null, Null, Null> clientEventBehaviour =
            new();

        private readonly EventBehaviour<
            DataBuffer,
            NetworkPeer,
            int,
            Null,
            Null
        > serverEventBehaviour = new();

        [SerializeField]
        private string m_ServiceName;

        [SerializeField]
        private byte m_Id = 0;

        /// <summary>
        /// Gets or sets the identifier of this instance.
        /// </summary>
        /// <value>The identifier as a byte.</value>
        public byte Id
        {
            get { return m_Id; }
            internal set { m_Id = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="NetworkIdentity"/> associated with this instance.
        /// </summary>
        /// <value>The <see cref="NetworkIdentity"/> associated with this instance.</value>
        public NetworkIdentity Identity { get; internal set; }

        /// <summary>
        /// Gets the identifier of the associated <see cref="NetworkIdentity"/>.
        /// </summary>
        /// <value>The identifier of the associated <see cref="NetworkIdentity"/> as an integer.</value>
        public int IdentityId => Identity.IdentityId;

        /// <summary>
        /// Gets a value indicating whether this instance represents the local player.
        /// </summary>
        /// <value><c>true</c> if this instance represents the local player; otherwise, <c>false</c>.</value>
        public bool IsLocalPlayer => Identity.IsLocalPlayer;

        /// <summary>
        /// Gets a value indicating whether this instance represents the local player.
        /// </summary>
        /// <value><c>true</c> if this instance represents the local player; otherwise, <c>false</c>.</value>
        /// <remarks>This property is an alias for <see cref="IsLocalPlayer"/>.</remarks>
        public bool IsMine => IsLocalPlayer;

        /// <summary>
        /// Gets a value indicating whether this instance is on the server.
        /// </summary>
        /// <value><c>true</c> if this instance is on the server; otherwise, <c>false</c>.</value>
        public bool IsServer => Identity.IsServer;

        /// <summary>
        /// Gets a value indicating whether this instance is on the client.
        /// </summary>
        /// <value><c>true</c> if this instance is on the client; otherwise, <c>false</c>.</value>
        public bool IsClient => !IsServer;

        private NbClient _local;

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbClient"/> instance used to invoke messages on the server from the client.
        /// </summary>
        public NbClient Local
        {
            get
            {
                if (_local == null)
                {
                    throw new Exception(
                        "This property(Local) is intended for client-side use only. It appears to be accessed from the server side."
                    );
                }

                return _local;
            }
            private set => _local = value;
        }

        private NbServer _remote;

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbServer"/> instance used to invoke messages on the client from the server.
        /// </summary>
        public NbServer Remote
        {
            get
            {
                if (_remote == null)
                {
                    throw new Exception(
                        "This property(Remote) is intended for server-side use only. It appears to be accessed from the client side."
                    );
                }

                return _remote;
            }
            private set => _remote = value;
        }

        /// <summary>
        /// Called after the object is instantiated and registered, but before it is active.
        /// </summary>
        /// <remarks>
        /// Override this method to perform any initialization that needs to happen
        /// before the object becomes active.
        /// </remarks>
        /// <param name="buffer">The buffer containing data associated with the instantiation process.</param>
        protected internal virtual void OnAwake(DataBuffer buffer) { }

        /// <summary>
        /// Called after the object is instantiated and after it becomes active.
        /// </summary>
        /// <remarks>
        /// Override this method to perform any initialization or setup that needs to happen
        /// after the object has become active.
        /// </remarks>
        /// <param name="buffer">The buffer containing data associated with the instantiation process.</param>
        protected internal virtual void OnStart(DataBuffer buffer) { }

        internal void Register()
        {
            if (Identity.IsServer)
            {
                serverEventBehaviour.FindEvents<ServerAttribute>(this);
                Remote = new NbServer(this);
            }
            else
            {
                clientEventBehaviour.FindEvents<ClientAttribute>(this);
                Local = new NbClient(this);
            }

            AddEventBehaviour();
            InitializeServiceLocator();
        }

        internal void Unregister()
        {
            var eventBehaviours = Identity.IsServer
                ? NetworkManager.Server.LocalEventBehaviours
                : NetworkManager.Client.LocalEventBehaviours;

            var key = (IdentityId, m_Id);
            if (!eventBehaviours.Remove(key))
            {
                NetworkLogger.__Log__(
                    $"Unregister Error: EventBehaviour with ID '{m_Id}' and peer ID '{IdentityId}' does not exist. Please ensure the EventBehaviour is registered before attempting to unregister.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        private void InitializeServiceLocator()
        {
            Identity.Register(this, m_ServiceName);
        }

        private void AddEventBehaviour()
        {
            var eventBehaviours = Identity.IsServer
                ? NetworkManager.Server.LocalEventBehaviours
                : NetworkManager.Client.LocalEventBehaviours;

            var key = (IdentityId, m_Id);
            if (!eventBehaviours.TryAdd(key, this))
            {
                NetworkLogger.__Log__(
                    $"Failed to add: EventBehaviour with ID {m_Id} and peer ID {IdentityId} already exists.",
                    NetworkLogger.LogType.Error
                );
            }
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Internal_OnMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            bool _,
            int seqChannel
        )
        {
            if (Identity.IsServer)
            {
                TryServerLocate(msgId, buffer, peer, seqChannel);
            }
            else
            {
                TryClientLocate(msgId, buffer, seqChannel);
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id < 0)
            {
                m_Id = 0;
            }
            else if (m_Id > 255)
            {
                m_Id = 255;
            }

            if (string.IsNullOrEmpty(m_ServiceName))
            {
                int uniqueId = 0;
                string serviceName = GetType().Name;
                NetworkBehaviour[] services =
                    transform.root.GetComponentsInChildren<NetworkBehaviour>(true);

                if ((uniqueId = services.Count(x => x.m_ServiceName == serviceName)) >= 1)
                {
                    m_ServiceName = $"{serviceName}_{uniqueId}";
                }
                else
                {
                    m_ServiceName = serviceName;
                }
            }
        }

        protected virtual void Reset()
        {
            OnValidate();
        }
    }
}
