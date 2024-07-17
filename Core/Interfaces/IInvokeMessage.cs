namespace Omni.Core.Interfaces
{
    internal interface IInvokeMessage
    {
        int IdentityId { get; }
        void OnMessageInvoked(
            byte methodId,
            DataBuffer buffer,
            NetworkPeer peer,
            bool isServer,
            int seqChannel
        );
    }
}
