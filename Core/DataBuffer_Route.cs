namespace Omni.Core
{
    // Exclusively for Routes API.
    // Represents a buffer for managing data, used primarily within the Routes API.
    public sealed partial class DataBuffer
    {
        internal bool SendEnabled { get; set; }
        internal DeliveryMode DeliveryMode { get; private set; } = DeliveryMode.ReliableOrdered;
        internal RouteTarget Target { get; private set; } = RouteTarget.Self;
        internal NetworkGroup Group { get; private set; }
        internal byte SequenceChannel { get; private set; }

        /// <summary>
        /// Sends a GET/POST response from the server with specified delivery and target settings.
        /// </summary>
        /// <param name="target">The target of the response, defaulting to <see cref="RouteTarget.Self"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the response, defaulting to <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The ID of the target group for the response, defaulting to 0 (no group).</param>
        /// <param name="dataCache">The data cache configuration, defaulting to <see cref="DataCache.None"/>.</param>
        /// <param name="sequenceChannel">The channel used for sequencing responses, defaulting to 0.</param>
        /// <remarks>
        /// Use this method for fine-grained control over the delivery of server responses.
        /// Defaults are provided for common scenarios, ensuring ease of use for typical use cases.
        /// </remarks>
        public void Send(RouteTarget target = RouteTarget.Self, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, NetworkGroup group = null, byte sequenceChannel = 0)
        {
            SendEnabled = true;
            Target = target;
            DeliveryMode = deliveryMode;
            Group = group;
            SequenceChannel = sequenceChannel;
        }
    }
}