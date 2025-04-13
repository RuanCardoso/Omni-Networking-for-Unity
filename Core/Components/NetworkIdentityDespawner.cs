using System.Linq;

namespace Omni.Core.Components
{
    public class NetworkIdentityDespawner : NetworkBehaviour
    {
        protected internal override void OnAwake()
        {
            if (IsClient)
            {
                Destroy(this);
                return;
            }

            NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
            // NetworkManager.Matchmaking.Server.OnPlayerLeftGroup += OnPlayerLeftGroup;
        }

        // private void OnPlayerLeftGroup(NetworkGroup group, NetworkPeer peer, Phase phase, string reason)
        // {
        //     if (phase == Phase.Begin && peer.Id == Identity.Owner.Id)
        //     {
        //         if (peer.Data.Remove("_groupId", out object value) && value is int groupId && groupId == group.Id)
        //         {
        //             // Only despawn the object if the peer is the owner of the identity.
        //             {
        //                 NetworkManager.Matchmaking.Server.OnPlayerLeftGroup -= OnPlayerLeftGroup;

        //                 // foreach (NetworkIdentity p in NetworkManager.ServerSide.Identities.Values.ToList().Where(x => x.IsServerOwner))
        //                 // {
        //                 //     p.DespawnToPeer(peer);
        //                 // }

        //                 // Identity.Despawn();
        //             }
        //         }
        //     }
        // }

        private void OnServerPeerDisconnected(NetworkPeer peer, Phase phase)
        {
            // Only despawn the object if the peer is the owner of the identity.
            if (phase == Phase.Begin && peer.Id == Identity.Owner.Id)
            {
                NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
                Identity.Despawn();
            }
        }

        protected override void OnNetworkDestroy()
        {
            NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
            // NetworkManager.Matchmaking.Server.OnPlayerLeftGroup -= OnPlayerLeftGroup;
        }

        void OnDestroy()
        {
            OnNetworkDestroy();
        }
    }
}