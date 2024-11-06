using System;
using Newtonsoft.Json;

namespace Omni.Core
{
	internal class NetworkConstants
	{
		internal const byte NET_VAR_RPC_ID = 255; // TODO: Add it to the source generator. Don't change, used by Source Generator.
		internal const int INVALID_MASTER_CLIENT_ID = -1;
	}

	enum ScriptingBackend
	{
		IL2CPP,
		Mono
	}

	public enum SceneOperationMode
	{
		Load,
		Unload
	}

	internal class MessageType // not a enum to avoid casting
	{
		internal const byte SetOwner = 240;
		internal const byte Destroy = 241;
		internal const byte Spawn = 242;
		internal const byte SyncGroupSerializedData = 243;
		internal const byte SyncPeerSerializedData = 244;
		internal const byte HttpPostResponseAsync = 245;
		internal const byte HttpPostFetchAsync = 246;
		internal const byte HttpGetResponseAsync = 247;
		internal const byte HttpGetFetchAsync = 248;
		internal const byte NtpQuery = 249;
		internal const byte BeginHandshake = 250;
		internal const byte EndHandshake = 251;
		internal const byte LocalInvoke = 252;
		internal const byte GlobalInvoke = 253;
		internal const byte LeaveGroup = 254;
		internal const byte JoinGroup = 255;
	}

	[Flags]
	public enum BindingFlags
	{
		DeclaredOnly = 2,
		Instance = 4,
		Public = 16,
		NonPublic = 32,
	}

	[Flags]
	public enum CacheMode
	{
		// Unique
		None = 0,

		// Combine
		New = 1,
		Overwrite = 2,

		// Combine
		Global = 4,
		Group = 8,
		Peer = 16,

		// Unique
		AutoDestroy = 32,
	}

	public enum Module
	{
		Console,
		Connection,
		Matchmaking,
		NtpClock,
		TickSystem
	}

	public enum Phase
	{
		/// <summary>
		/// Indicates the initial phase of an event.
		/// Typically used to signal the start of a process.
		/// </summary>
		Begin,

		/// <summary>
		/// Represents the intermediate phase of an event.
		/// This status is used when the main actions or operations are being performed.
		/// </summary>
		Normal,

		/// <summary>
		/// Marks the final phase of an event.
		/// It signifies the completion and cleanup of the process.
		/// </summary>
		End
	}

	/// <summary>
	/// Specifies the target recipients for a GET/POST message.
	/// </summary>
	public enum HttpTarget
	{
		/// <summary>
		/// Sends the message to the current client itself. If the peer ID is 0 (server), the message is not executed.
		/// </summary>
		Self,

		/// <summary>
		/// Broadcasts the message to all connected players.
		/// </summary>
		All,

		/// <summary>
		/// Sends the message to all players who are members of the same groups as the sender.
		/// </summary>
		GroupMembers,

		/// <summary>
		/// Sends the message to all players who are not members of any groups.
		/// </summary>
		NonGroupMembers,
	}

	/// <summary>
	/// Specifies the target recipients for a network message.
	/// </summary>
	public enum Target : byte
	{
		/// <summary>
		/// Broadcasts the message to all connected players.
		/// </summary>
		All,

		/// <summary>
		/// Sends the message to the current client itself. If the peer ID is 0 (server), the message is not executed.
		/// </summary>
		Self,

		/// <summary>
		/// Sends the message to all players except the sender.
		/// </summary>
		AllExceptSelf,

		/// <summary>
		/// Sends the message to all players who are members of the same groups as the sender(sub groups not included).
		/// </summary>
		GroupMembers,

		/// <summary>
		/// Sends the message to all players(except the sender) who are members of the same groups as the sender(sub groups not included).
		/// </summary>
		GroupMembersExceptSelf,

		/// <summary>
		/// Sends the message to all players who are not members of any groups.
		/// </summary>
		NonGroupMembers,

		/// <summary>
		/// Sends the message to all players who are not members of any groups. Except the sender.
		/// </summary>
		NonGroupMembersExceptSelf
	}

	/// <summary>
	/// Specifies the delivery mode for network packets within a communication protocol.
	/// </summary>
	public enum DeliveryMode : byte
	{
		/// <summary>
		/// Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive in order.
		/// </summary>
		ReliableOrdered,

		/// <summary>
		/// Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
		/// </summary>
		Unreliable,

		/// <summary>
		/// Reliable. Packets won't be dropped, won't be duplicated, can arrive without order.
		/// </summary>
		ReliableUnordered,

		/// <summary>
		/// Unreliable. Packets can be dropped, won't be duplicated, will arrive in order.
		/// </summary>
		Sequenced,

		/// <summary>
		/// Reliable only last packet. Packets can be dropped (except the last one), won't be duplicated, will arrive in order.
		/// Cannot be fragmented
		/// </summary>
		ReliableSequenced
	}

	public class CacheOp
	{
		public const CacheMode PEER_NEW = CacheMode.Peer | CacheMode.New;
		public const CacheMode PEER_NEW_DESTROY =
			CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy;

		public const CacheMode PEER_OVERWRITE = CacheMode.Peer | CacheMode.Overwrite;
		public const CacheMode PEER_OVERWRITE_DESTROY =
			CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy;

		public const CacheMode GROUP_NEW = CacheMode.Group | CacheMode.New;
		public const CacheMode GROUP_NEW_DESTROY =
			CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy;

		public const CacheMode GROUP_OVERWRITE = CacheMode.Group | CacheMode.Overwrite;
		public const CacheMode GROUP_OVERWRITE_DESTROY =
			CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy;

		public const CacheMode GLOBAL_NEW = CacheMode.Global | CacheMode.New;
		public const CacheMode GLOBAL_NEW_DESTROY =
			CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy;

		public const CacheMode GLOBAL_OVERWRITE = CacheMode.Global | CacheMode.Overwrite;
		public const CacheMode GLOBAL_OVERWRITE_DESTROY =
			CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy;
	}

	[JsonObject(MemberSerialization.OptIn)]
	internal class ImmutableKeyValuePair
	{
		[JsonProperty]
		internal string Key { get; set; }

		[JsonProperty]
		internal object Value { get; set; }

		[JsonConstructor]
		internal ImmutableKeyValuePair() { }

		internal ImmutableKeyValuePair(string key, object value)
		{
			Key = key;
			Value = value;
		}
	}

	/// <summary>
	/// Provides default synchronization options with the following settings:<br/>
	/// - <c>Target</c>: <see cref="Target.All"/> - Specifies that the target includes all recipients.<br/>
	/// - <c>DeliveryMode</c>: <see cref="DeliveryMode.ReliableOrdered"/> - Ensures messages are delivered reliably and in order.<br/>
	/// - <c>GroupId</c>: 0 - Indicates no specific group identifier.<br/>
	/// - <c>CacheId</c>: 0 - Indicates no specific cache identifier.<br/>
	/// - <c>CacheMode</c>: <see cref="CacheMode.None"/> - Specifies that no caching is used.<br/>
	/// - <c>SequenceChannel</c>: 0 - Uses the default sequence channel.
	/// </summary>
	public class NetworkVariableOptions
	{
		public Target Target { get; set; }
		public DeliveryMode DeliveryMode { get; set; }
		public int GroupId { get; set; }
		public int CacheId { get; set; }
		public CacheMode CacheMode { get; set; }
		public byte SequenceChannel { get; set; }

		public NetworkVariableOptions()
		{
			Target = Target.All;
			DeliveryMode = DeliveryMode.ReliableOrdered;
			GroupId = 0;
			CacheId = 0;
			CacheMode = CacheMode.None;
			SequenceChannel = 0;
		}
	}

	/// <summary>
	/// Provides default synchronization options with the following settings:<br/>
	/// - <c>Target</c>: <see cref="Target.All"/> - Specifies that the target includes all recipients.<br/>
	/// - <c>DeliveryMode</c>: <see cref="DeliveryMode.ReliableOrdered"/> - Ensures messages are delivered reliably and in order.<br/>
	/// - <c>GroupId</c>: 0 - Indicates no specific group identifier.<br/>
	/// - <c>CacheId</c>: 0 - Indicates no specific cache identifier.<br/>
	/// - <c>CacheMode</c>: <see cref="CacheMode.None"/> - Specifies that no caching is used.<br/>
	/// - <c>SequenceChannel</c>: 0 - Uses the default sequence channel.
	/// </summary>
	public ref struct SyncOptions
	{
		public Target Target { get; set; }
		public DeliveryMode DeliveryMode { get; set; }
		public int GroupId { get; set; }
		public int CacheId { get; set; }
		public CacheMode CacheMode { get; set; }
		public byte SequenceChannel { get; set; }
		public DataBuffer Buffer { get; set; }

		/// <summary>
		/// Provides default synchronization options with the following settings:<br/>
		/// - <c>Target</c>: <see cref="Target.All"/> - Specifies that the target includes all recipients.<br/>
		/// - <c>DeliveryMode</c>: <see cref="DeliveryMode.ReliableOrdered"/> - Ensures messages are delivered reliably and in order.<br/>
		/// - <c>GroupId</c>: 0 - Indicates no specific group identifier.<br/>
		/// - <c>CacheId</c>: 0 - Indicates no specific cache identifier.<br/>
		/// - <c>CacheMode</c>: <see cref="CacheMode.None"/> - Specifies that no caching is used.<br/>
		/// - <c>SequenceChannel</c>: 0 - Uses the default sequence channel.
		/// </summary>
		public SyncOptions(DataBuffer buffer)
		{
			Buffer = buffer;
			Target = Target.All;
			DeliveryMode = DeliveryMode.ReliableOrdered;
			GroupId = 0;
			CacheId = 0;
			CacheMode = CacheMode.None;
			SequenceChannel = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SyncOptions"/> struct with the specified parameters.
		/// </summary>
		public SyncOptions(
			DataBuffer buffer,
			Target target,
			DeliveryMode deliveryMode,
			int groupId,
			int cacheId,
			CacheMode cacheMode,
			byte sequenceChannel
		)
		{
			Buffer = buffer;
			Target = target;
			DeliveryMode = deliveryMode;
			GroupId = groupId;
			CacheId = cacheId;
			CacheMode = cacheMode;
			SequenceChannel = sequenceChannel;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SyncOptions"/> struct with the specified parameters.
		/// </summary>
		public SyncOptions(
			Target target,
			DeliveryMode deliveryMode,
			int groupId,
			int cacheId,
			CacheMode cacheMode,
			byte sequenceChannel
		)
		{
			Buffer = null;
			Target = target;
			DeliveryMode = deliveryMode;
			GroupId = groupId;
			CacheId = cacheId;
			CacheMode = cacheMode;
			SequenceChannel = sequenceChannel;
		}

		/// <summary>
		/// Provides default synchronization options with the following settings:<br/>
		/// - <c>Target</c>: <see cref="Target.All"/> - Specifies that the target includes all recipients.<br/>
		/// - <c>DeliveryMode</c>: <see cref="DeliveryMode.ReliableOrdered"/> - Ensures messages are delivered reliably and in order.<br/>
		/// - <c>GroupId</c>: 0 - Indicates no specific group identifier.<br/>
		/// - <c>CacheId</c>: 0 - Indicates no specific cache identifier.<br/>
		/// - <c>CacheMode</c>: <see cref="CacheMode.None"/> - Specifies that no caching is used.<br/>
		/// - <c>SequenceChannel</c>: 0 - Uses the default sequence channel.
		/// </summary>
		public SyncOptions(bool useDefaultOptions)
		{
			Buffer = null;
			Target = Target.All;
			DeliveryMode = DeliveryMode.ReliableOrdered;
			GroupId = 0;
			CacheId = 0;
			CacheMode = CacheMode.None;
			SequenceChannel = 0;
		}

		/// <summary>
		/// Provides default synchronization options with the following settings:<br/>
		/// - <c>Target</c>: <see cref="Target.All"/> - Specifies that the target includes all recipients.<br/>
		/// - <c>DeliveryMode</c>: <see cref="DeliveryMode.ReliableOrdered"/> - Ensures messages are delivered reliably and in order.<br/>
		/// - <c>GroupId</c>: 0 - Indicates no specific group identifier.<br/>
		/// - <c>CacheId</c>: 0 - Indicates no specific cache identifier.<br/>
		/// - <c>CacheMode</c>: <see cref="CacheMode.None"/> - Specifies that no caching is used.<br/>
		/// - <c>SequenceChannel</c>: 0 - Uses the default sequence channel.
		/// </summary>
		public static SyncOptions Default => new SyncOptions(true);
	}
}
