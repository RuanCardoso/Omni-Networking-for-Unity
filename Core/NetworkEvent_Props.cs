using Omni.Core.Interfaces;
using System.Reflection;
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

        internal NetworkEventClient(IRpcMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
            m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
        }

        /// <summary>
        /// Sends a manual 'NetworkVariable' message to the server with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions _)
        {
            using DataBuffer message = m_NetworkVariablesBehaviour.CreateNetworkVariableMessage(property, propertyId);
            Rpc(NetworkConstants.k_NetworkVariableRpcId, message);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID and optional parameters.
        /// </summary>
        /// <param name="rpcId">The unique identifier for the RPC message.</param>
        /// <param name="buffer">Optional data buffer containing additional information for the RPC. Defaults to null.</param>
        /// <param name="deliveryMode">The delivery mode for the RPC message, determining reliability and ordering. Defaults to ReliableOrdered.</param>
        /// <param name="sequenceChannel">The sequence channel used for ordered or sequenced delivery. Defaults to 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte rpcId, DataBuffer message = null)
        {
            m_NetworkMessage.SetupRpcMessage(rpcId, default, false, default);
            ClientSide.Rpc(rpcId, m_NetworkMessage.IdentityId, message);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, the serialized message, and client options.
        /// </summary>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="message">The message to be sent, implementing the IMessage interface.</param>
        /// <param name="options">The client options containing delivery mode, sequence channel, and the serialized data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendMessage(byte msgId, IMessage message)
        {
            using var buffer = message.Serialize();
            Rpc(msgId, buffer);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, a parameter, and client options.
        /// </summary>
        /// <typeparam name="T1">The type of the parameter to include in the RPC. It must be an unmanaged type.</typeparam>
        /// <param name="msgId">The identifier for the RPC message.</param>
        /// <param name="p1">The parameter value to send to the server.</param>
        /// <param name="options">The client options containing delivery mode, sequence channel, and optional data buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1>(byte msgId, T1 p1)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            Rpc(msgId, message);
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
        public void Rpc<T1, T2>(byte msgId, T1 p1, T2 p2)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            Rpc(msgId, message);
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
        public void Rpc<T1, T2, T3>(byte msgId, T1 p1, T2 p2, T3 p3)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            Rpc(msgId, message);
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
        public void Rpc<T1, T2, T3, T4>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            Rpc(msgId, message);
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
        public void Rpc<T1, T2, T3, T4, T5>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            Rpc(msgId, message);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with six unmanaged parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5, T6>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            message.WriteAsBinary(p6);
            Rpc(msgId, message);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with seven unmanaged parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5, T6, T7>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            message.WriteAsBinary(p6);
            message.WriteAsBinary(p7);
            Rpc(msgId, message);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the server with eight unmanaged parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5, T6, T7, T8>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            message.WriteAsBinary(p6);
            message.WriteAsBinary(p7);
            message.WriteAsBinary(p8);
            Rpc(msgId, message);
        }

        public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int channel = 0)
        {
            m_NetworkMessage.__ClientRpcHandler.SetRpcParameters(rpcId, deliveryMode, Target.Auto, (byte)channel);
        }
    }

    public class NetworkEventServer
    {
        private readonly IRpcMessage m_NetworkMessage;
        private readonly NetworkVariablesBehaviour m_NetworkVariablesBehaviour;
        private readonly BindingFlags m_BindingFlags;

        internal NetworkEventServer(IRpcMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
            m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
        }

        /// <summary>
        /// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
        public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
        {
            using DataBuffer message = m_NetworkVariablesBehaviour.CreateNetworkVariableMessage(property, propertyId);
            m_NetworkMessage.SetupRpcMessage(NetworkConstants.k_NetworkVariableRpcId, NetworkGroup.None, true, propertyId);
            ServerSide.SetTarget(Target.Self);
            ServerSide.Rpc(NetworkConstants.k_NetworkVariableRpcId, peer, m_NetworkMessage.IdentityId, message);
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
        public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options, NetworkPeer peer = null)
        {
            peer ??= ServerSide.ServerPeer;
            using DataBuffer message = m_NetworkVariablesBehaviour.CreateNetworkVariableMessage(property, propertyId);
            m_NetworkMessage.SetupRpcMessage(NetworkConstants.k_NetworkVariableRpcId, options.Group, true, propertyId);
            ServerSide.Rpc(NetworkConstants.k_NetworkVariableRpcId, peer, m_NetworkMessage.IdentityId, message);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) to the client's with detailed configuration.
        /// </summary>
        /// <param name="rpcId">The unique identifier of the RPC to send.</param>
        /// <param name="buffer">The buffer containing the RPC data. Default is null.</param>
        /// <param name="target">Specifies the target scope for the RPC. Default is <see cref="Target.Auto"/>.</param>
        /// <param name="deliveryMode">Specifies the delivery mode of the RPC. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">An optional group identifier for the RPC. Default is 0.</param>
        /// <param name="dataCache">Defines whether the RPC should be cached for later retrieval. Default is <see cref="DataCache.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel to send the RPC over. Default is 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc(byte rpcId, NetworkPeer peer, DataBuffer message = null, NetworkGroup group = null)
        {
            m_NetworkMessage.SetupRpcMessage(rpcId, group, true, default);
            m_NetworkMessage.__ServerRpcHandler.GetRpcParameters(rpcId, out _, out var target, out _);
            if (target == Target.Auto && peer.Id != 0)
                ServerSide.SetTarget(Target.Self);
            ServerSide.Rpc(rpcId, peer, m_NetworkMessage.IdentityId, message);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with a custom serialized message to the client.
        /// </summary>
        /// <param name="msgId">The unique identifier of the RPC to be sent.</param>
        /// <param name="peer">The target network peer for the RPC.</param>
        /// <param name="message">The custom message to serialize and send as part of the RPC.</param>
        /// <param name="options">The configuration options for the RPC, such as transmission parameters.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendMessage(byte rpcId, NetworkPeer peer, IMessage message, NetworkGroup group = null)
        {
            using var buffer = message.Serialize();
            Rpc(rpcId, peer, buffer, group);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with one unmanaged parameter to the client.w
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
        /// <param name="msgId">The unique identifier of the RPC to send.</param>
        /// <param name="p1">The first parameter of the RPC.</param>
        /// <param name="options">The server options containing configuration for the RPC.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1>(byte msgId, NetworkPeer peer, T1 p1, NetworkGroup group = null)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            Rpc(msgId, peer, message, group);
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
        public void Rpc<T1, T2>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, NetworkGroup group = null)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            Rpc(msgId, peer, message, group);
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
        public void Rpc<T1, T2, T3>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, NetworkGroup group = null)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            Rpc(msgId, peer, message, group);
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
        public void Rpc<T1, T2, T3, T4>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, T4 p4, NetworkGroup group = null)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            Rpc(msgId, peer, message, group);
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
        public void Rpc<T1, T2, T3, T4, T5>(byte msgId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, NetworkGroup group = null)
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            Rpc(msgId, peer, message, group);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with six unmanaged parameters to the client.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5, T6>(
            byte msgId,
            NetworkPeer peer,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6,
            NetworkGroup group = null)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            message.WriteAsBinary(p6);
            Rpc(msgId, peer, message, group);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with seven unmanaged parameters to the client.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5, T6, T7>(
            byte msgId,
            NetworkPeer peer,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7,
            NetworkGroup group = null)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            message.WriteAsBinary(p6);
            message.WriteAsBinary(p7);
            Rpc(msgId, peer, message, group);
        }

        /// <summary>
        /// Sends a Remote Procedure Call (RPC) with eight unmanaged parameters to the client.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rpc<T1, T2, T3, T4, T5, T6, T7, T8>(
            byte msgId,
            NetworkPeer peer,
            T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8,
            NetworkGroup group = null)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
        {
            using var message = Pool.Rent(enableTracking: false);
            message.WriteAsBinary(p1);
            message.WriteAsBinary(p2);
            message.WriteAsBinary(p3);
            message.WriteAsBinary(p4);
            message.WriteAsBinary(p5);
            message.WriteAsBinary(p6);
            message.WriteAsBinary(p7);
            message.WriteAsBinary(p8);
            Rpc(msgId, peer, message, group);
        }

        public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, Target target = Target.Auto, int channel = 0)
        {
            m_NetworkMessage.__ServerRpcHandler.SetRpcParameters(rpcId, deliveryMode, target, (byte)channel);
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.