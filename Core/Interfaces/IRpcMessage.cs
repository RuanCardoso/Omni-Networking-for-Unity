namespace Omni.Core.Interfaces
{
    internal interface IRpcMessage
    {
        int IdentityId { get; }
        void OnRpcInvoked(byte methodId, DataBuffer buffer, NetworkPeer peer, bool isServer, int seqChannel);
    }
}