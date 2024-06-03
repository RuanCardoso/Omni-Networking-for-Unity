using UnityEngine;

namespace Omni.Core
{
    internal class NetworkCache
    {
        internal int Id { get; }
        internal CacheMode Mode { get; }
        internal byte[] Data { get; }
        internal NetworkPeer Peer { get; }
        internal DeliveryMode DeliveryMode { get; }
        internal Target Target { get; }
        internal byte SequenceChannel { get; }
        internal bool DestroyOnDisconnect { get; }

        internal NetworkCache(
            int id,
            CacheMode mode,
            byte[] data,
            NetworkPeer peer,
            DeliveryMode deliveryMode,
            Target target,
            byte sequenceChannel,
            bool destroyOnDisconnect = false
        )
        {
            Id = id;
            Mode = mode;
            Data = data;
            Peer = peer; // owner
            DeliveryMode = deliveryMode;
            Target = target;
            SequenceChannel = sequenceChannel;
            DestroyOnDisconnect = destroyOnDisconnect;
        }
    }
}
