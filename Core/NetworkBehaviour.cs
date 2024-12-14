using Omni.Core.Interfaces;
using Omni.Core.Modules.Ntp;
using Omni.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TriInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable

namespace Omni.Core
{
    /// <summary>
    /// Represents a behavior for network entities providing synchronization and network messaging capabilities.
    /// </summary>
    /// <remarks>
    /// This class is part of the core networking module and extends functionalities required for network-aware behaviors.
    /// It includes auto-synced network variables and supports client and server execution contexts.
    /// </remarks>
    [DeclareFoldoutGroup("Network Variables", Expanded = true, Title = "Network Variables - (Auto Synced)")]
    [DeclareBoxGroup("Service Settings")]
    [StackTrace]
    public class NetworkBehaviour : NetworkVariablesBehaviour, IRpcMessage, ITickSystem, IEquatable<NetworkBehaviour>
    {
        // Hacky: DIRTY CODE!
        // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
        // Despite its appearance, this approach is essential to achieve high performance.
        // Avoid refactoring as these techniques are crucial for optimizing execution speed.
        // Works with il2cpp.

        public class NetworkBehaviourClient
        {
            private readonly NetworkBehaviour m_NetworkBehaviour;

            internal NetworkBehaviourClient(NetworkBehaviour networkBehaviour)
            {
                m_NetworkBehaviour = networkBehaviour;
            }

            /// <summary>
            /// Sends a manual 'NetworkVariable' message to the server with the specified property and property id.
            /// </summary>
            /// <typeparam name="T">The type of the property to synchronize.</typeparam>
            /// <param name="property">The property value to synchronize.</param>
            /// <param name="propertyId">The ID of the property being synchronized.</param>
            public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options)
            {
                NetworkVariableSync<T>(property, propertyId, options.DeliveryMode, options.SequenceChannel);
            }

            /// <summary>
            /// Sends a manual 'NetworkVariable' message to the server with the specified property and property id.
            /// </summary>
            /// <typeparam name="T">The type of the property to synchronize.</typeparam>
            /// <param name="property">The property value to synchronize.</param>
            /// <param name="propertyId">The ID of the property being synchronized.</param>
            /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            public void NetworkVariableSync<T>(T property, byte propertyId,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                using DataBuffer message = m_NetworkBehaviour.CreateHeader(property, propertyId);
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
                IPropertyInfo property =
                    m_NetworkBehaviour.GetPropertyInfoWithCallerName<T>(___, m_NetworkBehaviour.m_BindingFlags);

                if (property is IPropertyInfo<T> propertyGeneric)
                {
                    using DataBuffer message = m_NetworkBehaviour.CreateHeader(propertyGeneric.Invoke(), property.Id);
                    Rpc(NetworkConstants.NETWORK_VARIABLE_RPC_ID, message, deliveryMode, sequenceChannel);
                }
            }

            /// <summary>
            /// Invokes a message on the server using a Remote Procedure Call (RPC) with the specified message ID and options.
            /// </summary>
            /// <param name="msgId">The ID of the message to invoke on the server.</param>
            /// <param name="options">The client options used for the RPC call.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc(byte msgId, ClientOptions options)
            {
                Rpc(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) message to the server with the specified parameters.
            /// </summary>
            /// <param name="msgId">The unique identifier for the RPC message to be sent.</param>
            /// <param name="buffer">The data buffer containing the message payload. Defaults to null if no payload is provided.</param>
            /// <param name="deliveryMode">Specifies the message delivery mode, such as reliable or ordered. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The designated sequence channel for the message. Default is 0.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc(byte msgId, DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                NetworkManager.ClientSide.Rpc(msgId, m_NetworkBehaviour.IdentityId, m_NetworkBehaviour.Id, buffer,
                    deliveryMode, sequenceChannel);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) message to the server with the specified parameters,
            /// using the provided message object and client options.
            /// </summary>
            /// <param name="msgId">The identifier for the RPC message to be delivered.</param>
            /// <param name="message">The IMessage implementation representing the data to serialize and send.</param>
            /// <param name="options">The configuration settings defining buffer options for the RPC call. Defaults to a standard configuration if not provided.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc(byte msgId, IMessage message, ClientOptions options = default)
            {
                using var _ = message.Serialize();
                options.Buffer = _;
                Rpc(msgId, options);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, parameter, and optional client options.
            /// </summary>
            /// <typeparam name="T1">The type of the parameter to send. Must be unmanaged.</typeparam>
            /// <param name="msgId">The unique identifier for the RPC message.</param>
            /// <param name="p1">The parameter value to send with the RPC.</param>
            /// <param name="options">Optional client options to configure the RPC behavior.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1>(byte msgId, T1 p1, ClientOptions options = default) where T1 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                using var _ = NetworkManager.FastWrite(p1);
                options.Buffer = _;
                Rpc(msgId, options);
            }

            /// <summary>
            /// Sends a remote procedure call (RPC) message to the server, utilizing the specified message ID and parameters.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter.</typeparam>
            /// <typeparam name="T2">The type of the second parameter.</typeparam>
            /// <param name="msgId">The unique identifier for the RPC message.</param>
            /// <param name="p1">The first parameter to include in the RPC message.</param>
            /// <param name="p2">The second parameter to include in the RPC message.</param>
            /// <param name="options">Optional client-specific configuration for the RPC message.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2>(byte msgId, T1 p1, T2 p2, ClientOptions options = default)
                where T1 : unmanaged where T2 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
                using var _ = NetworkManager.FastWrite(p1, p2);
                options.Buffer = _;
                Rpc(msgId, options);
            }

            /// <summary>
            /// Sends a remote procedure call (RPC) to a server with the specified parameters and configuration options.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <param name="msgId">The message ID that identifies the RPC call.</param>
            /// <param name="p1">The first parameter to include in the RPC call.</param>
            /// <param name="p2">The second parameter to include in the RPC call.</param>
            /// <param name="p3">The third parameter to include in the RPC call.</param>
            /// <param name="options">The options for configuring the RPC call's behavior and delivery.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3>(byte msgId, T1 p1, T2 p2, T3 p3, ClientOptions options = default)
                where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
                using var _ = NetworkManager.FastWrite(p1, p2, p3);
                options.Buffer = _;
                Rpc(msgId, options);
            }

            /// <summary>
            /// Sends a remote procedure call (RPC) with the specified message ID and parameters to the server.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
            /// <param name="msgId">The ID of the message to be sent.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="p4">The fourth parameter to include in the RPC.</param>
            /// <param name="options">Additional client options for the RPC, which includes buffers and configurations.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, ClientOptions options = default)
                where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
                using var _ = NetworkManager.FastWrite(p1, p2, p3, p4);
                options.Buffer = _;
                Rpc(msgId, options);
            }

            /// <summary>
            /// Sends an RPC (Remote Procedure Call) message to the server with the specified arguments and message ID.
            /// </summary>
            /// <typeparam name="T1">The type of the first argument, must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second argument, must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third argument, must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth argument, must be unmanaged.</typeparam>
            /// <param name="msgId">The ID of the message to send.</param>
            /// <param name="p1">The first argument to be included in the RPC message.</param>
            /// <param name="p2">The second argument to be included in the RPC message.</param>
            /// <param name="p3">The third argument to be included in the RPC message.</param>
            /// <param name="p4">The fourth argument to be included in the RPC message.</param>
            /// <param name="p5">The fifth argument to be included in the RPC message.</param>
            /// <param name="options">The options for configuring the client RPC message.</param>
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
                using var _ = NetworkManager.FastWrite(p1, p2, p3, p4, p5);
                options.Buffer = _;
                Rpc(msgId, options);
            }
        }

        public class NetworkBehaviourServer
        {
            private readonly NetworkBehaviour m_NetworkBehaviour;

            internal NetworkBehaviourServer(NetworkBehaviour networkBehaviour)
            {
                m_NetworkBehaviour = networkBehaviour;
            }

            /// <summary>
            /// Sends a manual 'NetworkVariable' message to clients with the specified property and property ID.
            /// </summary>
            /// <typeparam name="T">The type of the property to synchronize.</typeparam>
            /// <param name="property">The property value to synchronize.</param>
            /// <param name="propertyId">The ID of the property being synchronized.</param>
            public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options)
            {
                NetworkVariableSync(property, propertyId, options.Target, options.DeliveryMode, options.GroupId,
                    options.DataCache, options.SequenceChannel);
            }

            /// <summary>
            /// Sends a manual 'NetworkVariable' message to clients with the specified property and property ID.
            /// </summary>
            /// <typeparam name="T">The type of the property to synchronize.</typeparam>
            /// <param name="property">The property value to synchronize.</param>
            /// <param name="propertyId">The ID of the property being synchronized.</param>
            /// <param name="target">The target for the message. Default is <see cref="Target.Auto"/>.</param>
            /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
            /// <param name="groupId">The group ID for the message. Default is 0.</param>
            /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            public void NetworkVariableSync<T>(T property, byte propertyId, Target target = Target.Auto,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0)
            {
                dataCache ??= DataCache.None;
                using DataBuffer message = m_NetworkBehaviour.CreateHeader(property, propertyId);
                Rpc(NetworkConstants.NETWORK_VARIABLE_RPC_ID, message, target, deliveryMode, groupId, dataCache,
                    sequenceChannel);
            }

            /// <summary>
            /// Automatically sends a 'NetworkVariable' message to clients based on the caller member name.
            /// </summary>
            /// <typeparam name="T">The type of the property to synchronize.</typeparam>
            public void NetworkVariableSync<T>(NetworkVariableOptions options, [CallerMemberName] string ___ = "")
            {
                NetworkVariableSync<T>(options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                    options.SequenceChannel, ___);
            }

            /// <summary>
            /// Automatically sends a 'NetworkVariable' message to clients based on the caller member name.
            /// </summary>
            /// <typeparam name="T">The type of the property to synchronize.</typeparam>
            /// <param name="target">The target for the message. Default is <see cref="Target.Auto"/>.</param>
            /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
            /// <param name="groupId">The group ID for the message. Default is 0.</param>
            /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            public void NetworkVariableSync<T>(Target target = Target.Auto,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0, [CallerMemberName] string ___ = "")
            {
                dataCache ??= DataCache.None;
                IPropertyInfo propertyInfo = m_NetworkBehaviour.GetPropertyInfoWithCallerName<T>(
                    ___,
                    m_NetworkBehaviour.m_BindingFlags
                );

                if (propertyInfo is IPropertyInfo<T> propertyInfoGeneric)
                {
                    using DataBuffer message =
                        m_NetworkBehaviour.CreateHeader(propertyInfoGeneric.Invoke(), propertyInfo.Id);

                    Rpc(NetworkConstants.NETWORK_VARIABLE_RPC_ID, message, target, deliveryMode, groupId, dataCache,
                        sequenceChannel);
                }
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) to the client's using predefined server options.
            /// </summary>
            /// <param name="msgId">The unique identifier of the RPC to send.</param>
            /// <param name="options">The server options containing configuration for the RPC, such as buffering and targeting.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc(byte msgId, ServerOptions options)
            {
                Rpc(msgId, options.Buffer, options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                    options.SequenceChannel);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) to a specific network peer using server options.
            /// </summary>
            /// <param name="msgId">The unique identifier of the RPC to send.</param>
            /// <param name="peer">The target network peer to receive the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RpcToPeer(byte msgId, NetworkPeer peer, ServerOptions options)
            {
                RpcToPeer(msgId, peer, options.Buffer, options.DeliveryMode, options.DataCache,
                    options.SequenceChannel);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) to a specific peer with detailed configuration.
            /// </summary>
            /// <param name="msgId">The unique identifier of the RPC to send.</param>
            /// <param name="peer">The target network peer to receive the RPC.</param>
            /// <param name="buffer">The buffer containing the RPC data. Default is null.</param>
            /// <param name="deliveryMode">Specifies the delivery mode of the RPC. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
            /// <param name="dataCache">Defines whether the RPC should be cached for later retrieval. Default is <see cref="DataCache.None"/>.</param>
            /// <param name="sequenceChannel">The sequence channel to send the RPC over. Default is 0.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RpcToPeer(byte msgId, NetworkPeer peer, DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, DataCache dataCache = default,
                byte sequenceChannel = 0)
            {
                dataCache ??= DataCache.None;
                Internal_RpcToPeer(msgId, peer, buffer, Target.SelfOnly, deliveryMode, 0, dataCache, sequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Internal_RpcToPeer(byte msgId, NetworkPeer peer, DataBuffer buffer = null,
                Target target = Target.Auto, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0)
            {
                dataCache ??= DataCache.None;
                NetworkManager.ServerSide.Rpc(msgId, peer, m_NetworkBehaviour.IdentityId, m_NetworkBehaviour.Id, buffer,
                    target, deliveryMode, groupId, dataCache, sequenceChannel);
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
            public void Rpc(byte msgId, DataBuffer buffer = null, Target target = Target.Auto,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0)
            {
                dataCache ??= DataCache.None;
                Internal_RpcToPeer(msgId, m_NetworkBehaviour.Identity.Owner, buffer, target, deliveryMode, groupId,
                    dataCache, sequenceChannel);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with a custom serialized message to the client.
            /// </summary>
            /// <param name="msgId">The unique identifier of the RPC to send.</param>
            /// <param name="message">The message to serialize and send as part of the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc(byte msgId, IMessage message, ServerOptions options = default)
            {
                using var _ = message.Serialize();
                options.Buffer = _;
                Rpc(msgId, options);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with one unmanaged parameter to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <param name="msgId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter of the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1>(byte msgId, T1 p1, ServerOptions options = default) where T1 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                using var _ = NetworkManager.FastWrite(p1);
                options.Buffer = _;
                Rpc(msgId, options);
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
            public void Rpc<T1, T2>(byte msgId, T1 p1, T2 p2, ServerOptions options = default)
                where T1 : unmanaged where T2 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
                using var _ = NetworkManager.FastWrite(p1, p2);
                options.Buffer = _;
                Rpc(msgId, options);
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
            public void Rpc<T1, T2, T3>(byte msgId, T1 p1, T2 p2, T3 p3, ServerOptions options = default)
                where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
                using var _ = NetworkManager.FastWrite(p1, p2, p3);
                options.Buffer = _;
                Rpc(msgId, options);
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
            public void Rpc<T1, T2, T3, T4>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, ServerOptions options = default)
                where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
            {
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
                NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
                using var _ = NetworkManager.FastWrite(p1, p2, p3, p4);
                options.Buffer = _;
                Rpc(msgId, options);
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
            public void Rpc<T1, T2, T3, T4, T5>(byte msgId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5,
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
                using var _ = NetworkManager.FastWrite(p1, p2, p3, p4, p5);
                options.Buffer = _;
                Rpc(msgId, options);
            }
        }

        // Hacky: DIRTY CODE!
        // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
        // Despite its appearance, this approach is essential to achieve high performance.
        // Avoid refactoring as these techniques are crucial for optimizing execution speed.
        // Works with il2cpp.

        private readonly RpcHandler<DataBuffer, int, Null, Null, Null> clientRpcHandler = new();
        private readonly RpcHandler<DataBuffer, NetworkPeer, int, Null, Null> serverRpcHandler = new();

        [SerializeField] [Group("Service Settings")]
        private string m_ServiceName = "";

        [SerializeField] [Group("Service Settings")]
        private byte m_Id = 0;

        internal BindingFlags m_BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Gets the unique identifier for this instance.
        /// </summary>
        /// <value>A <see cref="byte"/> representing the identifier of the instance.</value>
        public byte Id
        {
            get { return m_Id; }
            internal set { m_Id = value; }
        }

        /// <summary>
        /// Gets the <see cref="NetworkIdentity"/> associated with this instance.
        /// </summary>
        /// <value>
        /// The <see cref="NetworkIdentity"/> associated with this instance.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="NetworkIdentity"/> has not been assigned before accessing this property.
        /// </exception>
        /// <remarks>
        /// The <see cref="NetworkIdentity"/> represents the unique identity of this object within the network.
        /// Ensure the property is properly assigned before attempting to access it, especially during object initialization.
        /// </remarks>
        public NetworkIdentity Identity
        {
            get
            {
                if (_identity != null)
                    return _identity;

                NetworkLogger.PrintHyperlink();
                throw new InvalidOperationException(
                    "The 'NetworkIdentity' property has not been assigned yet. Ensure to assign it before accessing this property. If this occurs during object initialization, confirm the object is fully initialized before usage to avoid runtime issues."
                );
            }
            internal set => _identity = value;
        }

        /// <summary>
        /// Gets the unique identifier of the associated <see cref="NetworkIdentity"/>.
        /// </summary>
        /// <value>
        /// An integer representing the identifier of the associated <see cref="NetworkIdentity"/>.
        /// </value>
        public int IdentityId => Identity.IdentityId;

        /// <summary>
        /// Indicates whether this instance represents the local player.
        /// </summary>
        /// <value><c>true</c> if this instance represents the local player; otherwise, <c>false</c>.</value>
        public bool IsLocalPlayer => Identity.IsLocalPlayer;

        /// <summary>
        /// Indicates whether this instance represents the local player.
        /// </summary>
        /// <value><c>true</c> if this instance represents the local player; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This property is an alias for <see cref="IsLocalPlayer"/> to improve code readability.
        /// </remarks>
        public bool IsMine => IsLocalPlayer;

        /// <summary>
        /// Indicates whether this instance exists on the server.
        /// </summary>
        /// <value><c>true</c> if this instance exists on the server; otherwise, <c>false</c>.</value>
        public bool IsServer => Identity.IsServer;

        /// <summary>
        /// Indicates whether this instance exists on the client.
        /// </summary>
        /// <value><c>true</c> if this instance exists on the client; otherwise, <c>false</c>.</value>
        public bool IsClient => Identity.IsClient;

        /// <summary>
        /// Provides access to the synchronized network time protocol (NTP) instance used by the server and clients.
        /// </summary>
        /// <value>
        /// The <see cref="SimpleNtp"/> instance that synchronizes the server and client time.
        /// </value>
        protected SimpleNtp Sntp => NetworkManager.Sntp;

        /// <summary>
        /// Gets the synchronized time between the server and the clients.
        /// </summary>
        /// <value>
        /// A <see cref="double"/> representing the current synchronized time across all clients and the server.
        /// </value>
        /// <remarks>
        /// The synchronized time provides a consistent reference point between the server and clients. 
        /// While not perfectly identical due to precision differences, it is as close as possible between all nodes.
        /// </remarks>
        protected double SynchronizedTime => IsServer ? Sntp.Server.Time : Sntp.Client.Time;

        /// <summary>
        /// Gets the synchronized time between the client and the server from the perspective of the current peer.
        /// </summary>
        /// <value>
        /// A <see cref="double"/> representing the synchronized time for the specific client relative to the server.
        /// </value>
        /// <remarks>
        /// Unlike <see cref="SynchronizedTime"/>, this property reflects the perspective of the individual client. 
        /// The value may differ slightly between clients due to network latency or other factors.
        /// </remarks>
        protected double PeerTime => Identity.Owner.Time;

        private NetworkBehaviourClient _local;

        /// <summary>
        /// Provides access to the <see cref="NetworkBehaviourClient"/> instance, 
        /// enabling the client to send Remote Procedure Calls (RPCs) to the server.
        /// </summary>
        /// <value>
        /// The <see cref="NetworkBehaviourClient"/> instance used for client-to-server communication.
        /// </value>
        /// <exception cref="Exception">
        /// Thrown if this property is accessed on the server side. It is strictly intended for client-side use only.
        /// </exception>
        /// <remarks>
        /// Use this property to invoke server-side operations via RPC from the client. 
        /// Attempting to access it on the server side will result in an exception. 
        /// Ensure this property is used only in client-side logic to avoid runtime errors.
        /// </remarks>
        public NetworkBehaviourClient Client
        {
            get
            {
                if (_local != null)
                    return _local;

                NetworkLogger.PrintHyperlink();
                throw new Exception(
                    "Client-side-only property 'Client' was accessed improperly from the server side. This usage is invalid. Ensure this property is accessed exclusively on the client side to avoid runtime errors."
                );
            }
            private set => _local = value;
        }

        private NetworkBehaviourServer _remote;
        private NetworkIdentity _identity;

        /// <summary>
        /// Provides access to the <see cref="NetworkBehaviourServer"/> instance, 
        /// enabling the server to send Remote Procedure Calls (RPCs) to the client.
        /// </summary>
        /// <value>
        /// The <see cref="NetworkBehaviourServer"/> instance used for server-to-client communication.
        /// </value>
        /// <exception cref="Exception">
        /// Thrown if this property is accessed on the client side. It is strictly intended for server-side use only.
        /// </exception>
        /// <remarks>
        /// Use this property to invoke client-side operations via RPC from the server. 
        /// Attempting to access this property on the client side will throw an exception. 
        /// Ensure this property is used exclusively within server-side logic to avoid runtime errors.
        /// </remarks>
        public NetworkBehaviourServer Server
        {
            get
            {
                if (_remote != null)
                    return _remote;

                NetworkLogger.PrintHyperlink();
                throw new Exception(
                    "Access to the 'Server' property is restricted to server-side use only. Detected an invalid access attempt from the client side. Please ensure this property is used exclusively in server-side logic."
                );
            }
            private set => _remote = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected internal virtual void ___InjectServices___()
        {
        }

        /// <summary>
        /// Called after the object is instantiated and registered, but before it becomes active.
        /// </summary>
        /// <remarks>
        /// Override this method to perform any setup or initialization that must occur before the object is active.
        /// Common use cases include preparing resources or initializing state that does not depend on active functionality.
        /// </remarks>
        protected internal virtual void OnAwake()
        {
        }

        /// <summary>
        /// Called after the object is instantiated and has become active.
        /// </summary>
        /// <remarks>
        /// Override this method to perform setup or initialization tasks that require the object to be active.
        /// Typical use cases include registering the object in systems that depend on its active state.
        /// </remarks>
        protected internal virtual void OnStart()
        {
        }

        /// <summary>
        /// Called after the local player object is instantiated.
        /// </summary>
        /// <remarks>
        /// Override this method to perform initialization or setup specific to the local player's instance.
        /// Common use cases include setting up UI or initializing local player-specific data.
        /// </remarks>
        protected internal virtual void OnStartLocalPlayer()
        {
        }

        /// <summary>
        /// Called after a remote player object is instantiated.
        /// </summary>
        /// <remarks>
        /// Override this method to perform initialization or setup specific to remote players.
        /// Typical use cases include initializing visual indicators for remote players or handling network synchronization.
        /// </remarks>
        protected internal virtual void OnStartRemotePlayer()
        {
        }

        /// <summary>
        /// Called on the server once the client-side object has been fully spawned and registered. 
        /// This method ensures that all initializations on the client have been completed before 
        /// allowing the server to perform any post-spawn actions or setups specific to the client. 
        /// 
        /// Override this method to implement server-side logic that depends on the client object's 
        /// full availability and readiness. Typical use cases may include initializing server-side 
        /// resources linked to the client or sending initial data packets to the client after 
        /// confirming it has been completely registered on the network.
        /// </summary>
        protected internal virtual void OnSpawned()
        {
        }

        private void Internal_OnSpawned()
        {
            // Synchronizes all network variables with the client to ensure that the client has 
            // the most up-to-date data from the server immediately after the spawning process.
            SyncNetworkState(null);
        }

        /// <summary>
        /// Called on each update tick.
        /// </summary>
        /// <param name="data">The data associated with the current tick.</param>
        /// <remarks>
        /// Override this method to perform per-tick processing during the object's active state.
        /// This method is called at regular intervals defined by the system's tick rate, making it suitable
        /// for time-sensitive logic such as physics updates or state synchronization.
        /// </remarks>
        public virtual void OnTick(ITickInfo data)
        {
        }

        /// <summary>
        /// Registers the network behaviour within the network system, configuring
        /// server or client-specific event handlers and services based on the
        /// current network identity state. This method also integrates the network
        /// behaviour with the tick system if the tick system module is enabled.
        /// </summary>
        protected internal void Register()
        {
            CheckIfOverridden();
            FindAllNetworkVariables();
            if (Identity.IsServer)
            {
                serverRpcHandler.FindAllRpcMethods<ServerAttribute>(this, m_BindingFlags);
                Server = new NetworkBehaviourServer(this);
            }
            else
            {
                clientRpcHandler.FindAllRpcMethods<ClientAttribute>(this, m_BindingFlags);
                Client = new NetworkBehaviourClient(this);
            }

            InitializeServiceLocator();
            AddEventBehaviour();

            if (NetworkManager.TickSystemModuleEnabled)
            {
                NetworkManager.TickSystem.Register(this);
            }

            Identity.OnRequestAction += OnRequestedAction;
            Identity.OnSpawn += OnSpawned;
            Identity.OnSpawn += Internal_OnSpawned;

            NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
            NetworkManager.OnSceneLoaded += OnSceneLoaded;
            NetworkManager.OnSceneUnloaded += OnSceneUnloaded;
        }

        [Conditional("OMNI_DEBUG")]
        private void CheckIfOverridden() // Warning only.
        {
            Type type = GetType();
            MethodInfo method = type.GetMethod(nameof(OnTick));

            if (method.DeclaringType.Name != nameof(NetworkBehaviour) && !NetworkManager.TickSystemModuleEnabled)
            {
                NetworkLogger.__Log__(
                    "The Tick System Module is required to use the OnTick method. Please enable the Tick System Module in the inspector to proceed.",
                    logType: NetworkLogger.LogType.Error);
            }
        }

        /// <summary>
        /// Unregisters the current network behaviour from associated events and systems.
        /// This method ensures that the event behaviours and services linked to the network identity
        /// are properly removed, preventing further invocation of network actions.
        /// </summary>
        protected internal void Unregister()
        {
            var eventBehaviours = Identity.IsServer
                ? NetworkManager.ServerSide.LocalRpcHandlers
                : NetworkManager.ClientSide.LocalRpcHandlers;

            var key = (IdentityId, m_Id);
            if (!eventBehaviours.Remove(key))
            {
                NetworkLogger.__Log__(
                    $"[Unregister Error] The NetworkBehaviour with ID '{m_Id}' and peer ID '{IdentityId}' could not be found. This indicates it was not registered or may have already been unregistered. Please verify that the NetworkBehaviour is properly registered before attempting to unregister it.",
                    NetworkLogger.LogType.Error);
            }

            if (NetworkManager.TickSystemModuleEnabled)
            {
                NetworkManager.TickSystem.Unregister(this);
            }

            Identity.OnRequestAction -= OnRequestedAction;
            Identity.OnSpawn -= OnSpawned;
            Identity.OnSpawn -= Internal_OnSpawned;

            if (!Identity.Unregister(m_ServiceName))
            {
                NetworkLogger.__Log__(
                    $"[Unregister Error] Failed to unregister the ServiceLocator. The specified service name '{m_ServiceName}' could not be found. Ensure the ServiceLocator is correctly registered and active before attempting to unregister it.",
                    NetworkLogger.LogType.Error);
            }

            OnNetworkDestroy();
        }

        /// <summary>
        /// Called when the object is unregistered from the network.
        /// </summary>
        /// <remarks>
        /// Override this method to implement custom cleanup logic that needs to run when the object is unregistered from
        /// all network-related systems. Typical use cases include releasing resources, unsubscribing from events, or resetting state.
        /// </remarks>
        protected virtual void OnNetworkDestroy()
        {
        }

        /// <summary>
        /// Invokes a remote action on the server-side entity, triggered by a client-side entity. 
        /// This method should be overridden to define the specific action that will be performed 
        /// by the server in response to a client request.
        /// </summary>
        protected virtual void OnRequestedAction(DataBuffer data)
        {
        }

        /// <summary>
        /// Called after a scene has been loaded.
        /// </summary>
        /// <param name="scene">The <see cref="Scene"/> that was loaded.</param>
        /// <param name="mode">The <see cref="LoadSceneMode"/> used to load the scene.</param>
        /// <remarks>
        /// Override this method to perform any initialization or setup that needs to occur 
        /// after a new scene is loaded. This method is invoked automatically by the network manager.
        /// The default implementation unsubscribes this method from the scene load event to avoid duplicate invocations.
        /// </remarks>
        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            NetworkManager.OnSceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Called after a scene has been unloaded.
        /// </summary>
        /// <param name="scene">The <see cref="Scene"/> that was unloaded.</param>
        /// <remarks>
        /// Override this method to clean up or release resources related to the unloaded scene. 
        /// The default implementation unsubscribes this method from the scene unload event to avoid duplicate invocations.
        /// </remarks>
        protected virtual void OnSceneUnloaded(Scene scene)
        {
            NetworkManager.OnSceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// Called before a scene begins to load.
        /// </summary>
        /// <param name="scene">The <see cref="Scene"/> that is about to be loaded.</param>
        /// <param name="op">The <see cref="SceneOperationMode"/> indicating the type of operation being performed.</param>
        /// <remarks>
        /// Override this method to prepare for the loading of a new scene. Typical use cases include saving the current state,
        /// unloading unnecessary resources, or initializing data required for the new scene.
        /// The default implementation unsubscribes this method from the scene preparation event to avoid duplicate invocations.
        /// </remarks>
        protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
        }

        private void InitializeServiceLocator()
        {
            if (!Identity.TryRegister(this, m_ServiceName))
            {
                // Update the old reference to the new one.
                Identity.UpdateService(this, m_ServiceName);
            }
        }

        private void AddEventBehaviour()
        {
            var eventBehaviours = Identity.IsServer
                ? NetworkManager.ServerSide.LocalRpcHandlers
                : NetworkManager.ClientSide.LocalRpcHandlers;

            var key = (IdentityId, m_Id);
            if (!eventBehaviours.TryAdd(key, this))
            {
                eventBehaviours[key] = this;
            }
        }

        /// <summary>
        /// Rents a <see cref="DataBuffer"/> from the network manager's buffer pool.
        /// </summary>
        /// <remarks>
        /// The rented buffer is a reusable instance aimed at reducing memory allocations.
        /// It must be disposed of properly or used within a <c>using</c> statement to ensure it is returned to the pool.
        /// </remarks>
        /// <returns>A rented <see cref="DataBuffer"/> instance from the pool.</returns>
        protected DataBuffer Rent()
        {
            return NetworkManager.Pool.Rent();
        }

        private void TryCallClientRpc(byte msgId, DataBuffer buffer, int seqChannel)
        {
            if (clientRpcHandler.Exists(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        clientRpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        clientRpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        clientRpcHandler.Rpc(msgId, buffer, seqChannel, default, default, default);
                        break;
                }
            }
        }

        private void TryCallServerRpc(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            if (serverRpcHandler.Exists(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        serverRpcHandler.Rpc(msgId);
                        break;
                    case 1:
                        serverRpcHandler.Rpc(msgId, buffer);
                        break;
                    case 2:
                        serverRpcHandler.Rpc(msgId, buffer, peer);
                        break;
                    case 3:
                        serverRpcHandler.Rpc(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        serverRpcHandler.Rpc(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        serverRpcHandler.Rpc(msgId, buffer, peer, seqChannel, default, default);
                        break;
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnRpcInvoked(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool _, int seqChannel)
        {
            if (Identity.IsServer)
            {
                bool requiresOwnership = true;
                bool isClientAuthority = false;

                if (rpcId == NetworkConstants.NETWORK_VARIABLE_RPC_ID)
                {
                    byte id = buffer.BufferAsSpan[0];
                    if (networkVariables.TryGetValue(id, out NetworkVariableField field))
                    {
                        requiresOwnership = field.RequiresOwnership;
                        isClientAuthority = field.IsClientAuthority;
                    }

                    if (!NetworkManager.AllowNetworkVariablesFromClients && !isClientAuthority)
                    {
#if OMNI_DEBUG
                        NetworkLogger.__Log__(
                            "Access Denied: The client attempted to send Network Variables without proper permissions.",
                            NetworkLogger.LogType.Error
                        );
#else
                        NetworkLogger.__Log__(
                            "Client disconnected: Unauthorized attempt to send Network Variables detected. Ensure the client has the required permissions before allowing this operation.",
                            NetworkLogger.LogType.Error
                        );

                        peer.Disconnect();
#endif
                        return;
                    }
                }
                else requiresOwnership = false;

                // Requires ownership! -> security flag!
                if ((serverRpcHandler.IsRequiresOwnership(rpcId) &&
                     rpcId != NetworkConstants.NETWORK_VARIABLE_RPC_ID) || requiresOwnership)
                {
                    if (peer.Id != Identity.Owner.Id)
                    {
                        NetworkLogger.__Log__(
                            "[RPC Ownership Error] RPC rejected: Only the client with ownership of the object can send RPCs to the server. " +
                            "Ensure the client has authority over the target object before attempting this operation." +
                            "You can disable this restriction if ownership verification is not required.",
                            NetworkLogger.LogType.Error
                        );

                        return;
                    }
                }

                TryCallServerRpc(rpcId, buffer, peer, seqChannel);
            }
            else
            {
                TryCallClientRpc(rpcId, buffer, seqChannel);
            }
        }

        protected virtual void OnValidate()
        {
            if (!string.IsNullOrEmpty(m_ServiceName))
                return;

            int uniqueId = 0;
            string serviceName = GetType().Name;
            NetworkBehaviour[] services =
                transform.root.GetComponentsInChildren<NetworkBehaviour>(true);

            m_ServiceName = (uniqueId = services.Count(x => x.m_ServiceName.StartsWith(serviceName))) >= 1
                ? $"{serviceName}_{uniqueId}"
                : serviceName;

            NetworkHelper.EditorSaveObject(gameObject);
        }

        protected virtual void Reset()
        {
            OnValidate();
        }

        public override bool Equals(object obj)
        {
            if (Application.isPlaying)
            {
                if (obj is NetworkBehaviour other)
                {
                    return Equals(other);
                }
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            if (Application.isPlaying && _identity != null)
            {
                return HashCode.Combine(IdentityId, m_Id, IsServer);
            }

            return base.GetHashCode();
        }

        public bool Equals(NetworkBehaviour other)
        {
            if (Application.isPlaying && _identity != null)
            {
                bool isTheSameBehaviour = m_Id == other.m_Id;
                bool isTheSameIdentity = Identity.Equals(other.Identity);
                return isTheSameBehaviour && isTheSameIdentity && IsServer == other.IsServer;
            }

            return false;
        }
    }
}