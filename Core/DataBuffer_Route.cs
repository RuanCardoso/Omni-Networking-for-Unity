namespace Omni.Core
{
	// Exclusively for Routes API.
	public sealed partial class DataBuffer
	{
		internal bool SendEnabled { get; set; }
		internal DeliveryMode DeliveryMode { get; private set; } = DeliveryMode.ReliableOrdered;
		internal RouteTarget Target { get; private set; } = RouteTarget.SelfOnly;
		internal int GroupId { get; private set; }
		internal DataCache DataCache { get; private set; } = DataCache.None;
		internal byte SequenceChannel { get; private set; }

		/// <summary>
		/// Sends a GET/POST response from the server using the specified synchronization options.
		/// </summary>
		/// <param name="target">The target of the response (e.g., a specific client or group).</param>
		/// <param name="options">Synchronization options that include delivery mode, group ID, data cache, and sequence channel.</param>
		/// <remarks>
		/// This method simplifies sending responses by encapsulating multiple configuration options into a single parameter.
		/// </remarks>
		public void Send(RouteTarget target, ServerOptions options)
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
		/// Sends a GET/POST response from the server with specified delivery and target settings.
		/// </summary>
		/// <param name="target">The target of the response, defaulting to <see cref="RouteTarget.SelfOnly"/>.</param>
		/// <param name="deliveryMode">The delivery mode for the response, defaulting to <see cref="DeliveryMode.ReliableOrdered"/>.</param>
		/// <param name="groupId">The ID of the target group for the response, defaulting to 0 (no group).</param>
		/// <param name="dataCache">The data cache configuration, defaulting to <see cref="DataCache.None"/>.</param>
		/// <param name="sequenceChannel">The channel used for sequencing responses, defaulting to 0.</param>
		/// <remarks>
		/// Use this method for fine-grained control over the delivery of server responses. 
		/// Defaults are provided for common scenarios, ensuring ease of use for typical use cases.
		/// </remarks>
		public void Send(
			RouteTarget target = RouteTarget.SelfOnly,
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
