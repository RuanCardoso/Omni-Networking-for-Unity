namespace Omni.Core.Interfaces
{
    internal interface INetworkMessage
    {
        int IdentityId { get; }
        void Internal_OnMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            bool isServer,
            int seqChannel
        );
    }
}
