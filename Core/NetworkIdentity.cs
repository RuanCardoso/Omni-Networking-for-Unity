using Omni.Core.Interfaces;
using System;
using System.Collections.Generic;
using Omni.Inspector;
using UnityEngine;
using Omni.Threading.Tasks;
using Newtonsoft.Json;
using MemoryPack;
using Omni.Shared;
using System.Linq;
#if OMNI_RELEASE
using System.Runtime.CompilerServices;
#endif

#pragma warning disable

namespace Omni.Core
{
    public enum EntityType
    {
        Generic,
        Player,
    }

    [DeclareTabGroup("Debug")]
    [JsonObject(MemberSerialization.OptIn)]
    [MemoryPackable(GenerateType.NoGenerate)]
    [Serializable]
    public sealed partial class NetworkIdentity : OmniBehaviour, IEquatable<NetworkIdentity>
    {
        enum EditorViewMode
        {
            Both,
            ClientOnly,
            ServerOnly
        }

        [MemoryPackIgnore, JsonIgnore]
        internal string _prefabName;

        [MemoryPackIgnore, JsonIgnore]
        internal Action<byte, DataBuffer, NetworkPeer> OnRequestAction;

        [MemoryPackIgnore, JsonIgnore]
        internal Action<NetworkPeer, NetworkPeer> OnServerOwnershipTransferred;

        // (Service Name, Service Instance) exclusively to identity
        [MemoryPackIgnore, JsonIgnore]
        private readonly Dictionary<string, object> m_Services = new();

        [MemoryPackIgnore, JsonIgnore, LabelWidth(140)]
        [SerializeField][ReadOnly] private int m_Id;

        [SerializeField, LabelWidth(140)]
        internal NetworkIdentity m_ServerPrefabOverride;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField]
        [ReadOnly]
        [LabelWidth(150)]
        [Group("Debug"), Tab("Debug")]
        private bool m_IsServer;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField]
        [ReadOnly]
        [LabelWidth(150)]
        [Group("Debug"), Tab("Debug")]
        private bool m_IsLocalPlayer;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField]
        [ReadOnly]
        [LabelWidth(150)]
        [Group("Debug"), Tab("Debug")]
        private bool m_IsMine;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField]
        [ReadOnly]
        [LabelWidth(150)]
        [Group("Debug"), Tab("Debug")]
        private bool isOwnedByTheServer;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField, HideInInspector] // TODO: Not implemented
        [LabelWidth(154), DisableInPlayMode]
        [Group("Debug"), Tab("Basic")]
        private EntityType m_EntityType;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField]
        [LabelWidth(154), DisableInPlayMode]
        [Group("Debug"), Tab("Basic")]
        private bool m_AutoDestroy = true;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField]
        [LabelWidth(154), DisableInPlayMode]
        [Group("Debug"), Tab("Basic")]
        private bool m_DontDestroyOnLoad;

        [MemoryPackIgnore, JsonIgnore]
        [SerializeField]
        [LabelWidth(200), DisableInPlayMode]
        [Group("Debug"), Tab("Basic")]
        private bool m_AllowInstantOwnershipTransfer = false;

        [SerializeField, ReadOnly]
        private List<NetworkBehaviour> m_NetworkBehaviours = new();

        public EntityType EntityType => m_EntityType;
        /// <summary>
        /// Gets the unique identifier for this network identity.
        /// </summary>
        [MemoryPackInclude, JsonProperty("Id")]
        public int Id
        {
            get { return m_Id; }
            internal set { m_Id = value; }
        }

        /// <summary>
        /// The local player instance. Set on the client(only).
        /// </summary>
        [MemoryPackIgnore, JsonIgnore]
        public static NetworkIdentity LocalPlayer { get; internal set; }

        /// <summary>
        /// The owner of this object, available on both server and client side. 
        /// On the client side, only a few properties are accessible, such as <c>SharedData</c>.
        /// </summary>
        [MemoryPackIgnore, JsonIgnore]
        public NetworkPeer Owner { get; internal set; }

        /// <summary>
        /// Indicates whether this object is obtained from the server side or checked on the client side.
        /// True if the object is obtained from the server side, false if it is checked on the client side.
        /// </summary>
        [MemoryPackIgnore, JsonIgnore]
        public bool IsServer
        {
            get { return m_IsServer; }
            internal set { m_IsServer = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is on the client side.
        /// </summary>
        /// <value><c>true</c> if this instance is on the client side; otherwise, <c>false</c>.</value>
        [MemoryPackIgnore, JsonIgnore]
        public bool IsClient => !IsServer;

        /// <summary>
        /// Indicates whether this instance represents the local player.
        /// </summary>
        [MemoryPackIgnore, JsonIgnore]
        public bool IsLocalPlayer
        {
            get { return m_IsLocalPlayer; }
            internal set
            {
                var localPlayer = NetworkIdentity.LocalPlayer;
                m_IsLocalPlayer = value && localPlayer != null && localPlayer.Equals(this);
            }
        }

        /// <summary>
        /// Determines whether this networked behaviour instance belongs to the local player.
        /// Use this property to verify if the current networked object is under the authority of the local player.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is controlled by the local player; otherwise, <c>false</c>.
        /// </value>
        public bool IsMine
        {
            get { return m_IsMine; }
            internal set { m_IsMine = value; }
        }

        /// <summary>
        /// Indicates whether this NetworkIdentity is registered.
        /// </summary>
        [MemoryPackIgnore, JsonIgnore]
        public bool IsRegistered
        {
            get { return Owner != null && m_Id != 0; }
        }

        /// <summary>
        /// Indicates whether this identity is owned by the server.
        /// </summary>
        [MemoryPackIgnore, JsonIgnore]
        public bool IsOwnedByServer
        {
            get { return isOwnedByTheServer; }
            internal set { isOwnedByTheServer = value; }
        }

        internal void Register()
        {
            OnRequestAction += OnRequestedAction;
        }

        void Awake()
        {
            if (m_DontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        async void Start()
        {
            if (m_AutoDestroy && IsRegistered && IsServer)
            {
                await UniTask.WaitUntil(() => !Owner.IsConnected);
                Despawn(true);
            }
        }

        void OnRequestedAction(byte actionId, DataBuffer data, NetworkPeer peer)
        {
            if (IsClient)
            {
                if (actionId == NetworkConstants.k_DestroyEntityId)
                {
                    Destroy(gameObject);
                }
                else if (actionId == NetworkConstants.k_SetOwnerId)
                {
                    int newPeerId = data.Read<int>();
                    bool isMine = peer.Id == newPeerId;

                    if (Owner.Id == newPeerId)
                        return;

                    if (IsPlayerObject())
                    {
                        if (!isMine) NetworkIdentity.LocalPlayer = null;
                        else NetworkIdentity.LocalPlayer = this;
                    }

                    IsLocalPlayer = isMine;
                    IsMine = isMine;
                    IsOwnedByServer = newPeerId == NetworkManager.ClientSide.ServerPeer.Id;
                    Owner = NetworkManager.ClientSide.GetOrCreatePeer(newPeerId);
                }
            }
        }

        void OnDestroy()
        {
            OnRequestAction -= OnRequestedAction;
            if (IsServer && m_AutoDestroy)
                Despawn(false);
        }

        /// <summary>
        /// Retrieves a service instance by its name from the service locator.
        /// Throws an exception if the service is not found or cannot be cast to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the service should be cast.</typeparam>
        /// <param name="serviceName">The name of the service to retrieve.</param>
        /// <returns>The service instance cast to the specified type.</returns>
        /// <exception cref="Exception">
        /// Thrown if the service is not found or cannot be cast to the specified type.
        /// </exception>
        public T Get<T>(string serviceName)
            where T : class
        {
            try
            {
                if (m_Services.TryGetValue(serviceName, out object service))
                {
#if OMNI_RELEASE
                    return Unsafe.As<T>(service);
#else
                    return (T)service;
#endif
                }
                else
                {
                    throw new Exception(
                        $"Could not find service with name: \"{serviceName}\" you must register the service before using it."
                    );
                }
            }
            catch (InvalidCastException)
            {
                throw new Exception(
                    $"Could not cast service with name: \"{serviceName}\" to type: \"{typeof(T)}\" check if you registered the service with the correct type."
                );
            }
        }

        /// <summary>
        /// Attempts to retrieve a service instance by its name from the service locator.
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve.</typeparam>
        /// <param name="serviceName">The name of the service to retrieve.</param>
        /// <param name="service">When this method returns, contains the service instance cast to the specified type if the service is found; otherwise, the default value for the type of the service parameter.</param>
        /// <returns>True if the service is found and successfully cast to the specified type; otherwise, false.</returns>
        public bool TryGet<T>(string serviceName, out T service) where T : class
        {
            service = default;
            if (m_Services.TryGetValue(serviceName, out object @obj))
            {
                if (@obj is T)
                {
                    service = Get<T>(serviceName);
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a service instance by its type name from the service locator.
        /// </summary>
        /// <typeparam name="T">The type to which the service should be cast.</typeparam>
        /// <returns>The service instance cast to the specified type.</returns>
        /// <exception cref="Exception">
        /// Thrown if the service is not found or cannot be cast to the specified type.
        /// </exception>
        public T Get<T>() where T : class
        {
            return Get<T>(typeof(T).Name);
        }

        /// <summary>
        /// Attempts to retrieve a service instance by its type from the service locator.
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve.</typeparam>
        /// <param name="service">When this method returns, contains the service instance cast to the specified type if the service is found; otherwise, the default value for the type of the service parameter.</param>
        /// <returns>True if the service is found and successfully cast to the specified type; otherwise, false.</returns>
        public bool TryGet<T>(out T service) where T : class
        {
            service = default;
            string serviceName = typeof(T).Name;
            if (m_Services.TryGetValue(serviceName, out object @obj))
            {
                if (@obj is T)
                {
                    service = Get<T>(serviceName);
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Adds a new service instance to the service locator with a specified name.
        /// Throws an exception if a service with the same name already exists.
        /// </summary>
        /// <typeparam name="T">The type of the service to add.</typeparam>
        /// <param name="service">The service instance to add.</param>
        /// <param name="serviceName">The name to associate with the service instance.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name already exists.
        /// </exception>
        public void Register<T>(T service)
        {
            Register<T>(service, typeof(T).Name);
        }

        /// <summary>
        /// Adds a new service instance to the service locator with a specified name.
        /// Throws an exception if a service with the same name already exists.
        /// </summary>
        /// <typeparam name="T">The type of the service to add.</typeparam>
        /// <param name="service">The service instance to add.</param>
        /// <param name="serviceName">The name to associate with the service instance.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name already exists.
        /// </exception>
        public void Register<T>(T service, string serviceName)
        {
            if (!m_Services.TryAdd(serviceName, service))
            {
                throw new Exception(
                    $"Could not add service with name: \"{serviceName}\" because it already exists."
                );
            }
        }

        /// <summary>
        /// Attempts to retrieve adds a new service instance to the service locator with a specified name.
        /// Throws an exception if a service with the same name already exists.
        /// </summary>
        /// <typeparam name="T">The type of the service to add.</typeparam>
        /// <param name="service">The service instance to add.</param>
        /// <param name="serviceName">The name to associate with the service instance.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name already exists.
        /// </exception>
        public bool TryRegister<T>(T service)
        {
            return TryRegister<T>(service, typeof(T).Name);
        }

        /// <summary>
        /// Attempts to retrieve adds a new service instance to the service locator with a specified name.
        /// Throws an exception if a service with the same name already exists.
        /// </summary>
        /// <typeparam name="T">The type of the service to add.</typeparam>
        /// <param name="service">The service instance to add.</param>
        /// <param name="serviceName">The name to associate with the service instance.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name already exists.
        /// </exception>
        public bool TryRegister<T>(T service, string serviceName)
        {
            return m_Services.TryAdd(serviceName, service);
        }

        /// <summary>
        /// Updates an existing service instance in the service locator with a specified name.
        /// Throws an exception if a service with the specified name does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the service to update.</typeparam>
        /// <param name="service">The new service instance to associate with the specified name.</param>
        /// <param name="serviceName">The name associated with the service instance to update.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name does not exist in the.
        /// </exception>
        public void UpdateService<T>(T service)
        {
            UpdateService<T>(service, typeof(T).Name);
        }

        /// <summary>
        /// Updates an existing service instance in the service locator with a specified name.
        /// Throws an exception if a service with the specified name does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the service to update.</typeparam>
        /// <param name="service">The new service instance to associate with the specified name.</param>
        /// <param name="serviceName">The name associated with the service instance to update.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name does not exist in the.
        /// </exception>
        public void UpdateService<T>(T service, string serviceName)
        {
            if (m_Services.ContainsKey(serviceName))
            {
                m_Services[serviceName] = service;
            }
            else
            {
                throw new Exception(
                    $"Could not update service with name: \"{serviceName}\" because it does not exist."
                );
            }
        }

        /// <summary>
        /// Attempts to retrieve updates an existing service instance in the service locator with a specified name.
        /// Throws an exception if a service with the specified name does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the service to update.</typeparam>
        /// <param name="service">The new service instance to associate with the specified name.</param>
        /// <param name="serviceName">The name associated with the service instance to update.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name does not exist in the.
        /// </exception>
        public bool TryUpdateService<T>(T service)
        {
            return TryUpdateService<T>(service, typeof(T).Name);
        }

        /// <summary>
        /// Attempts to retrieve updates an existing service instance in the service locator with a specified name.
        /// Throws an exception if a service with the specified name does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the service to update.</typeparam>
        /// <param name="service">The new service instance to associate with the specified name.</param>
        /// <param name="serviceName">The name associated with the service instance to update.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name does not exist in the.
        /// </exception>
        public bool TryUpdateService<T>(T service, string serviceName)
        {
            if (m_Services.ContainsKey(serviceName))
            {
                m_Services[serviceName] = service;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deletes a service instance from the service locator by its name.
        /// </summary>
        /// <param name="serviceName">The name of the service to delete.</param>
        /// <returns>True if the service was successfully removed; otherwise, false.</returns>
        public bool Unregister<T>()
        {
            return Unregister(typeof(T).Name);
        }

        /// <summary>
        /// Deletes a service instance from the service locator by its name.
        /// </summary>
        /// <param name="serviceName">The name of the service to delete.</param>
        /// <returns>True if the service was successfully removed; otherwise, false.</returns>
        public bool Unregister(string serviceName)
        {
            return m_Services.Remove(serviceName);
        }

        /// <summary>
        /// Determines whether a service with the specified name exists in the service locator.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public bool Exists<T>()
        {
            return Exists(typeof(T).Name);
        }

        /// <summary>
        /// Determines whether a service with the specified name exists in the service locator.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public bool Exists(string serviceName)
        {
            return m_Services.ContainsKey(serviceName);
        }

        public void GetAsComponent<T>(out T service) where T : class
        {
            GetAsComponent<T>(typeof(T).Name, out service);
        }

        public void GetAsComponent<T>(string componentName, out T service) where T : class
        {
            service = Get<INetworkComponentService>(componentName).Component as T;
        }

        public T GetAsComponent<T>() where T : class
        {
            return GetAsComponent<T>(typeof(T).Name);
        }

        public T GetAsComponent<T>(string componentName) where T : class
        {
            return Get<INetworkComponentService>(componentName).Component as T;
        }

        public bool TryGetAsComponent<T>(out T service) where T : class
        {
            return TryGetAsComponent<T>(typeof(T).Name, out service);
        }

        public bool TryGetAsComponent<T>(string componentName, out T service) where T : class
        {
            service = null;
            bool success = TryGet<INetworkComponentService>(componentName, out var componentService) &&
                           componentService.Component is T;
            if (success)
            {
                service = componentService.Component as T;
            }

            return success;
        }

        public GameObject GetAsGameObject<T>()
        {
            return GetAsGameObject(typeof(T).Name);
        }

        public GameObject GetAsGameObject(string gameObjectName)
        {
            return Get<INetworkComponentService>(gameObjectName).GameObject;
        }

        public bool TryGetAsGameObject<T>(out GameObject service)
        {
            return TryGetAsGameObject(typeof(T).Name, out service);
        }

        public bool TryGetAsGameObject(string gameObjectName, out GameObject service)
        {
            service = null;
            bool success = TryGet<INetworkComponentService>(gameObjectName, out var componentService);
            if (success)
            {
                service = componentService.GameObject;
            }

            return success;
        }

        /// <summary>
        /// Requests destruction of this networked entity for all targeted clients.  
        /// Must be called on the server.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown if invoked on a non-server instance.
        /// </exception>
        public void Despawn(bool destroyInServer, Target target = Target.Auto, NetworkGroup group = null, NetworkPeer peer = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte seqChannel = 0)
        {
            if (!IsServer)
            {
                throw new NotSupportedException("Despawn() can only be called on the server.");
            }

            RequestActionToClient(NetworkConstants.k_DestroyEntityId, null, target, group, peer, deliveryMode, seqChannel);
            if (destroyInServer)
                Destroy(gameObject);
        }

        public NetworkIdentity SpawnOnServer(int peerId, EntityType entityType)
        {
            return SpawnOnServer(peerId, entityType == EntityType.Generic ? 0 : peerId);
        }

        /// <summary>
        /// Instantiates a network identity on the server.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peerId">The ID of the peer who will receive the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object. If not provided, a dynamic unique ID will be generated.</param>
        /// <returns>The instantiated network identity.</returns>
        public NetworkIdentity SpawnOnServer(int peerId, int identityId)
        {
            if (identityId < 0)
                throw new ArgumentException("Identity Id cannot be negative. Pass zero to generate a dynamic unique id.");

            if (identityId == 0)
                identityId = NetworkHelper.GenerateDynamicUniqueId();

            return NetworkHelper.SpawnAndRegister(this, NetworkManager.ServerSide.Peers[peerId], identityId, isServer: true, isLocalPlayer: false);
        }


        /// <summary>
        /// Instantiates a network identity on the client.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peerId">The ID of the peer who owns the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public NetworkIdentity SpawnOnClient(int peerId, int identityId)
        {
            bool isLocalPlayer = NetworkManager.LocalPeer.Id == peerId;
            NetworkIdentity @obj = NetworkHelper.SpawnAndRegister(
                this,
                peerId != 0
                    ? isLocalPlayer
                        ? NetworkManager.LocalPeer
                        : NetworkManager.ClientSide.GetOrCreatePeer(peerId)
                    : NetworkManager.ServerSide.ServerPeer,
                identityId,
                isServer: false,
                isLocalPlayer: isLocalPlayer
            );

            // Notify the server that this identity has been spawned on the client side.
            @obj.RequestActionToServer(NetworkConstants.k_SpawnNotificationId);
            return @obj;
        }

        public bool IsPlayerObject()
        {
            return name.Contains("Player", StringComparison.OrdinalIgnoreCase) || tag.Contains("Player", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds a network component of type <typeparamref name="T"/> to this network identity.
        /// The component is registered with the network system and initialized with the
        /// current network identity.
        /// This method does not synchronize the component across the network.
        /// </summary>
        /// <typeparam name="T">The type of the network component to add. Must be a subclass of <see cref="NetworkBehaviour"/>.</typeparam>
        public T AddNetworkComponent<T>(string serviceName = null) where T : NetworkBehaviour
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                serviceName = typeof(T).Name;
            }

            int length = GetComponentsInChildren<NetworkBehaviour>(true).Length;

            T behaviour = gameObject.AddComponent<T>();
            behaviour.ServiceName = serviceName;
            behaviour.Identity = this;
            behaviour.Id = (byte)(length + 1);

            behaviour.Register();
            behaviour.___InjectServices___();

            behaviour.OnAwake();
            behaviour.OnStart();

            if (IsLocalPlayer) behaviour.OnStartLocalPlayer();
            else if (IsPlayerObject())
            {
                behaviour.OnStartRemotePlayer();
            }

            return behaviour;
        }

        private const int k_TransitioningTime = 250;
        private const int k_InstantTransitioningTime = 2500;
        internal bool isOwnershipTransitioning = false;
        /// <summary>
        /// Sets the owner of the network identity to the specified network peer.
        /// </summary>
        /// <param name="peer">The new owner of the network identity.</param>
        /// <param name="target">
        /// Specifies the target(s) for the ownership change notification. Defaults to <see cref="Target.Auto"/>.
        /// </param>
        public async void TransferOwnership(NetworkPeer peer, Target target = Target.Auto, NetworkGroup group = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte seqChannel = 0)
        {
            if (IsServer)
            {
                NetworkPeer oldPeer = Owner;
                NetworkPeer newPeer = peer;

                if (newPeer.Equals(oldPeer))
                    return;

                if (isOwnershipTransitioning && !m_AllowInstantOwnershipTransfer)
                {
                    NetworkLogger.__Log__(
                        $"Ownership transfer ignored: another transfer is already in progress. " +
                        $"Object='{name}', CurrentOwner={Owner?.Id}, RequestedOwner={peer?.Id}",
                        NetworkLogger.LogType.Warning
                    );

                    return;
                }

                isOwnershipTransitioning = true;
                var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.Write(newPeer.Id);
                // Request action to old peer to notify it that it is no longer the owner.
                RequestActionToClient(NetworkConstants.k_SetOwnerId, message, Target.Self, peer: oldPeer);
                // Request action to new peer to notify it that it is now the owner.
                RequestActionToClient(NetworkConstants.k_SetOwnerId, message, Target.Self, peer: newPeer);
                Owner = newPeer;
                IsOwnedByServer = Owner.Id == NetworkManager.ServerSide.ServerPeer.Id;
                OnServerOwnershipTransferred?.Invoke(oldPeer, newPeer);
                message.Dispose();
                // Wait for the ownership transition to complete, block all warnings and errors.
                await UniTask.Delay(m_AllowInstantOwnershipTransfer ? k_InstantTransitioningTime : k_TransitioningTime);
                isOwnershipTransitioning = false;
            }
            else
            {
                throw new Exception(
                    $"Operation denied: Only the server can set the owner of the game object '{name}'. Please ensure this action is performed on the server."
                );
            }
        }

        public void RequestActionToServer(byte actionId, DataBuffer data = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte seqChannel = 0)
        {
            if (IsServer)
            {
                throw new NotSupportedException(
                    "RequestAction failed: This operation is only allowed on the client. It invokes a remote action on the server-side entity, triggered by a client-side entity.");
            }

            using var message = NetworkManager.Pool.Rent(enableTracking: false);
            message.Write(m_Id);
            message.Write(actionId);
            message.Internal_CopyFrom(data);

            NetworkManager.ClientSide.SetDeliveryMode(deliveryMode);
            NetworkManager.ClientSide.SetSequenceChannel(seqChannel);
            NetworkManager.ClientSide.SendMessage(NetworkPacketType.k_RequestEntityAction, message);
        }

        public void RequestActionToClient(byte actionId, DataBuffer data = null, Target target = Target.Auto, NetworkGroup group = null, NetworkPeer peer = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte seqChannel = 0)
        {
            if (IsClient)
            {
                throw new NotSupportedException(
                    "RequestAction failed: This operation is only allowed on the server. It invokes a remote action on the client-side entity, triggered by a server-side entity.");
            }

            using var message = NetworkManager.Pool.Rent(enableTracking: false);
            message.Write(m_Id);
            message.Write(actionId);
            message.Internal_CopyFrom(data);

            NetworkManager.ServerSide.SetDefaultNetworkConfiguration(deliveryMode, target, group, seqChannel);
            NetworkManager.ServerSide.SendMessage(NetworkPacketType.k_RequestEntityAction, peer ?? Owner, message);
        }

        private void Reset()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            m_NetworkBehaviours = GetComponentsInChildren<NetworkBehaviour>(true).ToList();
        }

        public override bool Equals(object obj)
        {
            if (Application.isPlaying)
            {
                if (obj is null)
                    return false;

                if (ReferenceEquals(this, obj))
                    return true;

                if (obj is NetworkIdentity other)
                {
                    return Id == other.Id;
                }
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            if (Application.isPlaying)
            {
                return Id.GetHashCode();
            }

            return base.GetHashCode();
        }

        public bool Equals(NetworkIdentity other)
        {
            if (Application.isPlaying)
            {
                return Id == other.Id;
            }

            return false;
        }
    }
}