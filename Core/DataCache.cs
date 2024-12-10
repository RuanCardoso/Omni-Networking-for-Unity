namespace Omni.Core
{
    /// <summary>
    /// Represents a cache for storing network messages that can be retrieved later.
    /// </summary>
    public class DataCache
    {
        static int ___Id___ = 0; // Id is only valid if it contains the 'Overwrite' flag, otherwise it is not used.

        /// <summary>
        /// Represents a static instance of <see cref="DataCache"/> with a default configuration of zero ID and <see cref="CacheMode.None"/>.
        /// </summary>
        public static DataCache None { get; } = new(0, CacheMode.None);

        /// <summary>
        /// Gets the cache identifier used for distinguishing instances of <see cref="DataCache"/>.
        /// This identifier is only relevant if the cache mode includes the 'Overwrite' flag.
        /// </summary>
        public int Id { get; } = 0;

        /// <summary>
        /// Gets the caching mode which determines how the cache behaves in terms of storage and retrieval.
        /// </summary>
        public CacheMode Mode { get; } = CacheMode.None;

        /// <summary>
        /// Represents a cache for storing network messages that can be retrieved later.
        /// </summary>
        public DataCache(CacheMode mode)
        {
            if (mode != CacheMode.None)
            {
                // Id is only valid if it contains the 'Overwrite' flag, otherwise it is not used.
                Id = ++___Id___;
            }

            Mode = mode;
        }

        /// <summary>
        /// Represents a cache for storing network messages that can be retrieved later.
        /// </summary>
        public DataCache(int id, CacheMode mode)
        {
            Id = id;
            Mode = mode;
        }

        /// <summary>
        /// Sends cached data to a specified network peer based on the provided cache.
        /// </summary>
        /// <param name="peer">The network peer to whom the cache data will be sent.</param>
        /// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
        /// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
        public void SendToPeer(NetworkPeer peer, int groupId = 0, bool sendMyOwnCacheToMe = false)
        {
            NetworkManager.ServerSide.Internal_SendCache(peer, this, groupId, sendMyOwnCacheToMe);
        }

        /// <summary>
        /// Sends cached data from one network peer to another based on the specified data cache.
        /// </summary>
        /// <param name="fromPeer">The network peer that originates the cache data.</param>
        /// <param name="toPeer">The network peer to which the cache data will be sent.</param>
        /// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
        /// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
        public void SendFromToPeer(NetworkPeer fromPeer, NetworkPeer toPeer, int groupId = 0,
            bool sendMyOwnCacheToMe = false)
        {
            NetworkManager.ServerSide.Internal_SendPeerCache(fromPeer, toPeer, this, groupId, sendMyOwnCacheToMe);
        }
    }
}