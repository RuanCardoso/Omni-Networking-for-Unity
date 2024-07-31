namespace Omni.Core
{
    public interface ISerializable
    {
        void Serialize(DataBuffer writer);
        void Deserialize(DataBuffer reader);
    }

    public interface ISerializableWithPeer : ISerializable
    {
        /// <summary>
        /// Useful for encryption and authentication.
        /// </summary>
        NetworkPeer SharedPeer { get; set; }
        bool IsServer { get; set; }
    }
}
