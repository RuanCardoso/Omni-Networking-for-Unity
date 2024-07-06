namespace Omni.Core
{
    // Used for Lite HTTP
    public sealed partial class DataBuffer
    {
        internal bool SendEnabled { get; private set; }
        internal DeliveryMode DeliveryMode { get; private set; } = DeliveryMode.ReliableOrdered;
        internal bool ForceSendToSelf { get; private set; }
        internal Target Target { get; private set; } = Target.Self;
        internal int GroupId { get; private set; }
        internal int CacheId { get; private set; }
        internal CacheMode CacheMode { get; private set; } = CacheMode.None;
        internal byte SequenceChannel { get; private set; }

        /// <summary>
        /// Sends a GET/POST response from the server.
        /// </summary>
        public void Send(
            Target target = Target.Self,
            bool forceSendToSelf = false,
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
            ForceSendToSelf = forceSendToSelf;
            GroupId = groupId;
            CacheId = cacheId;
            CacheMode = cacheMode;
            SequenceChannel = sequenceChannel;
        }
    }
}
