using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Omni.Core.Cryptography;
using Omni.Core.Interfaces;
using Omni.Shared;

namespace Omni.Core
{
    public partial class NetworkManager
    {
        private static void Internal_SendMessage(
            byte msgId,
            int peerId,
            DataBuffer buffer,
            Target target,
            DeliveryMode deliveryMode,
            bool isServer,
            int groupId,
            int cacheId,
            CacheMode cacheMode,
            byte seqChannel
        )
        {
            buffer ??= DataBuffer.Empty;
            if (!isServer)
            {
                SendToServer(msgId, buffer, deliveryMode, seqChannel);
            }
            else
            {
                NetworkPeer targetPeer = Server.GetPeerById(peerId);
                if (targetPeer != null)
                {
                    SendToClient(
                        msgId,
                        buffer,
                        targetPeer.EndPoint,
                        target,
                        deliveryMode,
                        groupId,
                        cacheId,
                        cacheMode,
                        seqChannel
                    );
                }
            }
        }

        public static class Client
        {
            /// <summary>
            /// Gets the server RSA public key .
            /// This property stores the public key used in RSA cryptographic operations.
            /// The public key is utilized to encrypt data, ensuring that only the holder of the corresponding private key can decrypt it.
            /// </summary>
            internal static string RsaServerPublicKey { get; set; }

            internal static Dictionary<int, NetworkIdentity> Identities { get; } = new();
            internal static Dictionary<int, INetworkMessage> GlobalEventBehaviours { get; } = new(); // int: identifier(identity id)
            internal static Dictionary<(int, byte), INetworkMessage> LocalEventBehaviours { get; } =
                new();

            public static event Action<byte, DataBuffer, int> OnMessage
            {
                add => OnClientCustomMessage += value;
                remove => OnClientCustomMessage -= value;
            }

            public static Dictionary<int, NetworkIdentity> GetIdentities()
            {
                return Identities;
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

            public static void SendMessage(
                byte msgId,
                DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            ) =>
                Internal_SendMessage(
                    msgId,
                    0,
                    buffer,
                    Target.Self,
                    deliveryMode,
                    false,
                    0,
                    0,
                    CacheMode.None,
                    sequenceChannel
                );

            public static void GlobalInvoke(
                byte msgId,
                DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            ) => SendMessage(msgId, buffer, deliveryMode, sequenceChannel);

            public static void Invoke(
                byte msgId,
                int identityId,
                DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                buffer ??= DataBuffer.Empty;
                using DataBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(msgId);
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.GlobalInvoke, message, deliveryMode, sequenceChannel);
            }

            public static void Invoke(
                byte msgId,
                int identityId,
                byte instanceId,
                DataBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                buffer ??= DataBuffer.Empty;
                using DataBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(instanceId);
                message.FastWrite(msgId);
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.LocalInvoke, message, deliveryMode, sequenceChannel);
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
                message.FastWrite(groupName);
                message.Write(buffer.BufferAsSpan);
                SendMessage(MessageType.JoinGroup, message, DeliveryMode.ReliableOrdered, 0);
            }

            internal static void LeaveGroup(
                string groupName,
                string reason = "Leave called by user."
            )
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
                message.FastWrite(groupName);
                message.FastWrite(reason);
                SendMessage(MessageType.LeaveGroup, message, DeliveryMode.ReliableOrdered, 0);
            }

            internal static void AddEventBehaviour(int identityId, INetworkMessage behaviour)
            {
                if (!GlobalEventBehaviours.TryAdd(identityId, behaviour))
                {
                    GlobalEventBehaviours[identityId] = behaviour;
                }
            }
        }

        public static class Server
        {
            public static NetworkPeer ServerPeer { get; } =
                new(new IPEndPoint(IPAddress.None, 0), 0);

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

            internal static List<NetworkCache> CACHES_APPEND_GLOBAL { get; } = new();
            internal static Dictionary<int, NetworkCache> CACHES_OVERWRITE_GLOBAL { get; } = new();

            internal static Dictionary<int, NetworkIdentity> Identities { get; } = new();
            internal static Dictionary<int, INetworkMessage> GlobalEventBehaviours { get; } = new();
            internal static Dictionary<(int, byte), INetworkMessage> LocalEventBehaviours { get; } =
                new();

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

            public static Dictionary<int, NetworkIdentity> GetIdentities()
            {
                return Identities;
            }

            internal static Dictionary<int, NetworkGroup> GetGroups()
            {
                return Groups;
            }

            public static Dictionary<int, NetworkPeer> GetPeers()
            {
                return PeersById;
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

            public static NetworkPeer GetPeerById(int peerId, int groupId = 0)
            {
                if (groupId == 0)
                {
                    if (PeersById.TryGetValue(peerId, out var peer))
                    {
                        return peer;
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            $"Peer Retrieval Error: Peer with ID '{peerId}' not found. Please verify the peer ID and ensure the peer is properly registered(connected!).",
                            NetworkLogger.LogType.Error
                        );

                        return null;
                    }
                }
                else
                {
                    if (Groups.TryGetValue(groupId, out NetworkGroup group))
                    {
                        if (group._peersById.TryGetValue(peerId, out var peer))
                        {
                            return peer;
                        }
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            $"Get Error: Group with ID {groupId} not found. ensure that the group exists and that the provided groupId is correct.",
                            NetworkLogger.LogType.Error
                        );

                        return null;
                    }
                }

                NetworkLogger.__Log__(
                    $"Peer Retrieval Error: Peer with ID '{peerId}' not found. Please verify the peer ID and ensure the peer is properly registered(connected!).",
                    NetworkLogger.LogType.Error
                );

                return null;
            }

            public static void SendMessage(
                byte msgId,
                int peerId,
                DataBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                int cacheId = 0,
                CacheMode cacheMode = CacheMode.None,
                byte sequenceChannel = 0
            ) =>
                Internal_SendMessage(
                    msgId,
                    peerId,
                    buffer,
                    target,
                    deliveryMode,
                    true,
                    groupId,
                    cacheId,
                    cacheMode,
                    sequenceChannel
                );

            public static void GlobalInvoke(
                byte msgId,
                int peerId,
                DataBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                int cacheId = 0,
                CacheMode cacheMode = CacheMode.None,
                byte sequenceChannel = 0
            ) =>
                SendMessage(
                    msgId,
                    peerId,
                    buffer,
                    target,
                    deliveryMode,
                    groupId,
                    cacheId,
                    cacheMode,
                    sequenceChannel
                );

            public static void Invoke(
                byte msgId,
                int peerId,
                int identityId,
                DataBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                int cacheId = 0,
                CacheMode cacheMode = CacheMode.None,
                byte sequenceChannel = 0
            )
            {
                buffer ??= DataBuffer.Empty;
                using DataBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(msgId);
                message.Write(buffer.BufferAsSpan);
                SendMessage(
                    MessageType.GlobalInvoke,
                    peerId,
                    message,
                    target,
                    deliveryMode,
                    groupId,
                    cacheId,
                    cacheMode,
                    sequenceChannel
                );

                // byte count per empty message: 4 + 1 = 5 + header;
                // TODO: reduce bandwidth usage
            }

            public static void Invoke(
                byte msgId,
                int peerId,
                int identityId,
                byte instanceId,
                DataBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                int cacheId = 0,
                CacheMode cacheMode = CacheMode.None,
                byte sequenceChannel = 0
            )
            {
                buffer ??= DataBuffer.Empty;
                using DataBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(instanceId);
                message.FastWrite(msgId);
                message.Write(buffer.BufferAsSpan);
                SendMessage(
                    MessageType.LocalInvoke,
                    peerId,
                    message,
                    target,
                    deliveryMode,
                    groupId,
                    cacheId,
                    cacheMode,
                    sequenceChannel
                );

                // byte count per empty message: 4 + 1 + 1 = 6 + header;
                // TODO: reduce bandwidth usage
            }

            internal static int GetGroupIdByName(string groupName)
            {
                return groupName.GetHashCode();
            }

            internal static NetworkGroup GetGroupById(int groupId)
            {
                if (Groups.TryGetValue(groupId, out NetworkGroup group))
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
                return Groups.TryGetValue(groupId, out group);
            }

            internal static void JoinGroup(
                string groupName,
                DataBuffer buffer,
                NetworkPeer peer,
                bool writeBufferToClient
            )
            {
                void SendResponseToClient()
                {
                    using DataBuffer message = Pool.Rent();
                    message.FastWrite(groupName);

                    if (writeBufferToClient)
                    {
                        message.Write(buffer.BufferAsSpan);
                    }

                    SendMessage(
                        MessageType.JoinGroup,
                        peer.Id,
                        message,
                        Target.Self,
                        DeliveryMode.ReliableOrdered,
                        0,
                        0
                    );
                }

                int uniqueId = GetGroupIdByName(groupName);
                if (Groups.TryGetValue(uniqueId, out NetworkGroup group))
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
                    group = new NetworkGroup(uniqueId, groupName);
                    group._peersById.Add(peer.Id, peer);
                    if (!Groups.TryAdd(uniqueId, group))
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

                    _allowZeroGroupForInternalMessages = true;
                    SendResponseToClient();
                    OnPlayerJoinedGroup?.Invoke(buffer, group, peer);
                }
            }

            internal static NetworkGroup AddGroup(string groupName)
            {
                int groupId = GetGroupIdByName(groupName);
                NetworkGroup group = new(groupId, groupName);
                if (!Groups.TryAdd(groupId, group))
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
                group = new(groupId, groupName);
                return Groups.TryAdd(groupId, group);
            }

            internal static void LeaveGroup(string groupName, string reason, NetworkPeer peer)
            {
                void SendResponseToClient()
                {
                    using DataBuffer message = Pool.Rent();
                    message.FastWrite(groupName);
                    message.FastWrite(reason);

                    SendMessage(
                        MessageType.LeaveGroup,
                        peer.Id,
                        message,
                        Target.Self,
                        DeliveryMode.ReliableOrdered,
                        0,
                        0
                    );
                }

                int groupId = GetGroupIdByName(groupName);
                if (Groups.TryGetValue(groupId, out NetworkGroup group))
                {
                    OnPlayerLeftGroup?.Invoke(group, peer, Status.Begin, reason);
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
                        OnPlayerLeftGroup?.Invoke(group, peer, Status.Normal, reason);

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
                    if (!Groups.Remove(group.Id))
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
            /// Sends cached data to a specified network peer based on the provided cache mode and cache ID.
            /// </summary>
            /// <param name="peer">The network peer to whom the cache data will be sent.</param>
            /// <param name="cacheId">The identifier of the cache to be sent.</param>
            /// <param name="cacheMode">The mode of the cache, indicating whether it is global, group, new, or overwrite.</param>
            /// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
            /// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
            /// <exception cref="Exception">Thrown when required cacheId and cacheMode are not set together or an unsupported cache mode is set.</exception>
            public static void SendCache(
                NetworkPeer peer,
                int cacheId,
                CacheMode cacheMode,
                int groupId = 0,
                bool sendMyOwnCacheToMe = false
            )
            {
                if (cacheMode != CacheMode.None || cacheId != 0)
                {
                    if (
                        (cacheId != 0 && cacheMode == CacheMode.None)
                        || (cacheMode != CacheMode.None && cacheId == 0)
                    )
                    {
                        throw new Exception(
                            "Cache: Required cacheId and cacheMode must be set together."
                        );
                    }
                    else
                    {
                        if (
                            cacheMode == (CacheMode.Global | CacheMode.New)
                            || cacheMode
                                == (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
                        )
                        {
                            List<NetworkCache> caches = CACHES_APPEND_GLOBAL
                                .Where(x => x.Mode == cacheMode && x.Id == cacheId)
                                .ToList();

                            foreach (NetworkCache cache in caches)
                            {
                                if (!sendMyOwnCacheToMe)
                                {
                                    if (cache.Peer.Id == peer.Id)
                                    {
                                        continue;
                                    }
                                }

                                Connection.Server.Send(
                                    cache.Data,
                                    peer.EndPoint,
                                    cache.DeliveryMode,
                                    cache.SequenceChannel
                                );
                            }
                        }
                        else if (
                            cacheMode == (CacheMode.Group | CacheMode.New)
                            || cacheMode
                                == (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
                        )
                        {
                            if (Groups.TryGetValue(groupId, out NetworkGroup group))
                            {
                                List<NetworkCache> caches = group
                                    .CACHES_APPEND.Where(x =>
                                        x.Mode == cacheMode && x.Id == cacheId
                                    )
                                    .ToList();

                                foreach (NetworkCache cache in caches)
                                {
                                    if (!sendMyOwnCacheToMe)
                                    {
                                        if (cache.Peer.Id == peer.Id)
                                        {
                                            continue;
                                        }
                                    }

                                    Connection.Server.Send(
                                        cache.Data,
                                        peer.EndPoint,
                                        cache.DeliveryMode,
                                        cache.SequenceChannel
                                    );
                                }
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Send Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else if (
                            cacheMode == (CacheMode.Global | CacheMode.Overwrite)
                            || cacheMode
                                == (CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy)
                        )
                        {
                            if (
                                CACHES_OVERWRITE_GLOBAL.TryGetValue(cacheId, out NetworkCache cache)
                            )
                            {
                                if (!sendMyOwnCacheToMe)
                                {
                                    if (cache.Peer.Id == peer.Id)
                                    {
                                        return;
                                    }
                                }

                                Connection.Server.Send(
                                    cache.Data,
                                    peer.EndPoint,
                                    cache.DeliveryMode,
                                    cache.SequenceChannel
                                );
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Cache Error: Cache with Id: {cacheId} and search mode: [{cacheMode}] not found.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else if (
                            cacheMode == (CacheMode.Group | CacheMode.Overwrite)
                            || cacheMode
                                == (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
                        )
                        {
                            if (Groups.TryGetValue(groupId, out NetworkGroup group))
                            {
                                if (
                                    group.CACHES_OVERWRITE.TryGetValue(
                                        cacheId,
                                        out NetworkCache cache
                                    )
                                )
                                {
                                    if (!sendMyOwnCacheToMe)
                                    {
                                        if (cache.Peer.Id == peer.Id)
                                        {
                                            return;
                                        }
                                    }

                                    Connection.Server.Send(
                                        cache.Data,
                                        peer.EndPoint,
                                        cache.DeliveryMode,
                                        cache.SequenceChannel
                                    );
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"Cache Error: Cache with Id: {cacheId} and search mode: [{cacheMode}] not found.",
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
                        }
                        else
                        {
                            NetworkLogger.__Log__(
                                "Cache Error: Unsupported cache mode set.",
                                NetworkLogger.LogType.Error
                            );
                        }
                    }
                }
                else
                {
                    throw new Exception(
                        "Cache: Required cacheId and cacheMode must be set together."
                    );
                }
            }

            public static void DeleteCache(CacheMode cacheMode, int cacheId, int groupId = 0)
            {
                if (cacheMode != CacheMode.None || cacheId != 0)
                {
                    if (
                        (cacheId != 0 && cacheMode == CacheMode.None)
                        || (cacheMode != CacheMode.None && cacheId == 0)
                    )
                    {
                        throw new Exception(
                            "Delete Cache Error: Required cacheId and cacheMode must be set together."
                        );
                    }
                    else
                    {
                        if (
                            cacheMode == (CacheMode.Global | CacheMode.New)
                            || cacheMode
                                == (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
                        )
                        {
                            CACHES_APPEND_GLOBAL.RemoveAll(x =>
                                x.Mode == cacheMode && x.Id == cacheId
                            );
                        }
                        else if (
                            cacheMode == (CacheMode.Group | CacheMode.New)
                            || cacheMode
                                == (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
                        )
                        {
                            if (Groups.TryGetValue(groupId, out NetworkGroup group))
                            {
                                group.CACHES_APPEND.RemoveAll(x =>
                                    x.Mode == cacheMode && x.Id == cacheId
                                );
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else if (
                            cacheMode == (CacheMode.Global | CacheMode.Overwrite)
                            || cacheMode
                                == (CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy)
                        )
                        {
                            CACHES_OVERWRITE_GLOBAL.Remove(cacheId);
                        }
                        else if (
                            cacheMode == (CacheMode.Group | CacheMode.Overwrite)
                            || cacheMode
                                == (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
                        )
                        {
                            if (Groups.TryGetValue(groupId, out NetworkGroup group))
                            {
                                group.CACHES_OVERWRITE.Remove(cacheId);
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else
                        {
                            NetworkLogger.__Log__(
                                "Delete Cache Error: Unsupported cache mode set.",
                                NetworkLogger.LogType.Error
                            );
                        }
                    }
                }
                else
                {
                    throw new Exception(
                        "Cache: Required cacheId and cacheMode must be set together."
                    );
                }
            }

            public static void DeleteCache(
                CacheMode cacheMode,
                int cacheId,
                NetworkPeer peer,
                int groupId = 0
            )
            {
                if (cacheMode != CacheMode.None || cacheId != 0)
                {
                    if (
                        (cacheId != 0 && cacheMode == CacheMode.None)
                        || (cacheMode != CacheMode.None && cacheId == 0)
                    )
                    {
                        throw new Exception(
                            "Delete Cache Error: Required cacheId and cacheMode must be set together."
                        );
                    }
                    else
                    {
                        if (
                            cacheMode == (CacheMode.Global | CacheMode.New)
                            || cacheMode
                                == (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
                        )
                        {
                            CACHES_APPEND_GLOBAL.RemoveAll(x =>
                                x.Mode == cacheMode && x.Id == cacheId && x.Peer.Id == peer.Id
                            );
                        }
                        else if (
                            cacheMode == (CacheMode.Group | CacheMode.New)
                            || cacheMode
                                == (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
                        )
                        {
                            if (Groups.TryGetValue(groupId, out NetworkGroup group))
                            {
                                group.CACHES_APPEND.RemoveAll(x =>
                                    x.Mode == cacheMode && x.Id == cacheId && x.Peer.Id == peer.Id
                                );
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else if (
                            cacheMode == (CacheMode.Global | CacheMode.Overwrite)
                            || cacheMode
                                == (CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy)
                        )
                        {
                            CACHES_OVERWRITE_GLOBAL.Remove(cacheId);
                        }
                        else if (
                            cacheMode == (CacheMode.Group | CacheMode.Overwrite)
                            || cacheMode
                                == (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
                        )
                        {
                            if (Groups.TryGetValue(groupId, out NetworkGroup group))
                            {
                                group.CACHES_OVERWRITE.Remove(cacheId);
                            }
                            else
                            {
                                NetworkLogger.__Log__(
                                    $"Delete Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                        else
                        {
                            NetworkLogger.__Log__(
                                "Delete Cache Error: Unsupported cache mode set.",
                                NetworkLogger.LogType.Error
                            );
                        }
                    }
                }
                else
                {
                    throw new Exception(
                        "Cache: Required cacheId and cacheMode must be set together."
                    );
                }
            }

            public static void DestroyAllCaches(NetworkPeer peer)
            {
                CACHES_APPEND_GLOBAL.RemoveAll(x => x.Peer.Id == peer.Id && x.AutoDestroyCache);
                var caches = CACHES_OVERWRITE_GLOBAL
                    .Values.Where(x => x.Peer.Id == peer.Id && x.AutoDestroyCache)
                    .ToList();

                foreach (var cache in caches)
                {
                    if (!CACHES_OVERWRITE_GLOBAL.Remove(cache.Id))
                    {
                        NetworkLogger.__Log__(
                            $"Destroy All Cache Error: Failed to remove cache {cache.Id} from peer {peer.Id}.",
                            NetworkLogger.LogType.Error
                        );
                    }
                }
            }

            public static void ClearCaches()
            {
                CACHES_APPEND_GLOBAL.Clear();
                CACHES_OVERWRITE_GLOBAL.Clear();
            }

            internal static void AddEventBehaviour(int identityId, INetworkMessage behaviour)
            {
                if (!GlobalEventBehaviours.TryAdd(identityId, behaviour))
                {
                    GlobalEventBehaviours[identityId] = behaviour;
                }
            }
        }
    }
}
