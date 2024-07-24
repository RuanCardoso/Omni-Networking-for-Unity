using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Omni.Core.Components;
using Omni.Shared;
using UnityEngine;

namespace Omni.Core
{
    public static class NetworkHelper
    {
        private static int d_UniqueId = int.MinValue;

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

        internal static void Destroy(NetworkIdentity identity, bool isServer)
        {
            var identities = isServer
                ? NetworkManager.Server.Identities
                : NetworkManager.Client.Identities;

            if (identities.Remove(identity.IdentityId))
            {
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

#if UNITY_EDITOR || !UNITY_SERVER
            identity.name = $"{prefab.name}(On {(isServer ? "Server" : "Client")})";
            if (!isServer)
            {
                NetworkIsolate[] _ = identity.GetComponentsInChildren<NetworkIsolate>(true);
                foreach (NetworkIsolate isolate in _)
                {
                    UnityEngine.Object.Destroy(isolate);
                }
            }
#endif

            var identities = isServer
                ? NetworkManager.Server.Identities
                : NetworkManager.Client.Identities;

            if (!identities.TryAdd(identity.IdentityId, identity))
            {
                NetworkLogger.__Log__(
                    $"Instantiation Error: Failed to add identity with ID '{identity.IdentityId}' to {(isServer ? "server" : "client")} identities. The identity might already exist.",
                    NetworkLogger.LogType.Error
                );
            }

            prefab.gameObject.SetActive(true); // After registration, enable the prefab again.
            identity.gameObject.SetActive(true); // Enable instantiated object!

            // After Start
            foreach (var behaviour in networkBehaviours)
            {
                behaviour.OnStart();
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
                using StreamReader reader = new(fileName);
                JsonConvert.PopulateObject(reader.ReadToEnd(), target);
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
