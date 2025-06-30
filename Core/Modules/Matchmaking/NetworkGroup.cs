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
    public enum SearchMode
    {
        DepthFirst,
        BreadthFirst
    }

    /// <summary>
    /// Represents a network group that manages a collection of peers and shared data.
    /// It provides functionality for group management, data synchronization, and sub-group handling.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [MemoryPackable]
    [Serializable]
    public partial class NetworkGroup : IEquatable<NetworkGroup>
    {
        [MemoryPackIgnore]
        public static NetworkGroup None { get; } = new(0, "None", false);

        [MemoryPackIgnore] private readonly bool isNameBuilder;
        [MemoryPackIgnore] private bool _autoSyncSharedData;

        [MemoryPackIgnore] internal readonly Dictionary<int, NetworkPeer> _peersById = new();
        [MemoryPackIgnore] internal readonly List<NetworkGroup> _subGroups = new();

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
        public NetworkGroup Parent { get; internal set; }

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
                    throw new InvalidOperationException(
                        "Cannot retrieve peers in name builder mode. This NetworkGroup instance was created as a template " +
                        "using the name-based constructor and doesn't contain actual peer data. Use a fully initialized " +
                        "NetworkGroup created through Matchmaking.Server.AddGroup() instead."
                    );
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
                throw new InvalidOperationException(
                    "Cannot retrieve peers in name builder mode. This NetworkGroup instance was created as a template " +
                    "using the name-based constructor and doesn't contain actual peer data. Use a fully initialized " +
                    "NetworkGroup created through Matchmaking.Server.AddGroup() instead."
                );
            }

            if (_peersById.TryGetValue(peerId, out var peer))
            {
                return peer;
            }

            NetworkLogger.__Log__(
                $"[NetworkGroup {Id}:{Name}] Peer with ID {peerId} not found in group. This could happen if:" +
                $"\n- The peer has disconnected" +
                $"\n- The peer never joined this group" +
                $"\n- The peer ID is incorrect" +
                $"\n- The group has been cleared or reset" +
                $"\nPeer count in group: {PeerCount}, Group Name: '{Name}'",
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
                throw new InvalidOperationException(
                    "Cannot retrieve peers in name builder mode. This NetworkGroup instance was created as a template " +
                    "using the name-based constructor and doesn't contain actual peer data. Use a fully initialized " +
                    "NetworkGroup created through Matchmaking.Server.AddGroup() instead."
                );
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
            if (!isNameBuilder)
            {
                var nextGroup = Matchmaking.Server.AddGroup(newIdentifier);
                nextGroup.Parent = this;
                _subGroups.Add(nextGroup);
                return nextGroup;
            }
            else
            {
                var ng = new NetworkGroup(newIdentifier);
                ng.Parent = this; // Adiciona referência ao pai
                return ng;
            }
        }

        /// <summary>
        /// Attempts to add a sub-group to the current group.
        /// </summary>
        /// <param name="subGroupName">The name of the sub-group.</param>
        /// <param name="nextGroup">The resulting sub-group, if successful.</param>
        /// <returns>True if the sub-group was added successfully; otherwise, false.</returns>
        public bool TryAddSubGroup(string subGroupName, out NetworkGroup nextGroup)
        {
            string newIdentifier = $"{Identifier}->{subGroupName}";
            if (!isNameBuilder)
            {
                bool success = Matchmaking.Server.TryAddGroup(newIdentifier, out nextGroup);
                if (success)
                {
                    nextGroup.Parent = this;
                    _subGroups.Add(nextGroup);
                }
                return success;
            }

            nextGroup = new NetworkGroup(newIdentifier);
            nextGroup.Parent = this;
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

        public void SyncSharedData()
        {
            SyncSharedData(NetworkConstants.k_ShareAllKeys);
        }

        public void SyncSharedData(string key)
        {
            Internal_SyncSharedData(key);
        }

        private void Internal_SyncSharedData(string key)
        {
            EnsureServerActive();

            if (MasterClient == null)
            {
                throw new InvalidOperationException(
                    $"[NetworkGroup {Id}:{Name}] MasterClient is not set for this group. " +
                    $"Please call SetMasterClient() before attempting to synchronize shared data. " +
                    $"The MasterClient is required to determine the origin of synchronized data."
                );
            }

            if (SharedData.TryGetValue(key, out object value) || key == NetworkConstants.k_ShareAllKeys)
            {
                value = key != NetworkConstants.k_ShareAllKeys ? value : SharedData;
                ImmutableKeyValuePair keyValuePair = new(key, value);
                using var message = Pool.Rent(enableTracking: false);
                message.Write(Id);
                message.WriteAsJson(keyValuePair);
                ServerSide.SendMessage(NetworkPacketType.k_SyncGroupSharedData, MasterClient, message);
            }
            else
            {
                NetworkLogger.__Log__(
                    $"SyncSharedData Group Error: Failed to sync '{key}' because it doesn't exist.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        public NetworkGroup GetRoot()
        {
            var current = this;
            while (current.Parent != null)
                current = current.Parent;
            return current;
        }

        public bool TryGetNextGroup(out NetworkGroup nextGroup, SearchMode mode = SearchMode.DepthFirst)
        {
            var root = GetRoot();
            var list = new List<NetworkGroup>();
            LinearizeGroups(root, mode, list);

            int idx = list.IndexOf(this);
            if (idx >= 0 && idx < list.Count - 1)
            {
                nextGroup = list[idx + 1];
                return true;
            }
            nextGroup = null;
            return false;
        }

        public bool TryGetPreviousGroup(out NetworkGroup previousGroup, SearchMode mode = SearchMode.DepthFirst)
        {
            var root = GetRoot();
            var list = new List<NetworkGroup>();
            LinearizeGroups(root, mode, list);

            int idx = list.IndexOf(this);
            if (idx > 0)
            {
                previousGroup = list[idx - 1];
                return true;
            }
            previousGroup = null;
            return false;
        }

        private static void LinearizeGroups(NetworkGroup root, SearchMode mode, List<NetworkGroup> output)
        {
            if (mode == SearchMode.DepthFirst)
            {
                output.Add(root);
                foreach (var sub in root._subGroups)
                    LinearizeGroups(sub, mode, output);
            }
            else // BreadthFirst
            {
                var queue = new Queue<NetworkGroup>();
                queue.Enqueue(root);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    output.Add(current);
                    foreach (var sub in current._subGroups)
                        queue.Enqueue(sub);
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
                MainGroup = Parent?.Name,
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
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj is NetworkGroup group)
                return Id == group.Id;

            return false;
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