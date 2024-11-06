namespace Omni.Core
{
	/// <summary>
	/// Represents a cache for storing network messages that can be retrieved later.
	/// </summary>
	public class DataCache
	{
		public static DataCache None { get; } = new(0, CacheMode.None);

		/// <summary>
		/// Gets the unique identifier of the cached message.
		/// </summary>
		public int Id { get; } = 0;

		/// <summary>
		/// Gets the caching mode that determines how the message is stored and retrieved.
		/// </summary>
		public CacheMode Mode { get; } = CacheMode.None;

		public DataCache(int id, CacheMode mode)
		{
			Id = id;
			Mode = mode;
		}
	}
}