using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using Newtonsoft.Json;
using Omni.Shared;
using Omni.Shared.Collections;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
	[JsonObject(MemberSerialization.OptIn)]
	[MemoryPackable]
	public partial class NetworkGroup : IEquatable<NetworkGroup>
	{
		[MemoryPackIgnore]
		internal readonly Dictionary<int, NetworkPeer> _peersById = new();

		[MemoryPackIgnore]
		public int Id { get; }

		[MemoryPackIgnore]
		public string Identifier { get; }

		/// <summary>
		/// The name of the group(Beautiful identifier).
		/// </summary>
		[MemoryPackIgnore]
		public string Name { get; }

		[MemoryPackIgnore]
		public int MasterClientId { get; private set; } = NetworkConstants.INVALID_MASTER_CLIENT_ID;

		[MemoryPackIgnore]
		public int PeerCount => _peersById.Count;

		[MemoryPackIgnore]
		internal List<NetworkCache> CACHES_APPEND { get; } = new();

		[MemoryPackIgnore]
		internal Dictionary<int, NetworkCache> CACHES_OVERWRITE { get; } = new();

		[MemoryPackIgnore]
		public ObservableDictionary<string, object> Data { get; } = new();

		[MemoryPackIgnore, JsonProperty("Data")]
		public ObservableDictionary<string, object> SerializedData { get; internal set; } = new();

		[MemoryPackIgnore]
		public Dictionary<int, NetworkPeer> Peers
		{
			get
			{
				if (__namebuilder__)
				{
					throw new Exception("Peers: Cannot get peers from name builder mode.");
				}

				return _peersById;
			}
		}

		[MemoryPackIgnore]
		public bool DestroyWhenEmpty { get; set; } = true;

		[MemoryPackIgnore]
		public bool IsSubGroup => Depth != 1;

		[MemoryPackIgnore]
		public bool AllowAcrossGroupMessage { get; set; } = true;

		[MemoryPackIgnore]
		public int Depth { get; }

		[MemoryPackIgnore]
		private readonly bool __namebuilder__;

		[MemoryPackConstructor]
		[JsonConstructor]
		internal NetworkGroup() { }

		public NetworkGroup(string groupName)
		{
			string[] subGroups = groupName.Split("->");

			Identifier = groupName;
			Depth = subGroups.Length;
			Name = subGroups[Depth - 1];
			__namebuilder__ = true;
		}

		internal NetworkGroup(int groupId, string groupName)
		{
			string[] subGroups = groupName.Split("->");

			Id = groupId;
			Identifier = groupName;
			Depth = subGroups.Length;
			Name = subGroups[Depth - 1];
		}

		public NetworkPeer GetPeer(int peerId)
		{
			if (__namebuilder__)
			{
				throw new Exception("GetPeer: Cannot get peer from name builder mode.");
			}

			if (_peersById.TryGetValue(peerId, out var peer))
			{
				return peer;
			}

			NetworkLogger.__Log__(
				$"Group: Peer with ID {peerId}:{Id} not found. Please verify the peer ID and ensure the peer is properly registered(connected!) in the group.",
				NetworkLogger.LogType.Error
			);

			return null;
		}

		public bool TryGetPeer(int peerId, out NetworkPeer peer)
		{
			if (__namebuilder__)
			{
				throw new Exception("GetPeer: Cannot get peer from name builder mode.");
			}

			return _peersById.TryGetValue(peerId, out peer);
		}

		public NetworkGroup AddSubGroup(string subGroupName)
		{
			string newIdentifier = $"{Identifier}->{subGroupName}";
			if (!__namebuilder__)
			{
				return Matchmaking.Server.AddGroup(newIdentifier);
			}

			return new NetworkGroup(newIdentifier);
		}

		public bool TryAddSubGroup(string subGroupName, out NetworkGroup subGroup)
		{
			string newIdentifier = $"{Identifier}->{subGroupName}";
			if (!__namebuilder__)
			{
				return Matchmaking.Server.TryAddGroup(newIdentifier, out subGroup);
			}

			subGroup = new NetworkGroup(newIdentifier);
			return true;
		}

		public void SetMasterClient(NetworkPeer peer)
		{
			MasterClientId = peer.Id;
		}

		public void SyncSerializedData(SyncOptions options)
		{
			SyncSerializedData(
				options.Target,
				options.DeliveryMode,
				options.GroupId,
				options.DataCache,
				options.SequenceChannel
			);
		}

		public void SyncSerializedData(
			Target target = Target.GroupMembers,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0
		)
		{
			dataCache ??= DataCache.None;
			SyncSerializedData(
				"_AllKeys_",
				target,
				deliveryMode,
				groupId,
				dataCache,
				sequenceChannel
			);
		}

		public void SyncSerializedData(string key, SyncOptions options)
		{
			SyncSerializedData(
				key,
				options.Target,
				options.DeliveryMode,
				options.GroupId,
				options.DataCache,
				options.SequenceChannel
			);
		}

		public void SyncSerializedData(
			string key,
			Target target = Target.GroupMembers,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
		   DataCache dataCache = default,
			byte sequenceChannel = 0
		)
		{
			dataCache ??= DataCache.None;
			Internal_SyncSerializedData(
				key,
				target,
				deliveryMode,
				groupId,
				dataCache,
				sequenceChannel
			);
		}

		private void Internal_SyncSerializedData(
			string key = "_AllKeys_",
			Target target = Target.GroupMembers,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0
		)
		{
			dataCache ??= DataCache.None;
			if (!IsServerActive)
			{
				throw new Exception("Can't use this method on client.");
			}

			if (MasterClientId <= NetworkConstants.INVALID_MASTER_CLIENT_ID)
			{
				throw new Exception(
					"MasterClientId is not set. Please set it before using this method."
				);
			}

			if (Server.Peers.TryGetValue(MasterClientId, out var peer))
			{
				if (SerializedData.TryGetValue(key, out object value) || key == "_AllKeys_")
				{
					value = key != "_AllKeys_" ? value : SerializedData;
					ImmutableKeyValuePair keyValuePair = new(key, value);
					using var message = Pool.Rent();
					message.Write(Id);
					message.WriteAsJson(keyValuePair);
					Server.SendMessage(
						MessageType.SyncGroupSerializedData,
						peer,
						message,
						target,
						deliveryMode,
						groupId,
						dataCache,
						sequenceChannel
					);
				}
				else
				{
					NetworkLogger.__Log__(
						$"SyncSerializedData Group Error: Failed to sync '{key}' because it doesn't exist.",
						NetworkLogger.LogType.Error
					);
				}
			}
			else
			{
				NetworkLogger.__Log__(
					$"SyncSerializedData Group Error: Peer(Master) with ID '{MasterClientId}' not found. Please verify the master ID and ensure the peer is properly registered(connected!).",
					NetworkLogger.LogType.Error
				);
			}
		}

		public void DeleteCache(DataCache dataCache)
		{
			if (
				dataCache.Mode == (CacheMode.Group | CacheMode.New)
				|| dataCache.Mode == (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
			)
			{
				CACHES_APPEND.RemoveAll(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id);
			}
			else if (
				dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite)
				|| dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
			)
			{
				CACHES_OVERWRITE.Remove(dataCache.Id);
			}
			else
			{
				NetworkLogger.__Log__(
					"Delete Cache Error: Unsupported cache mode set.",
					NetworkLogger.LogType.Error
				);
			}
		}

		public void DeleteCache(DataCache dataCache, NetworkPeer peer)
		{
			if (
				dataCache.Mode == (CacheMode.Group | CacheMode.New)
				|| dataCache.Mode == (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
			)
			{
				CACHES_APPEND.RemoveAll(x =>
					x.Mode == dataCache.Mode && x.Id == dataCache.Id && x.Peer.Id == peer.Id
				);
			}
			else if (
				dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite)
				|| dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
			)
			{
				CACHES_OVERWRITE.Remove(dataCache.Id);
			}
			else
			{
				NetworkLogger.__Log__(
					"Delete Cache Error: Unsupported cache mode set.",
					NetworkLogger.LogType.Error
				);
			}
		}

		public void DestroyAllCaches(NetworkPeer peer)
		{
			CACHES_APPEND.RemoveAll(x => x.Peer.Id == peer.Id && x.AutoDestroyCache);
			var caches = CACHES_OVERWRITE
				.Values.Where(x => x.Peer.Id == peer.Id && x.AutoDestroyCache)
				.ToList();

			foreach (var cache in caches)
			{
				if (!CACHES_OVERWRITE.Remove(cache.Id))
				{
					NetworkLogger.__Log__(
						$"Destroy All Cache Error: Failed to remove cache {cache.Id} from peer {peer.Id}.",
						NetworkLogger.LogType.Error
					);
				}
			}
		}

		public void ClearPeers()
		{
			_peersById.Clear();
		}

		public void ClearData()
		{
			Data.Clear();
			SerializedData.Clear();
		}

		public void ClearCaches()
		{
			CACHES_APPEND.Clear();
			CACHES_OVERWRITE.Clear();
		}

		public override string ToString()
		{
			return ToJson(this);
		}

		public override bool Equals(object obj)
		{
			NetworkGroup other = (NetworkGroup)obj;
			return Id == other.Id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public bool Equals(NetworkGroup other)
		{
			return Id == other.Id;
		}
	}
}
