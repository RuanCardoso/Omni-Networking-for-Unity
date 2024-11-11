namespace Omni.Core
{
	/// <summary>
	/// Represents a cache for storing network messages that can be retrieved later.
	/// </summary>
	public class DataCache
	{
		static int ___Id___ = 0; // Id is only valid if it contains the 'Overwrite' flag, otherwise it is not used.
		public static DataCache None { get; } = new(0, CacheMode.None);

		/// <summary>
		/// Gets the unique identifier of the cached message.
		/// </summary>
		public int Id { get; } = 0;

		/// <summary>
		/// Gets the caching mode that determines how the message is stored and retrieved.
		/// </summary>
		public CacheMode Mode { get; } = CacheMode.None;

		public DataCache(CacheMode mode)
		{
			if (mode != CacheMode.None)
			{
				// Id is only valid if it contains the 'Overwrite' flag, otherwise it is not used.
				Id = ++___Id___;
			}

			Mode = mode;
		}

		public DataCache(int id, CacheMode mode)
		{
			Id = id;
			Mode = mode;
		}

		/// <summary>
		/// Sends cached data to a specified network peer based on the provided cache.
		/// </summary>
		/// <param name="peer">The network peer to whom the cache data will be sent.</param>
		/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
		/// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
		/// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
		public void SendToPeer(
			NetworkPeer peer,
			int groupId = 0,
			bool sendMyOwnCacheToMe = false
		)
		{
			NetworkManager.Server.Internal_SendCache(peer, this, groupId, sendMyOwnCacheToMe);
		}

		/// <summary>
		/// Sends cached data to a specified network peer based on the provided data cache.
		/// </summary>
		/// <param name="fromPeer">The originating network peer from which the cache data will be sent.</param>
		/// <param name="toPeer">The network peer to whom the cache data will be sent from the originating peer.</param>
		/// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
		/// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
		public void SendFromToPeer(
			NetworkPeer fromPeer,
			NetworkPeer toPeer,
			int groupId = 0,
			bool sendMyOwnCacheToMe = false
		)
		{
			NetworkManager.Server.Internal_SendPeerCache(fromPeer, toPeer, this, groupId, sendMyOwnCacheToMe);
		}
	}
}