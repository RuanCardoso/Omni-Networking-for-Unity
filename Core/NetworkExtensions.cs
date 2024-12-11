using System;
using UnityEngine;

namespace Omni.Core
{
    public static class NetworkExtensions
    {
        private static readonly string[] SizeSuffixes =
        {
            "B/s",
            "kB/s",
            "mB/s",
            "gB/s",
            "tB/s",
            "pB/s",
            "eB/s",
            "zB/s",
            "yB/s"
        };

        /// <summary>
        /// Spawns a network identity on both server and specified target clients with given delivery and caching options.
        /// </summary>
        /// <param name="prefab">The network identity prefab to spawn.</param>
        /// <param name="peer">The network peer to receive the spawned object.</param>
        /// <param name="target">Specifies the target clients for the spawned object.</param>
        /// <param name="deliveryMode">Defines how the spawned object is delivered over the network.</param>
        /// <param name="groupId">The group identifier for organizing network messages.</param>
        /// <param name="dataCache">Optional data cache for storing additional data associated with the spawn operation.</param>
        /// <param name="sequenceChannel">The sequence channel used for ordering network messages.</param>
        /// <returns>The spawned network identity instance.</returns>
        public static NetworkIdentity Spawn(this NetworkIdentity prefab, NetworkPeer peer, ServerOptions options)
        {
            var identity = NetworkManager.SpawnOnServer(prefab, peer);
            identity.SpawnOnClient(options);
            return identity;
        }

        /// <summary>
        /// Spawns a network identity on the server and specified client targets with defined delivery and caching options.
        /// </summary>
        /// <param name="prefab">The network identity prefab to instantiate.</param>
        /// <param name="peer">The network peer that will receive the instantiated object.</param>
        /// <param name="target">Specifies the target clients for the instantiated object.</param>
        /// <param name="deliveryMode">Determines the manner in which the instantiated object is delivered over the network.</param>
        /// <param name="groupId">An identifier used for organizing network messages into groups.</param>
        /// <param name="dataCache">Optional parameter for caching additional data associated with the instantiation process.</param>
        /// <param name="sequenceChannel">The sequence channel used to maintain message order for network delivery.</param>
        /// <returns>The instantiated network identity as observed on the server.</returns>
        public static NetworkIdentity Spawn(this NetworkIdentity prefab, NetworkPeer peer, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            var identity = NetworkManager.SpawnOnServer(prefab, peer);
            identity.SpawnOnClient(target, deliveryMode, groupId, dataCache, sequenceChannel);
            return identity;
        }

        /// <summary>
        /// Spawns a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peer">The peer who will receive the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnServer(this NetworkIdentity prefab, NetworkPeer peer)
        {
            return NetworkManager.SpawnOnServer(prefab, peer);
        }

        /// <summary>
        /// Spawns a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peer">The peer who will receive the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnServer(this NetworkIdentity prefab, NetworkPeer peer, int identityId)
        {
            return NetworkManager.SpawnOnServer(prefab, peer, identityId);
        }

        /// <summary>
        /// Spawns a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The network identity prefab to instantiate on the server.</param>
        /// <param name="peerId">The network peer associated with the spawned object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity object on the server.</returns>
        public static NetworkIdentity SpawnOnServer(this NetworkIdentity prefab, int peerId, int identityId = 0)
        {
            return NetworkManager.SpawnOnServer(prefab, peerId, identityId);
        }

        /// <summary>
        /// Spawns a network identity on a client with specified peer and identity identifiers.
        /// </summary>
        /// <param name="prefab">The network identity prefab to instantiate on the client.</param>
        /// <param name="peerId">The identifier of the peer who will own the instantiated object.</param>
        /// <param name="identityId">The unique identifier for the instantiated network identity.</param>
        /// <returns>The instantiated network identity on the client.</returns>
        public static NetworkIdentity SpawnOnClient(this NetworkIdentity prefab, int peerId, int identityId)
        {
            return NetworkManager.SpawnOnClient(prefab, peerId, identityId);
        }

        internal static string ToSizeSuffix(this double value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException("decimalPlaces < 0");
            }

            if (value < 0)
            {
                return "-" + ToSizeSuffix(-value, decimalPlaces);
            }

            if (value == 0)
            {
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0);
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag)
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        /// <summary>
        /// Retrieves the <see cref="NetworkIdentity"/> component from the root of the transform
        /// affected by the 3D raycast hit.
        /// </summary>
        /// <param name="hit">The <see cref="RaycastHit"/> instance resulting from a raycast operation.</param>
        /// <returns>
        /// The <see cref="NetworkIdentity"/> component located on the root object of the impacted transform,
        /// or <c>null</c> if no <see cref="NetworkIdentity"/> is found.
        /// </returns>
        public static NetworkIdentity GetIdentity(this RaycastHit hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the <see cref="NetworkIdentity"/> component from the root of the transform
        /// impacted by the 2D raycast hit.
        /// </summary>
        /// <returns>
        /// The <see cref="NetworkIdentity"/> component located on the root object of the impacted transform,
        /// or <c>null</c> if no <see cref="NetworkIdentity"/> is found.
        /// </returns>
        public static NetworkIdentity GetIdentity(this RaycastHit2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity associated with the root of the transform involved in the collision.
        /// </summary>
        /// <param name="hit">The Collision object from which to extract the NetworkIdentity.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the collision, or null if not found.</returns>
        public static NetworkIdentity GetIdentity(this Collision hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component associated with the given Collider.
        /// </summary>
        /// <param name="hit">The Collider from which to retrieve the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the Collider, or null if none is found.</returns>
        public static NetworkIdentity GetIdentity(this Collider hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component from the root transform of a Collision2D instance.
        /// </summary>
        /// <param name="hit">The Collision2D instance from which to obtain the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component associated with the root transform of the collision, or null if not found.</returns>
        public static NetworkIdentity GetIdentity(this Collision2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component associated with the given Collider2D.
        /// </summary>
        /// <param name="hit">The Collider2D from which to obtain the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the specified Collider2D, or null if no such component exists.</returns>
        public static NetworkIdentity GetIdentity(this Collider2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static float ScaleDelta(this float input, float multiplier)
        {
            return input * multiplier * Time.deltaTime;
        }
    }
}