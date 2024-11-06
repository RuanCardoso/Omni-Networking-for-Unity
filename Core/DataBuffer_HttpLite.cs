namespace Omni.Core
{
	// Used for HTTP Lite
	public sealed partial class DataBuffer
	{
		internal bool SendEnabled { get; set; }
		internal DeliveryMode DeliveryMode { get; private set; } = DeliveryMode.ReliableOrdered;
		internal HttpTarget Target { get; private set; } = HttpTarget.Self;
		internal int GroupId { get; private set; }
		internal DataCache DataCache { get; private set; } = DataCache.None;
		internal byte SequenceChannel { get; private set; }

		/// <summary>
		/// Sends a GET/POST response from the server.
		/// </summary>
		public void Send(HttpTarget target, SyncOptions options)
		{
			Send(
				target,
				options.DeliveryMode,
				options.GroupId,
				options.DataCache,
				options.SequenceChannel
			);
		}

		/// <summary>
		/// Sends a GET/POST response from the server.
		/// </summary>
		public void Send(
			HttpTarget target = HttpTarget.Self,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0
		)
		{
			dataCache ??= DataCache.None;
			SendEnabled = true;
			Target = target;
			DeliveryMode = deliveryMode;
			GroupId = groupId;
			DataCache = dataCache;
			SequenceChannel = sequenceChannel;
		}
	}
}
