using Omni.Core.Attributes;
using Omni.Core.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;
#if OMNI_RELEASE
using System.Runtime.CompilerServices;
#endif

namespace Omni.Core
{
	public sealed class NetworkIdentity : MonoBehaviour, IEquatable<NetworkIdentity>
	{
		internal string _prefabName;
		internal Action OnSpawn;
		internal Action<DataBuffer> OnRequestAction;

		private readonly Dictionary<string, object> m_Services = new(); // (Service Name, Service Instance) exclusively to identity

		[SerializeField]
		[ReadOnly]
		private int m_Id;

		[SerializeField]
		[ReadOnly]
		private bool m_IsServer;

		[SerializeField]
		[ReadOnly]
		private bool m_IsLocalPlayer;

		[SerializeField]
		[ReadOnly]
		private bool isServerOwner;

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
		/// Owner of this object. Only available on server, returns <c>NetworkManager.LocalPeer</c> on client.
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
			get { return isServerOwner; }
			internal set { isServerOwner = value; }
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
		public bool TryGet<T>(string serviceName, out T service)
			where T : class
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
		public T Get<T>()
			where T : class
		{
			return Get<T>(typeof(T).Name);
		}

		/// <summary>
		/// Attempts to retrieve a service instance by its type from the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to retrieve.</typeparam>
		/// <param name="service">When this method returns, contains the service instance cast to the specified type if the service is found; otherwise, the default value for the type of the service parameter.</param>
		/// <returns>True if the service is found and successfully cast to the specified type; otherwise, false.</returns>
		public bool TryGet<T>(out T service)
			where T : class
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
		public bool TryUpdate<T>(T service, string serviceName)
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
		public bool Unregister(string serviceName)
		{
			return m_Services.Remove(serviceName);
		}

		public void GetAsComponent<T>(out T service)
			where T : class
		{
			GetAsComponent<T>(typeof(T).Name, out service);
		}

		public void GetAsComponent<T>(string componentName, out T service)
			where T : class
		{
			service = Get<INetworkComponentService>(componentName).Component as T;
		}

		public T GetAsComponent<T>()
			where T : class
		{
			return GetAsComponent<T>(typeof(T).Name);
		}

		public T GetAsComponent<T>(string componentName)
			where T : class
		{
			return Get<INetworkComponentService>(componentName).Component as T;
		}

		public bool TryGetAsComponent<T>(out T service)
			where T : class
		{
			return TryGetAsComponent<T>(typeof(T).Name, out service);
		}

		public bool TryGetAsComponent<T>(string componentName, out T service)
			where T : class
		{
			service = null;
			bool success =
				TryGet<INetworkComponentService>(componentName, out var componentService)
				&& componentService.Component is T;

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
			bool success = TryGet<INetworkComponentService>(
				gameObjectName,
				out var componentService
			);

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
		public void SpawnOnClient(SyncOptions options)
		{
			SpawnOnClient(
				options.Target,
				options.DeliveryMode,
				options.GroupId,
				options.DataCache,
				options.SequenceChannel
			);
		}

		/// <summary>
		/// Automatic instantiates a network identity on the client.
		/// </summary>
		/// <returns>The instantiated network identity.</returns>
		public void SpawnOnClient(
			Target target = Target.All,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0
		)
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
				throw new Exception($"Only server can spawn the game object '{name}'.");
			}

			using var message = NetworkManager.Pool.Rent();
			message.WriteString(_prefabName);
			message.WriteIdentity(this);
			NetworkManager.Server.SendMessage(
				MessageType.Spawn,
				Owner,
				message,
				target,
				deliveryMode,
				groupId,
				dataCache,
				sequenceChannel
			);
		}

		/// <summary>
		/// Automatic destroys a network identity on the client.
		/// </summary>
		/// <returns>The instantiated network identity.</returns>
		public void Destroy(SyncOptions options)
		{
			Destroy(
				options.Target,
				options.DeliveryMode,
				options.GroupId,
				options.DataCache,
				options.SequenceChannel
			);
		}

		/// <summary>
		/// Automatic destroys a network identity on the client and server for a specific peer.
		/// </summary>
		/// <returns>The instantiated network identity.</returns>
		public void DestroyByPeer(NetworkPeer peer,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			DataCache dataCache = default,
			byte sequenceChannel = 0)
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
				throw new Exception($"Only server can destroy the game object '{name}'.");
			}

			using var message = NetworkManager.Pool.Rent();
			message.Write(m_Id);
			NetworkManager.Server.SendMessage(
				MessageType.Destroy,
				peer,
				message,
				Target.Self,
				deliveryMode,
				0,
				dataCache,
				sequenceChannel
			);

			NetworkHelper.Destroy(m_Id, IsServer);
		}

		/// <summary>
		/// Automatic destroys a network identity on the client and server.
		/// </summary>
		/// <returns>The instantiated network identity.</returns>
		public void Destroy(
			Target target = Target.All,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0
		)
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
				throw new Exception($"Only server can destroy the game object '{name}'.");
			}

			using var message = NetworkManager.Pool.Rent();
			message.Write(m_Id);
			NetworkManager.Server.SendMessage(
				MessageType.Destroy,
				Owner,
				message,
				target,
				deliveryMode,
				groupId,
				dataCache,
				sequenceChannel
			);

			NetworkHelper.Destroy(m_Id, IsServer);
		}

		/// <summary>
		/// Destroys a network identity only on the client, but the object will be destroyed only for you.
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
				throw new Exception($"Only client can destroy the game object '{name}'. But the object will be destroyed only for you.");
			}

			NetworkHelper.Destroy(IdentityId, false);
		}

		/// <summary>
		/// Sets the owner of the network identity to the specified peer.
		/// </summary>
		/// <param name="peer">The new owner of the network identity.</param>
		public void SetOwner(
			NetworkPeer peer,
			Target target = Target.All,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0
		)
		{
			dataCache ??= DataCache.None;
			if (IsServer)
			{
				Owner = peer;
				// Send the new owner to the client's
				using var message = NetworkManager.Pool.Rent();
				message.Write(m_Id);
				message.Write(peer.Id);
				NetworkManager.Server.SendMessage(
					MessageType.SetOwner,
					Owner,
					message,
					target,
					deliveryMode,
					groupId,
					dataCache,
					sequenceChannel
				);
			}
			else
			{
				throw new Exception(
					$"Only server can set the owner of the game object -> '{name}'."
				);
			}
		}

		/// <summary>
		/// Invokes a remote action on the server-side entity, triggered by a client-side entity. 
		/// </summary>
		public void RequestAction(DataBuffer data = default, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
		{
			if (IsServer)
			{
				throw new NotSupportedException("Only the client can request this action. Obs: Invokes a remote action on the server-side entity, triggered by a client-side entity.");
			}

			data ??= DataBuffer.Empty;
			using var message = NetworkManager.Pool.Rent();
			message.Write(m_Id);
			message.RawWrite(data.BufferAsSpan);
			NetworkManager.Client.SendMessage(MessageType.RequestEntityAction, message, deliveryMode, sequenceChannel);
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
