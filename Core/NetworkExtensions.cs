using System;

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
        /// Instantiates a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peer">The peer who will receive the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnServer(this NetworkIdentity prefab, NetworkPeer peer)
        {
            return NetworkManager.SpawnOnServer(prefab, peer);
        }

        /// <summary>
        /// Instantiates a network identity on the server for a specific peer.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peer">The peer who will receive the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnServer(
            this NetworkIdentity prefab,
            NetworkPeer peer,
            int identityId
        )
        {
            return NetworkManager.SpawnOnServer(prefab, peer, identityId);
        }

        /// <summary>
        /// Instantiates a network identity on the server.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peerId">The ID of the peer who will receive the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object. If not provided, a dynamic unique ID will be generated.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnServer(
            this NetworkIdentity prefab,
            int peerId,
            int identityId = 0
        )
        {
            return NetworkManager.SpawnOnServer(prefab, peerId, identityId);
        }

        /// <summary>
        /// Instantiates a network identity on the client.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="peerId">The ID of the peer who owns the instantiated object.</param>
        /// <param name="identityId">The ID of the instantiated object.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity SpawnOnClient(
            this NetworkIdentity prefab,
            int peerId,
            int identityId
        )
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
    }
}
