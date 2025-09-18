using Omni.Core.Cryptography;
using Omni.Core.Interfaces;
using Omni.Shared;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Omni.Threading.Tasks;

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
                    return identity;

                return null;
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

            internal static void JoinGroup(string groupName, NetworkPeer peer, bool autoJoinParents, bool isRecursiveCall = false)
            {
                int uniqueId = GetGroupIdByName(groupName);
                if (GroupsById.TryGetValue(uniqueId, out NetworkGroup group))
                {
                    if (!group._peersById.TryAdd(peer.Id, peer))
                    {
                        string msg = $"JoinGroup failed: peer {peer.Id} is already in group '{group.Name}'.";
                        OnPlayerFailedJoinGroup?.Invoke(peer, msg);
                        NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                        return;
                    }

                    if (!isRecursiveCall)
                    {
                        var parents = new Stack<NetworkGroup>();
                        var current = group.Parent;

                        while (current != null)
                        {
                            parents.Push(current);
                            current = current.Parent;
                        }

                        while (parents.Count > 0)
                        {
                            var parent = parents.Pop();
                            if (!parent.TryGetPeer(peer.Id, out _))
                            {
                                if (!autoJoinParents)
                                {
                                    string msg = $"JoinGroup failed: Peer {peer.Id} is not in required parent group(s) for '{group.Name}'. " +
                                                 "Enable autoJoinParents or join parent groups manually first.";

                                    OnPlayerFailedJoinGroup?.Invoke(peer, msg);
                                    NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                                    return;
                                }

                                JoinGroup(parent.Identifier, peer, autoJoinParents, isRecursiveCall: true);
                            }
                        }
                    }

                    EnterGroup(peer, group);
                }

                static void EnterGroup(NetworkPeer peer, NetworkGroup group)
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
                        if (peer.MainGroup != null)
                        {
                            var msg = $"JoinGroup failed: Peer {peer.Id} is already member of a main group '{peer.MainGroup.Name}' (Id={peer.MainGroup.Id}). " +
                                      "Each peer can only be member of one main group at the same time.";

                            OnPlayerFailedJoinGroup?.Invoke(peer, msg);
                            NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                            return;
                        }

                        peer.MainGroup = group;
                    }

                    // Set the master client if it's the first player in the group.
                    if (group.MasterClient == null)
                    {
                        group.SetMasterClient(peer);
                    }

                    OnPlayerJoinedGroup?.Invoke(group, peer);
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

            internal static void LeaveGroup(string groupName, string reason, NetworkPeer peer, bool autoLeaveChildren, bool isRecursiveCall = false)
            {
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

                        static void CollectSubGroupsBottomUp(NetworkGroup parent, List<NetworkGroup> result)
                        {
                            for (int i = parent._subGroups.Count - 1; i >= 0; i--)
                            {
                                NetworkGroup child = parent._subGroups[i];
                                CollectSubGroupsBottomUp(child, result);
                                result.Add(child);
                            }
                        }

                        if (!isRecursiveCall)
                        {
                            var ordered = new List<NetworkGroup>();
                            CollectSubGroupsBottomUp(group, ordered);

                            foreach (var child in ordered)
                            {
                                if (child.TryGetPeer(peer.Id, out _))
                                {
                                    if (!autoLeaveChildren)
                                    {
                                        string msg = $"LeaveGroup failed: Peer {peer.Id} is still in child/descendant group(s) of '{group.Name}'. " +
                                                     "Enable autoLeaveChildren or leave child groups manually first.";

                                        OnPlayerFailedLeaveGroup?.Invoke(peer, msg);
                                        NetworkLogger.__Log__(msg, NetworkLogger.LogType.Error);
                                        return;
                                    }

                                    LeaveGroup(child.Identifier, reason, peer, autoLeaveChildren, isRecursiveCall: true);
                                }
                            }
                        }

                        if (!group.IsSubGroup)
                            peer.MainGroup = null;

                        OnPlayerLeftGroup?.Invoke(group, peer, Phase.Active, reason);
                        if (group.DestroyWhenEmpty)
                            DestroyGroupWhenEmpty(group);
                        OnPlayerLeftGroup?.Invoke(group, peer, Phase.Ended, reason);
                    }
                    else
                    {
                        var msg = $"LeaveGroup failed: peer {peer.Id} cannot be removed from group '{group.Name}'.";
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

            internal static async void DestroyGroupWhenEmpty(NetworkGroup group)
            {
                // Let's schedule the destruction and cleanup for a few seconds in the future, to avoid problems while a group is still being used even after it is immediately empty.
                await UniTask.Delay(2500); // 2.5 seconds it's fine.
                if (group._peersById.Count == 0)
                {
                    if (GroupsById.Remove(group.Id))
                    {
                        // Dereferencing to allow for GC(Garbage Collector).
                        group.ClearAllPeers();
                        group.ResetDataCollections();
                        group._subGroups.Clear();
                    }
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