using Omni.Core.Interfaces;
using Omni.Core.Modules.Ntp;
using Omni.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Omni.Inspector;
using UnityEngine;
using Omni.Core.Modules.Matchmaking;

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
    [DeclareFoldoutGroup("Service Settings")]
    [StackTrace]
    public class NetworkBehaviour : NetworkVariablesBehaviour, IRpcMessage, IBasedTickSystem, IEquatable<NetworkBehaviour>
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
            /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions _)
            {
                using DataBuffer message = m_NetworkBehaviour.CreateNetworkVariableMessage(property, propertyId);
                Rpc(NetworkConstants.k_NetworkVariableRpcId, message);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) message to the server with the specified parameters.
            /// </summary>
            /// <param name="rpcId">The unique identifier for the RPC message to be sent.</param>
            /// <param name="buffer">The data buffer containing the message payload. Defaults to null if no payload is provided.</param>
            /// <param name="deliveryMode">Specifies the message delivery mode, such as reliable or ordered. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The designated sequence channel for the message. Default is 0.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc(byte rpcId, DataBuffer message = null)
            {
                m_NetworkBehaviour.SetupRpcMessage(rpcId, default, false, default);
                NetworkManager.ClientSide.Rpc(rpcId, m_NetworkBehaviour.IdentityId, m_NetworkBehaviour.Id, message);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) message to the server with the specified parameters,
            /// using the provided message object and client options.
            /// </summary>
            /// <param name="rpcId">The identifier for the RPC message to be delivered.</param>
            /// <param name="message">The IMessage implementation representing the data to serialize and send.</param>
            /// <param name="options">The configuration settings defining buffer options for the RPC call. Defaults to a standard configuration if not provided.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SendMessage(byte rpcId, in IMessage message)
            {
                using var buffer = message.Serialize();
                Rpc(rpcId, buffer);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) to the server with the specified message ID, parameter, and optional client options.
            /// </summary>
            /// <typeparam name="T1">The type of the parameter to send. Must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier for the RPC message.</param>
            /// <param name="p1">The parameter value to send with the RPC.</param>
            /// <param name="options">Optional client options to configure the RPC behavior.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1>(byte rpcId, T1 p1)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                Rpc(rpcId, message);
            }

            /// <summary>
            /// Sends a remote procedure call (RPC) message to the server, utilizing the specified message ID and parameters.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter.</typeparam>
            /// <typeparam name="T2">The type of the second parameter.</typeparam>
            /// <param name="rpcId">The unique identifier for the RPC message.</param>
            /// <param name="p1">The first parameter to include in the RPC message.</param>
            /// <param name="p2">The second parameter to include in the RPC message.</param>
            /// <param name="options">Optional client-specific configuration for the RPC message.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2>(byte rpcId, T1 p1, T2 p2)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                Rpc(rpcId, message);
            }

            /// <summary>
            /// Sends a remote procedure call (RPC) to a server with the specified parameters and configuration options.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The message ID that identifies the RPC call.</param>
            /// <param name="p1">The first parameter to include in the RPC call.</param>
            /// <param name="p2">The second parameter to include in the RPC call.</param>
            /// <param name="p3">The third parameter to include in the RPC call.</param>
            /// <param name="options">The options for configuring the RPC call's behavior and delivery.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3>(byte rpcId, T1 p1, T2 p2, T3 p3)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                Rpc(rpcId, message);
            }

            /// <summary>
            /// Sends a remote procedure call (RPC) with the specified message ID and parameters to the server.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The ID of the message to be sent.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="p4">The fourth parameter to include in the RPC.</param>
            /// <param name="options">Additional client options for the RPC, which includes buffers and configurations.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                Rpc(rpcId, message);
            }

            /// <summary>
            /// Sends an RPC (Remote Procedure Call) message to the server with the specified arguments and message ID.
            /// </summary>
            /// <typeparam name="T1">The type of the first argument, must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second argument, must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third argument, must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth argument, must be unmanaged.</typeparam>
            /// <param name="rpcId">The ID of the message to send.</param>
            /// <param name="p1">The first argument to be included in the RPC message.</param>
            /// <param name="p2">The second argument to be included in the RPC message.</param>
            /// <param name="p3">The third argument to be included in the RPC message.</param>
            /// <param name="p4">The fourth argument to be included in the RPC message.</param>
            /// <param name="p5">The fifth argument to be included in the RPC message.</param>
            /// <param name="options">The options for configuring the client RPC message.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                Rpc(rpcId, message);
            }

            /// <summary>
            /// Sends an RPC (Remote Procedure Call) message to the server with the specified arguments and message ID.
            /// </summary>
            /// <typeparam name="T1">The type of the first argument, must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second argument, must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third argument, must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T6">The type of the sixth argument, must be unmanaged.</typeparam>
            /// <param name="rpcId">The ID of the message to send.</param>
            /// <param name="p1">The first argument to be included in the RPC message.</param>
            /// <param name="p2">The second argument to be included in the RPC message.</param>
            /// <param name="p3">The third argument to be included in the RPC message.</param>
            /// <param name="p4">The fourth argument to be included in the RPC message.</param>
            /// <param name="p5">The fifth argument to be included in the RPC message.</param>
            /// <param name="p6">The sixth argument to be included in the RPC message.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5, T6>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                message.WriteAsBinary(p6);
                Rpc(rpcId, message);
            }

            /// <summary>
            /// Sends an RPC (Remote Procedure Call) message to the server with the specified arguments and message ID.
            /// </summary>
            /// <typeparam name="T1">The type of the first argument, must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second argument, must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third argument, must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T6">The type of the sixth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T7">The type of the seventh argument, must be unmanaged.</typeparam>
            /// <param name="rpcId">The ID of the message to send.</param>
            /// <param name="p1">The first argument to be included in the RPC message.</param>
            /// <param name="p2">The second argument to be included in the RPC message.</param>
            /// <param name="p3">The third argument to be included in the RPC message.</param>
            /// <param name="p4">The fourth argument to be included in the RPC message.</param>
            /// <param name="p5">The fifth argument to be included in the RPC message.</param>
            /// <param name="p6">The sixth argument to be included in the RPC message.</param>
            /// <param name="p7">The seventh argument to be included in the RPC message.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5, T6, T7>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                message.WriteAsBinary(p6);
                message.WriteAsBinary(p7);
                Rpc(rpcId, message);
            }

            /// <summary>
            /// Sends an RPC (Remote Procedure Call) message to the server with the specified arguments and message ID.
            /// </summary>
            /// <typeparam name="T1">The type of the first argument, must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second argument, must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third argument, must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T6">The type of the sixth argument, must be unmanaged.</typeparam>
            /// <typeparam name="T7">The type of the seventh argument, must be unmanaged.</typeparam>
            /// <typeparam name="T8">The type of the eighth argument, must be unmanaged.</typeparam>
            /// <param name="rpcId">The ID of the message to send.</param>
            /// <param name="p1">The first argument to be included in the RPC message.</param>
            /// <param name="p2">The second argument to be included in the RPC message.</param>
            /// <param name="p3">The third argument to be included in the RPC message.</param>
            /// <param name="p4">The fourth argument to be included in the RPC message.</param>
            /// <param name="p5">The fifth argument to be included in the RPC message.</param>
            /// <param name="p6">The sixth argument to be included in the RPC message.</param>
            /// <param name="p7">The seventh argument to be included in the RPC message.</param>
            /// <param name="p8">The eighth argument to be included in the RPC message.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5, T6, T7, T8>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8)
            {
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                message.WriteAsBinary(p6);
                message.WriteAsBinary(p7);
                message.WriteAsBinary(p8);
                Rpc(rpcId, message);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte channel = 0)
            {
                m_NetworkBehaviour.__ClientRpcHandler.SetRpcParameters(rpcId, deliveryMode, Target.Auto, channel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetRpcParameters(byte rpcId, out DeliveryMode deliveryMode, out byte seqChannel)
            {
                m_NetworkBehaviour.__ClientRpcHandler.GetRpcParameters(rpcId, out deliveryMode, out _, out seqChannel);
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
            /// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
            /// </summary>
            /// <typeparam name="T">The type of the property to synchronize.</typeparam>
            /// <param name="property">The property value to synchronize.</param>
            /// <param name="propertyId">The ID of the property being synchronized.</param>
            /// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
            public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
            {
                using DataBuffer message = m_NetworkBehaviour.CreateNetworkVariableMessage(property, propertyId);
                m_NetworkBehaviour.SetupRpcMessage(NetworkConstants.k_NetworkVariableRpcId, NetworkGroup.None, true, propertyId);
                NetworkManager.ServerSide.SetTarget(Target.Self);
                NetworkManager.ServerSide.Rpc(NetworkConstants.k_NetworkVariableRpcId, peer, m_NetworkBehaviour.IdentityId, m_NetworkBehaviour.Id, message);
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
            public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options)
            {
                using DataBuffer message = m_NetworkBehaviour.CreateNetworkVariableMessage(property, propertyId);
                m_NetworkBehaviour.SetupRpcMessage(NetworkConstants.k_NetworkVariableRpcId, options.Group, true, propertyId);
                NetworkManager.ServerSide.Rpc(NetworkConstants.k_NetworkVariableRpcId, m_NetworkBehaviour.Identity.Owner, m_NetworkBehaviour.IdentityId, m_NetworkBehaviour.Id, message);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) to a specific <see cref="NetworkPeer"/>,
            /// bypassing the default ownership-based routing rules.
            /// </summary>
            /// <remarks>
            /// This overload is available **only on the server side** and supports two main usage scenarios:
            /// <list type="number">
            ///   <item>
            ///     <description>
            ///     <b>Direct targeting:</b>  
            ///     The RPC is executed on the same identity and behaviour script, but instead of being
            ///     routed back to the object's owner, it is delivered directly to the specified <paramref name="peer"/>.  
            ///     Useful when you want to send an RPC to a single peer that does not own the object.
            ///     </description>
            ///   </item>
            ///   <item>
            ///     <description>
            ///     <b>Ownership bypass for routing:</b>  
            ///     The specified <paramref name="peer"/> becomes the "source" of the RPC for routing purposes.  
            ///     This means if you provide a <paramref name="group"/> or set <c>Target.All</c> (or similar),
            ///     the message will be broadcast as if it originated from the given peer, instead of from the owner.  
            ///     This allows selective distribution of RPCs without being restricted by object ownership.
            ///     </description>
            ///   </item>
            /// </list>
            /// 
            /// Typical use cases include:
            /// <list type="bullet">
            ///   <item><description>Selective communication to a single peer without changing ownership</description></item>
            ///   <item><description>Broadcasting from a chosen peer to a group of others</description></item>
            ///   <item><description>Bypassing ownership restrictions in server-controlled scenarios</description></item>
            /// </list>
            /// </remarks>
            /// <param name="rpcId">The unique identifier for the RPC message.</param>
            /// <param name="peer">The target peer or the peer to be used as the routing source.</param>
            /// <param name="message">Optional data buffer containing additional RPC parameters.</param>
            /// <param name="group">Defines the network group scope for the RPC. Can be null if not used.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RpcViaPeer(byte rpcId, NetworkPeer peer, DataBuffer message = null, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                m_NetworkBehaviour.SetupRpcMessage(rpcId, group, true, default);
                m_NetworkBehaviour.__ServerRpcHandler.GetRpcParameters(rpcId, out _, out var target, out _);
                if (target == Target.Auto && peer.Id != 0 && group == null)
                    NetworkManager.ServerSide.SetTarget(Target.Self);
                NetworkManager.ServerSide.Rpc(rpcId, peer, m_NetworkBehaviour.IdentityId, m_NetworkBehaviour.Id, message);
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
            public void Rpc(byte rpcId, DataBuffer message = null, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                m_NetworkBehaviour.SetupRpcMessage(rpcId, group, true, default);
                NetworkManager.ServerSide.Rpc(rpcId, m_NetworkBehaviour.Identity.Owner, m_NetworkBehaviour.IdentityId, m_NetworkBehaviour.Id, message);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with a custom serialized message to the client.
            /// </summary>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="message">The message to serialize and send as part of the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SendMessage(byte rpcId, in IMessage message, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var buffer = message.Serialize();
                Rpc(rpcId, buffer, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with one unmanaged parameter to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter of the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1>(byte rpcId, T1 p1, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                Rpc(rpcId, message, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with two unmanaged parameters to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter of the RPC.</param>
            /// <param name="p2">The second parameter of the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2>(byte rpcId, T1 p1, T2 p2, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                Rpc(rpcId, message, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with three unmanaged parameters to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3>(byte rpcId, T1 p1, T2 p2, T3 p3, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                Rpc(rpcId, message, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with four unmanaged parameters to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="p4">The fourth parameter to include in the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                Rpc(rpcId, message, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with five unmanaged parameters to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="p4">The fourth parameter to include in the RPC.</param>
            /// <param name="p5">The fifth parameter to include in the RPC.</param>
            /// <param name="options">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                Rpc(rpcId, message, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with six unmanaged parameters to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T6">The type of the sixth parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="p4">The fourth parameter to include in the RPC.</param>
            /// <param name="p5">The fifth parameter to include in the RPC.</param>
            /// <param name="p6">The sixth parameter to include in the RPC.</param>
            /// <param name="group">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5, T6>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                message.WriteAsBinary(p6);
                Rpc(rpcId, message, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with seven unmanaged parameters to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T6">The type of the sixth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T7">The type of the seventh parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="p4">The fourth parameter to include in the RPC.</param>
            /// <param name="p5">The fifth parameter to include in the RPC.</param>
            /// <param name="p6">The sixth parameter to include in the RPC.</param>
            /// <param name="p7">The seventh parameter to include in the RPC.</param>
            /// <param name="group">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5, T6, T7>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                message.WriteAsBinary(p6);
                message.WriteAsBinary(p7);
                Rpc(rpcId, message, group);
            }

            /// <summary>
            /// Sends a Remote Procedure Call (RPC) with eight unmanaged parameters to the client.
            /// </summary>
            /// <typeparam name="T1">The type of the first parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T2">The type of the second parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T3">The type of the third parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T4">The type of the fourth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T5">The type of the fifth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T6">The type of the sixth parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T7">The type of the seventh parameter, which must be unmanaged.</typeparam>
            /// <typeparam name="T8">The type of the eighth parameter, which must be unmanaged.</typeparam>
            /// <param name="rpcId">The unique identifier of the RPC to send.</param>
            /// <param name="p1">The first parameter to include in the RPC.</param>
            /// <param name="p2">The second parameter to include in the RPC.</param>
            /// <param name="p3">The third parameter to include in the RPC.</param>
            /// <param name="p4">The fourth parameter to include in the RPC.</param>
            /// <param name="p5">The fifth parameter to include in the RPC.</param>
            /// <param name="p6">The sixth parameter to include in the RPC.</param>
            /// <param name="p7">The seventh parameter to include in the RPC.</param>
            /// <param name="p8">The eighth parameter to include in the RPC.</param>
            /// <param name="group">The server options containing configuration for the RPC.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Rpc<T1, T2, T3, T4, T5, T6, T7, T8>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, NetworkGroup group = null)
            {
                group ??= m_NetworkBehaviour.DefaultGroup;
                using var message = NetworkManager.Pool.Rent(enableTracking: false);
                message.WriteAsBinary(p1);
                message.WriteAsBinary(p2);
                message.WriteAsBinary(p3);
                message.WriteAsBinary(p4);
                message.WriteAsBinary(p5);
                message.WriteAsBinary(p6);
                message.WriteAsBinary(p7);
                message.WriteAsBinary(p8);
                Rpc(rpcId, message, group);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, Target target = Target.Auto, byte channel = 0)
            {
                m_NetworkBehaviour.__ServerRpcHandler.SetRpcParameters(rpcId, deliveryMode, target, channel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetRpcParameters(byte rpcId, out DeliveryMode deliveryMode, out Target target, out byte seqChannel)
            {
                m_NetworkBehaviour.__ServerRpcHandler.GetRpcParameters(rpcId, out deliveryMode, out target, out seqChannel);
            }
        }

        // Hacky: DIRTY CODE!
        // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
        // Despite its appearance, this approach is essential to achieve high performance.
        // Avoid refactoring as these techniques are crucial for optimizing execution speed.
        // Works with il2cpp.

        private readonly __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> m_ClientRpcHandler = new();
        private readonly __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> m_ServerRpcHandler = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, NetworkPeer, int, __Null__, __Null__> __ServerRpcHandler => m_ServerRpcHandler;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public __RpcHandler<DataBuffer, int, __Null__, __Null__, __Null__> __ClientRpcHandler => m_ClientRpcHandler;

        [SerializeField]
        [Group("Service Settings")]
        private string m_ServiceName = "";

        [SerializeField]
        [Group("Service Settings")]
        private byte m_Id = 0;

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
#if OMNI_DEBUG // Micro-optimization
                if (_identity != null)
                    return _identity;

                NetworkLogger.PrintHyperlink();
                throw new InvalidOperationException(
                    $"NetworkIdentity is missing on object '{transform.root.name}'. " +
                    "Make sure the object is instantiated and registered through the network system before accessing Identity."
                );
#else
                return _identity;
#endif
            }
            internal set => _identity = value;
        }

        /// <summary>
        /// Defines the default <see cref="NetworkGroup"/> associated with this object.  
        /// Used whenever no group is explicitly specified in a network operation  
        /// (e.g., RPCs, etc).
        /// </summary>
        public NetworkGroup DefaultGroup { get; set; } = null;

        /// <summary>
        /// Gets the unique identifier of the associated <see cref="NetworkIdentity"/>.
        /// </summary>
        /// <value>
        /// An integer representing the identifier of the associated <see cref="NetworkIdentity"/>.
        /// </value>
        public int IdentityId => Identity.Id;

        /// <summary>
        /// Indicates whether this instance represents the local player.
        /// </summary>
        /// <value><c>true</c> if this instance represents the local player; otherwise, <c>false</c>.</value>
        public bool IsLocalPlayer => Identity.IsLocalPlayer;

        /// <summary>
        /// Determines whether this networked behaviour instance belongs to the local player.
        /// Use this property to verify if the current networked object is under the authority of the local player.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is controlled by the local player; otherwise, <c>false</c>.
        /// </value>
        public bool IsMine => Identity.IsMine;

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
        /// Indicates whether the server is the owner of this networked behaviour's identity.
        /// </summary>
        /// <value>
        /// <c>true</c> if the associated <see cref="NetworkIdentity"/> is owned by the server; otherwise, <c>false</c>.
        /// </value>
        public bool IsOwnedByServer => Identity.IsOwnedByServer;

        /// <summary>
        /// Provides access to the synchronized network time protocol (NTP) instance used by the server and clients.
        /// </summary>
        /// <value>
        /// The <see cref="SimpleNtp"/> instance that synchronizes the server and client time.
        /// </value>
        protected SimpleNtp Sntp => NetworkManager.Sntp;

        /// <summary>
        /// Gets the synchronized time between the server and clients in ticks.
        /// </summary>
        /// <value>
        /// A <see cref="long"/> representing the synchronized time in ticks.
        /// </value>
        /// <remarks>
        /// This property provides a synchronized time reference between server and clients using the Network Time Protocol (NTP).
        /// On the server, returns the local time. On clients, returns the time synchronized with the server.
        /// While not perfectly identical across all nodes due to network latency and clock drift, it provides
        /// the best possible time synchronization between server and clients.
        /// </remarks>
        protected long SyncedTime => IsServer ? (long)Sntp.Server.LocalTime : (long)Sntp.Client.SyncedTime;

        /// <summary>
        /// Gets the synchronized time between the client and the server from the perspective of the current peer.
        /// </summary>
        /// <value>
        /// A <see cref="double"/> representing the synchronized time for the specific client relative to the server.
        /// </value>
        /// <remarks>
        /// Unlike <see cref="SyncedTime"/>, this property reflects the perspective of the individual client. 
        /// The value may differ slightly between clients due to network latency or other factors.
        /// </remarks>
        //protected double PeerTime => Identity.Owner.Time;

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
                    "Invalid access: the 'Client' property is client-side only and cannot be used on the server."
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
                    "Invalid access: the 'Server' property is server-side only and cannot be used on the client."
                );
            }
            private set => _remote = value;
        }

        /// <summary>
        /// Gets the name of the service associated with this instance.
        /// </summary>
        /// <value>
        /// The name of the service associated with this instance.
        /// </value>
        public string ServiceName
        {
            get => m_ServiceName;
            internal set => m_ServiceName = value;
        }

        /// <summary>
        /// Gets a value indicating whether this network behaviour has been registered with the network system.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is registered with the network system; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property is set internally when the object is registered with the network system.
        /// It can be used to check if the network behaviour is ready for network operations.
        /// </remarks>
        public bool IsRegistered { get; private set; }

        /// <summary>
        /// Gets the server's network peer instance that represents the server itself in the network.
        /// </summary>
        /// <value>
        /// The <see cref="NetworkPeer"/> instance that represents the server in the network.
        /// </value>
        /// <remarks>
        /// This property provides convenient access to the server's peer object, which contains
        /// information about the server's network identity and connection. Use this property
        /// when you need to reference the server as a network entity in server-side operations.
        /// </remarks>
        protected NetworkPeer ServerPeer
        {
            get
            {
                return NetworkManager.ServerSide.ServerPeer;
            }
        }

        /// <summary>
        /// Gets the server-side matchmaking manager that handles player grouping, matchmaking, and lobby functionality.
        /// This property provides access to methods for creating, managing, and monitoring player groups and matches.
        /// </summary>
        /// <remarks>
        /// This property offers a convenient shorthand to access the server matchmaking system without directly
        /// referencing the NetworkManager's matchmaking module. Use this for implementing features such as
        /// game lobbies, team assignment, custom matchmaking rules, and player grouping logic.
        /// </remarks>
        /// <value>
        /// The <see cref="NetworkMatchmaking"/> instance for handling server-side matchmaking operations.
        /// </value>
        protected NetworkMatchmaking Matchmaking => NetworkManager.Matchmaking;

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

        protected virtual void OnOwnershipGained()
        {

        }

        protected virtual void OnOwnershipLost()
        {

        }

        protected virtual void OnServerOwnershipTransferred(NetworkPeer oldPeer, NetworkPeer newPeer)
        {

        }

        private void Internal_OnServerObjectSpawnedForPeer(NetworkPeer peer)
        {
            OnServerObjectSpawnedForPeer(peer);
            // Synchronizes all network variables with the client to ensure that the client has 
            // the most up-to-date data from the server immediately after the spawning process.
            SyncNetworkState(peer);
            // Notify the owner that the object has been spawned for another peer.
            using DataBuffer msg = Rent(enableTracking: false);
            msg.Write(peer.Id);
            Identity.RequestActionToClient(NetworkConstants.k_OwnerObjectSpawnedForPeer, msg, Target.Self);
        }

        /// <summary>
        /// Called on the server when a specific peer (client) has finished spawning this object.
        /// This indicates that the peer has fully created and registered the object on their side,
        /// and is now ready to receive initial state or data synchronization for it.
        ///
        /// Override this method to implement server-side logic that initializes the object
        /// specifically for the new peer, such as sending initial state, authority data, or
        /// performing per-peer setup.
        /// 
        /// <para>
        /// Example: If Client A owns this object, when Client B joins and spawns it locally,
        /// this method will be invoked on the server with <paramref name="peer"/> representing Client B.
        /// </para>
        /// </summary>
        /// <param name="peer">
        /// The peer (client) that has just finished spawning this object
        /// and is ready to receive initialization data from the server.
        /// </param>
        protected virtual void OnServerObjectSpawnedForPeer(NetworkPeer peer)
        {
        }

        /// <summary>
        /// Called on the client that owns this object when it has been spawned 
        /// for another peer in the network.
        /// 
        /// This method allows the owner to react when a remote peer becomes 
        /// ready to receive the initial state of this object. Typically, the 
        /// owner can send RPCs or state updates to ensure the new peer is 
        /// synchronized with the correct data.
        /// 
        /// <para><b>Use case:</b></para>
        /// - In server-authoritative architectures, this is often not required, 
        ///   since the server provides the initial state.
        /// - In peer-authoritative or relay-only setups, this hook is important, 
        ///   as the server only relays spawns, and the owner must provide the 
        ///   authoritative state to the new peer.
        /// 
        /// <para><b>Parameters:</b></para>
        /// <paramref name="peerId"/> — The peer id of the peer that has just spawned this object 
        /// and is now ready to receive initial data from the owner.
        /// </summary>
        protected virtual void OnOwnerObjectSpawnedForPeer(int peerId)
        {
        }

        private void Internal_OnRequestedAction(byte actionId, DataBuffer rawData, NetworkPeer peer)
        {
            // allow multiples reads from the "same" databuffer.
            using var data = Rent(enableTracking: false);
            data.Internal_CopyFrom(rawData);
            data.SeekToBegin();

            if (IsServer)
            {
                if (actionId == NetworkConstants.k_SpawnNotificationId)
                {
                    Internal_OnServerObjectSpawnedForPeer(peer);
                    return;
                }
            }
            else
            {
                switch (actionId)
                {
                    case NetworkConstants.k_SetOwnerId:
                        {
                            if (IsMine) OnOwnershipGained();
                            else OnOwnershipLost();
                            return;
                        }
                    case NetworkConstants.k_DestroyEntityId:
                        return;
                    case NetworkConstants.k_OwnerObjectSpawnedForPeer:
                        {
                            int peerId = data.Read<int>();
                            OnOwnerObjectSpawnedForPeer(peerId);
                            return;
                        }
                }
            }

            OnRequestedAction(actionId, data, peer);
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
        public virtual void OnTick(ITickData data)
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
            if (IsRegistered)
                return;

            CheckIfOverridden();
            ___RegisterNetworkVariables___();
            ___NotifyCollectionChange___(); // Registers notifications for changes in the collection, enabling automatic updates when the collection is modified.
            if (Identity.IsServer)
            {
                m_ServerRpcHandler.RegisterRpcMethodHandlers<ServerAttribute>(this);
                Server = new NetworkBehaviourServer(this);
            }
            else
            {
                m_ClientRpcHandler.RegisterRpcMethodHandlers<ClientAttribute>(this);
                Client = new NetworkBehaviourClient(this);
            }

            InitializeServiceLocator();
            AddRpcHandler();

            if (NetworkManager.TickSystemModuleEnabled)
                NetworkManager.TickSystem.Register((IdentityId, Id, IsServer), this);

            Identity.OnServerOwnershipTransferred += OnServerOwnershipTransferred;
            Identity.OnRequestAction += Internal_OnRequestedAction;

            IsRegistered = true;
        }

        /// <summary>
        /// Unregisters the current network behaviour from associated events and systems.
        /// This method ensures that the event behaviours and services linked to the network identity
        /// are properly removed, preventing further invocation of network actions.
        /// </summary>
        protected internal void Unregister(bool destroy = true)
        {
            if (!IsRegistered)
                return;

            OnPreUnregister();
            var identities = IsServer
                  ? NetworkManager.ServerSide.Identities
                  : NetworkManager.ClientSide.Identities;

            if (identities.Remove(IdentityId, out var oldRef))
            {
                if (oldRef != null && destroy)
                    Destroy(oldRef.gameObject);
            }

            var rpcHandlers = Identity.IsServer
                ? NetworkManager.ServerSide.LocalRpcHandlers
                : NetworkManager.ClientSide.LocalRpcHandlers;

            var key = (IdentityId, m_Id);
            rpcHandlers.Remove(key);

            if (NetworkManager.TickSystemModuleEnabled)
                NetworkManager.TickSystem.Unregister((IdentityId, Id, IsServer));

            Identity.OnServerOwnershipTransferred -= OnServerOwnershipTransferred;
            Identity.OnRequestAction -= Internal_OnRequestedAction;

            Identity.Unregister(m_ServiceName);
            IsRegistered = false;
            OnPostUnregister();
        }

        [Conditional("OMNI_DEBUG")]
        private void CheckIfOverridden() // Warning only.
        {
            Type type = GetType();
            MethodInfo method = type.GetMethod(nameof(OnTick));
            if (method != null)
            {
                if (method.DeclaringType?.Name != nameof(NetworkBehaviour) && !NetworkManager.TickSystemModuleEnabled)
                {
                    NetworkLogger.__Log__(
                        "OnTick requires the Tick System Module. Enable it in the inspector.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
        }

        /// <summary>
        /// Called when the network behaviour is being destroyed.
        /// </summary>
        /// <remarks>
        /// This method is automatically called by Unity when the GameObject is destroyed.
        /// The default implementation unregisters this behaviour from the network system.
        /// Override this method to implement custom cleanup logic, but always call the base implementation
        /// to ensure proper network cleanup.
        /// </remarks>
        protected virtual void OnDestroy()
        {
            try
            {
                if (Application.isPlaying)
                    Unregister();

                OnNetworkDestroy();
            }
            catch { }
        }

        /// <summary>
        /// Called when the GameObject is being destroyed by Unity,
        /// after the component has been unregistered from the network.
        /// </summary>
        /// <remarks>
        /// This method is always invoked at the end of the <c>OnDestroy</c>
        /// sequence, ensuring that the object is no longer registered
        /// in the network when executed.  
        /// 
        /// Override this method to implement cleanup logic that should
        /// occur strictly during Unity's destruction cycle, such as:
        /// - Releasing Unity-specific resources
        /// - Cleaning up editor-only references
        /// - Logging or diagnostic information
        /// </remarks>
        protected virtual void OnNetworkDestroy()
        {

        }

        /// <summary>
        /// Called immediately before this component is unregistered
        /// from the network.
        /// </summary>
        /// <remarks>
        /// Override this method to implement logic that must occur
        /// before the component is removed from the network.  
        /// At this stage, the object is still fully registered and can
        /// interact with other network entities.  
        /// 
        /// Example use cases:
        /// - Sending a final state update
        /// - Notifying dependent systems
        /// - Stopping background coroutines
        /// </remarks>
        protected virtual void OnPreUnregister()
        {
        }

        /// <summary>
        /// Called immediately after this component has been unregistered
        /// from the network.
        /// </summary>
        /// <remarks>
        /// Override this method to implement logic that should occur
        /// once the component has been fully removed from the network.  
        /// At this stage, the object is no longer registered and should
        /// not be used for further network operations.  
        /// 
        /// Example use cases:
        /// - Final diagnostics or logging
        /// - Releasing transient data
        /// - Triggering dependent cleanup routines
        /// </remarks>
        protected virtual void OnPostUnregister()
        {
        }

        /// <summary>
        /// Invokes a remote action on the server-side entity, triggered by a client-side entity. 
        /// This method should be overridden to define the specific action that will be performed 
        /// by the server in response to a client request.
        /// </summary>
        protected virtual void OnRequestedAction(byte actionId, DataBuffer data, NetworkPeer peer)
        {
        }

        private void InitializeServiceLocator()
        {
            if (!Identity.TryRegister(this, m_ServiceName))
                Identity.UpdateService(this, m_ServiceName);
        }

        private void AddRpcHandler()
        {
            var rpcHandlers = Identity.IsServer
                ? NetworkManager.ServerSide.LocalRpcHandlers
                : NetworkManager.ClientSide.LocalRpcHandlers;

            var key = (IdentityId, m_Id);
            if (!rpcHandlers.TryAdd(key, this))
                rpcHandlers[key] = this;
        }

        /// <summary>
        /// Rents a <see cref="DataBuffer"/> from the network manager's buffer pool.
        /// </summary>
        /// <remarks>
        /// The rented buffer is a reusable instance aimed at reducing memory allocations.
        /// It must be disposed of properly or used within a <c>using</c> statement to ensure it is returned to the pool.
        /// </remarks>
        /// <returns>A rented <see cref="DataBuffer"/> instance from the pool.</returns>
        protected DataBuffer Rent(bool enableTracking = true, [CallerMemberName] string methodName = "")
        {
            return NetworkManager.Pool.Rent(enableTracking, methodName);
        }

        private void TryCallClientRpc(byte rpcId, DataBuffer buffer, int seqChannel)
        {
            if (m_ClientRpcHandler.IsValid(rpcId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        m_ClientRpcHandler.Rpc(rpcId);
                        break;
                    case 1:
                        m_ClientRpcHandler.Rpc(rpcId, buffer);
                        break;
                    case 2:
                        m_ClientRpcHandler.Rpc(rpcId, buffer, seqChannel);
                        break;
                    case 3:
                        m_ClientRpcHandler.Rpc(rpcId, buffer, seqChannel, default);
                        break;
                    case 4:
                        m_ClientRpcHandler.Rpc(rpcId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        m_ClientRpcHandler.Rpc(rpcId, buffer, seqChannel, default, default, default);
                        break;
                }
            }
        }

        private void TryCallServerRpc(byte rpcId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
        {
            if (m_ServerRpcHandler.IsValid(rpcId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        m_ServerRpcHandler.Rpc(rpcId);
                        break;
                    case 1:
                        m_ServerRpcHandler.Rpc(rpcId, buffer);
                        break;
                    case 2:
                        m_ServerRpcHandler.Rpc(rpcId, buffer, peer);
                        break;
                    case 3:
                        m_ServerRpcHandler.Rpc(rpcId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        m_ServerRpcHandler.Rpc(rpcId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        m_ServerRpcHandler.Rpc(rpcId, buffer, peer, seqChannel, default, default);
                        break;
                }
            }
        }

        public void SetupRpcMessage(byte rpcId, NetworkGroup group, bool _, byte networkVariableId)
        {
            if (rpcId != NetworkConstants.k_NetworkVariableRpcId)
            {
                if (IsServer)
                {
                    m_ServerRpcHandler.GetRpcParameters(rpcId, out var deliveryMode, out var target, out var sequenceChannel);
                    SetupRpcMessage(rpcId, deliveryMode, target, group, sequenceChannel, _);
                }
                else
                {
                    m_ClientRpcHandler.GetRpcParameters(rpcId, out var deliveryMode, out var __, out var sequenceChannel);
                    SetupRpcMessage(rpcId, deliveryMode, __, NetworkGroup.None, sequenceChannel, _);
                }
            }
            else
            {
                if (m_NetworkVariables.TryGetValue(networkVariableId, out NetworkVariableField field))
                {
                    if (IsServer)
                    {
                        SetupRpcMessage(rpcId, field.DeliveryMode, field.Target, group, field.SequenceChannel, _);
                    }
                    else
                    {
                        SetupRpcMessage(rpcId, field.DeliveryMode, default, NetworkGroup.None, field.SequenceChannel, _);
                    }
                }
            }
        }

        public void SetupRpcMessage(byte rpcId, DeliveryMode deliveryMode, Target target, NetworkGroup group, byte seqChannel, bool __)
        {
            if (IsServer)
            {
                NetworkManager.ServerSide.SetDefaultNetworkConfiguration(deliveryMode, target, group, seqChannel);
            }
            else
            {
                NetworkManager.ClientSide.SetDeliveryMode(deliveryMode);
                NetworkManager.ClientSide.SetSequenceChannel(seqChannel);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnRpcReceived(byte rpcId, DataBuffer buffer, NetworkPeer peer, bool _, int seqChannel)
        {
            try
            {
                if (IsServer)
                {
                    bool requiresOwnership = true;
                    bool isClientAuthority = false;

                    if (rpcId == NetworkConstants.k_NetworkVariableRpcId)
                    {
                        byte id = buffer.BufferAsSpan[0];
                        if (m_NetworkVariables.TryGetValue(id, out NetworkVariableField field))
                        {
                            requiresOwnership = field.RequiresOwnership;
                            isClientAuthority = field.IsClientAuthority;

                            if (!isClientAuthority)
                            {
#if OMNI_DEBUG
                                NetworkLogger.__Log__(
                                    $"NetworkVariable modification rejected. " +
                                    $"Client {peer.Id} is not allowed to modify '{field.Name}' (Id={id}).",
                                    NetworkLogger.LogType.Error
                                );
#else
                                NetworkLogger.__Log__(
                                    $"Client {peer.Id} disconnected: tried to modify '{field.Name}' (Id={id}) without permission. " +
                                    "Enable 'IsClientAuthority' if client writes are intended.",
                                    NetworkLogger.LogType.Error
                                );

                                peer.Disconnect();
#endif
                                return;
                            }
                        }
                        else
                        {
                            NetworkLogger.__Log__($"The 'NetworkVariable' with ID '{id}' does not exist. " +
                                "Ensure the ID is valid and registered in the network variables collection.",
                                NetworkLogger.LogType.Error
                            );

                            return;
                        }
                    }
                    else requiresOwnership = false;

                    // Requires ownership! -> security flag!
                    if ((m_ServerRpcHandler.IsRequiresOwnership(rpcId) &&
                         rpcId != NetworkConstants.k_NetworkVariableRpcId) || requiresOwnership)
                    {
                        if (peer.Id != Identity.Owner.Id)
                        {
                            if (!Identity.isOwnershipTransitioning)
                            {
                                NetworkLogger.__Log__(
                                    $"RPC rejected: only the owning client can call this RPC. " +
                                    $"RpcId={rpcId}, Name={m_ServerRpcHandler.GetRpcName(rpcId)}, Object={GetType().Name}",
                                    NetworkLogger.LogType.Error
                                );
                            }

                            // Ignore RPCs from clients that do not have ownership of the object.
                            return;
                        }
                    }

                    m_ServerRpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                    TryCallServerRpc(rpcId, buffer, peer, seqChannel);
                }
                else
                {
                    m_ClientRpcHandler.ThrowIfNoRpcMethodFound(rpcId);
                    TryCallClientRpc(rpcId, buffer, seqChannel);
                }
            }
            catch (Exception ex)
            {
                string methodName = NetworkConstants.k_InvalidRpcName;
                if (IsServer) methodName = m_ServerRpcHandler.GetRpcName(rpcId);
                else methodName = m_ClientRpcHandler.GetRpcName(rpcId);

                NetworkLogger.__Log__(
                    $"An exception occurred while processing the RPC -> " +
                    $"Rpc Id: '{rpcId}', Rpc Name: '{methodName}' in Class: '{GetType().Name}' -> " +
                    $"Exception Details: {ex.Message}. ",
                    NetworkLogger.LogType.Error
                );

                NetworkLogger.PrintHyperlink(ex);
#if OMNI_DEBUG
                throw;
#endif
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