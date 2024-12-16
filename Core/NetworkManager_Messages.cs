using Omni.Core.Cryptography;
using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace Omni.Core
{
    // High-level methods for sending network messages.
    public partial class NetworkManager
    {
        private static void Internal_SendMessage(byte msgId, NetworkPeer peer, DataBuffer buffer, Target target,
            DeliveryMode deliveryMode, bool isServer, int groupId, DataCache dataCache, byte seqChannel)
        {
            buffer ??= DataBuffer.Empty;
            if (!isServer)
            {
                SendToServer(msgId, buffer, deliveryMode, seqChannel);
            }
            else
            {
                if (peer == null)
                {
                    throw new NullReferenceException("Peer cannot be null.");
                }

                SendToClient(msgId, buffer, peer, target, deliveryMode, groupId, dataCache, seqChannel);
            }
        }

        public static class ClientSide
        {
            public static BandwidthMonitor SentBandwidth => Connection.Client.SentBandwidth;
            public static BandwidthMonitor ReceivedBandwidth => Connection.Client.ReceivedBandwidth;

            /// <summary>
            /// Gets the server peer used for exclusively for encryption keys.
            /// </summary>
            public static NetworkPeer ServerPeer { get; } = new(new IPEndPoint(IPAddress.None, 0), 0, isServer: false);

            /// <summary>
            /// Gets the server RSA public key .
            /// This property stores the public key used in RSA cryptographic operations.
            /// The public key is utilized to encrypt data, ensuring that only the holder of the corresponding private key can decrypt it.
            /// </summary>
            internal static string RsaServerPublicKey { get; set; }

            internal static Dictionary<int, NetworkGroup> Groups { get; } = new();

            // int: identifier(identity id)
            internal static Dictionary<int, IRpcMessage> GlobalRpcHandlers { get; } = new();
            internal static Dictionary<(int, byte), IRpcMessage> LocalRpcHandlers { get; } = new();

            public static Dictionary<int, NetworkIdentity> Identities { get; } = new();
            public static Dictionary<int, NetworkPeer> Peers { get; } = new();

            /// <summary>
            /// Represents an event that is triggered when a message is received.
            /// </summary>
            public static event Action<byte, DataBuffer, int> OnMessage
            {
                add => OnClientCustomMessage += value;
                remove => OnClientCustomMessage -= value;
            }

            public static NetworkIdentity GetIdentity(int identityId)
            {
                if (Identities.TryGetValue(identityId, out NetworkIdentity identity))
                {
                    return identity;
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"Get Error: Identity with ID {identityId} not found.",
                        NetworkLogger.LogType.Error
                    );

                    return null;
                }
            }

            public static bool TryGetIdentity(int identityId, out NetworkIdentity identity)
            {
                return Identities.TryGetValue(identityId, out identity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendMessage(byte msgId, ClientOptions options)
            {
                SendMessage(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendMessage(byte msgId, DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                Internal_SendMessage(msgId, LocalPeer, buffer, Target.SelfOnly, deliveryMode, false, 0, DataCache.None,
                    sequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void GlobalRpc(byte msgId, ClientOptions options)
            {
                GlobalRpc(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void GlobalRpc(byte msgId, DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                SendMessage(msgId, buffer, deliveryMode, sequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Rpc(byte msgId, int identityId, ClientOptions options)
            {
                Rpc(msgId, identityId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
            }

            public static void Rpc(byte msgId, int identityId, DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                buffer ??= DataBuffer.Empty;
                using DataBuffer message = Pool.Rent();
                message.Write(identityId);
                message.Write(msgId);
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.GlobalRpc, message, deliveryMode, sequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Rpc(byte msgId, int identityId, byte instanceId, ClientOptions options)
            {
                Rpc(msgId, identityId, instanceId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
            }

            public static void Rpc(byte msgId, int identityId, byte instanceId, DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                buffer ??= DataBuffer.Empty;
                using DataBuffer message = Pool.Rent();
                message.Internal_Write(identityId);
                message.Write(instanceId);
                message.Write(msgId);
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.LocalRpc, message, deliveryMode, sequenceChannel);
            }

            internal static void JoinGroup(string groupName, DataBuffer buffer)
            {
                buffer ??= DataBuffer.Empty;
                if (string.IsNullOrEmpty(groupName))
                {
                    throw new Exception("Group name cannot be null or empty.");
                }

                if (groupName.Length > 256)
                {
                    throw new Exception("Group name cannot be longer than 256 characters.");
                }

                using DataBuffer message = Pool.Rent();
                message.WriteString(groupName);
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.JoinGroup, message, DeliveryMode.ReliableOrdered, 0);
            }

            internal static void LeaveGroup(string groupName, string reason = "Leave called by user.")
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    throw new Exception("Group name cannot be null or empty.");
                }

                if (groupName.Length > 256)
                {
                    throw new Exception("Group name cannot be longer than 256 characters.");
                }

                using DataBuffer message = Pool.Rent();
                message.WriteString(groupName);
                message.WriteString(reason);
                SendMessage(MessageType.LeaveGroup, message, DeliveryMode.ReliableOrdered, 0);
            }

            internal static void AddRpcMessage(int identityId, IRpcMessage behaviour)
            {
                if (!GlobalRpcHandlers.TryAdd(identityId, behaviour))
                {
                    GlobalRpcHandlers[identityId] = behaviour;
                }
            }

            internal static void SendSpawnNotification(NetworkIdentity identity)
            {
                // Notifies the server that the spawn has completed.
                using var message = Pool.Rent();
                message.Write(identity.IdentityId);
                SendMessage(MessageType.Spawn, message, DeliveryMode.ReliableOrdered, 0);
                OnClientIdentitySpawned?.Invoke(identity);
            }
        }

        public static class ServerSide
        {
            public static BandwidthMonitor SentBandwidth => Connection.Server.SentBandwidth;
            public static BandwidthMonitor ReceivedBandwidth => Connection.Server.ReceivedBandwidth;

            /// <summary>
            /// Gets the server peer.
            /// </summary>
            /// <remarks>
            /// The server peer is a special peer that is used to represent the server.
            /// </remarks>
            public static NetworkPeer ServerPeer { get; } = new(new IPEndPoint(IPAddress.None, 0), 0, isServer: true);

            /// <summary>
            /// Gets the RSA public key.
            /// This property stores the public key used in RSA cryptographic operations.
            /// The public key is utilized to encrypt data, ensuring that only the holder of the corresponding private key can decrypt it.
            /// </summary>
            internal static string RsaPublicKey { get; private set; }

            /// <summary>
            /// Gets the RSA private key.
            /// This property stores the private key used in RSA cryptographic operations.
            /// The private key is crucial for decrypting data that has been encrypted with the corresponding public key.
            /// It is also used to sign data, ensuring the authenticity and integrity of the message.
            /// </summary>
            internal static string RsaPrivateKey { get; private set; }

            internal static List<NetworkCache> AppendCachesGlobal { get; } = new();
            internal static Dictionary<int, NetworkCache> OverwriteCachesGlobal { get; } = new();

            internal static Dictionary<int, NetworkGroup> Groups => GroupsById;
            internal static Dictionary<int, IRpcMessage> GlobalRpcHandlers { get; } = new();
            internal static Dictionary<(int, byte), IRpcMessage> LocalRpcHandlers { get; } = new();

            public static Dictionary<int, NetworkPeer> Peers => PeersById;
            public static Dictionary<int, NetworkIdentity> Identities { get; } = new();

            public static event Action<byte, DataBuffer, NetworkPeer, int> OnMessage
            {
                add => OnServerCustomMessage += value;
                remove => OnServerCustomMessage -= value;
            }

            internal static void GenerateRsaKeys()
            {
                RsaCryptography.GetRsaKeys(out var rsaPrivateKey, out var rsaPublicKey);
                RsaPrivateKey = rsaPrivateKey;
                RsaPublicKey = rsaPublicKey;
            }

            public static NetworkIdentity GetIdentity(int identityId)
            {
                if (Identities.TryGetValue(identityId, out NetworkIdentity identity))
                {
                    return identity;
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"Get Error: Identity with ID {identityId} not found.",
                        NetworkLogger.LogType.Error
                    );

                    return null;
                }
            }

            /// <summary>
            /// Attempts to retrieve a NetworkIdentity instance by its unique identity ID.
            /// </summary>
            /// <param name="identityId">The unique ID of the NetworkIdentity to retrieve.</param>
            /// <param name="identity">The retrieved NetworkIdentity instance, or null if not found.</param>
            /// <returns>True if the NetworkIdentity was found, false otherwise.</returns>
            public static bool TryGetIdentity(int identityId, out NetworkIdentity identity)
            {
                return Identities.TryGetValue(identityId, out identity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendMessage(byte msgId, NetworkPeer peer, ServerOptions options)
            {
                SendMessage(msgId, peer, options.Buffer, options.Target, options.DeliveryMode, options.GroupId,
                    options.DataCache, options.SequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendMessage(byte msgId, NetworkPeer peer, DataBuffer buffer = null,
                Target target = Target.Auto, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0)
            {
                dataCache ??= DataCache.None;
                Internal_SendMessage(msgId, peer, buffer, target, deliveryMode, true, groupId, dataCache,
                    sequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void GlobalRpc(byte msgId, NetworkPeer peer, ServerOptions options)
            {
                GlobalRpc(msgId, peer, options.Buffer, options.Target, options.DeliveryMode, options.GroupId,
                    options.DataCache, options.SequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void GlobalRpc(byte msgId, NetworkPeer peer, DataBuffer buffer = null,
                Target target = Target.Auto, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0)
            {
                dataCache ??= DataCache.None;
                SendMessage(msgId, peer, buffer, target, deliveryMode, groupId, dataCache, sequenceChannel);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Rpc(byte msgId, NetworkPeer peer, int identityId, ServerOptions options)
            {
                Rpc(msgId, peer, identityId, options.Buffer, options.Target, options.DeliveryMode, options.GroupId,
                    options.DataCache, options.SequenceChannel);
            }

            public static void Rpc(byte msgId, NetworkPeer peer, int identityId, DataBuffer buffer = null,
                Target target = Target.Auto, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0)
            {
                dataCache ??= DataCache.None;
                buffer ??= DataBuffer.Empty;

                using DataBuffer message = Pool.Rent();
                message.Write(identityId);
                message.Write(msgId);
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.GlobalRpc, peer, message, target, deliveryMode, groupId, dataCache,
                    sequenceChannel);

                // byte count per empty message: 4 + 1 = 5 + header;
                // TODO: reduce bandwidth usage
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Rpc(byte msgId, NetworkPeer peer, int identityId, byte instanceId, ServerOptions options)
            {
                Rpc(msgId, peer, identityId, instanceId, options.Buffer, options.Target, options.DeliveryMode,
                    options.GroupId, options.DataCache, options.SequenceChannel);
            }

            public static void Rpc(byte msgId, NetworkPeer peer, int identityId, byte instanceId,
                DataBuffer buffer = null, Target target = Target.Auto,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0,
                DataCache dataCache = default, byte sequenceChannel = 0)
            {
                buffer ??= DataBuffer.Empty;
                dataCache ??= DataCache.None;

                using DataBuffer message = Pool.Rent();
                message.Internal_Write(identityId); // min: 1 byte, max = 4 bytes
                message.Write(instanceId); // 1 byte
                message.Write(msgId); // 1 byte
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.LocalRpc, peer, message, target, deliveryMode, groupId, dataCache,
                    sequenceChannel); // 1 byte

                // byte count per empty message: 1 + 1 + 1 + 1 = 4;
                // TODO: reduce bandwidth usage
            }

            internal static int GetGroupIdByName(string groupName)
            {
                return groupName.GetHashCode();
            }

            internal static NetworkGroup GetGroupById(int groupId)
            {
                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                {
                    return group;
                }

                NetworkLogger.__Log__(
                    $"Get Error: Group with ID {groupId} not found.",
                    NetworkLogger.LogType.Error
                );

                return null;
            }

            internal static bool TryGetGroupById(int groupId, out NetworkGroup group)
            {
                return GroupsById.TryGetValue(groupId, out group);
            }

            internal static void JoinGroup(string groupName, DataBuffer buffer, NetworkPeer peer,
                bool writeBufferToClient)
            {
                void SendResponseToClient()
                {
                    using DataBuffer message = Pool.Rent();
                    message.WriteString(groupName);

                    if (writeBufferToClient)
                    {
                        message.Write(buffer.BufferAsSpan);
                    }

                    SendMessage(MessageType.JoinGroup, peer, message, Target.SelfOnly, DeliveryMode.ReliableOrdered, 0,
                        DataCache.None, 0);
                }

                int uniqueId = GetGroupIdByName(groupName);
                if (GroupsById.TryGetValue(uniqueId, out NetworkGroup group))
                {
                    if (!group._peersById.TryAdd(peer.Id, peer))
                    {
                        NetworkLogger.__Log__(
                            $"JoinGroup: Failed to add peer: {peer.Id} to group: {groupName} because it already exists.",
                            NetworkLogger.LogType.Error
                        );

                        OnPlayerFailedJoinGroup?.Invoke(
                            peer,
                            $"JoinGroup: Failed to add peer: {peer.Id} to group: {groupName} because it already exists."
                        );

                        return;
                    }

                    EnterGroup(buffer, peer, group);
                }
                else
                {
                    group = new NetworkGroup(uniqueId, groupName, isServer: true);
                    group._peersById.Add(peer.Id, peer);
                    if (!GroupsById.TryAdd(uniqueId, group))
                    {
                        NetworkLogger.__Log__(
                            $"JoinGroup: Failed to add group: {groupName} because it already exists.",
                            NetworkLogger.LogType.Error
                        );

                        OnPlayerFailedJoinGroup?.Invoke(
                            peer,
                            $"JoinGroup: Failed to add group: {groupName} because it already exists."
                        );

                        return;
                    }

                    EnterGroup(buffer, peer, group);
                }

                void EnterGroup(DataBuffer buffer, NetworkPeer peer, NetworkGroup group)
                {
                    if (!peer._groups.TryAdd(group.Id, group))
                    {
                        OnPlayerFailedJoinGroup?.Invoke(
                            peer,
                            "JoinGroup: Failed to add group to peer!!!"
                        );

                        NetworkLogger.__Log__("JoinGroup: Failed to add group to peer!!!");
                        return;
                    }

                    if (!group.IsSubGroup)
                    {
                        peer.MainGroup = group;
                    }

                    // Set the master client if it's the first player in the group.
                    if (group.MasterClient == null)
                    {
                        group.SetMasterClient(peer);
                    }

                    _allowZeroGroupForInternalMessages = true;
                    SendResponseToClient();
                    OnPlayerJoinedGroup?.Invoke(buffer, group, peer);
                }
            }

            internal static NetworkGroup AddGroup(string groupName)
            {
                int groupId = GetGroupIdByName(groupName);
                NetworkGroup group = new(groupId, groupName, isServer: true);
                if (!GroupsById.TryAdd(groupId, group))
                {
                    throw new Exception(
                        $"Failed to add group: [{groupName}] because it already exists."
                    );
                }

                return group;
            }

            internal static bool TryAddGroup(string groupName, out NetworkGroup group)
            {
                int groupId = GetGroupIdByName(groupName);
                group = new NetworkGroup(groupId, groupName, isServer: true);
                return GroupsById.TryAdd(groupId, group);
            }

            internal static void LeaveGroup(string groupName, string reason, NetworkPeer peer)
            {
                void SendResponseToClient()
                {
                    using DataBuffer message = Pool.Rent();
                    message.WriteString(groupName);
                    message.WriteString(reason);

                    SendMessage(MessageType.LeaveGroup, peer, message, Target.SelfOnly, DeliveryMode.ReliableOrdered, 0,
                        DataCache.None, 0);
                }

                int groupId = GetGroupIdByName(groupName);
                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                {
                    OnPlayerLeftGroup?.Invoke(group, peer, Phase.Begin, reason);
                    if (group._peersById.Remove(peer.Id, out _))
                    {
                        if (!peer._groups.Remove(group.Id))
                        {
                            NetworkLogger.__Log__(
                                "LeaveGroup: Failed to remove group from peer!!!"
                            );

                            return;
                        }

                        _allowZeroGroupForInternalMessages = true;
                        SendResponseToClient();
                        OnPlayerLeftGroup?.Invoke(group, peer, Phase.Normal, reason);

                        // Dereferencing to allow for GC(Garbage Collector).
                        // All resources should be released at this point.
                        group.DestroyAllCaches(peer);
                        if (group.DestroyWhenEmpty)
                        {
                            DestroyGroup(group);
                        }
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            $"LeaveGroup: Failed to remove peer: {peer.Id} from group: {groupName} because it does not exist.",
                            NetworkLogger.LogType.Error
                        );

                        OnPlayerFailedLeaveGroup?.Invoke(
                            peer,
                            $"LeaveGroup: Failed to remove peer: {peer.Id} from group: {groupName} because it does not exist."
                        );
                    }
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"LeaveGroup: {groupName} not found. Please verify the group name and ensure the group is properly registered.",
                        NetworkLogger.LogType.Error
                    );

                    OnPlayerFailedLeaveGroup?.Invoke(
                        peer,
                        $"LeaveGroup: {groupName} not found. Please verify the group name and ensure the group is properly registered."
                    );
                }
            }

            internal static void DestroyGroup(NetworkGroup group)
            {
                if (group._peersById.Count == 0)
                {
                    if (!GroupsById.Remove(group.Id))
                    {
                        NetworkLogger.__Log__(
                            $"LeaveGroup: Destroy was called on group: {group.Name} but it does not exist.",
                            NetworkLogger.LogType.Error
                        );
                    }

                    // Dereferencing to allow for GC(Garbage Collector).
                    group.ClearPeers();
                    group.ClearData();
                    group.ClearCaches();
                }
            }

            /// <summary>
            /// Sends cached data to a specified network peer based on the provided data cache.
            /// </summary>
            /// <param name="fromPeer">The originating network peer from which the cache data will be sent.</param>
            /// <param name="toPeer">The network peer to whom the cache data will be sent from the originating peer.</param>
            /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
            /// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
            /// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
            internal static void Internal_SendPeerCache(NetworkPeer fromPeer, NetworkPeer toPeer, DataCache dataCache,
                int groupId = 0, bool sendMyOwnCacheToMe = false)
            {
                Internal_SendCache(fromPeer, toPeer, dataCache, groupId, sendMyOwnCacheToMe);
            }

            /// <summary>
            /// Sends cached data to a specified network peer based on the provided data cache.
            /// </summary>
            /// <param name="peer">The network peer to whom the cache data will be sent.</param>
            /// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
            /// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
            /// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
            internal static void Internal_SendCache(NetworkPeer peer, DataCache dataCache, int groupId = 0,
                bool sendMyOwnCacheToMe = false)
            {
                Internal_SendCache(peer, peer, dataCache, groupId, sendMyOwnCacheToMe);
            }

            internal static void Internal_SendCache(NetworkPeer fromPeer, NetworkPeer toPeer, DataCache dataCache,
                int groupId, bool sendMyOwnCacheToMe)
            {
                if (dataCache.Mode != CacheMode.None || dataCache.Id != 0)
                {
                    if (
                        (dataCache.Id != 0 && dataCache.Mode == CacheMode.None)
                        || (dataCache.Mode != CacheMode.None && dataCache.Id == 0)
                    )
                    {
                        throw new Exception(
                            "Data Cache: Required Id and Mode must be set together."
                        );
                    }
                    else
                    {
                        switch (dataCache.Mode)
                        {
                            case CacheMode.Global | CacheMode.New:
                            case CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy:
                            {
                                List<NetworkCache> caches = AppendCachesGlobal
                                    .Where(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id).ToList();

                                foreach (NetworkCache cache in caches)
                                {
                                    if (!sendMyOwnCacheToMe)
                                    {
                                        if (cache.Peer.Id == toPeer.Id)
                                        {
                                            continue;
                                        }
                                    }

                                    Connection.Server.Send(cache.Data, toPeer.EndPoint, cache.DeliveryMode,
                                        cache.SequenceChannel);
                                }

                                break;
                            }
                            case CacheMode.Group | CacheMode.New:
                            case CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy:
                            {
                                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                                {
                                    List<NetworkCache> caches = group.AppendCaches
                                        .Where(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id).ToList();

                                    foreach (NetworkCache cache in caches)
                                    {
                                        if (!sendMyOwnCacheToMe)
                                        {
                                            if (cache.Peer.Id == toPeer.Id)
                                            {
                                                continue;
                                            }
                                        }

                                        Connection.Server.Send(cache.Data, toPeer.EndPoint, cache.DeliveryMode,
                                            cache.SequenceChannel);
                                    }
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Send Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                break;
                            }
                            case CacheMode.Global | CacheMode.Overwrite:
                            case CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy:
                            {
                                if (
                                    OverwriteCachesGlobal.TryGetValue(dataCache.Id, out NetworkCache cache)
                                )
                                {
                                    if (!sendMyOwnCacheToMe)
                                    {
                                        if (cache.Peer.Id == toPeer.Id)
                                        {
                                            return;
                                        }
                                    }

                                    Connection.Server.Send(cache.Data, toPeer.EndPoint, cache.DeliveryMode,
                                        cache.SequenceChannel);
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Cache Error: Cache with Id: {dataCache.Id} and search mode: [{dataCache.Mode}] not found.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                break;
                            }
                            case CacheMode.Group | CacheMode.Overwrite:
                            case CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy:
                            {
                                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                                {
                                    if (
                                        group.OverwriteCaches.TryGetValue(
                                            dataCache.Id,
                                            out NetworkCache cache
                                        )
                                    )
                                    {
                                        if (!sendMyOwnCacheToMe)
                                        {
                                            if (cache.Peer.Id == toPeer.Id)
                                            {
                                                return;
                                            }
                                        }

                                        Connection.Server.Send(cache.Data, toPeer.EndPoint, cache.DeliveryMode,
                                            cache.SequenceChannel);
                                    }
                                    else
                                    {
                                        NetworkLogger.__Log__(
                                            $"Cache Error: Cache with Id: {dataCache.Id} and search mode: [{dataCache.Mode}] not found.",
                                            NetworkLogger.LogType.Error
                                        );
                                    }
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                break;
                            }
                            case CacheMode.Peer | CacheMode.New:
                            case CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy:
                            {
                                List<NetworkCache> caches = fromPeer.AppendCaches
                                    .Where(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id).ToList();

                                foreach (NetworkCache cache in caches)
                                {
                                    if (!sendMyOwnCacheToMe)
                                    {
                                        if (cache.Peer.Id == toPeer.Id)
                                        {
                                            continue;
                                        }
                                    }

                                    Connection.Server.Send(cache.Data, toPeer.EndPoint, cache.DeliveryMode,
                                        cache.SequenceChannel);
                                }

                                break;
                            }
                            case CacheMode.Peer | CacheMode.Overwrite:
                            case CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy:
                            {
                                if (fromPeer.OverwriteCaches.TryGetValue(dataCache.Id, out NetworkCache cache))
                                {
                                    if (!sendMyOwnCacheToMe)
                                    {
                                        if (cache.Peer.Id == toPeer.Id)
                                        {
                                            return;
                                        }
                                    }

                                    Connection.Server.Send(cache.Data, toPeer.EndPoint, cache.DeliveryMode,
                                        cache.SequenceChannel);
                                }

                                break;
                            }
                            default:
                                NetworkLogger.__Log__(
                                    "Cache Error: Unsupported cache mode set.",
                                    NetworkLogger.LogType.Error
                                );
                                break;
                        }
                    }
                }
                else
                {
                    throw new Exception(
                        "Data Cache: Required Id and Mode must be set together."
                    );
                }
            }

            /// <summary>
            /// Deletes a cache based on the provided data cache and group ID.
            /// </summary>
            /// <param name="dataCache">The data cache to be deleted.</param>
            /// <param name="groupId">The ID of the group to which the cache belongs (optional, default is 0).</param>
            public static void DeleteCache(DataCache dataCache, int groupId = 0)
            {
                if (dataCache.Mode != CacheMode.None || dataCache.Id != 0)
                {
                    if (
                        (dataCache.Id != 0 && dataCache.Mode == CacheMode.None)
                        || (dataCache.Mode != CacheMode.None && dataCache.Id == 0)
                    )
                    {
                        throw new Exception(
                            "Delete Cache Error: Required dataCache.Id and dataCache.Mode must be set together."
                        );
                    }
                    else
                    {
                        switch (dataCache.Mode)
                        {
                            case CacheMode.Global | CacheMode.New:
                            case CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy:
                                AppendCachesGlobal.RemoveAll(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id);
                                break;
                            case CacheMode.Group | CacheMode.New:
                            case CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy:
                            {
                                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                                {
                                    group.AppendCaches.RemoveAll(x =>
                                        x.Mode == dataCache.Mode && x.Id == dataCache.Id);
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                break;
                            }
                            case CacheMode.Global | CacheMode.Overwrite:
                            case CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy:
                                OverwriteCachesGlobal.Remove(dataCache.Id);
                                break;
                            case CacheMode.Group | CacheMode.Overwrite:
                            case CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy:
                            {
                                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                                {
                                    group.OverwriteCaches.Remove(dataCache.Id);
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                break;
                            }
                            default:
                                NetworkLogger.__Log__(
                                    "Delete Cache Error: Unsupported cache mode set.",
                                    NetworkLogger.LogType.Error
                                );
                                break;
                        }
                    }
                }
                else
                {
                    throw new Exception(
                        "Cache: Required dataCache.Id and dataCache.Mode must be set together."
                    );
                }
            }

            /// <summary>
            /// Deletes a cache based on the provided data cache and network peer.
            /// </summary>
            /// <param name="dataCache">The data cache to be deleted.</param>
            /// <param name="peer">The network peer associated with the cache.</param>
            /// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
            public static void DeleteCache(DataCache dataCache, NetworkPeer peer, int groupId = 0)
            {
                if (dataCache.Mode != CacheMode.None || dataCache.Id != 0)
                {
                    if (
                        (dataCache.Id != 0 && dataCache.Mode == CacheMode.None)
                        || (dataCache.Mode != CacheMode.None && dataCache.Id == 0)
                    )
                    {
                        throw new Exception(
                            "Delete Cache Error: Required dataCache.Id and dataCache.Mode must be set together."
                        );
                    }
                    else
                    {
                        switch (dataCache.Mode)
                        {
                            case CacheMode.Global | CacheMode.New:
                            case CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy:
                                AppendCachesGlobal.RemoveAll(x =>
                                    x.Mode == dataCache.Mode && x.Id == dataCache.Id && x.Peer.Id == peer.Id);
                                break;
                            case CacheMode.Group | CacheMode.New:
                            case CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy:
                            {
                                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                                {
                                    group.AppendCaches.RemoveAll(x =>
                                        x.Mode == dataCache.Mode && x.Id == dataCache.Id && x.Peer.Id == peer.Id);
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                break;
                            }
                            case CacheMode.Global | CacheMode.Overwrite:
                            case CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy:
                                OverwriteCachesGlobal.Remove(dataCache.Id);
                                break;
                            case CacheMode.Group | CacheMode.Overwrite:
                            case CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy:
                            {
                                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                                {
                                    group.OverwriteCaches.Remove(dataCache.Id);
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                        NetworkLogger.LogType.Error
                                    );
                                }

                                break;
                            }
                            case CacheMode.Peer | CacheMode.New:
                            case CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy:
                                peer.AppendCaches.RemoveAll(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id);
                                break;
                            case CacheMode.Peer | CacheMode.Overwrite:
                            case CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy:
                                peer.OverwriteCaches.Remove(dataCache.Id);
                                break;
                            default:
                                NetworkLogger.__Log__(
                                    "Delete Cache Error: Unsupported cache mode set.",
                                    NetworkLogger.LogType.Error
                                );
                                break;
                        }
                    }
                }
                else
                {
                    throw new Exception(
                        "Cache: Required dataCache.Id and dataCache.Mode must be set together."
                    );
                }
            }

            /// <summary>
            /// Destroys all caches associated with the specified network peer.
            /// </summary>
            /// <param name="peer">The network peer for which to destroy all caches.</param>
            public static void DestroyAllCaches(NetworkPeer peer)
            {
                AppendCachesGlobal.RemoveAll(x => x.Peer.Id == peer.Id && x.AutoDestroyCache);
                var caches = OverwriteCachesGlobal.Values.Where(x => x.Peer.Id == peer.Id && x.AutoDestroyCache)
                    .ToList();

                foreach (var cache in caches)
                {
                    if (!OverwriteCachesGlobal.Remove(cache.Id))
                    {
                        NetworkLogger.__Log__(
                            $"Destroy All Cache Error: Failed to remove cache {cache.Id} from peer {peer.Id}.",
                            NetworkLogger.LogType.Error
                        );
                    }
                }
            }

            /// <summary>
            /// Clears all global caches.
            /// </summary>
            public static void ClearCaches()
            {
                AppendCachesGlobal.Clear();
                OverwriteCachesGlobal.Clear();
            }

            internal static void AddRpcMessage(int identityId, IRpcMessage behaviour)
            {
                if (!GlobalRpcHandlers.TryAdd(identityId, behaviour))
                {
                    GlobalRpcHandlers[identityId] = behaviour;
                }
            }
        }
    }
}