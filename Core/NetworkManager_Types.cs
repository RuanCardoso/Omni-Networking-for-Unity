using Newtonsoft.Json;
using System;

namespace Omni.Core
{
    internal class NetworkConstants
    {
        // TODO: Add it to the source generator. Don't change, used by Source Generator
        internal const byte NETWORK_VARIABLE_RPC_ID = 255;
        internal const int INVALID_MASTER_CLIENT_ID = -1;
        internal const string SHARED_ALL_KEYS = "_All_";
        internal const string INVALID_RPC_NAME = "Unknown Rpc";
    }

    internal enum ScriptingBackend
    {
        IL2CPP,
        Mono
    }

    /// <summary>
    /// Defines the mode of operation for scene-related actions,
    /// such as loading or unloading scenes within the networked environment.
    /// </summary>
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
        internal const byte Despawn = 241;
        internal const byte Spawn = 242;
        internal const byte SyncGroupSharedData = 243;
        internal const byte SyncPeerSharedData = 244;
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

    /// <summary>
    /// Provides predefined caching modes for various scoped cache operations.
    /// </summary>
    /// <remarks>
    /// The class defines multiple cache presets by combining specific <see cref="CacheMode"/> flags to represent common cache usage scenarios.
    /// These presets are categorized into the following scopes:
    /// <list type="bullet">
    /// <item>
    /// <term>Peer-Scoped Cache Operations</term>
    /// <description>
    /// <list type="bullet">
    /// <item><term><c>PeerNew</c></term><description>Combines <see cref="CacheMode.Peer"/> and <see cref="CacheMode.New"/>. Creates a new cache entry specific to a peer.</description></item>
    /// <item><term><c>PeerNewWithAutoDestroy</c></term><description>Extends <c>PeerNew</c> with <see cref="CacheMode.AutoDestroy"/>. Automatically destroys the cache entry when it is no longer needed.</description></item>
    /// <item><term><c>PeerOverwrite</c></term><description>Combines <see cref="CacheMode.Peer"/> and <see cref="CacheMode.Overwrite"/>. Overwrites an existing cache entry specific to a peer.</description></item>
    /// <item><term><c>PeerOverwriteWithAutoDestroy</c></term><description>Extends <c>PeerOverwrite</c> with <see cref="CacheMode.AutoDestroy"/>. Automatically destroys the existing entry after overwriting.</description></item>
    /// </list>
    /// </description>
    /// </item>
    /// <item>
    /// <term>Group-Scoped Cache Operations</term>
    /// <description>
    /// <list type="bullet">
    /// <item><term><c>GroupNew</c></term><description>Combines <see cref="CacheMode.Group"/> and <see cref="CacheMode.New"/>. Creates a new cache entry for a group.</description></item>
    /// <item><term><c>GroupNewWithAutoDestroy</c></term><description>Extends <c>GroupNew</c> with <see cref="CacheMode.AutoDestroy"/>. Automatically destroys the cache entry when no longer needed.</description></item>
    /// <item><term><c>GroupOverwrite</c></term><description>Combines <see cref="CacheMode.Group"/> and <see cref="CacheMode.Overwrite"/>. Overwrites an existing group cache entry.</description></item>
    /// <item><term><c>GroupOverwriteWithAutoDestroy</c></term><description>Extends <c>GroupOverwrite</c> with <see cref="CacheMode.AutoDestroy"/>. Automatically destroys the existing entry after overwriting.</description></item>
    /// </list>
    /// </description>
    /// </item>
    /// <item>
    /// <term>Global-Scoped Cache Operations</term>
    /// <description>
    /// <list type="bullet">
    /// <item><term><c>ServerNew</c></term><description>Combines <see cref="CacheMode.Global"/> and <see cref="CacheMode.New"/>. Creates a new global cache entry.</description></item>
    /// <item><term><c>ServerNewWithAutoDestroy</c></term><description>Extends <c>ServerNew</c> with <see cref="CacheMode.AutoDestroy"/>. Automatically destroys the cache entry when no longer needed.</description></item>
    /// <item><term><c>ServerOverwrite</c></term><description>Combines <see cref="CacheMode.Global"/> and <see cref="CacheMode.Overwrite"/>. Overwrites an existing global cache entry.</description></item>
    /// <item><term><c>ServerOverwriteWithAutoDestroy</c></term><description>Extends <c>ServerOverwrite</c> with <see cref="CacheMode.AutoDestroy"/>. Automatically destroys the existing entry after overwriting.</description></item>
    /// </list>
    /// </description>
    /// </item>
    /// </list>
    /// These presets are useful for simplifying cache management in various scoped scenarios.
    /// </remarks>
    public class CachePresets
    {
        // Peer-scoped cache operations
        /// Represents a combination of cache modes specific to peer-scoped cache operations
        /// and a new cache creation. This constant is used to define a caching behavior
        /// that associates operations with individual peers while ensuring the creation
        /// of new cache entries.
        public const CacheMode PeerNew = CacheMode.Peer | CacheMode.New;

        /// Represents a peer-scoped cache mode configuration with the combination of
        /// `CacheMode.Peer`, `CacheMode.New`, and `CacheMode.AutoDestroy`.
        /// This configuration ensures that the cache is scoped to the peer, marks
        /// it as new, and enables automatic destruction upon invalidation or specific
        /// conditions.
        public const CacheMode PeerNewWithAutoDestroy = CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy;

        /// <summary>
        /// Represents a cache operation mode intended for peer-scoped overwrite operations.
        /// Combines the <see cref="CacheMode.Peer"/> and <see cref="CacheMode.Overwrite"/> flags.
        /// Used to indicate that existing cached data for a specific peer should be replaced with new data.
        /// </summary>
        public const CacheMode PeerOverwrite = CacheMode.Peer | CacheMode.Overwrite;

        /// Represents a combination of cache modes for operations scoped to a peer.
        /// This preset combines the `Peer`, `Overwrite`, and `AutoDestroy` flags from the `CacheMode` enumeration.
        /// It is used to specify that cached data should be overwritten, scoped to a peer,
        /// and automatically destroyed when no longer needed.
        public const CacheMode PeerOverwriteWithAutoDestroy =
            CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy;

        // Group-scoped cache operations
        /// <summary>
        /// Represents a cache operation mode where a new entry is created within the group scope.
        /// Combines <see cref="CacheMode.Group"/> and <see cref="CacheMode.New"/> to ensure that the
        /// cache is scoped to the group and a new entry is created. Does not overwrite existing entries.
        /// </summary>
        public const CacheMode GroupNew = CacheMode.Group | CacheMode.New;

        /// Represents a caching mode that operates on a group scope where new cache entries
        /// are created, and they are automatically destroyed when their scope ends.
        /// Combines the Group, New, and AutoDestroy flags from the CacheMode enumeration.
        public const CacheMode GroupNewWithAutoDestroy = CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy;

        /// <summary>
        /// Represents a cache mode where existing data within the specified group is overwritten.
        /// Combines the <see cref="CacheMode.Group"/> and <see cref="CacheMode.Overwrite"/> flags.
        /// Typically used to update or replace existing cached information for group-scoped operations.
        /// </summary>
        public const CacheMode GroupOverwrite = CacheMode.Group | CacheMode.Overwrite;

        /// Represents a combined cache mode configuration that applies the following settings:
        /// 1. Group-scoped cache operations (CacheMode.Group).
        /// 2. Overwrite behavior for existing entries (CacheMode.Overwrite).
        /// 3. Automatic destruction of cache entries when no longer needed (CacheMode.AutoDestroy).
        /// This is useful for managing group-level cached data with automatic cleanup.
        public const CacheMode GroupOverwriteWithAutoDestroy =
            CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy;

        // Global-scoped cache operations
        /// <summary>
        /// Represents a cache operation mode configured for creating new global-scoped cache entities on the server.
        /// Combines the <see cref="CacheMode.Global"/> and <see cref="CacheMode.New"/> flags to define a unique
        /// operational behavior for managing server-level cache.
        /// </summary>
        public const CacheMode ServerNew = CacheMode.Global | CacheMode.New;

        /// <summary>
        /// Represents a global-scoped cache operation mode that combines the creation of a new cache entry
        /// with automatic cleanup.
        /// This constant is defined using a combination of the <c>CacheMode.Global</c>, <c>CacheMode.New</c>,
        /// and <c>CacheMode.AutoDestroy</c> flags, enabling new cache data to be added globally and
        /// automatically removed when no longer needed.
        /// </summary>
        public const CacheMode ServerNewWithAutoDestroy = CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy;

        /// <summary>
        /// Represents a cache operation mode that applies globally across the network (Global scope),
        /// and is used to overwrite existing cache entries with new data.
        /// </summary>
        public const CacheMode ServerOverwrite = CacheMode.Global | CacheMode.Overwrite;

        /// Represents a cache operation mode that performs an overwrite operation on the global (server) cache
        /// and marks the cached entries for automatic destruction when they are no longer needed.
        /// Combines the CacheMode flags `Global`, `Overwrite`, and `AutoDestroy`.
        /// Used to manage server-scoped cache entries with automatic cleanup after they are updated.
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