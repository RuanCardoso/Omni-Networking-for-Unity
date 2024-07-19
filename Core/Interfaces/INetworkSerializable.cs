namespace Omni.Core
{
    public interface ISerializable
    {
        void Serialize(DataBuffer writer);
        void Deserialize(DataBuffer reader);
    }

    public interface ISerializableWithPeer : ISerializable
    {
        NetworkPeer Peer { get; set; }
        bool IsServer { get; set; }
    }
}
