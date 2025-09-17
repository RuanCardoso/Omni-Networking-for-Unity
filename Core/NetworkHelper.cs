using Omni.Shared;
using Omni.Threading.Tasks;
using OpenNat;
using System;
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
            int length = random.Next(16, 129); // 129 is exclusive
            while (tokenBuilder.Length < length)
                tokenBuilder.Append(randomString[random.Next(0, randomString.Length)]);

            byte[] base64bytes = Encoding.UTF8.GetBytes(tokenBuilder.ToString());
            return Convert.ToBase64String(base64bytes);
        }

        internal static NetworkIdentity SpawnAndRegister(NetworkIdentity prefab, NetworkPeer peer, int identityId,
            bool isServer, bool isLocalPlayer)
        {
            if (prefab.m_ServerPrefabOverride != null && isServer)
                prefab = prefab.m_ServerPrefabOverride;

            // Disable the prefab to avoid Awake and Start being called multiple times before the registration.
            prefab.gameObject.SetActive(false);
            NetworkIdentity identity = UnityEngine.Object.Instantiate(prefab);
            if (isLocalPlayer && identity.IsPlayerObject())
                NetworkIdentity.LocalPlayer = identity;

            identity.Register();
            identity.Id = identityId;
            identity.Owner = peer;
            identity.IsServer = isServer;
            identity.IsLocalPlayer = isLocalPlayer;
            identity.IsMine = isLocalPlayer;
            identity.IsOwnedByServer = identity.Owner.Id == (isServer ? NetworkManager.ServerSide.ServerPeer.Id : NetworkManager.ClientSide.ServerPeer.Id);
            identity._prefabName = prefab.name;
            identity.name = $"{prefab.name}(In {(isServer ? "Server" : "Client")})";

            var identities = isServer
                ? NetworkManager.ServerSide.Identities
                : NetworkManager.ClientSide.Identities;

            if (identities.TryGetValue(identityId, out var oldRef))
            {
                if (oldRef != null)
                {
                    NetworkBehaviour[] behaviours = oldRef.GetComponentsInChildren<NetworkBehaviour>(true);
                    foreach (var behaviour in behaviours)
                        behaviour.Unregister();
                }
            }

            identities[identityId] = identity;
            NetworkBehaviour[] networkBehaviours = identity.GetComponentsInChildren<NetworkBehaviour>(true);
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
                if (identity.IsLocalPlayer) behaviour.OnStartLocalPlayer();
                else if (identity.IsPlayerObject()) behaviour.OnStartRemotePlayer();
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
            return NetworkManager.UnityMainThreadId == Thread.CurrentThread.ManagedThreadId;
        }

        [Conditional("OMNI_DEBUG")]
        internal static void EnsureRunningOnMainThread()
        {
            if (!IsRunningOnMainThread())
            {
                NetworkLogger.__Log__(
                    $"[ThreadViolation] Operation attempted from thread ID {Thread.CurrentThread.ManagedThreadId} but must run on main thread (ID: {NetworkManager.UnityMainThreadId}). " +
                    $"Unity and Omni Networking APIs are not thread-safe. Use NetworkHelper.RunOnMainThreadAsync() or similar methods to dispatch operations to the main thread.",
                    NetworkLogger.LogType.Error);

                throw new NotSupportedException(
                    $"[ThreadViolation] Operation attempted from thread ID {Thread.CurrentThread.ManagedThreadId} but must run on main thread (ID: {NetworkManager.UnityMainThreadId}). " +
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
            return obj.scene.name == null || obj.scene.name.ToLowerInvariant() == "null";
        }

        /// <summary>
        /// Shows a debug label on the screen at the position of the given object.
        /// </summary>
        /// <param name="label">The label to be displayed.</param>
        /// <remarks>
        /// This method is useful for debugging purposes, as it allows you to display a label on the screen at the position of a given object.
        /// </remarks>
        [Conditional("OMNI_DEBUG")]
        public static void ShowGUILabel(string label, Transform transform, float up = 1.0f, Camera camera = null)
        {
            Vector3 worldPosition = transform.position + Vector3.up * up;

            if (camera == null)
                camera = Camera.main;

            if (camera == null)
            {
                NetworkLogger.Print(
                    "No camera available. Assign a camera to render debug labels.",
                    NetworkLogger.LogType.Error
                );

                return;
            }

            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
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
            if (target == null)
                return;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
#endif
        }

        internal static double Truncate(double value, int decimalPlaces)
        {
            double factor = Math.Pow(10, decimalPlaces);
            return Math.Truncate(value * factor) / factor;
        }

        /// <summary>
        /// Determines whether the specified host belongs to an internal or private network.
        /// Includes localhost, loopback addresses, private IPv4 ranges (10/8, 192.168/16, 172.16–31/12, 169.254/16),
        /// IPv6 local addresses (::1, link-local, site-local, fc00::/7), docker container names, etc.
        /// </summary>
        /// <param name="host">The host string, which may be an IP address or hostname/container name.</param>
        /// <returns><c>true</c> if the host is internal; otherwise, <c>false</c>.</returns>
        public static bool IsInternalHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            // Check for localhost explicitly
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            // Try to parse as IP address first
            if (System.Net.IPAddress.TryParse(host, out var ip))
            {
                return IsInternalIpAddress(ip);
            }

            // If not a valid IP, try to resolve the hostname/container name
            try
            {
                var hostAddresses = System.Net.Dns.GetHostAddresses(host);
                foreach (var address in hostAddresses)
                {
                    if (IsInternalIpAddress(address))
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Helper method to check if an IPAddress is internal/private.
        /// </summary>
        /// <param name="ip">The IPAddress to check.</param>
        /// <returns><c>true</c> if the IP is internal; otherwise, <c>false</c>.</returns>
        private static bool IsInternalIpAddress(System.Net.IPAddress ip)
        {
            if (System.Net.IPAddress.IsLoopback(ip))
                return true;

            byte[] bytes = ip.GetAddressBytes();

            return ip.AddressFamily switch
            {
                System.Net.Sockets.AddressFamily.InterNetwork => // IPv4
                    (bytes[0] == 10) || // 10.0.0.0/8
                    (bytes[0] == 192 && bytes[1] == 168) || // 192.168.0.0/16
                    (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.0.0/12
                    (bytes[0] == 169 && bytes[1] == 254), // 169.254.0.0/16 (APIPA)

                System.Net.Sockets.AddressFamily.InterNetworkV6 => // IPv6
                    ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal ||
                    ip.Equals(System.Net.IPAddress.IPv6Loopback) ||
                    bytes[0] == 0xfc || bytes[0] == 0xfd, // fc00::/7 (Unique Local)

                _ => false
            };
        }
    }
}