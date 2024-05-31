using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using Omni.Core.Interfaces;
using Omni.Core.Modules.Matchmaking;
using Omni.Shared;

namespace Omni.Core
{
    public partial class NetworkManager
    {
        private static void Internal_SendMessage(
            byte msgId,
            int peerId,
            NetworkBuffer buffer,
            Target target,
            DeliveryMode deliveryMode,
            bool isServer,
            int groupId,
            byte seqChannel
        )
        {
            buffer ??= NetworkBuffer.Empty;
            if (!isServer)
            {
                SendToServer(msgId, buffer, deliveryMode, seqChannel);
            }
            else
            {
                if (peerId > 0)
                {
                    NetworkPeer peer = Server.GetPeerById(peerId);
                    if (peer != null)
                    {
                        IPEndPoint fromPeer = peer.EndPoint;
                        SendToClient(
                            msgId,
                            buffer,
                            fromPeer,
                            target,
                            deliveryMode,
                            groupId,
                            seqChannel
                        );
                    }
                }
                else
                {
                    throw new Exception("Server-Send: Invalid peer id! Must be greater than 0.");
                }
            }
        }

        public static class Client
        {
            // int: identifier(identity id)
            internal static Dictionary<int, INetworkMessage> EventBehaviours { get; } = new();
            internal static Dictionary<(int, byte), INetworkMessage> PeerEventBehaviours { get; } =
                new();

            public static event Action<byte, NetworkBuffer, int> OnMessage
            {
                add => OnClientCustomMessage += value;
                remove => OnClientCustomMessage -= value;
            }

            public static void SendMessage(
                byte msgId,
                NetworkBuffer buffer = null,
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
                    sequenceChannel
                );

            public static void GlobalInvoke(
                byte msgId,
                NetworkBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            ) => SendMessage(msgId, buffer, deliveryMode, sequenceChannel);

            public static void Invoke(
                byte msgId,
                int identityId,
                NetworkBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                buffer ??= NetworkBuffer.Empty;
                using NetworkBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(msgId);
                message.Write(buffer.WrittenSpan);
                SendMessage(MessageType.Invoke, message, deliveryMode, sequenceChannel);
            }

            public static void Invoke(
                byte msgId,
                int identityId,
                byte instanceId,
                NetworkBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                buffer ??= NetworkBuffer.Empty;
                using NetworkBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(instanceId);
                message.FastWrite(msgId);
                message.Write(buffer.WrittenSpan);
                SendMessage(MessageType.InvokeByPeer, message, deliveryMode, sequenceChannel);
            }

            internal static void JoinGroup(string groupName, NetworkBuffer buffer)
            {
                buffer ??= NetworkBuffer.Empty;
                if (string.IsNullOrEmpty(groupName))
                {
                    throw new Exception("Group name cannot be null or empty.");
                }

                if (groupName.Length > 100)
                {
                    throw new Exception("Group name cannot be longer than 100 characters.");
                }

                using NetworkBuffer message = Pool.Rent();
                message.FastWrite(groupName);
                message.Write(buffer.WrittenSpan);
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

                if (groupName.Length > 100)
                {
                    throw new Exception("Group name cannot be longer than 100 characters.");
                }

                using NetworkBuffer message = Pool.Rent();
                message.FastWrite(groupName);
                message.FastWrite(reason);
                SendMessage(MessageType.LeaveGroup, message, DeliveryMode.ReliableOrdered, 0);
            }

            internal static void AddEventBehaviour(int identityId, INetworkMessage behaviour)
            {
                // Generate a unique identityId for the INetworkMessage behaviour
                // and add it to the EventBehaviours dictionary.
                // If the identityId already exists in the dictionary, generate a new one
                // and repeat the process until a unique identityId is found.

                while (EventBehaviours.ContainsKey(identityId))
                {
                    // Generate a new identityId
                    identityId = NetworkHelper.GenerateUniqueId();
                }

                // Add the behaviour to the EventBehaviours dictionary with the generated identityId
                EventBehaviours.Add(identityId, behaviour);
            }
        }

        public static class Server
        {
            internal static Dictionary<int, INetworkMessage> EventBehaviours { get; } = new();
            internal static Dictionary<(int, byte), INetworkMessage> PeerEventBehaviours { get; } =
                new();

            public static event Action<byte, NetworkBuffer, NetworkPeer, int> OnMessage
            {
                add => OnServerCustomMessage += value;
                remove => OnServerCustomMessage -= value;
            }

            internal static Dictionary<int, NetworkGroup> GetGroups()
            {
                return Groups;
            }

            public static Dictionary<int, NetworkPeer> GetPeers()
            {
                return PeersById;
            }

            public static NetworkPeer GetPeerById(int peerId, int groupId = 0)
            {
                if (groupId == 0)
                {
                    if (PeersById.TryGetValue(peerId, out var peer))
                    {
                        return peer;
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
                            $"Get: Group with ID {groupId} not found.",
                            NetworkLogger.LogType.Error
                        );

                        return null;
                    }
                }

                NetworkLogger.__Log__(
                    $"Get: Peer with ID {peerId}:{groupId} not found.",
                    NetworkLogger.LogType.Error
                );

                return null;
            }

            public static void SendMessage(
                byte msgId,
                int peerId,
                NetworkBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
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
                    sequenceChannel
                );

            public static void GlobalInvoke(
                byte msgId,
                int peerId,
                NetworkBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                byte sequenceChannel = 0
            ) => SendMessage(msgId, peerId, buffer, target, deliveryMode, groupId, sequenceChannel);

            public static void Invoke(
                byte msgId,
                int peerId,
                int identityId,
                NetworkBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                byte sequenceChannel = 0
            )
            {
                buffer ??= NetworkBuffer.Empty;
                using NetworkBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(msgId);
                message.Write(buffer.WrittenSpan);
                SendMessage(
                    MessageType.Invoke,
                    peerId,
                    message,
                    target,
                    deliveryMode,
                    groupId,
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
                NetworkBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                byte sequenceChannel = 0
            )
            {
                buffer ??= NetworkBuffer.Empty;
                using NetworkBuffer message = Pool.Rent();
                message.FastWrite(identityId);
                message.FastWrite(instanceId);
                message.FastWrite(msgId);
                message.Write(buffer.WrittenSpan);
                SendMessage(
                    MessageType.InvokeByPeer,
                    peerId,
                    message,
                    target,
                    deliveryMode,
                    groupId,
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
                    $"Get: Group with ID {groupId} not found.",
                    NetworkLogger.LogType.Error
                );

                return null;
            }

            internal static void JoinGroup(
                string groupName,
                NetworkBuffer buffer,
                NetworkPeer peer,
                bool writeBufferToClient
            )
            {
                void SendResponseToClient()
                {
                    using NetworkBuffer message = Pool.Rent();
                    message.FastWrite(groupName);

                    if (writeBufferToClient)
                    {
                        message.Write(buffer.WrittenSpan);
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

                        return;
                    }

                    EnterGroup(buffer, peer, group);
                }

                void EnterGroup(NetworkBuffer buffer, NetworkPeer peer, NetworkGroup group)
                {
                    if (!peer.Groups.TryAdd(group.Id, group))
                    {
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
                if (Groups.TryAdd(groupId, group))
                {
                    return group;
                }

                throw new Exception($"Failed to add group: {groupName} because it already exists.");
            }

            internal static void LeaveGroup(string groupName, string reason, NetworkPeer peer)
            {
                void SendResponseToClient()
                {
                    using NetworkBuffer message = Pool.Rent();
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
                    if (group._peersById.Remove(peer.Id, out _))
                    {
                        if (!peer.Groups.Remove(group.Id))
                        {
                            NetworkLogger.__Log__(
                                "LeaveGroup: Failed to remove group from peer!!!"
                            );

                            return;
                        }

                        _allowZeroGroupForInternalMessages = true;
                        SendResponseToClient();
                        OnPlayerLeftGroup?.Invoke(group, peer, reason);

                        if (group.DestroyWhenEmpty)
                        {
                            if (!Groups.Remove(groupId))
                            {
                                NetworkLogger.__Log__(
                                    $"LeaveGroup: Destroy was called on group: {groupName} but it does not exist.",
                                    NetworkLogger.LogType.Error
                                );
                            }
                        }
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            $"LeaveGroup: Failed to remove peer: {peer.Id} from group: {groupName} because it does not exist.",
                            NetworkLogger.LogType.Error
                        );
                    }
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"LeaveGroup: {groupName} not found.",
                        NetworkLogger.LogType.Error
                    );
                }
            }

            internal static void AddEventBehaviour(int identityId, INetworkMessage behaviour)
            {
                // Generate a unique identityId for the INetworkMessage behaviour
                // and add it to the EventBehaviours dictionary.
                // If the identityId already exists in the dictionary, generate a new one
                // and repeat the process until a unique identityId is found.

                while (EventBehaviours.ContainsKey(identityId))
                {
                    // Generate a new identityId
                    identityId = NetworkHelper.GenerateUniqueId();
                    NetworkLogger.__Log__(
                        $"AddEventBehaviour: Generating new unique identityId: {identityId}.",
                        NetworkLogger.LogType.Warning
                    );
                }

                // Add the behaviour to the EventBehaviours dictionary with the generated identityId
                EventBehaviours.Add(identityId, behaviour);
            }
        }
    }
}
