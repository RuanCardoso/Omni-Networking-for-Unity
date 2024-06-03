using System;
using System.Collections.Generic;
using System.Linq;

namespace Omni.Core.Modules.Matchmaking
{
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

            public event Action<NetworkPeer, string> OnPlayerFailedJoinGroup
            {
                add => NetworkManager.OnPlayerFailedJoinGroup += value;
                remove => NetworkManager.OnPlayerFailedJoinGroup -= value;
            }

            public event Action<NetworkPeer, string> OnPlayerFailedLeaveGroup
            {
                add => NetworkManager.OnPlayerFailedLeaveGroup += value;
                remove => NetworkManager.OnPlayerFailedLeaveGroup -= value;
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

            public NetworkGroup[] GetGroups(int depth)
            {
                return Groups.Values.Where(x => x.Depth == depth).ToArray();
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
