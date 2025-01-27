using Omni.Core.Interfaces;
using System;
using System.Collections.Generic;
using Omni.Inspector;
using UnityEngine;
#if OMNI_RELEASE
using System.Runtime.CompilerServices;
#endif

#pragma warning disable

namespace Omni.Core
{
    [DeclareBoxGroup("Infor")]
    public sealed class NetworkIdentity : MonoBehaviour, IEquatable<NetworkIdentity>
    {
        internal string _prefabName;
        internal Action<NetworkPeer> OnSpawn;
        internal Action<DataBuffer> OnRequestAction;

        // (Service Name, Service Instance) exclusively to identity
        private readonly Dictionary<string, object> m_Services = new();

        [SerializeField][ReadOnly] private int m_Id;

        [SerializeField]
        [ReadOnly]
        [LabelWidth(150)]
        [Group("Infor")]
        private bool m_IsServer;

        [SerializeField]
        [ReadOnly]
        [LabelWidth(150)]
        [Group("Infor")]
        private bool m_IsLocalPlayer;

        [SerializeField]
        [ReadOnly]
        [LabelWidth(150)]
        [Group("Infor")]
        private bool isOwnedByTheServer;

        public int IdentityId
        {
            get { return m_Id; }
            internal set { m_Id = value; }
        }

        /// <summary>
        /// The local player instance. Set on the client(only).
        /// </summary>
        public static NetworkIdentity LocalPlayer { get; internal set; }

        /// <summary>
        /// The owner of this object, available on both server and client side. 
        /// On the client side, only a few properties are accessible, such as <c>SharedData</c>.
        /// </summary>
        public NetworkPeer Owner { get; internal set; }

        /// <summary>
        /// Indicates whether this object is obtained from the server side or checked on the client side.
        /// True if the object is obtained from the server side, false if it is checked on the client side.
        /// </summary>
        public bool IsServer
        {
            get { return m_IsServer; }
            internal set { m_IsServer = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is on the client side.
        /// </summary>
        /// <value><c>true</c> if this instance is on the client side; otherwise, <c>false</c>.</value>
        public bool IsClient => !IsServer;

        /// <summary>
        /// Indicates whether this object is owned by the local player.
        /// </summary>
        public bool IsLocalPlayer
        {
            get { return m_IsLocalPlayer; }
            internal set { m_IsLocalPlayer = value; }
        }

        /// <summary>
        /// Indicates whether this NetworkIdentity is registered.
        /// </summary>
        public bool IsRegistered
        {
            get { return Owner != null && m_Id != 0; }
        }

        /// <summary>
        /// Indicates whether this identity is owned by the server.
        /// </summary>
        public bool IsServerOwner
        {
            get { return isOwnedByTheServer; }
            internal set { isOwnedByTheServer = value; }
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
        /// Automatic instantiates a network identity on the client.
        /// </summary>
        /// <returns>The instantiated network identity.</returns>
        public void SpawnOnClient(ServerOptions options)
        {
            SpawnOnClient(options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                options.SequenceChannel);
        }

        /// <summary>
        /// Automatic spawns a network identity on the client.
        /// </summary>
        /// <returns>The instantiated network identity.</returns>
        public void SpawnOnClient(Target target = Target.Auto, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            if (!IsRegistered)
            {
                throw new Exception(
                    $"The game object '{name}' is not registered. Please register it first."
                );
            }

            if (!IsServer)
            {
                throw new InvalidOperationException(
                    $"Operation failed: Only the server is authorized to spawn the game object '{name}'. Ensure the operation is being performed on the server.");
            }

            using var message = NetworkManager.Pool.Rent();
            message.WriteString(_prefabName);
            message.WriteIdentity(this);
            NetworkManager.ServerSide.SendMessage(MessageType.Spawn, Owner, message, target, deliveryMode, groupId,
                dataCache, sequenceChannel);
        }

        /// <summary>
        /// Despawns the network identity using the specified server synchronization options.
        /// </summary>
        /// <param name="options">The server options containing target, delivery mode, group ID, data cache, and sequence channel settings for despawning.</param>
        public void Despawn(ServerOptions options)
        {
            Despawn(options.Target, options.DeliveryMode, options.GroupId, options.DataCache, options.SequenceChannel);
        }

        /// <summary>
        /// Despawns the network identity for a specific peer with specified delivery options.
        /// </summary>
        public void DespawnToPeer(NetworkPeer peer, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            DataCache dataCache = default, byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            if (!IsRegistered)
            {
                throw new Exception(
                    $"The game object '{name}' is not registered. Please register it first."
                );
            }

            if (!IsServer)
            {
                throw new InvalidOperationException(
                    $"Operation failed: The game object '{name}' can only be despawned by the server. Ensure the operation is being executed on the server.");
            }

            using var message = NetworkManager.Pool.Rent();
            message.Write(m_Id);
            NetworkManager.ServerSide.SendMessage(MessageType.Despawn, peer, message, Target.SelfOnly, deliveryMode, 0,
                dataCache, sequenceChannel);

            NetworkHelper.Destroy(m_Id, IsServer);
        }

        /// <summary>
        /// Despawns the network identity for all connected clients with specified delivery options.
        /// </summary>
        public void Despawn(Target target = Target.Auto, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            if (!IsRegistered)
            {
                throw new Exception(
                    $"The game object '{name}' is not registered. Please register it first."
                );
            }

            if (!IsServer)
            {
                throw new InvalidOperationException(
                    $"Operation failed: The game object '{name}' can only be despawned by the server. Ensure the operation is being executed on the server.");
            }

            using var message = NetworkManager.Pool.Rent();
            message.Write(m_Id);
            NetworkManager.ServerSide.SendMessage(MessageType.Despawn, Owner, message, target, deliveryMode, groupId,
                dataCache, sequenceChannel);

            NetworkHelper.Destroy(m_Id, IsServer);
        }

        /// <summary>
        /// Destroys a network identity on the client only. This action is local to the client 
        /// and does not synchronize across the network or affect other clients.
        /// </summary>
        public void DestroyOnClient()
        {
            if (!IsRegistered)
            {
                throw new Exception(
                    $"The game object '{name}' is not registered. Please register it first."
                );
            }

            if (IsServer)
            {
                throw new Exception(
                    $"Only client can destroy the game object '{name}'. But the object will be destroyed only for you(local).");
            }

            NetworkHelper.Destroy(IdentityId, false);
        }

        /// <summary>
        /// Destroys a network identity on the server only. This action is local to the server 
        /// and does not synchronize across the network or affect other clients.
        /// </summary>
        public void DestroyOnServer()
        {
            if (!IsRegistered)
            {
                throw new Exception(
                    $"The game object '{name}' is not registered. Please register it first."
                );
            }

            if (!IsServer)
            {
                throw new InvalidOperationException(
                    $"Operation failed: Only the server is authorized to destroy the game object '{name}'. Ensure the operation is being performed on the server.");
            }

            NetworkHelper.Destroy(IdentityId, true);
        }

        /// <summary>
        /// Destroys the network identity, determining the appropriate behavior based on the current context.
        /// If called on the server, it destroys the object locally on the server.
        /// If called on a client, it destroys the object locally on that client.
        /// This method does not synchronize the destruction across the network.
        /// </summary>
        public void Destroy()
        {
            if (IsServer)
            {
                DestroyOnServer();
            }
            else
            {
                DestroyOnClient();
            }
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
            else behaviour.OnStartRemotePlayer();

            return behaviour;
        }

        /// <summary>
        /// Sets the owner of the network identity to the specified network peer.
        /// </summary>
        /// <param name="peer">The new owner of the network identity.</param>
        /// <param name="target">
        /// Specifies the target(s) for the ownership change notification. Defaults to <see cref="Target.Auto"/>.
        /// </param>
        public void SetOwner(NetworkPeer peer, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            if (IsServer)
            {
                Owner = peer;
                // Send the new owner to the client's
                using var message = NetworkManager.Pool.Rent();
                message.Write(m_Id);
                message.Write(peer.Id);
                NetworkManager.ServerSide.SendMessage(MessageType.SetOwner, Owner, message, target, deliveryMode,
                    groupId, dataCache, sequenceChannel);
            }
            else
            {
                throw new Exception(
                    $"Operation denied: Only the server can set the owner of the game object '{name}'. Please ensure this action is performed on the server."
                );
            }
        }

        /// <summary>
        /// Invokes a remote action on the server-side entity, triggered by a client-side entity. 
        /// </summary>
        public void RequestAction(DataBuffer data = default, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0)
        {
            if (IsServer)
            {
                throw new NotSupportedException(
                    "RequestAction failed: This operation is only allowed on the client. It invokes a remote action on the server-side entity, triggered by a client-side entity.");
            }

            data ??= DataBuffer.Empty;
            using var message = NetworkManager.Pool.Rent();
            message.Write(m_Id);
            message.WriteRawBytes(data.BufferAsSpan);
            NetworkManager.ClientSide.SendMessage(MessageType.RequestEntityAction, message, deliveryMode,
                sequenceChannel);
        }

        public override bool Equals(object obj)
        {
            if (Application.isPlaying)
            {
                if (obj is NetworkIdentity other)
                {
                    return IdentityId == other.IdentityId;
                }
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            if (Application.isPlaying)
            {
                return IdentityId.GetHashCode();
            }

            return base.GetHashCode();
        }

        public bool Equals(NetworkIdentity other)
        {
            if (Application.isPlaying)
            {
                return IdentityId == other.IdentityId;
            }

            return false;
        }
    }
}