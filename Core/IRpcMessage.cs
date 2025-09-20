#pragma warning disable
namespace Omni.Core.Interfaces
{
    internal interface IRpcMessage
    {
        NetworkGroup DefaultGroup { get; set; }
        int IdentityId { get; }
        __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> __ServerRpcHandler { get; }
        __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> __ClientRpcHandler { get; }
        void OnRpcReceived(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool isServer, int seqChannel);
        void SetupRpcMessage(byte rpcId, NetworkGroup group, bool isServer, byte networkVariableId);
        void SetupRpcMessage(byte rpcId, DeliveryMode deliveryMode, Target target, NetworkGroup group, byte sequenceChannel, bool isServer);
    }
}