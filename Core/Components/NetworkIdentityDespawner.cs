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
        }

        private void OnServerPeerDisconnected(NetworkPeer peer, Phase phase)
        {
            // Only despawn the object if the peer is the owner of the identity.
            if (phase == Phase.Begin && peer.Id == Identity.Owner.Id)
            {
                NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
                Identity.Despawn();
            }
        }
    }
}