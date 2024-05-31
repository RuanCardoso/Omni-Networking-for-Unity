using UnityEngine;

namespace Omni.Core
{
    public static class NetworkExtensions
    {
        public static NetworkIdentity InstantiateOnClient(
            this NetworkIdentity prefab,
            int identityId,
            int peerId
        )
        {
            bool isLocalPlayer = NetworkManager.LocalPeer.Id == peerId;
            return Instantiate(prefab, null, identityId, false, isLocalPlayer);
        }

        public static NetworkIdentity InstantiateOnServer(
            this NetworkIdentity prefab,
            NetworkPeer peer
        )
        {
            return Instantiate(prefab, peer, NetworkHelper.GenerateUniqueId(), true, false);
        }

        private static NetworkIdentity Instantiate(
            this NetworkIdentity prefab,
            NetworkPeer peer,
            int identityId,
            bool isServer,
            bool isLocalPlayer
        )
        {
            prefab.gameObject.SetActive(false);

            NetworkIdentity networkIdentity = Object.Instantiate(prefab);
            networkIdentity.IdentityId = identityId;
            networkIdentity.Owner = peer;
            networkIdentity.IsServer = isServer;
            networkIdentity.IsLocalPlayer = isLocalPlayer;

            NetworkBehaviour[] networkBehaviours =
                networkIdentity.GetComponentsInChildren<NetworkBehaviour>(true);

            for (int i = 0; i < networkBehaviours.Length; i++)
            {
                NetworkBehaviour networkBehaviour = networkBehaviours[i];
                networkBehaviour.Identity = networkIdentity;

                if (networkBehaviour.Id == 0)
                {
                    networkBehaviour.Id = (byte)(i + 1);
                }

                networkBehaviour.Register();
            }

            prefab.gameObject.SetActive(true);

            networkIdentity.gameObject.SetActive(true);
            return networkIdentity;
        }
    }
}
