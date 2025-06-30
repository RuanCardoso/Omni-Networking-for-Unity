using MemoryPack;
using Newtonsoft.Json;
using Omni.Shared;
using Omni.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using static Omni.Core.NetworkManager;

#pragma warning disable

namespace Omni.Core
{
    /// <summary>
    /// Represents a network peer that facilitates communication and data synchronization between the client and server in a networked environment.
    /// This class handles peer data, group memberships, and shared data synchronization 
    /// between clients and the server.
    /// </summary> 
    [JsonObject(MemberSerialization.OptIn)]
    [MemoryPackable(GenerateType.NoGenerate)]
    public partial class NetworkPeer : IEquatable<NetworkPeer>
    {
        [MemoryPackIgnore] internal byte[] _aesKey;
        [MemoryPackIgnore] internal NativePeer _nativePeer;
        [MemoryPackIgnore] internal Dictionary<int, NetworkGroup> _groups = new();

        [MemoryPackIgnore] private readonly string __endpoint__;
        [MemoryPackIgnore] private bool _autoSyncSharedData;

        /// <summary>
        /// Gets the endpoint (IP and port) associated with this peer.
        /// </summary>
        [MemoryPackIgnore]
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// Gets the unique identifier for this peer, assigned by the server.
        /// </summary>
        [MemoryPackInclude, JsonProperty("Id")]
        public int Id { get; }

        /// <summary>
        /// Gets or sets the main network group that this peer belongs to.
        /// </summary>
        [MemoryPackIgnore]
        public NetworkGroup MainGroup { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the peer is currently connected to the network.
        /// </summary>
        [MemoryPackIgnore]
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the peer has been successfully connected and authenticated.
        /// </summary>
        [MemoryPackIgnore]
        public bool IsAuthenticated { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the peer belongs to any network group.
        /// </summary>
        [MemoryPackIgnore]
        public bool IsInAnyGroup => _groups.Count > 0;

        /// <summary>
        /// Gets a dictionary of non-synchronized data available exclusively on the server-side.
        /// This data is not sent to clients.
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
        /// Gets or sets a dictionary of shared data that is synchronized between the server and clients.
        /// Changes to this data are propagated to all connected peers if <see cref="AutoSyncSharedData"/> is enabled. 
        /// </summary>
        [MemoryPackIgnore, JsonProperty("SharedData")]
        public ObservableDictionary<string, object> SharedData { get; internal set; } = new();

        /// <summary>
        /// Gets the current network time for this peer.
        /// </summary>
        // [MemoryPackIgnore]
        // public double Time => _nativePeer.Time;

        /// <summary>
        /// Gets the current network ping (latency) for this peer in milliseconds.
        /// </summary>
        //[MemoryPackIgnore]
        //public double Ping => _nativePeer.Ping;

        [MemoryPackConstructor]
        [JsonConstructor]
        internal NetworkPeer()
        {
        }

        internal NetworkPeer(IPEndPoint endPoint, int id, bool isServer)
        {
            // Avoid self reference loop when serializing.
            __endpoint__ = endPoint.ToString();

            // Parameters:
            // - endPoint: The IPEndPoint of the peer.
            // - id: The ID of the peer used in server-side.
            EndPoint = endPoint;
            Id = id;

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
        /// Clears all network groups associated with this peer.
        /// </summary>
        public void ClearAllGroups()
        {
            EnsureServerActive();
            _groups.Clear();
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
        /// Disconnects the current network peer from the server.
        /// </summary>
        public void Disconnect()
        {
            EnsureServerActive();
            DisconnectPeer(this);
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

            if (SharedData.TryGetValue(key, out object value) || key == NetworkConstants.k_ShareAllKeys)
            {
                value = key != NetworkConstants.k_ShareAllKeys ? value : SharedData;
                ImmutableKeyValuePair keyValuePair = new(key, value);
                using var message = Pool.Rent(enableTracking: false);
                message.Write(Id);
                message.WriteAsJson(keyValuePair);
                ServerSide.SendMessage(NetworkPacketType.k_SyncPeerSharedData, this, message);
            }
            else
            {
                NetworkLogger.__Log__(
                    $"SyncSharedData Peer Error: Failed to sync '{key}' because it doesn't exist.",
                    NetworkLogger.LogType.Error
                );
            }
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
        /// Converts the <see cref="NetworkPeer"/> instance to its equivalent string representation.
        /// </summary>
        /// <returns>A string that represents the value of this instance.</returns>
        public override string ToString()
        {
            object peer = new
            {
                Id,
                EndPoint = __endpoint__,
                MainGroup = MainGroup?.Name,
                Data,
                SharedData,
            };

            return NetworkManager.ToJson(peer);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="NetworkPeer"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="NetworkPeer"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the current <see cref="NetworkPeer"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is used to compare the current instance with another object.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            NetworkPeer other = obj as NetworkPeer;
            return other != null && Id == other.Id;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current <see cref="NetworkPeer"/>.</returns>
        /// <remarks>
        /// This method is used to generate a hash code of the current <see cref="NetworkPeer"/> instance.
        /// </remarks>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="NetworkPeer"/> is equal to the current <see cref="NetworkPeer"/>.
        /// </summary>
        /// <param name="other">The object to compare with the current <see cref="NetworkPeer"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="NetworkPeer"/> is equal to the current <see cref="NetworkPeer"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(NetworkPeer other)
        {
            return other != null && Id == other.Id;
        }
    }
}