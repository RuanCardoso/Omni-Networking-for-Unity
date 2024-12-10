using Newtonsoft.Json;
using System;

namespace Omni.Core
{
    internal class NetworkConstants
    {
        // TODO: Add it to the source generator. Don't change, used by Source Generator
        internal const byte NET_VAR_RPC_ID = 255;
        internal const int INVALID_MASTER_CLIENT_ID = -1;
    }

    internal enum ScriptingBackend
    {
        IL2CPP,
        Mono
    }

    public enum SceneOperationMode
    {
        Load,
        Unload
    }

    // not a enum to avoid casting
    internal class MessageType
    {
        internal const byte KCP_PING_REQUEST_RESPONSE = 238;
        internal const byte RequestEntityAction = 239;
        internal const byte SetOwner = 240;
        internal const byte Destroy = 241;
        internal const byte Spawn = 242;
        internal const byte SyncGroupSerializedData = 243;
        internal const byte SyncPeerSerializedData = 244;
        internal const byte PostResponseAsync = 245;
        internal const byte PostFetchAsync = 246;
        internal const byte GetResponseAsync = 247;
        internal const byte GetFetchAsync = 248;
        internal const byte NtpQuery = 249;
        internal const byte BeginHandshake = 250;
        internal const byte EndHandshake = 251;
        internal const byte LocalRpc = 252;
        internal const byte GlobalRpc = 253;
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

    /// <summary>
    /// Represents the phase or stage of a network-related event, such as connection, disconnection, or destruction.
    /// </summary>
    public enum Phase
    {
        /// <summary>
        /// The initial phase of the event, typically signaling the start of a process.
        /// For example, this phase may occur when a connection or object is being initialized.
        /// </summary>
        Begin,

        /// <summary>
        /// The intermediate phase of the event, representing its active or ongoing state.
        /// During this phase, main operations, processing, or interactions are performed.
        /// </summary>
        Normal,

        /// <summary>
        /// The final phase of the event, indicating its conclusion and cleanup.
        /// This phase is triggered when a connection is closed, an object is destroyed, or processes are finalized.
        /// </summary>
        End
    }

    /// <summary>
    /// Specifies the target recipients for a GET/POST message.
    /// </summary>
    public enum RouteTarget
    {
        /// <summary>
        /// Sends the message to the current client only. If the peer ID is 0 (server), the message is ignored.
        /// </summary>
        SelfOnly,

        /// <summary>
        /// Broadcasts the message to all connected players, including the sender.
        /// </summary>
        AllPlayers,

        /// <summary>
        /// Sends the message to all players who belong to the same group(s) as the sender.
        /// Sub-groups are not included.
        /// </summary>
        GroupOnly,

        /// <summary>
        /// Sends the message to all players who do not belong to any group.
        /// </summary>
        UngroupedPlayers,
    }

    /// <summary>
    /// Specifies the target recipients for a network message.
    /// </summary>
    public enum Target : byte
    {
        /// <summary>
        /// Automatically determines the target recipients for the network message based on the context.
        /// </summary>
        Auto,

        /// <summary>
        /// Broadcasts the message to all connected players, including the sender.
        /// </summary>
        AllPlayers,

        /// <summary>
        /// Sends the message to the sender itself. If the sender's peer ID is 0 (server), the message is ignored.
        /// </summary>
        SelfOnly,

        /// <summary>
        /// Sends the message to all connected players except the sender.
        /// </summary>
        AllPlayersExceptSelf,

        /// <summary>
        /// Sends the message to all players who are members of the same group(s) as the sender.
        /// Sub-groups are not included.
        /// </summary>
        GroupOnly,

        /// <summary>
        /// Sends the message to all players in the same group(s) as the sender, excluding the sender itself.
        /// Sub-groups are not included.
        /// </summary>
        GroupExceptSelf,

        /// <summary>
        /// Sends the message to all players who are not members of any group.
        /// </summary>
        UngroupedPlayers,

        /// <summary>
        /// Sends the message to all players who are not members of any group, excluding the sender.
        /// </summary>
        UngroupedPlayersExceptSelf,
    }

    /// <summary>
    /// Specifies the delivery mode for network packets within a communication protocol.
    /// </summary>
    public enum DeliveryMode : byte
    {
        /// <summary>
        /// Ensures packets are delivered reliably and in the exact order they were sent.
        /// No packets will be dropped, duplicated, or arrive out of order.
        /// </summary>
        ReliableOrdered,

        /// <summary>
        /// Sends packets without guarantees. Packets may be dropped, duplicated, or arrive out of order.
        /// This mode offers the lowest latency but no reliability.
        /// </summary>
        Unreliable,

        /// <summary>
        /// Ensures packets are delivered reliably but without enforcing any specific order.
        /// Packets won't be dropped or duplicated, but they may arrive out of sequence.
        /// </summary>
        ReliableUnordered,

        /// <summary>
        /// Sends packets without reliability but guarantees they will arrive in order.
        /// Packets may be dropped, but no duplicates will occur, and order is preserved.
        /// </summary>
        Sequenced,

        /// <summary>
        /// Ensures only the latest packet in a sequence is delivered reliably and in order.
        /// Intermediate packets may be dropped, but duplicates will not occur, and the last packet is guaranteed.
        /// This mode does not support fragmentation.
        /// </summary>
        ReliableSequenced
    }

    public class CachePresets
    {
        // Peer-scoped cache operations
        public const CacheMode PeerNew = CacheMode.Peer | CacheMode.New;
        public const CacheMode PeerNewWithAutoDestroy = CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy;

        public const CacheMode PeerOverwrite = CacheMode.Peer | CacheMode.Overwrite;

        public const CacheMode PeerOverwriteWithAutoDestroy =
            CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy;

        // Group-scoped cache operations
        public const CacheMode GroupNew = CacheMode.Group | CacheMode.New;
        public const CacheMode GroupNewWithAutoDestroy = CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy;

        public const CacheMode GroupOverwrite = CacheMode.Group | CacheMode.Overwrite;

        public const CacheMode GroupOverwriteWithAutoDestroy =
            CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy;

        // Global-scoped cache operations
        public const CacheMode ServerNew = CacheMode.Global | CacheMode.New;
        public const CacheMode ServerNewWithAutoDestroy = CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy;

        public const CacheMode ServerOverwrite = CacheMode.Global | CacheMode.Overwrite;

        public const CacheMode ServerOverwriteWithAutoDestroy =
            CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy;
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal class ImmutableKeyValuePair
    {
        [JsonProperty] internal string Key { get; set; }

        [JsonProperty] internal object Value { get; set; }

        [JsonConstructor]
        internal ImmutableKeyValuePair()
        {
        }

        internal ImmutableKeyValuePair(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Represents the default synchronization options for network variables.
    /// </summary>
    /// <remarks>
    /// The following default settings are applied:
    /// <list type="bullet">
    /// <item><term><c>Target</c></term><description>Set to <see cref="Target.Auto"/>. Automatically determines the target recipients for the network message based on the context. <b>[Not valid on the client side]</b></description></item>
    /// <item><term><c>DeliveryMode</c></term><description>Set to <see cref="DeliveryMode.ReliableOrdered"/>. Ensures messages are delivered reliably and in sequential order.</description></item>
    /// <item><term><c>GroupId</c></term><description>Set to <c>0</c>. Indicates no specific group identifier. <b>[Not valid on the client side]</b></description></item>
    /// <item><term><c>DataCache</c></term><description>Set to <see cref="DataCache.None"/>. Specifies that no caching mechanism is used. <b>[Not valid on the client side]</b></description></item>
    /// <item><term><c>SequenceChannel</c></term><description>Set to <c>0</c>. Uses the default sequence channel for ordered message delivery.</description></item>
    /// </list>
    /// </remarks>
    public class NetworkVariableOptions
    {
        public Target Target { get; set; }
        public DeliveryMode DeliveryMode { get; set; }
        public int GroupId { get; set; }
        public DataCache DataCache { get; set; }
        public byte SequenceChannel { get; set; }

        public NetworkVariableOptions()
        {
            Target = Target.Auto; // NOT VALID FOR CLIENT SIDE
            DeliveryMode = DeliveryMode.ReliableOrdered;
            GroupId = 0; // NOT VALID FOR CLIENT SIDE
            DataCache = DataCache.None; // NOT VALID FOR CLIENT SIDE
            SequenceChannel = 0;
        }

        public static implicit operator NetworkVariableOptions(ClientOptions options)
        {
            return new NetworkVariableOptions()
            {
                DeliveryMode = options.DeliveryMode,
                SequenceChannel = options.SequenceChannel,
            };
        }

        public static implicit operator NetworkVariableOptions(ServerOptions options)
        {
            return new NetworkVariableOptions()
            {
                Target = options.Target,
                DeliveryMode = options.DeliveryMode,
                GroupId = options.GroupId,
                DataCache = options.DataCache,
                SequenceChannel = options.SequenceChannel
            };
        }
    }

    /// <summary>
    /// Represents the synchronization options for network communication on the client.
    /// </summary>
    public ref struct ClientOptions
    {
        public DeliveryMode DeliveryMode { get; set; }
        public byte SequenceChannel { get; set; }
        public DataBuffer Buffer { get; set; }

        public ClientOptions(DataBuffer message = null)
        {
            message ??= DataBuffer.Empty;
            Buffer = message;
            DeliveryMode = DeliveryMode.ReliableOrdered;
            SequenceChannel = 0;
        }
    }

    /// <summary>
    /// Represents the synchronization options for network communication on the server.
    /// </summary>
    public ref struct ServerOptions
    {
        public Target Target { get; set; }
        public DeliveryMode DeliveryMode { get; set; }
        public int GroupId { get; set; }
        public DataCache DataCache { get; set; }
        public byte SequenceChannel { get; set; }
        public DataBuffer Buffer { get; set; }

        public ServerOptions(DataBuffer message = null)
        {
            message ??= DataBuffer.Empty;
            Buffer = message;
            Target = Target.Auto;
            DeliveryMode = DeliveryMode.ReliableOrdered;
            GroupId = 0;
            DataCache = DataCache.None;
            SequenceChannel = 0;
        }
    }
}