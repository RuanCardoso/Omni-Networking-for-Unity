namespace Omni.Core
{
    /// <summary>
    /// Represents a cache of network data with properties that define cache behavior, ownership, and delivery.
    /// </summary>
    internal sealed class NetworkCache
    {
        internal int Id { get; }
        internal CacheMode Mode { get; }
        internal byte[] Data { get; }
        internal NetworkPeer Peer { get; }
        internal DeliveryMode DeliveryMode { get; }
        internal Target Target { get; }
        internal byte SequenceChannel { get; }
        internal bool AutoDestroyCache { get; }

        internal NetworkCache(int id, CacheMode mode, byte[] data, NetworkPeer peer, DeliveryMode deliveryMode,
            Target target, byte sequenceChannel, bool destroyOnDisconnect = false)
        {
            Id = id;
            Mode = mode;
            Data = data;
            Peer = peer; // owner
            DeliveryMode = deliveryMode;
            Target = target;
            SequenceChannel = sequenceChannel;
            AutoDestroyCache = destroyOnDisconnect;
        }
    }
}