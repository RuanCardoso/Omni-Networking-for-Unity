using Omni.Core.Interfaces;
using System.Runtime.CompilerServices;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    public class NetworkEventClient
    {
        private readonly IRpcMessage m_NetworkMessage;
        private readonly NetworkVariablesBehaviour m_NetworkVariablesBehaviour;
        private readonly BindingFlags m_BindingFlags;

        internal NetworkEventClient(IRpcMessage networkMessage, BindingFlags flags)
        {
            m_NetworkMessage = networkMessage;
            m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
            m_BindingFlags = flags;
        }

        /// <summary>
        /// Sends a manual 'NetworkVariable' message to the server with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions syncOptions)
        {
            NetworkVariableSync<T>(property, propertyId, syncOptions.DeliveryMode, syncOptions.SequenceChannel);
        }

        /// <summary>
        /// Sends a manual 'NetworkVariable' message to the server with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void NetworkVariableSync<T>(T property, byte propertyId,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0)
        {
            using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(property, propertyId);
            Rpc(NetworkConstants.NETWORK_VARIABLE_RPC_ID, message, deliveryMode, sequenceChannel);
        }

        /// <summary>
        /// Automatically sends a 'NetworkVariable' message to the server based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        public void NetworkVariableSync<T>(NetworkVariableOptions options, [CallerMemberName] string ___ = "")
        {
            NetworkVariableSync<T>(options.DeliveryMode, options.SequenceChannel, ___);
        }

        /// <summary>
        /// Automatically sends a 'NetworkVariable' message to the server based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void NetworkVariableSync<T>(DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0,
            [CallerMemberName] string ___ = "")
        {
            IPropertyInfo property = m_NetworkVariablesBehaviour.GetPropertyInfoWithCallerName<T>(___, m_BindingFlags);
            if (property is IPropertyInfo<T> propertyGeneric)
            {
                using DataBuffer message =
                    m_NetworkVariablesBehaviour.CreateHeader(propertyGeneric.Invoke(), property.Id);
                Rpc(NetworkConstants.NETWORK_VARIABLE_RPC_ID, message, deliveryMode, sequenceChannel);
            }
        }

        /// <summary>
        /// Sends a global Remote Procedure Call (RPC) to the server with the specified message ID and client options.
        /// </summary>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="options">The client options containing delivery mode, sequence channel, and optional data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, ClientOptions options)
        {
            GlobalRpc(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
        }

        /// <summary>
        /// Sends a global Remote Procedure Call (RPC) to the server with the specified message ID and optional parameters.
        /// </summary>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="buffer">An optional data buffer containing additional information for the RPC.</param>
        /// <param name="deliveryMode">The mode of message delivery, specifying reliability and order.</param>
        /// <param name="sequenceChannel">The sequence channel used for the RPC message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, DataBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
        {
            ClientSide.GlobalRpc(msgId, buffer, deliveryMode, sequenceChannel);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with a specified message ID and client options.
        /// </summary>
        /// <param name="msgId">The identifier for the RPC message being sent.</param>
        /// <param name="options">The client options specifying delivery mode, sequence channel, and optional data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, ClientOptions options)
        {
            Rpc(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID and optional parameters.
        /// </summary>
        /// <param name="msgId">The unique identifier for the RPC message.</param>
        /// <param name="buffer">Optional data buffer containing additional information for the RPC. Defaults to null.</param>
        /// <param name="deliveryMode">The delivery mode for the RPC message, determining reliability and ordering. Defaults to ReliableOrdered.</param>
        /// <param name="sequenceChannel">The sequence channel used for ordered or sequenced delivery. Defaults to 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, DataBuffer buffer = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0)
        {
            ClientSide.Rpc(msgId, m_NetworkMessage.IdentityId, buffer, deliveryMode, sequenceChannel);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, the serialized message, and client options.
        /// </summary>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="message">The message to be sent, implementing the IMessage interface.</param>
        /// <param name="options">The client options containing delivery mode, sequence channel, and the serialized data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, IMessage message, ClientOptions options = default)
        {
            using var _ = message.Serialize();
            options.Buffer = _;
            Rpc(msgId, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, a parameter, and client options.
        /// </summary>
        /// <typeparam name="T1">The type of the parameter to include in the RPC. It must be an unmanaged type.</typeparam>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="p1">The parameter value to send to the server.</param>
        /// <param name="options">The client options containing delivery mode, sequence channel, and optional data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1>(byte msgId, T1 p1, ClientOptions options = default) where T1 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            using var _ = FastWrite(p1);
            options.Buffer = _;
            Rpc(msgId, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, parameters, and client options.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="p1">The first parameter to include in the RPC.</param>
        /// <param name="p2">The second parameter to include in the RPC.</param>
        /// <param name="options">The client options containing delivery mode, sequence channel, and optional data buffer. Default is used if not provided.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2>(byte msgId, T1 p1, T2 p2, ClientOptions options = default)
            where T1 : unmanaged where T2 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            using var _ = FastWrite(p1, p2);
            options.Buffer = _;
            Rpc(msgId, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server using the specified message ID and parameters.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, must be unmanaged.</typeparam>
        /// <typeparam name="T2">The type of the second parameter, must be unmanaged.</typeparam>
        /// <typeparam name="T3">The type of the third parameter, must be unmanaged.</typeparam>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="p1">The first parameter to include in the RPC.</param>
        /// <param name="p2">The second parameter to include in the RPC.</param>
        /// <param name="p3">The third parameter to include in the RPC.</param>
        /// <param name="options">Optional client options that contain delivery mode, sequence channel, and data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3>(byte msgId, T1 p1, T2 p2, T3 p3, ClientOptions options = default)
            where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
            using var _ = FastWrite(p1, p2, p3);
            options.Buffer = _;
            Rpc(msgId, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID and client options, including four parameters.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter to be sent with the RPC.</typeparam>
        /// <typeparam name="T2">The type of the second parameter to be sent with the RPC.</typeparam>
        /// <typeparam name="T3">The type of the third parameter to be sent with the RPC.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter to be sent with the RPC.</typeparam>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="p1">The first parameter to send in the RPC message.</param>
        /// <param name="p2">The second parameter to send in the RPC message.</param>
        /// <param name="p3">The third parameter to send in the RPC message.</param>
        /// <param name="p4">The fourth parameter to send in the RPC message.</param>
        /// <param name="options">The client options including delivery settings and optional data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, ClientOptions options = default)
            where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
            using var _ = FastWrite(p1, p2, p3, p4);
            options.Buffer = _;
            Rpc(msgId, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, arguments, and client options.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, must be unmanaged.</typeparam>
        /// <typeparam name="T2">The type of the second parameter, must be unmanaged.</typeparam>
        /// <typeparam name="T3">The type of the third parameter, must be unmanaged.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter, must be unmanaged.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter, must be unmanaged.</typeparam>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="p1">The first parameter to be sent.</param>
        /// <param name="p2">The second parameter to be sent.</param>
        /// <param name="p3">The third parameter to be sent.</param>
        /// <param name="p4">The fourth parameter to be sent.</param>
        /// <param name="p5">The fifth parameter to be sent.</param>
        /// <param name="options">The client options containing the delivery mode, sequence channel, and optional data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5,
            ClientOptions options = default) where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p5);
            using var _ = FastWrite(p1, p2, p3, p4, p5);
            options.Buffer = _;
            Rpc(msgId, options);
        }
    }

    public class NetworkEventServer
    {
        private readonly IRpcMessage m_NetworkMessage;
        private readonly NetworkVariablesBehaviour m_NetworkVariablesBehaviour;
        private readonly BindingFlags m_BindingFlags;

        internal NetworkEventServer(IRpcMessage networkMessage, BindingFlags flags)
        {
            m_NetworkMessage = networkMessage;
            m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
            m_BindingFlags = flags;
        }

        /// <summary>
        /// Sends a manual 'NetworkVariable' message to all(default) clients with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options,
            NetworkPeer peer = null)
        {
            NetworkVariableSync<T>(property, propertyId, peer, options.Target, options.DeliveryMode, options.GroupId,
                options.DataCache, options.SequenceChannel);
        }

        /// <summary>
        /// Sends a manual 'NetworkVariable' message to all(default) clients with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="target">The target for the message. Default is <see cref="Target.Auto"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void NetworkVariableSync<T>(T property, byte propertyId, NetworkPeer peer = null,
            Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            peer ??= ServerSide.ServerPeer;

            using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(property, propertyId);
            Rpc(NetworkConstants.NETWORK_VARIABLE_RPC_ID, peer, message, target, deliveryMode, groupId, dataCache,
                sequenceChannel);
        }

        /// <summary>
        /// Automatically sends a 'NetworkVariable' message to all(default) clients based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        public void NetworkVariableSync<T>(NetworkVariableOptions options, NetworkPeer peer = null,
            [CallerMemberName] string ___ = "")
        {
            NetworkVariableSync<T>(peer, options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                options.SequenceChannel, ___);
        }

        /// <summary>
        /// Automatically sends a 'NetworkVariable' message to all(default) clients based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="target">The target for the message. Default is <see cref="Target.Auto"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void NetworkVariableSync<T>(NetworkPeer peer = null, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0, [CallerMemberName] string ___ = "")
        {
            dataCache ??= DataCache.None;
            IPropertyInfo property = m_NetworkVariablesBehaviour.GetPropertyInfoWithCallerName<T>(___, m_BindingFlags);
            if (property is IPropertyInfo<T> propertyGeneric)
            {
                peer ??= ServerSide.ServerPeer;
                using DataBuffer message =
                    m_NetworkVariablesBehaviour.CreateHeader(propertyGeneric.Invoke(), property.Id);

                Rpc(NetworkConstants.NETWORK_VARIABLE_RPC_ID, peer, message, target, deliveryMode, groupId, dataCache,
                    sequenceChannel);
            }
        }

        /// <summary>
        /// Sends a global Remote Procedure Call (RPC) to a specific network peer.
        /// </summary>
        /// <param name="msgId">The unique identifier of the RPC to invoke.</param>
        /// <param name="peer">The network peer that will receive the RPC.</param>
        /// <param name="options">
        /// The server options containing configuration settings for the RPC, such as buffering, delivery mode, and targeting.
        /// </param>
        /// <remarks>
        /// This method simplifies the invocation of a global RPC using server-side configuration.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, NetworkPeer peer, ServerOptions options)
        {
            GlobalRpc(msgId, peer, options.Buffer, options.Target, options.DeliveryMode, options.GroupId,
                options.DataCache, options.SequenceChannel);
        }

        /// <summary>
        /// Sends a global Remote Procedure Call (RPC) to a specific network peer with detailed configuration.
        /// </summary>
        /// <param name="msgId">The message identifier for the RPC.</param>
        /// <param name="peer">The target network peer to receive the RPC.</param>
        /// <param name="buffer">Optional data buffer to pass along with the RPC message.</param>
        /// <param name="target">The target scope for delivering the RPC (e.g., self, group, all).</param>
        /// <param name="deliveryMode">The delivery mode for the RPC, such as reliable or unreliable.</param>
        /// <param name="groupId">Identifier for the group if the target is group-specific.</param>
        /// <param name="dataCache">The data cache configuration for the RPC message.</param>
        /// <param name="sequenceChannel">The sequence channel for ordered delivery if applicable.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.SelfOnly,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            ServerSide.GlobalRpc(msgId, peer, buffer, target, deliveryMode, groupId, dataCache, sequenceChannel);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the client's using predefined server options.
        /// </summary>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="options">The server options containing configuration for the RPC, such as buffering and targeting.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, NetworkPeer peer, ServerOptions options)
        {
            Rpc(msgId, peer, options.Buffer, options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                options.SequenceChannel);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the client's with detailed configuration.
        /// </summary>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="buffer">The buffer containing the RPC data. Default is null.</param>
        /// <param name="target">Specifies the target scope for the RPC. Default is <see cref="Target.Auto"/>.</param>
        /// <param name="deliveryMode">Specifies the delivery mode of the RPC. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">An optional group identifier for the RPC. Default is 0.</param>
        /// <param name="dataCache">Defines whether the RPC should be cached for later retrieval. Default is <see cref="DataCache.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel to send the RPC over. Default is 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            ServerSide.Rpc(msgId, peer, m_NetworkMessage.IdentityId, buffer, target, deliveryMode, groupId, dataCache,
                sequenceChannel);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with a custom serialized message to the client.
        /// </summary>
        /// <param name="msgId">The unique identifier of the RPC to be sent.</param>
        /// <param name="peer">The target network peer for the RPC.</param>
        /// <param name="message">The custom message to serialize and send as part of the RPC.</param>
        /// <param name="options">The configuration options for the RPC, such as transmission parameters.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, NetworkPeer peer, IMessage message, ServerOptions options = default)
        {
            using var _ = message.Serialize();
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with one unmanaged parameter to the client.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="p1">The first parameter of the RPC.</param>
        /// <param name="options">The server options containing configuration for the RPC.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1>(byte msgId, NetworkPeer peer, T1 p1, ServerOptions options = default) where T1 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            using var _ = FastWrite(p1);
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with two unmanaged parameters to the client.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="p1">The first parameter of the RPC.</param>
        /// <param name="p2">The second parameter of the RPC.</param>
        /// <param name="options">The server options containing configuration for the RPC.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, ServerOptions options = default)
            where T1 : unmanaged where T2 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            using var _ = FastWrite(p1, p2);
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with three unmanaged parameters to the client.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="p1">The first parameter to include in the RPC.</param>
        /// <param name="p2">The second parameter to include in the RPC.</param>
        /// <param name="p3">The third parameter to include in the RPC.</param>
        /// <param name="options">The server options containing configuration for the RPC.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, ServerOptions options = default)
            where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
            using var _ = FastWrite(p1, p2, p3);
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with four unmanaged parameters to the client.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="p1">The first parameter to include in the RPC.</param>
        /// <param name="p2">The second parameter to include in the RPC.</param>
        /// <param name="p3">The third parameter to include in the RPC.</param>
        /// <param name="p4">The fourth parameter to include in the RPC.</param>
        /// <param name="options">The server options containing configuration for the RPC.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, T4 p4,
            ServerOptions options = default) where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
            using var _ = FastWrite(p1, p2, p3, p4);
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with five unmanaged parameters to the client.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter, which must be unmanaged.</typeparam>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="p1">The first parameter to include in the RPC.</param>
        /// <param name="p2">The second parameter to include in the RPC.</param>
        /// <param name="p3">The third parameter to include in the RPC.</param>
        /// <param name="p4">The fourth parameter to include in the RPC.</param>
        /// <param name="p5">The fifth parameter to include in the RPC.</param>
        /// <param name="options">The server options containing configuration for the RPC.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5,
            ServerOptions options = default) where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p5);
            using var _ = FastWrite(p1, p2, p3, p4, p5);
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.