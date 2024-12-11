using Omni.Shared;
using Omni.Threading.Tasks;
using OpenNat;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    public static class NetworkHelper
    {
        private static int d_UniqueId = 1; // 0 - is reserved for server

        internal static async Task<bool> OpenPortAsync(int port, Protocol protocol)
        {
            const int lifetime = 86400; // Seconds - 86400(1 day/24 hours).

            try
            {
                using CancellationTokenSource cts = new CancellationTokenSource(2500);
                NatDevice device = await NatDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp | PortMapper.Pmp, cts);

                if (device == null)
                    return false;

                var mapping = new Mapping(protocol, port, port, lifetime, "Omni Networking");
                await device.CreatePortMapAsync(mapping);

                // Automatic renewal of mappings every N seconds.
                UniTask.Void(async () =>
                {
                    while (Application.isPlaying)
                    {
                        if (mapping.IsExpired())
                        {
                            mapping.Expiration = DateTime.UtcNow.AddSeconds(mapping.Lifetime);
                            await device.CreatePortMapAsync(mapping);
                            NetworkLogger.Print(
                                $"[Port Forwarding] Renewed mapping - ({mapping.Description}) | ({mapping.Protocol}:{mapping.PublicPort} -> {mapping.Protocol}:{mapping.PrivatePort})",
                                NetworkLogger.LogType.Log);
                        }

                        // Check if the port has is expired every.....
                        await UniTask.Delay(500);
                    }
                });

                // Return true if the port is open.
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

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
                ? NetworkManager.ServerSide.Identities
                : NetworkManager.ClientSide.Identities;

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
                    $"[Error] Failed to Destroy: Network Identity with ID '{identity.IdentityId}' was not found in the {(isServer ? "Server" : "Client")} identities. This might indicate a desynchronization issue or an invalid operation.",
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
            identity.IsServerOwner = identity.Owner.Id == NetworkManager.ServerSide.ServerPeer.Id;
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

                // Register on the network and add to the service locator.
                networkBehaviour.Register();
            }

            // After register all behaviours, call the OnAwake method.
            foreach (var behaviour in networkBehaviours)
            {
                behaviour.___InjectServices___();
                behaviour.OnAwake();
            }

            var identities = isServer
                ? NetworkManager.ServerSide.Identities
                : NetworkManager.ClientSide.Identities;

            if (!identities.TryAdd(identity.IdentityId, identity))
            {
                NetworkIdentity oldRef = identities[identity.IdentityId];
                MonoBehaviour.Destroy(oldRef.gameObject);
                // Update the reference.....
                identities[identity.IdentityId] = identity;

                NetworkLogger.__Log__(
                    $"An identity conflict occurred. Identity with Id: '{identity.IdentityId}' already exists. The old instance has been destroyed and replaced with the new one to maintain consistency.",
                    NetworkLogger.LogType.Warning);
            }

            if (IsPrefab(prefab.gameObject))
            {
                prefab.gameObject.SetActive(true); // After registration, enable the prefab again.
            }

            // Enable instantiated object!
            identity.gameObject.SetActive(true);

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
                    using Socket socket =
                        new Socket(useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);

                    if (useIPv6)
                    {
                        socket.DualMode = true;
                    }

                    socket.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
                    socket.Close();
                }
                else if (protocolType == ProtocolType.Tcp)
                {
                    using Socket socket =
                        new Socket(useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp);

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
            if (type is Target or DeliveryMode or CacheMode)
            {
                throw new InvalidOperationException(
                    "The type provided is internal and not permitted as an argument for the DataBuffer. If this was not intentional, use an appropriate alternative overload or consult the documentation for further guidance.");
            }
        }

        /// <summary>
        /// Retrieves the external IP address of the device asynchronously.
        /// </summary>
        /// <param name="useIPv6">A boolean indicating whether to retrieve the IPv6 address or the IPv4 address.</param>
        /// <returns>A Task representing the asynchronous operation, containing the external IP address as an IPAddress object.</returns>
        public static async Task<IPAddress> GetExternalIpAsync(bool useIPv6)
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
                    return !useIPv6 ? IPAddress.Loopback : IPAddress.IPv6Loopback;
                }

                return ipAddress;
            }
            catch
            {
                return !useIPv6 ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            }
        }

        /// <summary>
        /// Runs the specified action on the main thread.
        /// </summary>
        /// <param name="action">The action to be executed on the main thread.</param>
        public static async void RunOnMainThread(Action action)
        {
            await UniTask.SwitchToMainThread();
            // Run on main thread
            action();
        }

        /// <summary>
        /// Schedules the specified action to be executed on the main thread asynchronously.
        /// </summary>
        /// <param name="action">The action to be executed on the main thread.</param>
        public static async UniTask<T> RunOnMainThread<T>(Func<T> func)
        {
            await UniTask.SwitchToMainThread();
            // Run on main thread
            return func();
        }

        /// <summary>
        /// Checks whether the current thread is the main thread.
        /// </summary>
        /// <returns>True if the current thread is the main thread, false otherwise.</returns>
        public static bool IsRunningOnMainThread()
        {
            return NetworkManager.MainThreadId == Thread.CurrentThread.ManagedThreadId;
        }

        [Conditional("OMNI_DEBUG")]
        internal static void EnsureRunningOnMainThread()
        {
            if (!IsRunningOnMainThread())
            {
                NetworkLogger.__Log__(
                    "Operation must run on the main thread. Multi-threading is not supported in Omni. " +
                    "Hint: Use main thread dispatching to handle this operation.", NetworkLogger.LogType.Error);

                throw new NotSupportedException(
                    "Operation must run on the main thread. Multi-threading is not supported in Omni. Hint: Use main thread dispatching to handle this operation."
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
        /// Returns the minimum value between the given value and the specified minimum value.
        /// If the given value is less than the minimum value, returns 0; otherwise, returns the difference between the given value and the minimum value.
        /// </summary>
        /// <param name="value">The value to be compared with the minimum value.</param>
        /// <param name="min">The minimum value to compare with.</param>
        /// <returns>The minimum value or the difference between the given value and the minimum value.</returns>
        internal static double MinMax(double value, double min)
        {
            return value < min ? 0 : value - min;
        }

        //		/// <summary>
        //		/// Saves the configuration of a given component to a file in JSON format.
        //		/// This method is intended to be used only on the server side.
        //		/// </summary>
        //		/// <typeparam name="T">The type of the component to be saved.</typeparam>
        //		/// <param name="component">The component instance to be saved.</param>
        //		/// <param name="fileName">The name of the file where the component's configuration will be saved.</param>
        //#if OMNI_DEBUG
        //		[Conditional("UNITY_STANDALONE"), Conditional("UNITY_EDITOR")]
        //#else
        //        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
        //#endif
        //		public static void SaveComponent<T>(T component, string fileName)
        //		{
        //			using StreamWriter writer = new(fileName, false);
        //			writer.Write(NetworkManager.ToJson(component));
        //		}

        //		/// <summary>
        //		/// Loads the configuration from a file and populates the target object with this data.
        //		/// This method is intended to be used only on the server side.
        //		/// </summary>
        //		/// <param name="target">The object to be populated with the configuration data.</param>
        //		/// <param name="fileName">The name of the file from which the configuration will be read.</param>
        //#if OMNI_DEBUG
        //		[Conditional("UNITY_STANDALONE"), Conditional("UNITY_EDITOR")]
        //#else
        //        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
        //#endif
        //		public static void LoadComponent(object target, string fileName)
        //		{
        //			if (File.Exists(fileName))
        //			{
        //				try
        //				{
        //					using StreamReader reader = new(fileName);
        //					JsonConvert.PopulateObject(reader.ReadToEnd(), target);
        //				}
        //				catch
        //				{
        //					File.Delete(fileName);
        //				}
        //			}
        //		}

        public static void EditorSaveObject(GameObject target)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
#endif
        }
    }
}