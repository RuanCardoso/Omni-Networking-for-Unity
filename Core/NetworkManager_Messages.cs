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
		private static void Internal_SendMessage(
			byte msgId,
			NetworkPeer peer,
			DataBuffer buffer,
			Target target,
			DeliveryMode deliveryMode,
			bool isServer,
			int groupId,
			DataCache dataCache,
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
				if (peer == null)
				{
					throw new NullReferenceException("Peer cannot be null.");
				}

				SendToClient(
					msgId,
					buffer,
					peer,
					target,
					deliveryMode,
					groupId,
					dataCache,
					seqChannel
				);
			}
		}

		public static class Client
		{
			public static BandwidthMonitor SentBandwidth => Connection.Client.SentBandwidth;
			public static BandwidthMonitor ReceivedBandwidth => Connection.Client.ReceivedBandwidth;

			/// <summary>
			/// Gets the server peer used for exclusively for encryption keys.
			/// </summary>
			public static NetworkPeer ServerPeer { get; } =
				new(new IPEndPoint(IPAddress.None, 0), 0);

			/// <summary>
			/// Gets the server RSA public key .
			/// This property stores the public key used in RSA cryptographic operations.
			/// The public key is utilized to encrypt data, ensuring that only the holder of the corresponding private key can decrypt it.
			/// </summary>
			internal static string RsaServerPublicKey { get; set; }

			internal static Dictionary<int, NetworkGroup> Groups { get; } = new();
			internal static Dictionary<int, IInvokeMessage> GlobalEventBehaviours { get; } = new(); // int: identifier(identity id)
			internal static Dictionary<(int, byte), IInvokeMessage> LocalEventBehaviours { get; } = new();

			public static Dictionary<int, NetworkIdentity> Identities { get; } = new();
			public static Dictionary<int, NetworkPeer> Peers { get; } = new();

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
			public static void SendMessage(byte msgId, SyncOptions options)
			{
				SendMessage(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void SendMessage(
				byte msgId,
				DataBuffer buffer = null,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				byte sequenceChannel = 0
			)
			{
				Internal_SendMessage(
					msgId,
					LocalPeer,
					buffer,
					Target.Self,
					deliveryMode,
					false,
					0,
					DataCache.None,
					sequenceChannel
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void GlobalInvoke(byte msgId, SyncOptions options)
			{
				GlobalInvoke(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void GlobalInvoke(
				byte msgId,
				DataBuffer buffer = null,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				byte sequenceChannel = 0
			)
			{
				SendMessage(msgId, buffer, deliveryMode, sequenceChannel);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Invoke(byte msgId, int identityId, SyncOptions options)
			{
				Invoke(
					msgId,
					identityId,
					options.Buffer,
					options.DeliveryMode,
					options.SequenceChannel
				);
			}

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
				message.Write(identityId);
				message.Write(msgId);
				message.Write(buffer.BufferAsSpan);
				SendMessage(MessageType.GlobalInvoke, message, deliveryMode, sequenceChannel);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Invoke(
				byte msgId,
				int identityId,
				byte instanceId,
				SyncOptions options
			)
			{
				Invoke(
					msgId,
					identityId,
					instanceId,
					options.Buffer,
					options.DeliveryMode,
					options.SequenceChannel
				);
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
				message.Internal_Write(identityId);
				message.Write(instanceId);
				message.Write(msgId);
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
				message.WriteString(groupName);
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
				message.WriteString(groupName);
				message.WriteString(reason);
				SendMessage(MessageType.LeaveGroup, message, DeliveryMode.ReliableOrdered, 0);
			}

			internal static void AddEventBehaviour(int identityId, IInvokeMessage behaviour)
			{
				if (!GlobalEventBehaviours.TryAdd(identityId, behaviour))
				{
					GlobalEventBehaviours[identityId] = behaviour;
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

		public static class Server
		{
			public static BandwidthMonitor SentBandwidth => Connection.Server.SentBandwidth;
			public static BandwidthMonitor ReceivedBandwidth => Connection.Server.ReceivedBandwidth;

			/// <summary>
			/// Gets the server peer.
			/// </summary>
			/// <remarks>
			/// The server peer is a special peer that is used to represent the server.
			/// </remarks>
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

			internal static Dictionary<int, NetworkGroup> Groups => GroupsById;
			internal static Dictionary<int, IInvokeMessage> GlobalEventBehaviours { get; } = new();
			internal static Dictionary<(int, byte), IInvokeMessage> LocalEventBehaviours { get; } =
				new();

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

			public static bool TryGetIdentity(int identityId, out NetworkIdentity identity)
			{
				return Identities.TryGetValue(identityId, out identity);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void SendMessage(byte msgId, NetworkPeer peer, SyncOptions options)
			{
				SendMessage(
					msgId,
					peer,
					options.Buffer,
					options.Target,
					options.DeliveryMode,
					options.GroupId,
					options.DataCache,
					options.SequenceChannel
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void SendMessage(
				byte msgId,
				NetworkPeer peer,
				DataBuffer buffer = null,
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0
			)
			{
				dataCache ??= DataCache.None;
				Internal_SendMessage(
					msgId,
					peer,
					buffer,
					target,
					deliveryMode,
					true,
					groupId,
					dataCache,
					sequenceChannel
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void GlobalInvoke(byte msgId, NetworkPeer peer, SyncOptions options)
			{
				GlobalInvoke(
					msgId,
					peer,
					options.Buffer,
					options.Target,
					options.DeliveryMode,
					options.GroupId,
					options.DataCache,
					options.SequenceChannel
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void GlobalInvoke(
				byte msgId,
				NetworkPeer peer,
				DataBuffer buffer = null,
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0
			)
			{
				dataCache ??= DataCache.None;
				SendMessage(
					msgId,
					peer,
					buffer,
					target,
					deliveryMode,
					groupId,
					dataCache,
					sequenceChannel
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Invoke(
				byte msgId,
				NetworkPeer peer,
				int identityId,
				SyncOptions options
			)
			{
				Invoke(
					msgId,
					peer,
					identityId,
					options.Buffer,
					options.Target,
					options.DeliveryMode,
					options.GroupId,
					options.DataCache,
					options.SequenceChannel
				);
			}

			public static void Invoke(
				byte msgId,
				NetworkPeer peer,
				int identityId,
				DataBuffer buffer = null,
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0
			)
			{
				dataCache ??= DataCache.None;
				buffer ??= DataBuffer.Empty;

				using DataBuffer message = Pool.Rent();
				message.Write(identityId);
				message.Write(msgId);
				message.Write(buffer.BufferAsSpan);
				SendMessage(
					MessageType.GlobalInvoke,
					peer,
					message,
					target,
					deliveryMode,
					groupId,
					dataCache,
					sequenceChannel
				);

				// byte count per empty message: 4 + 1 = 5 + header;
				// TODO: reduce bandwidth usage
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Invoke(
				byte msgId,
				NetworkPeer peer,
				int identityId,
				byte instanceId,
				SyncOptions options
			)
			{
				Invoke(
					msgId,
					peer,
					identityId,
					instanceId,
					options.Buffer,
					options.Target,
					options.DeliveryMode,
					options.GroupId,
					options.DataCache,
					options.SequenceChannel
				);
			}

			public static void Invoke(
				byte msgId,
				NetworkPeer peer,
				int identityId,
				byte instanceId,
				DataBuffer buffer = null,
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0
			)
			{
				buffer ??= DataBuffer.Empty;
				dataCache ??= DataCache.None;

				using DataBuffer message = Pool.Rent();
				message.Internal_Write(identityId);
				message.Write(instanceId);
				message.Write(msgId); // rpc id
				message.Write(buffer.BufferAsSpan);
				SendMessage(
					MessageType.LocalInvoke,
					peer,
					message,
					target,
					deliveryMode,
					groupId,
					dataCache,
					sequenceChannel
				);

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
					message.WriteString(groupName);

					if (writeBufferToClient)
					{
						message.Write(buffer.BufferAsSpan);
					}

					SendMessage(
						MessageType.JoinGroup,
						peer,
						message,
						Target.Self,
						DeliveryMode.ReliableOrdered,
						0,
						DataCache.None,
						0
					);
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
					group = new NetworkGroup(uniqueId, groupName);
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

					// Set the master client if it's the first player in the group.
					if (group.MasterClientId <= NetworkConstants.INVALID_MASTER_CLIENT_ID)
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
				NetworkGroup group = new(groupId, groupName);
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
				group = new(groupId, groupName);
				return GroupsById.TryAdd(groupId, group);
			}

			internal static void LeaveGroup(string groupName, string reason, NetworkPeer peer)
			{
				void SendResponseToClient()
				{
					using DataBuffer message = Pool.Rent();
					message.WriteString(groupName);
					message.WriteString(reason);

					SendMessage(
						MessageType.LeaveGroup,
						peer,
						message,
						Target.Self,
						DeliveryMode.ReliableOrdered,
						0,
						DataCache.None,
						0
					);
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
			internal static void Internal_SendPeerCache(
				NetworkPeer fromPeer,
				NetworkPeer toPeer,
				DataCache dataCache,
				int groupId = 0,
				bool sendMyOwnCacheToMe = false
			)
			{
				Internal_SendCache(
					fromPeer,
					toPeer,
					dataCache,
					groupId,
					sendMyOwnCacheToMe
				);
			}

			/// <summary>
			/// Sends cached data to a specified network peer based on the provided data cache.
			/// </summary>
			/// <param name="peer">The network peer to whom the cache data will be sent.</param>
			/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
			/// <param name="groupId">The identifier of the group to which the cache belongs (optional, default is 0).</param>
			/// <param name="sendMyOwnCacheToMe">A flag indicating whether to send the cache data to the originating peer (optional, default is false).</param>
			internal static void Internal_SendCache(
				NetworkPeer peer,
				DataCache dataCache,
				int groupId = 0,
				bool sendMyOwnCacheToMe = false
			)
			{
				Internal_SendCache(peer, peer, dataCache, groupId, sendMyOwnCacheToMe);
			}

			internal static void Internal_SendCache(
				NetworkPeer fromPeer,
				NetworkPeer toPeer,
				DataCache dataCache,
				int groupId,
				bool sendMyOwnCacheToMe
			)
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
						if (
							dataCache.Mode == (CacheMode.Global | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							List<NetworkCache> caches = CACHES_APPEND_GLOBAL
								.Where(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id)
								.ToList();

							foreach (NetworkCache cache in caches)
							{
								if (!sendMyOwnCacheToMe)
								{
									if (cache.Peer.Id == toPeer.Id)
									{
										continue;
									}
								}

								Connection.Server.Send(
									cache.Data,
									toPeer.EndPoint,
									cache.DeliveryMode,
									cache.SequenceChannel
								);
							}
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
							{
								List<NetworkCache> caches = group
									.CACHES_APPEND.Where(x =>
										x.Mode == dataCache.Mode && x.Id == dataCache.Id
									)
									.ToList();

								foreach (NetworkCache cache in caches)
								{
									if (!sendMyOwnCacheToMe)
									{
										if (cache.Peer.Id == toPeer.Id)
										{
											continue;
										}
									}

									Connection.Server.Send(
										cache.Data,
										toPeer.EndPoint,
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
							dataCache.Mode == (CacheMode.Global | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							if (
								CACHES_OVERWRITE_GLOBAL.TryGetValue(dataCache.Id, out NetworkCache cache)
							)
							{
								if (!sendMyOwnCacheToMe)
								{
									if (cache.Peer.Id == toPeer.Id)
									{
										return;
									}
								}

								Connection.Server.Send(
									cache.Data,
									toPeer.EndPoint,
									cache.DeliveryMode,
									cache.SequenceChannel
								);
							}
							else
							{
								NetworkLogger.__Log__(
									$"Cache Error: Cache with Id: {dataCache.Id} and search mode: [{dataCache.Mode}] not found.",
									NetworkLogger.LogType.Error
								);
							}
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
							{
								if (
									group.CACHES_OVERWRITE.TryGetValue(
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

									Connection.Server.Send(
										cache.Data,
										toPeer.EndPoint,
										cache.DeliveryMode,
										cache.SequenceChannel
									);
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
						}
						else if (
							dataCache.Mode == (CacheMode.Peer | CacheMode.New)
							|| dataCache.Mode == (CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							List<NetworkCache> caches = fromPeer
								.CACHES_APPEND.Where(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id)
								.ToList();

							foreach (NetworkCache cache in caches)
							{
								if (!sendMyOwnCacheToMe)
								{
									if (cache.Peer.Id == toPeer.Id)
									{
										continue;
									}
								}

								Connection.Server.Send(
									cache.Data,
									toPeer.EndPoint,
									cache.DeliveryMode,
									cache.SequenceChannel
								);
							}
						}
						else if (
							dataCache.Mode == (CacheMode.Peer | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							if (
								fromPeer.CACHES_OVERWRITE.TryGetValue(
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

								Connection.Server.Send(
									cache.Data,
									toPeer.EndPoint,
									cache.DeliveryMode,
									cache.SequenceChannel
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
						"Data Cache: Required Id and Mode must be set together."
					);
				}
			}

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
						if (
							dataCache.Mode == (CacheMode.Global | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							CACHES_APPEND_GLOBAL.RemoveAll(x =>
								x.Mode == dataCache.Mode && x.Id == dataCache.Id
							);
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
							{
								group.CACHES_APPEND.RemoveAll(x =>
									x.Mode == dataCache.Mode && x.Id == dataCache.Id
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
							dataCache.Mode == (CacheMode.Global | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							CACHES_OVERWRITE_GLOBAL.Remove(dataCache.Id);
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
							{
								group.CACHES_OVERWRITE.Remove(dataCache.Id);
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
						"Cache: Required dataCache.Id and dataCache.Mode must be set together."
					);
				}
			}

			public static void DeleteCache(
				DataCache dataCache,
				NetworkPeer peer,
				int groupId = 0
			)
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
						if (
							dataCache.Mode == (CacheMode.Global | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							CACHES_APPEND_GLOBAL.RemoveAll(x =>
								x.Mode == dataCache.Mode && x.Id == dataCache.Id && x.Peer.Id == peer.Id
							);
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
							{
								group.CACHES_APPEND.RemoveAll(x =>
									x.Mode == dataCache.Mode && x.Id == dataCache.Id && x.Peer.Id == peer.Id
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
							dataCache.Mode == (CacheMode.Global | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							CACHES_OVERWRITE_GLOBAL.Remove(dataCache.Id);
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							if (GroupsById.TryGetValue(groupId, out NetworkGroup group))
							{
								group.CACHES_OVERWRITE.Remove(dataCache.Id);
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
							dataCache.Mode == (CacheMode.Peer | CacheMode.New)
							|| dataCache.Mode == (CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							peer.CACHES_APPEND.RemoveAll(x =>
								x.Mode == dataCache.Mode && x.Id == dataCache.Id
							);
						}
						else if (
							dataCache.Mode == (CacheMode.Peer | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							peer.CACHES_OVERWRITE.Remove(dataCache.Id);
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
						"Cache: Required dataCache.Id and dataCache.Mode must be set together."
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

			internal static void AddEventBehaviour(int identityId, IInvokeMessage behaviour)
			{
				if (!GlobalEventBehaviours.TryAdd(identityId, behaviour))
				{
					GlobalEventBehaviours[identityId] = behaviour;
				}
			}
		}
	}
}
