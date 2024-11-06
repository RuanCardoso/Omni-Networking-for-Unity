using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using MemoryPack;
using Newtonsoft.Json;
using Omni.Shared;
using Omni.Shared.Collections;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
	[JsonObject(MemberSerialization.OptIn)]
	[MemoryPackable]
	public partial class NetworkPeer : IEquatable<NetworkPeer>
	{
		[MemoryPackIgnore]
		internal byte[] _aesKey;

		[MemoryPackIgnore]
		internal NativePeer _nativePeer;

		[MemoryPackIgnore]
		internal Dictionary<int, NetworkGroup> _groups = new();

#pragma warning disable IDE0052
		[MemoryPackIgnore]
		readonly string __endpoint__;
#pragma warning restore IDE0052
		[MemoryPackIgnore]
		public IPEndPoint EndPoint { get; }

		[MemoryPackIgnore]
		public int Id { get; }

		[MemoryPackIgnore]
		public bool IsConnected { get; internal set; }

		[MemoryPackIgnore]
		public bool IsAuthenticated { get; internal set; }

		[MemoryPackIgnore]
		public ObservableDictionary<string, object> Data { get; } = new();

		[MemoryPackIgnore, JsonProperty("Data")]
		public ObservableDictionary<string, object> SerializedData { get; internal set; } = new();

		[MemoryPackIgnore]
		internal List<NetworkCache> CACHES_APPEND { get; } = new();

		[MemoryPackIgnore]
		internal Dictionary<int, NetworkCache> CACHES_OVERWRITE { get; } = new();

		[MemoryPackIgnore]
		public double Time => _nativePeer.Time;

		[MemoryPackIgnore]
		public double Ping => _nativePeer.Ping;

		[MemoryPackConstructor]
		[JsonConstructor]
		internal NetworkPeer() { }

		internal NetworkPeer(IPEndPoint endPoint, int id)
		{
			// Avoid self reference loop when serializing.
			__endpoint__ = endPoint.ToString();

			// Parameters:
			// - endPoint: The IPEndPoint of the peer.
			// - id: The ID of the peer used in server-side.
			EndPoint = endPoint;
			Id = id;
		}

		public void ClearGroups()
		{
			EnsureServerActive();
			_groups.Clear();
		}

		public void ClearData()
		{
			EnsureServerActive();
			Data.Clear();
			SerializedData.Clear();
		}

		public void Disconnect()
		{
			EnsureServerActive();
			DisconnectPeer(this);
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
			Target target = Target.Self,
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
			Target target = Target.Self,
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
			Target target = Target.Self,
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

			if (SerializedData.TryGetValue(key, out object value) || key == "_AllKeys_")
			{
				value = key != "_AllKeys_" ? value : SerializedData;
				ImmutableKeyValuePair keyValuePair = new(key, value);
				using var message = Pool.Rent();
				message.Write(Id);
				message.WriteAsJson(keyValuePair);
				Server.SendMessage(
					MessageType.SyncPeerSerializedData,
					this,
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
					$"SyncSerializedData Error: Failed to sync '{key}' because it doesn't exist.",
					NetworkLogger.LogType.Error
				);
			}
		}

		public void DeleteCache(DataCache dataCache)
		{
			if (
				dataCache.Mode == (CacheMode.Peer | CacheMode.New)
				|| dataCache.Mode == (CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy)
			)
			{
				CACHES_APPEND.RemoveAll(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id);
			}
			else if (
				dataCache.Mode == (CacheMode.Peer | CacheMode.Overwrite)
				|| dataCache.Mode == (CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy)
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

		public void DestroyAllCaches()
		{
			CACHES_APPEND.RemoveAll(x => x.AutoDestroyCache);
			var caches = CACHES_OVERWRITE.Values.Where(x => x.AutoDestroyCache).ToList();

			foreach (var cache in caches)
			{
				if (!CACHES_OVERWRITE.Remove(cache.Id))
				{
					NetworkLogger.__Log__(
						$"Destroy All Cache Error: Failed to remove cache {cache.Id} from peer {Id}.",
						NetworkLogger.LogType.Error
					);
				}
			}
		}

		public void ClearCaches()
		{
			CACHES_APPEND.Clear();
			CACHES_OVERWRITE.Clear();
		}

		[Conditional("OMNI_DEBUG")]
		private void EnsureServerActive()
		{
			if (!IsServerActive)
			{
				throw new Exception("Can't use this method on client.");
			}
		}

		public override string ToString()
		{
			return ToJson(this);
		}

		public override bool Equals(object obj)
		{
			NetworkPeer other = (NetworkPeer)obj;
			return Id == other.Id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public bool Equals(NetworkPeer other)
		{
			return Id == other.Id;
		}
	}
}
