using System;
using System.Collections.Generic;
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
                throw new Exception("Cannot get peers from name builder.");
            }

            return _peersById;
        }

        public NetworkPeer GetPeerById(int peerId)
        {
            if (__namebuilder__)
            {
                throw new Exception("Cannot get peer from name builder.");
            }

            if (_peersById.TryGetValue(peerId, out var peer))
            {
                return peer;
            }

            NetworkLogger.__Log__(
                $"Group: Peer with ID {peerId}:{Id} not found.",
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

        public override string ToString()
        {
            return NetworkManager.ToJson(this);
        }
    }

    public class NetworkMatchmaking
    {
        public MatchClient Client { get; private set; }
        public MatchServer Server { get; private set; }

        internal void Initialize()
        {
            Client = new MatchClient();
            Server = new MatchServer();
        }

        public class MatchClient
        {
            public event Action<string, NetworkBuffer> OnJoinedGroup
            {
                add => NetworkManager.OnJoinedGroup += value;
                remove => NetworkManager.OnJoinedGroup -= value;
            }

            public event Action<string, string> OnLeftGroup
            {
                add => NetworkManager.OnLeftGroup += value;
                remove => NetworkManager.OnLeftGroup -= value;
            }

            public void JoinGroup(string groupName, NetworkBuffer buffer = null)
            {
                NetworkManager.Client.JoinGroup(groupName, buffer);
            }

            public void LeaveGroup(string groupName)
            {
                NetworkManager.Client.LeaveGroup(groupName);
            }
        }

        public class MatchServer
        {
            public event Action<NetworkBuffer, NetworkGroup, NetworkPeer> OnPlayerJoinedGroup
            {
                add => NetworkManager.OnPlayerJoinedGroup += value;
                remove => NetworkManager.OnPlayerJoinedGroup -= value;
            }

            public event Action<NetworkGroup, NetworkPeer, string> OnPlayerLeftGroup
            {
                add => NetworkManager.OnPlayerLeftGroup += value;
                remove => NetworkManager.OnPlayerLeftGroup -= value;
            }

            public Dictionary<int, NetworkGroup> Groups => NetworkManager.Server.GetGroups();

            public NetworkGroup AddGroup(string groupName)
            {
                return NetworkManager.Server.AddGroup(groupName);
            }

            public void JoinGroup(NetworkGroup group, NetworkPeer peer)
            {
                NetworkManager.Server.JoinGroup(group.Name, NetworkBuffer.Empty, peer, false);
            }

            public void JoinGroup(NetworkGroup group, NetworkBuffer buffer, NetworkPeer peer)
            {
                buffer ??= NetworkBuffer.Empty;
                NetworkManager.Server.JoinGroup(group.Name, buffer, peer, true);
            }

            public void LeaveGroup(
                NetworkGroup group,
                NetworkPeer peer,
                string reason = "Leave event called by server."
            )
            {
                NetworkManager.Server.LeaveGroup(group.Name, reason, peer);
            }

            public NetworkGroup GetGroup(string groupName)
            {
                int groupId = NetworkManager.Server.GetGroupIdByName(groupName);
                return GetGroup(groupId);
            }

            public NetworkGroup GetGroup(int groupId)
            {
                return NetworkManager.Server.GetGroupById(groupId);
            }
        }
    }
}
