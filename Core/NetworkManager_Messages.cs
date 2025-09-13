using Omni.Core.Cryptography;
using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace Omni.Core
{
    // High-level methods for sending network messages.
    public partial class NetworkManager
    {
        private const string k_PublicKeyFile = "PinnedPublicKey.txt";
        public static partial class ClientSide
        {
            internal const DeliveryMode Default_DeliveryMode = DeliveryMode.ReliableOrdered;
            internal const byte Default_SequenceChannel = 0;

            internal static DeliveryMode DeliveryMode { get; private set; } = Default_DeliveryMode;
            internal static byte SequenceChannel { get; private set; } = Default_SequenceChannel;

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
            internal static string PEMServerPublicKey { get; set; }

            internal static Dictionary<int, NetworkGroup> Groups { get; } = new();

            // int: identifier(identity id)
            internal static Dictionary<int, IRpcMessage> StaticRpcHandlers { get; } = new();
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

            internal static string GetRsaPublicKeyFromResources()
            {
                TextAsset publicKeyAsset = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(k_PublicKeyFile));

                if (publicKeyAsset == null)
                {
                    throw new CryptographicException($"The public key resource '{k_PublicKeyFile}' was not found in Resources. Please ensure the file exists at 'Assets/Resources/{k_PublicKeyFile}'.");
                }

                return publicKeyAsset.text;
            }

            public static NetworkIdentity GetIdentity(int identityId)
            {
                if (Identities.TryGetValue(identityId, out NetworkIdentity identity))
                {
                    return identity;
                }
                else
                {
                    return null;
                }
            }

            public static bool TryGetIdentity(int identityId, out NetworkIdentity identity)
            {
                return Identities.TryGetValue(identityId, out identity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendMessage(byte msgId, DataBuffer message)
            {
                SendToServer(msgId, message);
            }

            public static void Rpc(byte msgId, int identityId, DataBuffer data)
            {
                using DataBuffer message = Pool.Rent(enableTracking: false);
                message.Internal_Write(identityId);
                message.Write(msgId);
                message.Internal_CopyFrom(data);
                SendMessage(NetworkPacketType.k_StaticRpc, message);
            }

            public static void Rpc(byte rpcId, int identityId, byte instanceId, DataBuffer data)
            {
                using DataBuffer message = Pool.Rent(enableTracking: false);
                message.Internal_Write(identityId);
                message.Write(instanceId);
                message.Write(rpcId);
                message.Internal_CopyFrom(data);
                SendMessage(NetworkPacketType.k_LocalRpc, message);
            }

            internal static void JoinGroup(string groupName, DataBuffer buffer)
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    throw new Exception("Group name cannot be null or empty.");
                }

                if (groupName.Length > 256)
                {
                    throw new Exception("Group name cannot be longer than 256 characters.");
                }

                using DataBuffer message = Pool.Rent(enableTracking: false);
                message.WriteString(groupName);
                message.Internal_CopyFrom(buffer);
                SendMessage(NetworkPacketType.k_JoinGroup, new(message));
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

                using DataBuffer message = Pool.Rent(enableTracking: false);
                message.WriteString(groupName);
                message.WriteString(reason);
                SendMessage(NetworkPacketType.k_LeaveGroup, new(message));
            }

            internal static void AddRpcMessage(int identityId, IRpcMessage behaviour)
            {
                if (!StaticRpcHandlers.TryAdd(identityId, behaviour))
                {
                    StaticRpcHandlers[identityId] = behaviour;
                }
            }

            internal static NetworkPeer GetOrCreatePeer(int peerId)
            {
                if (Peers.TryGetValue(peerId, out NetworkPeer peer))
                {
                    return peer;
                }

                peer = new NetworkPeer(new IPEndPoint(IPAddress.None, 0), peerId, false)
                {
                    IsConnected = true,
                    IsAuthenticated = true
                };

                Peers.Add(peerId, peer);
                return peer;
            }

            public static void SetDeliveryMode(DeliveryMode deliveryMode)
            {
                DeliveryMode = deliveryMode;
            }

            public static void SetSequenceChannel(byte sequenceChannel)
            {
                SequenceChannel = sequenceChannel;
            }

            internal static void RestoreDefaultNetworkConfiguration()
            {
                DeliveryMode = Default_DeliveryMode;
                SequenceChannel = Default_SequenceChannel;
            }
        }

        public static partial class ServerSide
        {
            internal const DeliveryMode Default_DeliveryMode = DeliveryMode.ReliableOrdered;
            internal const Target Default_Target = Target.Auto;
            internal const byte Default_SequenceChannel = 0;

            internal static DeliveryMode DeliveryMode { get; private set; } = Default_DeliveryMode;
            internal static Target Target { get; private set; } = Default_Target;
            internal static NetworkGroup Group { get; private set; } = NetworkGroup.None;
            internal static byte SequenceChannel { get; private set; } = Default_SequenceChannel;

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
            internal static string PEMPublicKey { get; private set; }

            /// <summary>
            /// Gets the RSA private key.
            /// This property stores the private key used in RSA cryptographic operations.
            /// The private key is crucial for decrypting data that has been encrypted with the corresponding public key.
            /// It is also used to sign data, ensuring the authenticity and integrity of the message.
            /// </summary>
            internal static string PEMPrivateKey { get; private set; }

            internal static Dictionary<int, NetworkGroup> Groups => GroupsById;
            internal static Dictionary<int, IRpcMessage> StaticRpcHandlers { get; } = new();
            internal static Dictionary<(int, byte), IRpcMessage> LocalRpcHandlers { get; } = new();

            public static Dictionary<int, NetworkPeer> Peers => PeersById;
            public static Dictionary<int, NetworkIdentity> Identities { get; } = new();

            public static event Action<byte, DataBuffer, NetworkPeer, int> OnMessage
            {
                add => OnServerCustomMessage += value;
                remove => OnServerCustomMessage -= value;
            }

            internal static void LoadRsaKeysFromCert(string certPath, bool validateCertificate)
            {
                if (!File.Exists(certPath))
                {
                    throw new CryptographicException($"The cert config file was not found at '{certPath}'. Please ensure the path is correct and the certificate exists.");
                }

                var dict = FromJson<Dictionary<string, string>>(File.ReadAllText(NetworkConstants.k_CertificateFile));
                string certName = dict["cert"];
                string certPassword = dict["password"];

                if (!File.Exists(certName))
                {
                    throw new CryptographicException($"The certificate file was not found at '{certName}'. Please ensure the path is correct and the certificate exists.");
                }

#if UNITY_SERVER && !UNITY_EDITOR // Only validate on server
                if (validateCertificate)
                {
                    if (!CertificateValidator.ValidateCertificate(certName, certPassword, ConnectAddress, out string details))
                    {
                        NetworkLogger.__Log__($"[NetworkManager] SSL Certificate Validation Failed - {details}", NetworkLogger.LogType.Warning);
                    }

                    NetworkLogger.__Log__(details, NetworkLogger.LogType.Warning);
                }
#endif
                using var cert = new X509Certificate2(certName, certPassword);
                using var priRsa = cert.GetRSAPrivateKey();
                PEMPrivateKey = priRsa.ToPemString(true);

                using var pubRsa = cert.GetRSAPublicKey();
                PEMPublicKey = pubRsa.ToPemString(false);
#if UNITY_EDITOR
                string resourceDir = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(resourceDir))
                    Directory.CreateDirectory(resourceDir);

                string fullPath = Path.Combine(resourceDir, k_PublicKeyFile);
                File.WriteAllText(fullPath, PEMPublicKey);
                UnityEditor.AssetDatabase.Refresh();
#endif
            }

            internal static void GenerateRsaKeys()
            {
                RsaProvider.GetKeys(out var rsaPrivateKey, out var rsaPublicKey);
                PEMPrivateKey = rsaPrivateKey;
                PEMPublicKey = rsaPublicKey;
            }

            public static NetworkIdentity GetIdentity(int identityId)
            {
                if (Identities.TryGetValue(identityId, out NetworkIdentity identity))
                {
                    return identity;
                }
                else
                {
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
            public static void SendMessage(byte msgId, NetworkPeer peer, DataBuffer message)
            {
                SendToClient(msgId, peer, message);
            }

            public static void Rpc(byte msgId, NetworkPeer peer, int identityId, DataBuffer data)
            {
                using DataBuffer message = Pool.Rent(enableTracking: false);
                message.Internal_Write(identityId);
                message.Write(msgId);
                message.Internal_CopyFrom(data);
                SendMessage(NetworkPacketType.k_StaticRpc, peer, message);
            }

            public static void Rpc(byte msgId, NetworkPeer peer, int identityId, byte instanceId, DataBuffer data)
            {
                using DataBuffer message = Pool.Rent(enableTracking: false);
                message.Internal_Write(identityId);
                message.Write(instanceId);
                message.Write(msgId);
                message.Internal_CopyFrom(data);
                SendMessage(NetworkPacketType.k_LocalRpc, peer, message);
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

                return null;
            }

            internal static bool TryGetGroupById(int groupId, out NetworkGroup group)
            {
                return GroupsById.TryGetValue(groupId, out group);
            }

            internal static void JoinGroup(string groupName, DataBuffer buffer, NetworkPeer peer, bool includeBufferInResponse)
            {
                void SendJoinGroupResponse()
                {
                    using DataBuffer message = Pool.Rent(enableTracking: false);
                    message.WriteString(groupName);
                    if (includeBufferInResponse)
                        message.Internal_CopyFrom(buffer);

                    SetTarget(Target.Self);
                    SendMessage(NetworkPacketType.k_JoinGroup, peer, message);
                }

                int uniqueId = GetGroupIdByName(groupName);
                if (GroupsById.TryGetValue(uniqueId, out NetworkGroup group))
                {
                    if (!group._peersById.TryAdd(peer.Id, peer))
                    {
                        string msg = $"JoinGroup failed: peer {peer.Id} is already in group '{groupName}'.";
                        OnPlayerFailedJoinGroup?.Invoke(peer, msg);
                        NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
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
                        string msg = $"JoinGroup failed: group '{groupName}' (Id={uniqueId}) already exists.";
                        OnPlayerFailedJoinGroup?.Invoke(peer, msg);
                        NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                        return;
                    }

                    EnterGroup(buffer, peer, group);
                }

                void EnterGroup(DataBuffer buffer, NetworkPeer peer, NetworkGroup group)
                {
                    if (!peer._groups.TryAdd(group.Id, group))
                    {
                        var msg = $"JoinGroup failed: group '{group.Name}' (Id={group.Id}) is already assigned to peer {peer.Id}.";
                        OnPlayerFailedJoinGroup?.Invoke(peer, msg);
                        NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
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

                    SendJoinGroupResponse();
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
                    using DataBuffer message = Pool.Rent(enableTracking: false);
                    message.WriteString(groupName);
                    message.WriteString(reason);

                    SetTarget(Target.Self);
                    SendMessage(NetworkPacketType.k_LeaveGroup, peer, message);
                }

                int groupId = GetGroupIdByName(groupName);
                if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
                {
                    OnPlayerLeftGroup?.Invoke(group, peer, Phase.Started, reason);
                    if (group._peersById.Remove(peer.Id, out _))
                    {
                        if (!peer._groups.Remove(group.Id))
                        {
                            NetworkLogger.__Log__(
                                $"LeaveGroup failed: group '{group.Name}' (Id={group.Id}) was never assigned to peer {peer.Id}.",
                                NetworkLogger.LogType.Error
                            );

                            return;
                        }

                        SendResponseToClient();
                        OnPlayerLeftGroup?.Invoke(group, peer, Phase.Active, reason);

                        if (group.DestroyWhenEmpty)
                            DestroyGroupWhenEmpty(group);
                    }
                    else
                    {
                        var msg = $"LeaveGroup failed: peer {peer.Id} cannot be removed from group '{groupName}'.";
                        NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                        OnPlayerFailedLeaveGroup?.Invoke(peer, msg);
                    }
                }
                else
                {
                    var msg = $"LeaveGroup failed: group '{groupName}' was not found.";
                    NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                    OnPlayerFailedLeaveGroup?.Invoke(peer, msg);
                }
            }

            internal static void DestroyGroupWhenEmpty(NetworkGroup group)
            {
                if (group._peersById.Count == 0)
                {
                    if (!GroupsById.Remove(group.Id))
                    {
                        var msg = $"Tried to destroy group '{group.Name}' (Id={group.Id}) but it was not found.";
                        NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                    }

                    // Dereferencing to allow for GC(Garbage Collector).
                    group.ClearAllPeers();
                    group.ResetDataCollections();
                    group._subGroups.Clear();
                }
            }

            internal static void AddRpcMessage(int identityId, IRpcMessage behaviour)
            {
                if (!StaticRpcHandlers.TryAdd(identityId, behaviour))
                {
                    StaticRpcHandlers[identityId] = behaviour;
                }
            }

            public static void SetDeliveryMode(DeliveryMode deliveryMode)
            {
                DeliveryMode = deliveryMode;
            }

            public static void SetTarget(Target target)
            {
                Target = target;
            }

            public static void SetGroup(NetworkGroup group)
            {
                Group = group;
            }
            public static void SetSequenceChannel(byte sequenceChannel)
            {
                SequenceChannel = sequenceChannel;
            }

            public static void SetDefaultNetworkConfiguration(DeliveryMode deliveryMode, Target target, NetworkGroup group, byte sequenceChannel)
            {
                SetDeliveryMode(deliveryMode);
                SetTarget(target);
                SetGroup(group);
                SetSequenceChannel(sequenceChannel);
            }

            internal static void RestoreDefaultNetworkConfiguration()
            {
                DeliveryMode = Default_DeliveryMode;
                Target = Default_Target;
                Group = NetworkGroup.None;
                SequenceChannel = Default_SequenceChannel;
            }
        }
    }
}