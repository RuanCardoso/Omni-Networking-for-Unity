namespace Omni.Core
{
    public static class NetworkExtensions
    {
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
    }
}
