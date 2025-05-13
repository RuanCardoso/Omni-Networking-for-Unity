#pragma warning disable
namespace Omni.Core.Interfaces
{
    internal interface IRpcMessage
    {
        int IdentityId { get; }
        __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> __ServerRpcHandler { get; }
        void OnRpcReceived(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool isServer, int seqChannel);
        void SetRpcConfiguration(byte rpcId, NetworkGroup group, bool isServer, byte networkVariableId);
        void SetRpcConfiguration(byte rpcId, DeliveryMode deliveryMode, Target target, NetworkGroup group, byte sequenceChannel, bool isServer);
    }
}