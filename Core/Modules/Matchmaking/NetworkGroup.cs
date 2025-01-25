using MemoryPack;
using Newtonsoft.Json;
using Omni.Shared;
using Omni.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static Omni.Core.NetworkManager;

#pragma warning disable

namespace Omni.Core
{
    /// <summary>
    /// Represents a network group that manages a collection of peers and shared data.
    /// It provides functionality for group management, data synchronization, and sub-group handling.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [MemoryPackable]
    public partial class NetworkGroup : IEquatable<NetworkGroup>
    {
        [MemoryPackIgnore] private readonly bool isNameBuilder;
        [MemoryPackIgnore] private bool _autoSyncSharedData;

        [MemoryPackIgnore] internal readonly Dictionary<int, NetworkPeer> _peersById = new();
        [MemoryPackIgnore] internal List<NetworkCache> AppendCaches { get; } = new();
        [MemoryPackIgnore] internal Dictionary<int, NetworkCache> OverwriteCaches { get; } = new();

        /// <summary>
        /// Gets the unique identifier for the group.
        /// </summary>
        [MemoryPackIgnore]
        public int Id { get; }

        /// <summary>
        /// Gets the full identifier (path) of the group, including all subgroups.
        /// </summary>
        [MemoryPackIgnore]
        public string Identifier { get; }

        /// <summary>
        /// Gets the name of the group. This is the last part of the group's identifier.
        /// </summary>
        [MemoryPackIgnore]
        public string Name { get; }

        /// <summary>
        /// Gets the master client (owner) of the group.
        /// </summary>
        [MemoryPackIgnore]
        public NetworkPeer MasterClient { get; private set; } = null;

        /// <summary>
        /// Gets the main group to which this group belongs.
        /// </summary>

        [MemoryPackIgnore]
        public NetworkGroup MainGroup { get; internal set; }

        /// <summary>
        /// Gets the number of peers currently in the group.
        /// </summary>
        [MemoryPackIgnore]
        public int PeerCount => _peersById.Count;

        /// <summary>
        /// Gets the non-synchronized data for the group.
        /// </summary>
        [MemoryPackIgnore]
        public ObservableDictionary<string, object> Data { get; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether changes to <see cref="SharedData"/> 
        /// should be automatically synchronized across the network.
        /// </summary>
        [MemoryPackIgnore]
        public bool AutoSyncSharedData
        {
            get => _autoSyncSharedData;
            set
            {
                if (value)
                {
                    SharedData.OnItemAdded += RegisterSharedDataHandler;
                    SharedData.OnItemUpdated += RegisterSharedDataHandler;
                    SharedData.OnItemRemoved += RegisterSharedDataHandler;
                }
                else
                {
                    SharedData.OnItemAdded -= RegisterSharedDataHandler;
                    SharedData.OnItemUpdated -= RegisterSharedDataHandler;
                    SharedData.OnItemRemoved -= RegisterSharedDataHandler;
                }

                _autoSyncSharedData = value;
            }
        }

        /// <summary>
        /// Gets the shared data that is synchronized across the network if <see cref="AutoSyncSharedData"/> is enabled.
        /// </summary>
        [MemoryPackIgnore, JsonProperty("SharedData")]
        public ObservableDictionary<string, object> SharedData { get; internal set; } = new();

        /// <summary>
        /// Gets the dictionary of peers currently registered in the group.
        /// </summary>
        /// <exception cref="Exception">Thrown if called in name builder mode.</exception>
        [MemoryPackIgnore]
        public Dictionary<int, NetworkPeer> Peers
        {
            get
            {
                if (isNameBuilder)
                {
                    throw new Exception("Peers: Cannot get peers from name builder mode.");
                }

                return _peersById;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the group should be destroyed when it becomes empty.
        /// </summary>
        [MemoryPackIgnore]
        public bool DestroyWhenEmpty { get; set; } = true;

        /// <summary>
        /// Gets a value indicating whether this group is a sub-group.
        /// </summary>
        [MemoryPackIgnore]
        public bool IsSubGroup => Depth != 1;

        /// <summary>
        /// Gets or sets a value indicating whether messages can be sent across groups.
        /// </summary>
        [MemoryPackIgnore]
        public bool AllowAcrossGroupMessage { get; set; } = true;

        /// <summary>
        /// Gets the depth of the group in the hierarchy. 
        /// The root group has a depth of 1.
        /// </summary>
        [MemoryPackIgnore]
        public int Depth { get; }

        [MemoryPackConstructor]
        [JsonConstructor]
        internal NetworkGroup()
        {
        }

        public NetworkGroup(string groupName)
        {
            string[] subGroups = groupName.Split("->");

            Identifier = groupName;
            Depth = subGroups.Length;
            Name = subGroups[Depth - 1];
            isNameBuilder = true;
        }

        internal NetworkGroup(int groupId, string groupName, bool isServer)
        {
            string[] subGroups = groupName.Split("->");

            Id = groupId;
            Identifier = groupName;
            Depth = subGroups.Length;
            Name = subGroups[Depth - 1];
            isNameBuilder = false;

            if (isServer)
            {
                AutoSyncSharedData = true;
            }
        }

        private void RegisterSharedDataHandler(string key, object item)
        {
            SyncSharedData(key);
        }

        /// <summary>
        /// Retrieves a peer by its ID from the current group.
        /// </summary>
        /// <param name="peerId">The ID of the peer to retrieve.</param>
        /// <returns>The <see cref="NetworkPeer"/> instance if found; otherwise, null.</returns>
        public NetworkPeer GetPeer(int peerId)
        {
            EnsureServerActive();
            if (isNameBuilder)
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

        /// <summary>
        /// Attempts to get a peer by its ID from the current group.
        /// </summary>
        /// <param name="peerId">The ID of the peer to retrieve.</param>
        /// <param name="peer">The retrieved peer, if found.</param>
        /// <returns>True if the peer was found; otherwise, false.</returns>
        public bool TryGetPeer(int peerId, out NetworkPeer peer)
        {
            EnsureServerActive();
            if (isNameBuilder)
            {
                throw new Exception("GetPeer: Cannot get peer from name builder mode.");
            }

            return _peersById.TryGetValue(peerId, out peer);
        }

        /// <summary>
        /// Adds a sub-group to the current group.
        /// </summary>
        /// <param name="subGroupName">The name of the sub-group to add.</param>
        /// <returns>The newly created sub-group.</returns>
        public NetworkGroup AddSubGroup(string subGroupName)
        {
            string newIdentifier = $"{Identifier}->{subGroupName}";
            return !isNameBuilder ? Matchmaking.Server.AddGroup(newIdentifier) : new NetworkGroup(newIdentifier);
        }

        /// <summary>
        /// Attempts to add a sub-group to the current group.
        /// </summary>
        /// <param name="subGroupName">The name of the sub-group.</param>
        /// <param name="subGroup">The resulting sub-group, if successful.</param>
        /// <returns>True if the sub-group was added successfully; otherwise, false.</returns>
        public bool TryAddSubGroup(string subGroupName, out NetworkGroup subGroup)
        {
            string newIdentifier = $"{Identifier}->{subGroupName}";
            if (!isNameBuilder)
            {
                return Matchmaking.Server.TryAddGroup(newIdentifier, out subGroup);
            }

            subGroup = new NetworkGroup(newIdentifier);
            return true;
        }

        /// <summary>
        /// Sets the master client (owner) of the group.
        /// </summary>
        /// <param name="peer">The peer to set as the master client.</param>
        public void SetMasterClient(NetworkPeer peer)
        {
            EnsureServerActive();
            MasterClient = peer;
        }

        public void SyncSharedData(ServerOptions options)
        {
            SyncSharedData(options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                options.SequenceChannel);
        }

        public void SyncSharedData(Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            SyncSharedData(NetworkConstants.SHARED_ALL_KEYS, target, deliveryMode, groupId, dataCache,
                sequenceChannel);
        }

        public void SyncSharedData(string key, ServerOptions options)
        {
            SyncSharedData(key, options.Target, options.DeliveryMode, options.GroupId, options.DataCache,
                options.SequenceChannel);
        }

        public void SyncSharedData(string key, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            Internal_SyncSharedData(key, target, deliveryMode, groupId, dataCache, sequenceChannel);
        }

        private void Internal_SyncSharedData(string key, Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default,
            byte sequenceChannel = 0)
        {
            dataCache ??= DataCache.None;
            EnsureServerActive();

            if (MasterClient == null)
            {
                throw new Exception(
                    "MasterClientId is not set. Please set it before using this method."
                );
            }

            if (SharedData.TryGetValue(key, out object value) || key == NetworkConstants.SHARED_ALL_KEYS)
            {
                value = key != NetworkConstants.SHARED_ALL_KEYS ? value : SharedData;
                ImmutableKeyValuePair keyValuePair = new(key, value);
                using var message = Pool.Rent();
                message.Write(Id);
                message.WriteAsJson(keyValuePair);
                ServerSide.SendMessage(MessageType.SyncGroupSharedData, MasterClient, message, target, deliveryMode,
                    groupId, dataCache, sequenceChannel);
            }
            else
            {
                NetworkLogger.__Log__(
                    $"SyncSharedData Group Error: Failed to sync '{key}' because it doesn't exist.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        /// <summary>
        /// Removes a cache from the group based on the provided data cache.
        /// </summary>
        /// <param name="dataCache">The data cache to remove.</param>
        public void RemoveCache(DataCache dataCache)
        {
            EnsureServerActive();
            switch (dataCache.Mode)
            {
                case CacheMode.Group | CacheMode.New:
                case CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy:
                    AppendCaches.RemoveAll(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id);
                    break;
                case CacheMode.Group | CacheMode.Overwrite:
                case CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy:
                    OverwriteCaches.Remove(dataCache.Id);
                    break;
                default:
                    NetworkLogger.__Log__(
                        "Delete Cache Error: Unsupported cache mode set.",
                        NetworkLogger.LogType.Error
                    );
                    break;
            }
        }

        /// <summary>
        /// Removes the specified cache from the group for the specified network peer.
        /// </summary>
        /// <param name="dataCache">The cache to remove.</param>
        /// <param name="peer">The network peer from which to remove the cache.</param>
        public void RemoveCacheFrom(DataCache dataCache, NetworkPeer peer)
        {
            EnsureServerActive();
            switch (dataCache.Mode)
            {
                case CacheMode.Group | CacheMode.New:
                case CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy:
                    AppendCaches.RemoveAll(x =>
                        x.Mode == dataCache.Mode && x.Id == dataCache.Id && x.Peer.Id == peer.Id
                    );
                    break;
                case CacheMode.Group | CacheMode.Overwrite:
                case CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy:
                    OverwriteCaches.Remove(dataCache.Id);
                    break;
                default:
                    NetworkLogger.__Log__(
                        "Delete Cache Error: Unsupported cache mode set.",
                        NetworkLogger.LogType.Error
                    );
                    break;
            }
        }

        /// <summary>
        /// Removes all caches associated with the specified network peer from the group.
        /// </summary>
        /// <param name="peer">The network peer from which to remove all caches.</param>
        public void RemoveAllCachesFrom(NetworkPeer peer)
        {
            EnsureServerActive();
            AppendCaches.RemoveAll(x => x.Peer.Id == peer.Id);
            var caches = OverwriteCaches.Values.Where(x => x.Peer.Id == peer.Id).ToList();

            foreach (var cache in caches)
            {
                if (!OverwriteCaches.Remove(cache.Id))
                {
                    NetworkLogger.__Log__(
                        $"RemoveAllCachesFrom: Failed to remove cache {cache.Id} from peer {peer.Id}.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
        }

        internal void Internal_RemoveAllCachesFrom(NetworkPeer peer)
        {
            EnsureServerActive();
            AppendCaches.RemoveAll(x => x.Peer.Id == peer.Id && x.AutoDestroyCache);
            var caches = OverwriteCaches
                .Values.Where(x => x.Peer.Id == peer.Id && x.AutoDestroyCache)
                .ToList();

            foreach (var cache in caches)
            {
                if (!OverwriteCaches.Remove(cache.Id))
                {
                    NetworkLogger.__Log__(
                        $"InternalRemoveAllCachesFrom: Failed to remove cache {cache.Id} from peer {peer.Id}.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
        }

        /// <summary>
        /// Clears all peers associated with this group.
        /// </summary>
        public void ClearAllPeers()
        {
            EnsureServerActive();
            _peersById.Clear();
        }

        /// <summary>
        /// Clears the data collections associated with this group. This includes
        /// the <see cref="Data"/> and <see cref="SharedData"/> collections.
        /// </summary>
        public void ResetDataCollections()
        {
            EnsureServerActive();
            Data.Clear();
            SharedData.Clear();
        }

        /// <summary>
        /// Clears the cache collections associated with this group, including
        /// the append and overwrite caches.
        /// </summary>
        public void ResetCacheCollections()
        {
            EnsureServerActive();
            AppendCaches.Clear();
            OverwriteCaches.Clear();
        }

        [Conditional("OMNI_DEBUG")]
        private void EnsureServerActive([CallerMemberName] string caller = "")
        {
            if (!IsServerActive)
            {
                throw new NotSupportedException($"[{caller}] -> Can't use this method on client.");
            }
        }

        /// <summary>
        /// Converts the <see cref="NetworkGroup"/> instance to its equivalent string representation.
        /// </summary>
        /// <returns>A string that represents the value of this instance.</returns>
        public override string ToString()
        {
            object peer = new
            {
                Id,
                Identifier,
                Name,
                MainGroup = MainGroup?.Name,
                Data,
                SharedData,
                PeerCount,
                IsSubGroup
            };

            return NetworkManager.ToJson(peer);
        }


        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="NetworkGroup"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="NetworkGroup"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the current <see cref="NetworkGroup"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            NetworkGroup other = (NetworkGroup)obj;
            return other != null && Id == other.Id;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current <see cref="NetworkGroup"/>.</returns>
        /// <remarks>
        /// This method is used to generate a hash code of the current <see cref="NetworkGroup"/> instance.
        /// </remarks>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="NetworkGroup"/> is equal to the current <see cref="NetworkGroup"/>.
        /// </summary>
        /// <param name="other">The <see cref="NetworkGroup"/> to compare with the current <see cref="NetworkGroup"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="NetworkGroup"/> is equal to the current <see cref="NetworkGroup"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(NetworkGroup other)
        {
            return other != null && Id == other.Id;
        }
    }
}