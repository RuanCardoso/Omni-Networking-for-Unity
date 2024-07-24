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
            /// <summary>
            /// Gets a dictionary containing all the <see cref="NetworkGroup"/> objects on the client.
            /// </summary>
            /// <remarks>
            /// The key is the unique identifier of each group.
            /// </remarks>
            public Dictionary<int, NetworkGroup> Groups => NetworkManager.Client.Groups;

            /// <summary>
            /// Event triggered when a client successfully joins a group on the server.
            /// </summary>
            /// <remarks>
            /// The first parameter is the name of the group that the client has joined.
            /// The second parameter is an optional <see cref="DataBuffer"/> containing additional data.
            /// </remarks>
            public event Action<string, DataBuffer> OnJoinedGroup
            {
                add => NetworkManager.OnJoinedGroup += value;
                remove => NetworkManager.OnJoinedGroup -= value;
            }

            /// <summary>
            /// Event triggered when a client leaves a group on the server.
            /// </summary>
            /// <remarks>
            /// The first parameter is the name of the group that the client has left.
            /// The second parameter is a string containing additional information, if any.
            /// </remarks>
            public event Action<string, string> OnLeftGroup
            {
                add => NetworkManager.OnLeftGroup += value;
                remove => NetworkManager.OnLeftGroup -= value;
            }

            /// <summary>
            /// Joins a specified group on the server.
            /// </summary>
            /// <param name="groupName">The name of the group to join.</param>
            /// <param name="buffer">
            /// An optional <see cref="DataBuffer"/> containing data to be sent to the group upon joining.
            /// </param>
            /// <remarks>
            /// This method allows a client to join a specified group on the server and optionally send additional data to the group.
            /// </remarks>
            public void JoinGroup(string groupName, DataBuffer buffer = null)
            {
                NetworkManager.Client.JoinGroup(groupName, buffer);
            }

            /// <summary>
            /// Leaves a specified group on the server.
            /// </summary>
            /// <param name="groupName">The name of the group to leave.</param>
            /// <remarks>
            /// This method allows a client to leave a specified group on the server.
            /// </remarks>
            public void LeaveGroup(string groupName)
            {
                NetworkManager.Client.LeaveGroup(groupName);
            }
        }

        public class MatchServer
        {
            /// <summary>
            /// Event that is raised when a player has successfully joined a group on the server.
            /// </summary>
            /// <remarks>
            /// The first parameter is a <see cref="DataBuffer"/> containing any data sent by the player upon joining.
            /// The second parameter is the <see cref="NetworkGroup"/> that the player has joined.
            /// The third parameter is the <see cref="NetworkPeer"/> object representing the player.
            /// </remarks>
            public event Action<DataBuffer, NetworkGroup, NetworkPeer> OnPlayerJoinedGroup
            {
                add => NetworkManager.OnPlayerJoinedGroup += value;
                remove => NetworkManager.OnPlayerJoinedGroup -= value;
            }

            /// <summary>
            /// Event that is raised when a player has left a group on the server.
            /// </summary>
            /// <remarks>
            /// The first parameter is the <see cref="NetworkGroup"/> that the player has left.
            /// The second parameter is the <see cref="NetworkPeer"/> object representing the player.
            /// The third parameter is a string containing additional information, if any.
            /// </remarks>
            public event Action<NetworkGroup, NetworkPeer, Phase, string> OnPlayerLeftGroup
            {
                add => NetworkManager.OnPlayerLeftGroup += value;
                remove => NetworkManager.OnPlayerLeftGroup -= value;
            }

            /// <summary>
            /// Event that is raised when a player has failed to join a group on the server.
            /// </summary>
            /// <remarks>
            /// The first parameter is the <see cref="NetworkPeer"/> object representing the player.
            /// The second parameter is a string containing the reason for the failure.
            /// </remarks>
            public event Action<NetworkPeer, string> OnPlayerFailedJoinGroup
            {
                add => NetworkManager.OnPlayerFailedJoinGroup += value;
                remove => NetworkManager.OnPlayerFailedJoinGroup -= value;
            }

            /// <summary>
            /// Event that is raised when a player has failed to leave a group on the server.
            /// </summary>
            /// <remarks>
            /// The first parameter is the <see cref="NetworkPeer"/> object representing the player.
            /// The second parameter is a string containing the reason for the failure.
            /// </remarks>
            public event Action<NetworkPeer, string> OnPlayerFailedLeaveGroup
            {
                add => NetworkManager.OnPlayerFailedLeaveGroup += value;
                remove => NetworkManager.OnPlayerFailedLeaveGroup -= value;
            }

            /// <summary>
            /// Gets a dictionary containing all the <see cref="NetworkGroup"/> objects on the server.
            /// </summary>
            /// <remarks>
            /// The key is the unique identifier of each group.
            /// </remarks>
            public Dictionary<int, NetworkGroup> Groups => NetworkManager.Server.Groups;

            /// <summary>
            /// Adds a new group to the server.
            /// </summary>
            /// <param name="groupName">The name of the group to add.</param>
            /// <returns>The newly created <see cref="NetworkGroup"/> object.</returns>
            /// <remarks>
            /// This method creates a new group on the server with the specified name and returns the created <see cref="NetworkGroup"/> object.
            /// </remarks>
            public NetworkGroup AddGroup(string groupName)
            {
                return NetworkManager.Server.AddGroup(groupName);
            }

            /// <summary>
            /// Adds a new group to the server.
            /// </summary>
            /// <param name="groupName">The name of the group to add.</param>
            /// <returns>The newly created <see cref="NetworkGroup"/> object.</returns>
            /// <remarks>
            /// This method creates a new group on the server with the specified name and returns the created <see cref="NetworkGroup"/> object.
            /// </remarks>
            public bool TryAddGroup(string groupName, out NetworkGroup group)
            {
                return NetworkManager.Server.TryAddGroup(groupName, out group);
            }

            /// <summary>
            /// Joins a specified group on the server.
            /// </summary>
            /// <param name="group">The <see cref="NetworkGroup"/> object to join.</param>
            /// <param name="peer">The <see cref="NetworkPeer"/> object representing the player.</param>
            /// <remarks>
            /// This method allows a player represented by a <see cref="NetworkPeer"/> object to join a specified <see cref="NetworkGroup"/> on the server.
            /// </remarks>
            public void JoinGroup(NetworkGroup group, NetworkPeer peer)
            {
                NetworkManager.Server.JoinGroup(group.Identifier, DataBuffer.Empty, peer, false);
            }

            /// <summary>
            /// Joins a specified group on the server and sends additional data to the group.
            /// </summary>
            /// <param name="group">The <see cref="NetworkGroup"/> object to join.</param>
            /// <param name="buffer">An optional <see cref="DataBuffer"/> containing data to be sent to the group upon joining.</param>
            /// <param name="peer">The <see cref="NetworkPeer"/> object representing the player.</param>
            /// <remarks>
            /// This method allows a player represented by a <see cref="NetworkPeer"/> object to join a specified <see cref="NetworkGroup"/> on the server,
            /// optionally sending additional data contained in a <see cref="DataBuffer"/> to the group upon joining.
            /// </remarks>
            public void JoinGroup(NetworkGroup group, DataBuffer buffer, NetworkPeer peer)
            {
                buffer ??= DataBuffer.Empty;
                NetworkManager.Server.JoinGroup(group.Identifier, buffer, peer, true);
            }

            /// <summary>
            /// Leaves a specified group on the server.
            /// </summary>
            /// <param name="group">The <see cref="NetworkGroup"/> object to leave.</param>
            /// <param name="peer">The <see cref="NetworkPeer"/> object representing the player.</param>
            /// <param name="reason">An optional string containing additional information, if any.</param>
            /// <remarks>
            /// This method allows a player represented by a <see cref="NetworkPeer"/> object to leave a specified <see cref="NetworkGroup"/> on the server.
            /// The optional reason parameter can provide additional context for why the player is leaving the group.
            /// </remarks>
            public void LeaveGroup(
                NetworkGroup group,
                NetworkPeer peer,
                string reason = "Leave event called by server."
            )
            {
                NetworkManager.Server.LeaveGroup(group.Identifier, reason, peer);
            }

            /// <summary>
            /// Retrieves the ID of the group associated with the specified group name.
            /// </summary>
            /// <param name="groupName">The name of the group.</param>
            /// <returns>The ID of the group corresponding to the specified group name.</returns>
            public int GetGroupId(string groupName)
            {
                return NetworkManager.Server.GetGroupIdByName(groupName);
            }

            /// <summary>
            /// Filters the groups based on the specified depth.
            /// </summary>
            /// <param name="depth">The depth of the groups to filter.</param>
            /// <returns>An array of <see cref="NetworkGroup"/> objects that match the specified depth.</returns>
            public NetworkGroup[] FilterGroupsByDepth(int depth)
            {
                return Groups.Values.Where(x => x.Depth == depth).ToArray();
            }

            /// <summary>
            /// Retrieves the <see cref="NetworkGroup"/> object associated with the specified group name.
            /// </summary>
            /// <param name="groupName">The name of the group.</param>
            /// <returns>The <see cref="NetworkGroup"/> object corresponding to the specified group name.</returns>
            /// <remarks>
            /// This method first retrieves the group ID associated with the specified group name and then
            /// returns the corresponding <see cref="NetworkGroup"/> object.
            /// </remarks>
            public NetworkGroup GetGroup(string groupName)
            {
                int groupId = GetGroupId(groupName);
                return GetGroup(groupId);
            }

            /// <summary>
            /// Retrieves the <see cref="NetworkGroup"/> object associated with the specified group name.
            /// </summary>
            /// <param name="groupName">The name of the group.</param>
            /// <returns>The <see cref="NetworkGroup"/> object corresponding to the specified group name.</returns>
            /// <remarks>
            /// This method first retrieves the group ID associated with the specified group name and then
            /// returns the corresponding <see cref="NetworkGroup"/> object.
            /// </remarks>
            public bool TryGetGroup(string groupName, out NetworkGroup group)
            {
                int groupId = GetGroupId(groupName);
                return TryGetGroup(groupId, out group);
            }

            /// <summary>
            /// Retrieves the <see cref="NetworkGroup"/> object associated with the specified group ID.
            /// </summary>
            /// <param name="groupId">The ID of the group.</param>
            /// <returns>The <see cref="NetworkGroup"/> object corresponding to the specified group ID.</returns>
            public NetworkGroup GetGroup(int groupId)
            {
                return NetworkManager.Server.GetGroupById(groupId);
            }

            /// <summary>
            /// Retrieves the <see cref="NetworkGroup"/> object associated with the specified group ID.
            /// </summary>
            /// <param name="groupId">The ID of the group.</param>
            /// <returns>The <see cref="NetworkGroup"/> object corresponding to the specified group ID.</returns>
            public bool TryGetGroup(int groupId, out NetworkGroup group)
            {
                return NetworkManager.Server.TryGetGroupById(groupId, out group);
            }

            /// <summary>
            /// Checks if a group with the specified name exists on the server.
            /// </summary>
            /// <param name="groupName">The name of the group to check for existence.</param>
            /// <returns>
            /// <c>true</c> if a group with the specified name exists; otherwise, <c>false</c>.
            /// </returns>
            /// <remarks>
            /// This method retrieves the group ID associated with the specified group name and checks if the group exists in the server's group collection.
            /// </remarks>
            public bool HasGroup(string groupName)
            {
                int groupId = GetGroupId(groupName);
                return Groups.ContainsKey(groupId);
            }
        }
    }
}
