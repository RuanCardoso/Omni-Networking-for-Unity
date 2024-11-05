namespace Omni.Core
{
	public interface IMessage
	{
		void Serialize(DataBuffer writer);
		void Deserialize(DataBuffer reader);
	}

	public interface IMessageWithPeer : IMessage
	{
		/// <summary>
		/// Useful for encryption and authentication.
		/// </summary>
		NetworkPeer SharedPeer { get; set; }
		bool IsServer { get; set; }
	}
}
