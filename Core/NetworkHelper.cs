using DG.Tweening;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Omni.Shared;
using Omni.Threading.Tasks;
using OpenNat;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

#pragma warning disable

namespace Omni.Core
{
    public static class NetworkHelper
    {
        private static readonly Random random = new Random();

        private const string randomString =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{};:,.<>/?`~";

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

        /// <summary>
        /// Generates a random token string of variable length between 16 and 128 characters.
        /// </summary>
        /// <remarks>
        /// The token is constructed using random characters from a predefined string and is then
        /// encoded in Base64 format to ensure safe transmission over protocols that require plain-text data.
        /// </remarks>
        /// <returns>A Base64 encoded string representing the random token.</returns>
        internal static string GenerateRandomToken()
        {
            StringBuilder tokenBuilder = new StringBuilder();
            int length = random.Next(16, 128);
            while (tokenBuilder.Length < length)
                tokenBuilder.Append(randomString[random.Next(0, randomString.Length)]);

            byte[] base64bytes = Encoding.UTF8.GetBytes(tokenBuilder.ToString());
            return Convert.ToBase64String(base64bytes);
        }

        internal static void Destroy(int identityId, bool isServer, bool isRoot = true)
        {
            var identities = isServer
                ? NetworkManager.ServerSide.Identities
                : NetworkManager.ClientSide.Identities;

            if (identities.Remove(identityId, out NetworkIdentity identity))
            {
                if (!isServer && NetworkIdentity.LocalPlayer != null)
                {
                    if (NetworkIdentity.LocalPlayer.Id == identityId)
                    {
                        NetworkIdentity.LocalPlayer = null;
                    }
                }

                // When destroying a root object, all its child NetworkIdentity components 
                // and nested children will be recursively destroyed as well (handled below)

                if (isRoot)
                {
                    var behaviours = identity.GetComponentsInChildren<NetworkBehaviour>(true);
                    foreach (NetworkBehaviour behaviour in behaviours)
                    {
                        behaviour.Unregister();
                    }
                }

                // When destroying a root object, all its child NetworkIdentity components 
                // and nested children will be recursively destroyed as well (handled below)

                if (isRoot)
                {
                    var recursiveIdentities = identity.GetComponentsInChildren<NetworkIdentity>(true);
                    for (int i = recursiveIdentities.Length - 1; i >= 0; i--)
                    {
                        int childIdentityId = recursiveIdentities[i].Id;
                        if (childIdentityId == identityId) // skip the parent(self), only process children
                            continue;

                        Destroy(childIdentityId, isServer, isRoot: false);
                    }
                }

                UnityEngine.Object.Destroy(identity.gameObject);
            }
            else
            {
                NetworkLogger.__Log__(
                     $"[Destroy] Failed to destroy Network Identity '{identityId}': Not found in {(isServer ? "Server" : "Client")} identities dictionary. " +
                     $"This could be caused by: (1) The object was already destroyed, (2) The object exists on the {(isServer ? "client" : "server")} side only, " +
                     $"or (3) The identityId is invalid. Check network synchronization in previous operations.",
                     NetworkLogger.LogType.Error
                );
            }
        }

        internal static NetworkIdentity Instantiate(NetworkIdentity prefab, NetworkPeer peer, int identityId,
            bool isServer, bool isLocalPlayer)
        {
            // Disable the prefab to avoid Awake and Start being called multiple times before the registration.
            prefab.gameObject.SetActive(false);
            NetworkIdentity identity = UnityEngine.Object.Instantiate(prefab);
            identity.Id = identityId;
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
                    // Hierarchy ID assignment: the hierarchy ID is assigned based on the order of the NetworkBehaviour components in the hierarchy.
                    // This ensures that the ID is unique and sequential for each NetworkBehaviour within the same hierarchy.
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

            if (!identities.TryAdd(identity.Id, identity))
            {
                NetworkIdentity oldRef = identities[identity.Id];
                MonoBehaviour.Destroy(oldRef.gameObject);
                identities[identity.Id] = identity; // Update the reference.....

                NetworkLogger.__Log__(
                     $"[Instantiate] Identity conflict detected for ID '{identity.Id}': An object with this ID already exists in the {(isServer ? "server" : "client")} identities collection. " +
                     $"The previous object has been destroyed and replaced with the new instance. This may indicate an issue with ID assignment or network synchronization. " +
                     $"If this happens frequently, consider implementing additional validation in your object creation logic.",
                     NetworkLogger.LogType.Warning);
            }

            // After registration, enable the prefab again.
            if (IsPrefab(prefab.gameObject))
            {
                prefab.gameObject.SetActive(true);
            }

            // Enable instantiated object and call the OnStart.... method's.
            identity.gameObject.SetActive(true);
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
                // This method checks if a given port is available for use.
                // It attempts to bind a socket to the port, which will throw an exception if the port is already in use.
                // However, simply using a "using" statement does not immediately free the port after execution.
                // Calling Close() explicitly is necessary because, without it, the port might still appear occupied due to the previous Bind() call.
                // Additionally, the second verification using IPGlobalProperties would fail without calling Close().
                // This is because Bind() would temporarily mark the port as occupied, leading to a false positive in IPGlobalProperties,
                // incorrectly indicating that the port is still in use even if it's actually free.
                // Additionally, in some environments (e.g., Docker, VMs), Bind() alone may not accurately detect all active ports.
                // Therefore, after closing the socket, we perform an additional check using IPGlobalProperties to ensure correctness.

                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                if (protocolType == ProtocolType.Udp)
                {
                    using Socket socket =
                        new Socket(useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);

                    if (useIPv6)
                    {
                        socket.DualMode = true;
                    }

                    // Bind throws an exception if the port is already in use.
                    socket.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
                    // If an exception doesn't occur, the port is available.
                    // Close the socket to release the resource.
                    // Explicitly close the socket to ensure the port is freed for rechecking.
                    // Without this, IPGlobalProperties would give a false positive, incorrectly marking the port as occupied.
                    socket.Close();

                    // After closing the socket, check if the port is still available.
                    // eg: Docker, Vms etc will return false if the port is still in use.
                    IPEndPoint[] endpoints = properties.GetActiveUdpListeners();
                    return !endpoints.Any(x => x.Port == port);
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

                    // Bind throws an exception if the port is already in use.
                    socket.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
                    // If an exception doesn't occur, the port is available.
                    // Close the socket to release the resource.
                    // Explicitly close the socket to ensure the port is freed for rechecking.
                    // Without this, IPGlobalProperties would give a false positive, incorrectly marking the port as occupied.
                    socket.Close();

                    // After closing the socket, check if the port is still available.
                    // eg: Docker, Vms etc will return false if the port is still in use.
                    TcpConnectionInformation[] tcpConnections = properties.GetActiveTcpConnections();
                    return !tcpConnections.Any(x => x.LocalEndPoint.Port == port && (x.State == TcpState.Listen || x.State == TcpState.Established));
                }

                // Return true by default if no specific check was performed
                return true;
            }
            catch
            {
                // If an exception occurs, it means the port is already in use
                return false;
            }
        }

        [Conditional("OMNI_DEBUG")]
        internal static void ThrowAnErrorIfIsInternalTypes<T>(T type) where T : unmanaged
        {
            if (type is Target or DeliveryMode or CacheMode)
            {
                throw new InvalidOperationException(
                    $"[TypeValidation] The type '{typeof(T).Name}' is internal to Omni Networking and cannot be used as a DataBuffer argument. This protection prevents misuse of system types that could cause serialization or networking errors. Please use appropriate public types or refer to the documentation for the correct DataBuffer overloads.");
            }
        }

        /// <summary>
        /// Retrieves the external IP address of the device asynchronously.
        /// </summary>
        /// <param name="useIPv6">A boolean indicating whether to retrieve the IPv6 address or the IPv4 address.</param>
        /// <returns>A Task representing the asynchronous operation, containing the external IP address as an IPAddress object.</returns>
        public static async Task<IPAddress> GetExternalIpAsync(bool useIPv6)
        {
            string[] ipv4Sources = { "https://ipv4.icanhazip.com/", "https://api.ipify.org", "https://checkip.amazonaws.com/" };
            string[] ipv6Sources = { "https://ipv6.icanhazip.com/", "https://api64.ipify.org", "https://ifconfig.co/ip" };

            using var http = new HttpClient();
            foreach (string url in useIPv6 ? ipv6Sources : ipv4Sources)
            {
                try
                {
                    string externalIp = await http.GetStringAsync(url);
                    externalIp = externalIp.Replace("\\r\\n", "");
                    externalIp = externalIp.Replace("\\n", "");
                    externalIp = externalIp.Trim();

                    if (IPAddress.TryParse(externalIp, out var ipAddress))
                    {
                        return ipAddress;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return !useIPv6 ? IPAddress.Loopback : IPAddress.IPv6Loopback;
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
        public static async UniTask RunOnMainThreadAsync(Action func, bool continueOnCapturedContext = true)
        {
            await UniTask.SwitchToMainThread();
            // Run on main thread
            func();
            if (!continueOnCapturedContext)
            {
                await UniTask.SwitchToThreadPool();
                // Exit from main thread, like ConfigureAwait();
            }
        }

        /// <summary>
        /// Schedules the specified action to be executed on the main thread asynchronously.
        /// </summary>
        /// <param name="action">The action to be executed on the main thread.</param>
        public static async UniTask<T> RunOnMainThreadAsync<T>(Func<T> func, bool continueOnCapturedContext = true)
        {
            await UniTask.SwitchToMainThread();
            // Run on main thread
            T value = func();
            if (!continueOnCapturedContext)
            {
                await UniTask.SwitchToThreadPool();
                // Exit from main thread, like ConfigureAwait();
            }
            return value;
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
                    $"[ThreadViolation] Operation attempted from thread ID {Thread.CurrentThread.ManagedThreadId} but must run on main thread (ID: {NetworkManager.MainThreadId}). " +
                    $"Unity and Omni Networking APIs are not thread-safe. Use NetworkHelper.RunOnMainThreadAsync() or similar methods to dispatch operations to the main thread.",
                    NetworkLogger.LogType.Error);

                throw new NotSupportedException(
                    $"[ThreadViolation] Operation attempted from thread ID {Thread.CurrentThread.ManagedThreadId} but must run on main thread (ID: {NetworkManager.MainThreadId}). " +
                    $"Unity and Omni Networking APIs are not thread-safe. Use NetworkHelper.RunOnMainThreadAsync() or similar methods to dispatch operations to the main thread.");
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

        /// <summary>
        /// Shows a debug label on the screen at the position of the given object.
        /// </summary>
        /// <param name="label">The label to be displayed.</param>
        /// <remarks>
        /// This method is useful for debugging purposes, as it allows you to display a label on the screen at the position of a given object.
        /// </remarks>
        [Conditional("OMNI_DEBUG")]
        public static void ShowGUILabel(string label, Transform transform, float up = 1.0f)
        {
            Vector3 worldPosition = transform.position + Vector3.up * up;

            if (Camera.main == null)
            {
                NetworkLogger.Print(
                     "[NetworkHelper.ShowGUILabel] Main camera not found. Please ensure a camera exists in the scene with the 'MainCamera' tag. Debug labels cannot be displayed without a properly tagged camera.",
                     NetworkLogger.LogType.Error);

                return;
            }

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            if (screenPosition.z > 0)
            {
                const float boxWidth = 145;
                const float boxHeight = 40;

                float x = screenPosition.x - boxWidth / 2;
                float y = Screen.height - screenPosition.y - boxHeight;

                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.fontSize = 20;
                style.alignment = TextAnchor.MiddleCenter;

                for (int i = 0; i < 4; i++) // to make the background and label more visible
                    GUI.Box(new Rect(x, y, boxWidth, boxHeight), label, style);
            }
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

        /// <summary>
        /// Checks if the current platform supports server hosting capabilities.
        /// </summary>
        /// <returns>
        /// Returns true if running on Unity Standalone (Windows, macOS, Linux), Server, or Editor platforms.
        /// Returns false on other platforms (mobile, consoles, etc).
        /// </returns>
        public static bool CanHostServer()
        {
#if UNITY_STANDALONE || UNITY_SERVER || UNITY_EDITOR
            // Desktop platforms (Windows, macOS, Linux) and Server platforms (Linux) will return true
            return true;
#else
            // Mobile platforms (iOS, Android), Consoles (PS4, Xbox, Switch) and others will return false
            return false;
#endif
        }

        public static void EditorSaveObject(GameObject target)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
#endif
        }

        internal static Dictionary<string, string> ParseQueryStringToDictionary(NameValueCollection queryString)
        {
            string[] allKeys = queryString.AllKeys;
            Dictionary<string, string> parameters = new(allKeys.Length);
            foreach (string key in allKeys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                parameters[key] = queryString[key];
            }

            return parameters;
        }

        internal static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput))
                return false;

            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) // For array
            {
                try
                {
                    JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}