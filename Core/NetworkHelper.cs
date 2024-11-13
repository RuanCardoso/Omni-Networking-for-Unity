using Newtonsoft.Json;
using Omni.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Omni.Core
{
	public static class NetworkHelper
	{
		private static int d_UniqueId = 1; // 0 - is reserved for server

		// the chances of collision are low, so it's fine to use hashcode.
		// because... do you have billions of network objects in the scene?
		// scene objects are different from dynamic objects(instantiated);
		internal static int GenerateSceneUniqueId()
		{
			Guid newGuid = Guid.NewGuid();
			return newGuid.GetHashCode();
		}

		// Used for dynamic objects (instantiated).
		// The chances of collision is zero(0) because each ID is unique(incremental).
		internal static int GenerateDynamicUniqueId()
		{
			if (d_UniqueId == 0)
			{
				d_UniqueId = 1;
			}

			return d_UniqueId++;
		}

		internal static void Destroy(int identityId, bool isServer)
		{
			var identities = isServer
				? NetworkManager.Server.Identities
				: NetworkManager.Client.Identities;

			if (identities.Remove(identityId, out var identity))
			{
				if (!isServer && NetworkIdentity.LocalPlayer != null)
				{
					if (NetworkIdentity.LocalPlayer.IdentityId == identityId)
					{
						NetworkIdentity.LocalPlayer = null;
					}
				}

				NetworkBehaviour[] networkBehaviours =
					identity.GetComponentsInChildren<NetworkBehaviour>(true);

				for (int i = 0; i < networkBehaviours.Length; i++)
				{
					networkBehaviours[i].Unregister();
				}

				UnityEngine.Object.Destroy(identity.gameObject);
			}
			else
			{
				NetworkLogger.__Log__(
					$"Server Destroy: Identity with ID {identity.IdentityId} not found.",
					NetworkLogger.LogType.Error
				);
			}
		}

		internal static NetworkIdentity Instantiate(
			NetworkIdentity prefab,
			NetworkPeer peer,
			int identityId,
			bool isServer,
			bool isLocalPlayer
		)
		{
			// Disable the prefab to avoid Awake and Start being called multiple times before the registration.
			prefab.gameObject.SetActive(false);

			NetworkIdentity identity = UnityEngine.Object.Instantiate(prefab);
			identity.IdentityId = identityId;
			identity.Owner = peer;
			identity.IsServer = isServer;
			identity.IsLocalPlayer = isLocalPlayer;
			identity.IsServerOwner = identity.Owner.Id == NetworkManager.Server.ServerPeer.Id;
			identity._prefabName = prefab.name;
			identity.name = $"{prefab.name}(On {(isServer ? "Server" : "Client")})";

			NetworkBehaviour[] networkBehaviours =
				identity.GetComponentsInChildren<NetworkBehaviour>(true);

			for (int i = 0; i < networkBehaviours.Length; i++)
			{
				NetworkBehaviour networkBehaviour = networkBehaviours[i];
				networkBehaviour.Identity = identity;

				if (networkBehaviour.Id == 0)
				{
					networkBehaviour.Id = (byte)(i + 1);
				}

				networkBehaviour.Register();
				networkBehaviour.OnAwake();
			}

			var identities = isServer
				? NetworkManager.Server.Identities
				: NetworkManager.Client.Identities;

			if (!identities.TryAdd(identity.IdentityId, identity))
			{
				NetworkIdentity oldRef = identities[identity.IdentityId];
				MonoBehaviour.Destroy(oldRef.gameObject);
				// Update the reference.....
				identities[identity.IdentityId] = identity;

				NetworkLogger.__Log__($"A Identity with Id: '{identity.IdentityId}' already exists. The old reference has been destroyed and replaced with the new one.",
					NetworkLogger.LogType.Warning);
			}

			if (IsPrefab(prefab.gameObject))
			{
				prefab.gameObject.SetActive(true); // After registration, enable the prefab again.
			}

			identity.gameObject.SetActive(true); // Enable instantiated object!
												 // After Start
			foreach (var behaviour in networkBehaviours)
			{
				behaviour.OnStart();
				// Checks if the current player is the local player
				// If true, calls the OnStartLocalPlayer method to handle any local player-specific setup
				// If false, calls the OnStartRemotePlayer method to handle any setup specific to remote players
				if (isLocalPlayer)
				{
					behaviour.OnStartLocalPlayer();
				}
				else
				{
					behaviour.OnStartRemotePlayer();
				}
			}

			return identity;
		}

		internal static bool IsPortAvailable(int port, ProtocolType protocolType, bool useIPv6)
		{
			try
			{
				if (protocolType == ProtocolType.Udp)
				{
					using Socket socket = new Socket(
						useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
						SocketType.Dgram,
						ProtocolType.Udp
					);

					if (useIPv6)
					{
						socket.DualMode = true;
					}

					socket.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
					socket.Close();
				}
				else if (protocolType == ProtocolType.Tcp)
				{
					using Socket socket = new Socket(
						useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
						SocketType.Stream,
						ProtocolType.Tcp
					);

					if (useIPv6)
					{
						socket.DualMode = true;
					}

					socket.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
					socket.Close();
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static int GetAvailablePort(int port, bool useIPv6)
		{
			while (!IsPortAvailable(port, ProtocolType.Udp, useIPv6))
			{
				port++;
				if (port > 65535)
				{
					port = 7777;
				}
			}

			return port;
		}

		[Conditional("OMNI_DEBUG")]
		internal static void ThrowAnErrorIfIsInternalTypes<T>(T type) where T : unmanaged
		{
			if (type is Target || type is DeliveryMode || type is CacheMode)
			{
				throw new InvalidOperationException("Internal types are not allowed as arguments for the DataBuffer. If this was not intentional, please consider using a different overload.");
			}
		}

		public static async Task<IPAddress> GetExternalIp(bool useIPv6)
		{
			try
			{
				using var httpClient = new HttpClient();
				string externalIp = (
					await httpClient.GetStringAsync(
						useIPv6 ? "http://ipv6.icanhazip.com/" : "http://ipv4.icanhazip.com/"
					)
				);

				externalIp = externalIp.Replace("\\r\\n", "");
				externalIp = externalIp.Replace("\\n", "");
				externalIp = externalIp.Trim();

				if (!IPAddress.TryParse(externalIp, out var ipAddress))
				{
					return IPAddress.Loopback;
				}

				return ipAddress;
			}
			catch
			{
				return IPAddress.Loopback;
			}
		}

		[Conditional("OMNI_DEBUG")]
		internal static void EnsureRunningOnMainThread()
		{
			if (NetworkManager.MainThreadId != Thread.CurrentThread.ManagedThreadId)
			{
				throw new Exception(
					"This operation must be performed on the main thread. Omni does not support multithreaded operations. Tip: Dispatch the events to the main thread."
				);
			}
		}

		/// <summary>
		/// Checks if the given <see cref="GameObject"/> is set to dont destroy on load.
		/// </summary>
		/// <param name="gameObject"></param>
		/// <returns>Returns true if the given <see cref="GameObject"/> is set to dont destroy on load.</returns>
		public static bool IsDontDestroyOnLoad(GameObject gameObject)
		{
			GameObject root = gameObject.transform.root.gameObject;
			return root.scene.name == "DontDestroyOnLoad"
				|| root.TryGetComponent<NetworkManager>(out _);
		}

		/// <summary>
		/// Checks if the given GameObject is a prefab.
		/// </summary>
		/// <param name="obj">The GameObject to check.</param>
		/// <returns>True if the GameObject is a prefab, false otherwise.</returns>
		public static bool IsPrefab(GameObject obj)
		{
			return obj.scene.name == null || obj.scene.name.ToLower() == "null";
		}

		/// <summary>
		/// Saves the configuration of a given component to a file in JSON format.
		/// This method is intended to be used only on the server side.
		/// </summary>
		/// <typeparam name="T">The type of the component to be saved.</typeparam>
		/// <param name="component">The component instance to be saved.</param>
		/// <param name="fileName">The name of the file where the component's configuration will be saved.</param>
#if OMNI_DEBUG
		[Conditional("UNITY_STANDALONE"), Conditional("UNITY_EDITOR")]
#else
        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
#endif
		public static void SaveComponent<T>(T component, string fileName)
		{
			using StreamWriter writer = new(fileName, false);
			writer.Write(NetworkManager.ToJson(component));
		}

		/// <summary>
		/// Loads the configuration from a file and populates the target object with this data.
		/// This method is intended to be used only on the server side.
		/// </summary>
		/// <param name="target">The object to be populated with the configuration data.</param>
		/// <param name="fileName">The name of the file from which the configuration will be read.</param>
#if OMNI_DEBUG
		[Conditional("UNITY_STANDALONE"), Conditional("UNITY_EDITOR")]
#else
        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
#endif
		public static void LoadComponent(object target, string fileName)
		{
			if (File.Exists(fileName))
			{
				try
				{
					using StreamReader reader = new(fileName);
					JsonConvert.PopulateObject(reader.ReadToEnd(), target);
				}
				catch
				{
					File.Delete(fileName);
				}
			}
		}

		public static void EditorSaveObject(GameObject target)
		{
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(target);
#endif
		}
	}
}
