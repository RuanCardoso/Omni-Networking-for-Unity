using Newtonsoft.Json;
using Omni.Collections;
using Omni.Inspector;
using System;

namespace Omni.Core
{
    [Serializable]
    [DeclareFoldoutGroup("Data"), DeclareFoldoutGroup("Shared Data")]
    internal class InspectorSerializableGroup
    {
        [ReadOnly]
        public int m_Id;

        [OnValueChanged(nameof(OnNameChanged))]
        public string m_Name;

        [InlineProperty, HideLabel]
        [Group("Data")]
        public ObservableDictionary<string, string> m_Data = new();

        [InlineProperty, HideLabel]
        [Group("Shared Data")]
        public ObservableDictionary<string, string> m_SharedData = new();

        public InspectorSerializableGroup()
        {
            m_Data.OnItemUpdated += OnItemUpdated;
            m_Data.OnItemRemoved += OnItemRemoved;
            m_Data.OnItemAdded += OnItemAdded;
            m_Data.OnUpdate += OnUpdated;
        }

        private void OnUpdated(bool obj)
        {
            UnityEngine.Debug.Log("updated" + m_Id);
        }

        private void OnItemAdded(string arg1, string arg2)
        {
            UnityEngine.Debug.Log("added" + m_Id);
        }

        private void OnItemRemoved(string arg1, string arg2)
        {
            UnityEngine.Debug.Log("removed" + m_Id);
        }

        private void OnItemUpdated(string arg1, string arg2)
        {
            UnityEngine.Debug.Log("updated" + m_Id);
        }

        public void OnNameChanged()
        {
            UnityEngine.Debug.Log("changed" + m_Id);
        }

        [Button("Next Group")]
        public void NextGroup()
        {
            UpdateGroup(nextGroup: true);
        }

        [Button("Previous Group")]
        public void PreviousGroup()
        {
            UpdateGroup(nextGroup: false);
        }

        private void UpdateGroup(bool nextGroup)
        {
#if UNITY_EDITOR
            var groups = NetworkManager.IsHost ? NetworkManager.ServerSide.Groups : NetworkManager.IsServerActive ? NetworkManager.ServerSide.Groups : NetworkManager.ClientSide.Groups;
            if (groups.TryGetValue(m_Id, out var currentGroup))
            {
                if (nextGroup ? currentGroup.TryGetNextGroup(out var group) : currentGroup.TryGetPreviousGroup(out group))
                {
                    m_Id = group.Id;
                    m_Name = group.Name;
                    m_Data = group.Data.ToObservableDictionary(x => x.Key, x => x.Value.ToString());
                }
            }
#endif
        }
    }

    public static class NetworkConstants
    {
        internal const string k_CertificateFile = "certificate.json";
        internal const byte k_NetworkVariableRpcId = 255; // TODO: Add it to the source generator. Don't change, used by Source Generator
        internal const int k_InvalidMasterClientId = -1;

        internal const string k_ShareAllKeys = "_All_";
        internal const string k_InvalidRpcName = "Unknown Rpc";

        internal const byte k_SpawnNotificationId = 252;
        internal const byte k_SetOwnerId = 253;
        internal const byte k_OwnerObjectSpawnedForPeer = 254;
        public const byte k_DestroyEntityId = 255;
    }

    public static class NetworkChannel
    {
        /// <summary>
        /// The default channel used for sending and receiving messages.
        /// </summary>
        public const byte Default = 0;

        /// <summary>
        /// Represents the default communication channel (value = 1).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel = 1;

        /// <summary>
        /// Represents the default communication channel (value = 2).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel2 = 2;

        /// <summary>
        /// Represents the default communication channel (value = 3).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel3 = 3;

        /// <summary>
        /// Represents the default communication channel (value = 4).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel4 = 4;

        /// <summary>
        /// Represents the default communication channel (value = 5).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel5 = 5;

        /// <summary>
        /// Represents the default communication channel (value = 6).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel6 = 6;

        /// <summary>
        /// Represents the default communication channel (value = 7).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel7 = 7;

        /// <summary>
        /// Represents the default communication channel (value = 8).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel8 = 8;

        /// <summary>
        /// Represents the default communication channel (value = 9).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel9 = 9;

        /// <summary>
        /// Represents the default communication channel (value = 10).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel10 = 10;

        /// <summary>
        /// Represents the default communication channel (value = 11).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel11 = 11;

        /// <summary>
        /// Represents the default communication channel (value = 12).
        /// This channel is used exclusively to send and receive messages
        /// when operating with the Lite transporter. It is not valid or
        /// recognized by other transport implementations and will be ignored.
        /// </summary>
        public const byte Channel12 = 12;

        /// <summary>
        /// Identifies a message that is compressed before transmission.
        /// When using the Lite transporter, this type will be delivered
        /// through a separate channel dedicated to compressed messages.
        /// </summary>
        public const byte Compressed = 13;

        /// <summary>
        /// Identifies a message that is encrypted before transmission.
        /// When using the Lite transporter, this type will be delivered
        /// through a separate channel dedicated to encrypted messages.
        /// </summary>
        public const byte Encrypted = 14;

        /// <summary>
        /// Identifies a message that is both compressed and encrypted before transmission.
        /// When using the Lite transporter, this type will be delivered
        /// through a separate channel dedicated to compressed and encrypted messages.
        /// </summary>
        public const byte CompressedEncrypted = 15;
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
    internal class NetworkPacketType
    {
        internal const byte k_Authenticate = 1;
        internal const byte k_BeginHandshake = 2;
        internal const byte k_EndHandshake = 3;
        internal const byte k_LocalRpc = 6;
        internal const byte k_StaticRpc = 7;
        internal const byte k_NtpQuery = 8;
        internal const byte k_GetFetchAsync = 9;
        internal const byte k_GetResponseAsync = 10;
        internal const byte k_PostFetchAsync = 11;
        internal const byte k_PostResponseAsync = 12;
        internal const byte k_SyncPeerSharedData = 13;
        internal const byte k_SyncGroupSharedData = 14;
        internal const byte k_RequestEntityAction = 15;
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
    /// Indicates the stage of a network event, such as connection, disconnection, join, leave or destruction.
    /// Useful for tracking whether the process has started, is active, or has ended.
    /// </summary>
    public enum Phase
    {
        /// <summary>
        /// The process has just started.
        /// For a connection event, this means the negotiation or handshake is in progress and the connection is not yet established.
        /// For a disconnection event, this means the disconnect process has just begun and initial cleanup may be occurring.
        /// </summary>
        Started,

        /// <summary>
        /// The process is in its active or ready state.
        /// For a connection event, this indicates the connection is fully established and authenticated, ready for communication.
        /// For a disconnection event, this indicates that the main cleanup and resource release are in progress.
        /// </summary>
        Active,

        /// <summary>
        /// The process has fully completed.
        /// For a connection event, this is functionally the same as Active (the connection is fully ready and usable).
        /// For a disconnection event, this means all cleanup is complete and the connection has been fully terminated.
        /// </summary>
        Ended
    }

    /// <summary>
    /// Defines the possible recipients for a network GET/POST message within the Omni Networking system.
    /// <para>
    /// This enumeration allows you to control the delivery scope of HTTP-like network requests, 
    /// such as targeting only the sender, broadcasting to all connected peers, or restricting 
    /// delivery to members of the same group. Use these options to optimize network traffic and 
    /// ensure messages reach only the intended audience.
    /// </para>
    /// </summary>
    public enum RouteTarget
    {
        /// <summary>
        /// Sends the message exclusively to the current client (the sender).
        /// <para>
        /// This is useful for operations where only the sender needs to receive the response or update.
        /// </para>
        /// <para>
        /// <b>Note:</b> If the sender is the server (peer ID: 0), the message will be ignored and not processed.
        /// This prevents unnecessary network traffic for server-only operations.
        /// </para>
        /// </summary>
        Self,

        /// <summary>
        /// Broadcasts the message to all connected players, including the sender.
        /// <para>
        /// Use this option when you want every participant in the session to receive the message, 
        /// such as for global announcements or synchronized state updates.
        /// </para>
        /// </summary>
        Everyone,

        /// <summary>
        /// Sends the message to all players who are members of the same group(s) as the sender.
        /// <para>
        /// This is ideal for group-based communication, such as team chat or group-specific events.
        /// Only direct group members will receive the message; sub-groups or nested groups are not included.
        /// </para>
        /// </summary>
        Group,
    }

    /// <summary>
    /// Defines the intended recipients for a network message within the Omni Networking system.
    /// <para>
    /// This enumeration allows you to control the scope and visibility of networked messages, 
    /// supporting scenarios such as broadcasting to all peers, targeting only the sender, 
    /// or restricting communication to specific groups. The <see cref="Auto"/> option provides 
    /// context-sensitive behavior, making it the recommended default for most use cases.
    /// </para>
    /// </summary>
    public enum Target : byte
    {
        /// <summary>
        /// Automatically selects the most appropriate recipients for the network message based on the current context.
        /// <para>
        /// On the server, this typically means broadcasting to all relevant clients. On the client, it may target the server or a specific group,
        /// depending on the operation being performed. This is the default and recommended option for most use cases.
        /// </para>
        /// </summary>
        Auto,

        /// <summary>
        /// Sends the message to all connected peers, including the sender itself.
        /// <para>
        /// Use this to broadcast updates or events that should be visible to every participant in the session, including the originator.
        /// </para>
        /// </summary>
        Everyone,

        /// <summary>
        /// Sends the message exclusively to the sender (the local peer).
        /// <para>
        /// This option is typically used for providing immediate feedback, confirmations, or updates that are only relevant to the sender and should not be visible to other peers.
        /// </para>
        /// <para>
        /// <b>Note:</b> If the sender is the server (peer id: 0), the message will be ignored and not processed. This ensures that server-only operations do not result in unnecessary or redundant network traffic.
        /// </para>
        /// </summary>
        Self,

        /// <summary>
        /// Sends the message to all connected peers except the sender.
        /// <para>
        /// Use this to broadcast information to all participants while excluding the originator, such as when relaying a player's action to others.
        /// </para>
        /// </summary>
        Others,

        /// <summary>
        /// Sends the message to all peers who are members of the same group(s) as the sender.
        /// <para>
        /// Sub-groups are not included. This is useful for group-based communication, such as team chat or localized events.
        /// </para>
        /// </summary>
        Group,

        /// <summary>
        /// Sends the message to all peers in the same group(s) as the sender, excluding the sender itself.
        /// <para>
        /// Sub-groups are not included. Use this to notify group members of an action performed by the sender, without echoing it back.
        /// </para>
        /// </summary>
        GroupOthers,
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

    public class NetworkVariableOptions
    {
        public NetworkGroup Group { get; set; }
    }

    public readonly struct Channel : IComparable, IComparable<Channel>, IComparable<int>, IEquatable<Channel>, IEquatable<int>, IConvertible, IFormattable
    {
        public static Channel MinValue => int.MinValue;
        public static Channel MaxValue => int.MaxValue;
        public static Channel Zero => 0;
        public static Channel One => 1;

        private readonly int value;

        public Channel(int value)
        {
            this.value = value;
        }

        public static implicit operator int(Channel d) => d.value;
        public static implicit operator Channel(int d) => new Channel(d);

        public bool Equals(Channel other) => value == other.value;
        public bool Equals(int other) => value == other;
        public override bool Equals(object obj) => obj is Channel mi ? value == mi.value : obj is int i && value == i;
        public override int GetHashCode() => value.GetHashCode();

        public static bool operator ==(Channel left, Channel right) => left.value == right.value;
        public static bool operator !=(Channel left, Channel right) => left.value != right.value;

        public int CompareTo(Channel other) => value.CompareTo(other.value);
        public int CompareTo(int other) => value.CompareTo(other);
        public int CompareTo(object obj)
        {
            if (obj is Channel mi) return value.CompareTo(mi.value);
            if (obj is int i) return value.CompareTo(i);
            throw new ArgumentException("Object is not a Channel or int");
        }

        public static bool operator <(Channel left, Channel right) => left.value < right.value;
        public static bool operator >(Channel left, Channel right) => left.value > right.value;
        public static bool operator <=(Channel left, Channel right) => left.value <= right.value;
        public static bool operator >=(Channel left, Channel right) => left.value >= right.value;

        public static Channel operator +(Channel a, Channel b) => new Channel(a.value + b.value);
        public static Channel operator -(Channel a, Channel b) => new Channel(a.value - b.value);
        public static Channel operator *(Channel a, Channel b) => new Channel(a.value * b.value);
        public static Channel operator /(Channel a, Channel b) => new Channel(a.value / b.value);
        public static Channel operator %(Channel a, Channel b) => new Channel(a.value % b.value);

        public static Channel operator -(Channel a) => new Channel(-a.value);
        public static Channel operator +(Channel a) => a;

        public static Channel operator &(Channel a, Channel b) => new Channel(a.value & b.value);
        public static Channel operator |(Channel a, Channel b) => new Channel(a.value | b.value);
        public static Channel operator ^(Channel a, Channel b) => new Channel(a.value ^ b.value);
        public static Channel operator ~(Channel a) => new Channel(~a.value);

        public static Channel operator <<(Channel a, int b) => new Channel(a.value << b);
        public static Channel operator >>(Channel a, int b) => new Channel(a.value >> b);

        public override string ToString() => value.ToString();
        public string ToString(string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);

        public TypeCode GetTypeCode() => TypeCode.Int32;
        public bool ToBoolean(IFormatProvider provider) => ((IConvertible)value).ToBoolean(provider);
        public byte ToByte(IFormatProvider provider) => ((IConvertible)value).ToByte(provider);
        public char ToChar(IFormatProvider provider) => ((IConvertible)value).ToChar(provider);
        public DateTime ToDateTime(IFormatProvider provider) => ((IConvertible)value).ToDateTime(provider);
        public decimal ToDecimal(IFormatProvider provider) => ((IConvertible)value).ToDecimal(provider);
        public double ToDouble(IFormatProvider provider) => ((IConvertible)value).ToDouble(provider);
        public short ToInt16(IFormatProvider provider) => ((IConvertible)value).ToInt16(provider);
        public int ToInt32(IFormatProvider provider) => value;
        public long ToInt64(IFormatProvider provider) => ((IConvertible)value).ToInt64(provider);
        public sbyte ToSByte(IFormatProvider provider) => ((IConvertible)value).ToSByte(provider);
        public float ToSingle(IFormatProvider provider) => ((IConvertible)value).ToSingle(provider);
        public string ToString(IFormatProvider provider) => value.ToString(provider);
        public object ToType(Type conversionType, IFormatProvider provider) => ((IConvertible)value).ToType(conversionType, provider);
        public ushort ToUInt16(IFormatProvider provider) => ((IConvertible)value).ToUInt16(provider);
        public uint ToUInt32(IFormatProvider provider) => ((IConvertible)value).ToUInt32(provider);
        public ulong ToUInt64(IFormatProvider provider) => ((IConvertible)value).ToUInt64(provider);
    }
}