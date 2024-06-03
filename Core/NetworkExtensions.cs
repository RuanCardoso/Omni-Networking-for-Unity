using System;
using Omni.Core.Components;
using Omni.Shared;
using Object = UnityEngine.Object;

namespace Omni.Core
{
    public static class NetworkExtensions
    {
        /// <summary>
        /// Instantiates a network identity on the client from serialized data in the network buffer.
        /// </summary>
        /// <param name="prefab">The prefab of the network identity to instantiate.</param>
        /// <param name="buffer">The network buffer containing serialized identity data.</param>
        /// <returns>The instantiated network identity.</returns>
        internal static NetworkIdentity InstantiateOnClient(
            this NetworkIdentity prefab,
            NetworkBuffer buffer,
            Action<NetworkIdentity> OnBeforeStart
        )
        {
            buffer.ReadIdentityData(out int identityId, out int peerId);
            bool isLocalPlayer = NetworkManager.LocalPeer.Id == peerId;
            return Instantiate(prefab, null, identityId, false, isLocalPlayer, OnBeforeStart);
        }

        /// <summary>
        /// Instantiates a network identity on the server for a specific network peer and serializes its data to the network buffer.
        /// </summary>
        /// <param name="prefab">The prefab of the network identity to instantiate.</param>
        /// <param name="peer">The network peer for which the identity is instantiated.</param>
        /// <param name="buffer">The network buffer to write identity data.</param>
        /// <returns>The instantiated network identity.</returns>
        internal static NetworkIdentity InstantiateOnServer(
            this NetworkIdentity prefab,
            NetworkPeer peer,
            NetworkBuffer buffer,
            Action<NetworkIdentity> OnBeforeStart
        )
        {
            int uniqueId = NetworkHelper.GenerateDynamicUniqueId();
            NetworkIdentity identity = Instantiate(
                prefab,
                peer,
                uniqueId,
                true,
                false,
                OnBeforeStart
            );

            buffer.Write(identity);
            return identity;
        }

        internal static void Internal_DestroyOnClient(this NetworkBuffer buffer)
        {
            buffer.ReadIdentityData(out int identityId, out _);
            NetworkIdentity identity = NetworkManager.Client.GetIdentity(identityId);
            if (identity == null)
            {
                throw new Exception("Identity not found");
            }

            Destroy(identity, false);
        }

        internal static void DestroyOnServer(this NetworkIdentity identity, NetworkBuffer buffer)
        {
            buffer.Write(identity);
            Destroy(identity, true);
        }

        private static void Destroy(NetworkIdentity identity, bool isServer)
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

                Object.Destroy(identity.gameObject);
            }
            else
            {
                NetworkLogger.__Log__(
                    $"Server Destroy: Identity with ID {identity.IdentityId} not found.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        private static NetworkIdentity Instantiate(
            NetworkIdentity prefab,
            NetworkPeer peer,
            int identityId,
            bool isServer,
            bool isLocalPlayer,
            Action<NetworkIdentity> OnBeforeStart
        )
        {
            prefab.gameObject.SetActive(false);

            NetworkIdentity identity = Object.Instantiate(prefab);
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
            }

#if UNITY_EDITOR || !UNITY_SERVER
            identity.name = $"{prefab.name}(On {(isServer ? "Server" : "Client")})";
            if (!isServer)
            {
                NetworkIsolate[] _ = identity.GetComponentsInChildren<NetworkIsolate>(true);
                foreach (NetworkIsolate isolate in _)
                {
                    Object.Destroy(isolate);
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

            prefab.gameObject.SetActive(true);
            OnBeforeStart?.Invoke(identity);
            identity.gameObject.SetActive(true);
            return identity;
        }
    }
}
