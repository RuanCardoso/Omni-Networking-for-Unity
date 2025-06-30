namespace Omni.Core
{
	/// <summary>
	/// Interface for network-serializable messages that can be sent over the network.
	/// </summary>
	public interface IMessage
	{
		/// <summary>
		/// Serializes the message data into a DataBuffer for network transmission.
		/// </summary>
		/// <param name="writer">The DataBuffer to write the serialized data to.</param>
		void Serialize(DataBuffer writer);

		/// <summary>
		/// Deserializes message data from a DataBuffer received from the network.
		/// </summary>
		/// <param name="reader">The DataBuffer containing the data to deserialize.</param>
		void Deserialize(DataBuffer reader);
	}

	/// <summary>
	/// Extended interface for messages that require peer information for network communication.
	/// Provides additional context for handling peer-specific operations like encryption and authentication.
	/// </summary>
	public interface IMessageWithPeer : IMessage
	{
		/// <summary>
		/// Gets or sets the NetworkPeer associated with this message.
		/// Useful for encryption and authentication.
		/// </summary>
		NetworkPeer SharedPeer { get; set; }

		/// <summary>
		/// Gets or sets whether the current context is running on the server side.
		/// </summary>
		bool IsServer { get; set; }
	}
}