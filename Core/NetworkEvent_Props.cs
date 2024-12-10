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
            Rpc(NetworkConstants.NET_VAR_RPC_ID, message, deliveryMode, sequenceChannel);
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
            IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;
            if (property != null)
            {
                using DataBuffer message =
                    m_NetworkVariablesBehaviour.CreateHeader(propertyGeneric.Invoke(), property.Id);
                Rpc(NetworkConstants.NET_VAR_RPC_ID, message, deliveryMode, sequenceChannel);
            }
        }

        /// <summary>
        /// Invokes a global message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, ClientOptions options)
        {
            GlobalRpc(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
        }

        /// <summary>
        /// Invokes a global message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, DataBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
        {
            ClientSide.GlobalRpc(msgId, buffer, deliveryMode, sequenceChannel);
        }

        /// <summary>
        /// Invokes a message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, ClientOptions options)
        {
            Rpc(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
        }

        /// <summary>
        /// Invokes a message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, DataBuffer buffer = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0)
        {
            ClientSide.Rpc(msgId, m_NetworkMessage.IdentityId, buffer, deliveryMode, sequenceChannel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, IMessage message, ClientOptions options = default)
        {
            using var _ = message.Serialize();
            options.Buffer = _;
            Rpc(msgId, options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1>(byte msgId, T1 p1, ClientOptions options = default) where T1 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            using var _ = FastWrite(p1);
            options.Buffer = _;
            Rpc(msgId, options);
        }

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
            Rpc(NetworkConstants.NET_VAR_RPC_ID, peer, message, target, deliveryMode, groupId, dataCache,
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
            IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;
            if (property != null)
            {
                peer ??= ServerSide.ServerPeer;
                using DataBuffer message =
                    m_NetworkVariablesBehaviour.CreateHeader(propertyGeneric.Invoke(), property.Id);

                Rpc(NetworkConstants.NET_VAR_RPC_ID, peer, message, target, deliveryMode, groupId, dataCache,
                    sequenceChannel);
            }
        }

        /// <summary>
        /// Invokes a global message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, NetworkPeer peer, ServerOptions options)
        {
            GlobalRpc(msgId, peer, options.Buffer, options.Target, options.DeliveryMode, options.GroupId,
                options.DataCache, options.SequenceChannel);
        }

        /// <summary>
        /// Invokes a global message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.Auto"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GlobalRpc(byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.SelfOnly,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            ServerSide.GlobalRpc(msgId, peer, buffer, target, deliveryMode, groupId, dataCache, sequenceChannel);
        }

        /// <summary>
        /// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, NetworkPeer peer, ServerOptions options)
        {
            Rpc(msgId, peer, options.Buffer, options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                options.SequenceChannel);
        }

        /// <summary>
        /// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.Auto"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            ServerSide.Rpc(msgId, peer, m_NetworkMessage.IdentityId, buffer, target, deliveryMode, groupId, dataCache,
                sequenceChannel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte msgId, NetworkPeer peer, IMessage message, ServerOptions options = default)
        {
            using var _ = message.Serialize();
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1>(byte msgId, NetworkPeer peer, T1 p1, ServerOptions options = default) where T1 : unmanaged
        {
            NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
            using var _ = FastWrite(p1);
            options.Buffer = _;
            Rpc(msgId, peer, options);
        }

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