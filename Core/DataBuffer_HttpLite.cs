namespace Omni.Core
{
    // Used for HTTP Lite
    public sealed partial class DataBuffer
    {
        internal bool SendEnabled { get; set; }
        internal DeliveryMode DeliveryMode { get; private set; } = DeliveryMode.ReliableOrdered;
        internal HttpTarget Target { get; private set; } = HttpTarget.Self;
        internal int GroupId { get; private set; }
        internal int CacheId { get; private set; }
        internal CacheMode CacheMode { get; private set; } = CacheMode.None;
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
                options.CacheId,
                options.CacheMode,
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
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            SendEnabled = true;
            Target = target;
            DeliveryMode = deliveryMode;
            GroupId = groupId;
            CacheId = cacheId;
            CacheMode = cacheMode;
            SequenceChannel = sequenceChannel;
        }
    }
}
