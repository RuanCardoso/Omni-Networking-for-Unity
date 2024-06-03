using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using Newtonsoft.Json;
using Omni.Shared;
using Omni.Shared.Collections;

namespace Omni.Core.Modules.Matchmaking
{
    [JsonObject(MemberSerialization.OptIn)]
    [MemoryPackable]
    public partial class NetworkGroup
    {
        [MemoryPackIgnore]
        internal readonly Dictionary<int, NetworkPeer> _peersById = new();

        [MemoryPackIgnore]
        public int Id { get; }

        [MemoryPackIgnore]
        public string Name { get; }

        [MemoryPackIgnore]
        public int PeerCount => _peersById.Count;

        [MemoryPackIgnore]
        internal List<NetworkCache> CACHES_APPEND { get; } = new();

        [MemoryPackIgnore]
        internal Dictionary<int, NetworkCache> CACHES_OVERWRITE { get; } = new();

        [MemoryPackIgnore]
        public ObservableDictionary<string, object> Data { get; } = new();

        [MemoryPackIgnore, JsonProperty("Data")]
        public ObservableDictionary<string, object> SerializedData { get; } = new();

        [MemoryPackIgnore]
        public bool DestroyWhenEmpty { get; set; } = true;

        [MemoryPackIgnore]
        public bool AllowAcrossGroupMessage { get; set; } = true;

        [MemoryPackIgnore]
        public int Depth { get; }

        [MemoryPackIgnore]
        private readonly bool __namebuilder__;

        [MemoryPackConstructor]
        internal NetworkGroup() { }

        public NetworkGroup(string groupName)
        {
            Name = groupName;
            Depth = groupName.Split(':').Length;
            __namebuilder__ = true;
        }

        internal NetworkGroup(int groupId, string groupName)
        {
            Id = groupId;
            Name = groupName;
            Depth = groupName.Split(':').Length;
        }

        public Dictionary<int, NetworkPeer> GetPeers()
        {
            if (__namebuilder__)
            {
                throw new Exception("Cannot get peers from name builder mode.");
            }

            return _peersById;
        }

        public NetworkPeer GetPeerById(int peerId)
        {
            if (__namebuilder__)
            {
                throw new Exception("Cannot get peer from name builder mode.");
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

        public NetworkGroup AddSubGroup(string name)
        {
            if (!__namebuilder__)
            {
                return NetworkManager.Matchmaking.Server.AddGroup($"{Name}:{name}");
            }
            else
            {
                return new NetworkGroup($"{Name}:{name}");
            }
        }

        public void DeleteCache(CacheMode cacheMode, int cacheId)
        {
            if (
                cacheMode == (CacheMode.Group | CacheMode.New)
                || cacheMode == (CacheMode.Group | CacheMode.New | CacheMode.DestroyOnDisconnect)
            )
            {
                CACHES_APPEND.RemoveAll(x => x.Mode == cacheMode && x.Id == cacheId);
            }
            else if (
                cacheMode == (CacheMode.Group | CacheMode.Overwrite)
                || cacheMode
                    == (CacheMode.Group | CacheMode.Overwrite | CacheMode.DestroyOnDisconnect)
            )
            {
                CACHES_OVERWRITE.Remove(cacheId);
            }
            else
            {
                NetworkLogger.__Log__(
                    "Delete Cache Error: Unsupported cache mode set.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        public void DeleteCache(CacheMode cacheMode, int cacheId, NetworkPeer peer)
        {
            if (
                cacheMode == (CacheMode.Group | CacheMode.New)
                || cacheMode == (CacheMode.Group | CacheMode.New | CacheMode.DestroyOnDisconnect)
            )
            {
                CACHES_APPEND.RemoveAll(x =>
                    x.Mode == cacheMode && x.Id == cacheId && x.Peer.Id == peer.Id
                );
            }
            else if (
                cacheMode == (CacheMode.Group | CacheMode.Overwrite)
                || cacheMode
                    == (CacheMode.Group | CacheMode.Overwrite | CacheMode.DestroyOnDisconnect)
            )
            {
                CACHES_OVERWRITE.Remove(cacheId);
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
            CACHES_APPEND.RemoveAll(x => x.Peer.Id == peer.Id && x.DestroyOnDisconnect);
            var caches = CACHES_OVERWRITE
                .Values.Where(x => x.Peer.Id == peer.Id && x.DestroyOnDisconnect)
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
            return NetworkManager.ToJson(this);
        }
    }
}
