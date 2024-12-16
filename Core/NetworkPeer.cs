using MemoryPack;
using Newtonsoft.Json;
using Omni.Shared;
using Omni.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    [MemoryPackable]
    public partial class NetworkPeer : IEquatable<NetworkPeer>
    {
        [MemoryPackIgnore] internal byte[] _aesKey;
        [MemoryPackIgnore] internal NativePeer _nativePeer;
        [MemoryPackIgnore] internal Dictionary<int, NetworkGroup> _groups = new();

        [MemoryPackIgnore] internal List<NetworkCache> AppendCaches { get; } = new();
        [MemoryPackIgnore] internal Dictionary<int, NetworkCache> OverwriteCaches { get; } = new();

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
        [MemoryPackIgnore]
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
        [MemoryPackIgnore]
        public double Time => _nativePeer.Time;

        /// <summary>
        /// Gets the current network ping (latency) for this peer in milliseconds.
        /// </summary>
        [MemoryPackIgnore]
        public double Ping => _nativePeer.Ping;

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

        public void ClearGroups()
        {
            EnsureServerActive();
            _groups.Clear();
        }

        public void ClearData()
        {
            EnsureServerActive();
            Data.Clear();
            SharedData.Clear();
        }

        public void Disconnect()
        {
            EnsureServerActive();
            DisconnectPeer(this);
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

            if (SharedData.TryGetValue(key, out object value) || key == NetworkConstants.SHARED_ALL_KEYS)
            {
                value = key != NetworkConstants.SHARED_ALL_KEYS ? value : SharedData;
                ImmutableKeyValuePair keyValuePair = new(key, value);
                using var message = Pool.Rent();
                message.Write(Id);
                message.WriteAsJson(keyValuePair);
                ServerSide.SendMessage(MessageType.SyncPeerSharedData, this, message, target, deliveryMode, groupId,
                    dataCache, sequenceChannel);
            }
            else
            {
                NetworkLogger.__Log__(
                    $"SyncSharedData Peer Error: Failed to sync '{key}' because it doesn't exist.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        public void DeleteCache(DataCache dataCache)
        {
            EnsureServerActive();
            switch (dataCache.Mode)
            {
                case CacheMode.Peer | CacheMode.New:
                case CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy:
                    AppendCaches.RemoveAll(x => x.Mode == dataCache.Mode && x.Id == dataCache.Id);
                    break;
                case CacheMode.Peer | CacheMode.Overwrite:
                case CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy:
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

        public void DestroyAllCaches()
        {
            EnsureServerActive();
            AppendCaches.RemoveAll(x => x.AutoDestroyCache);
            var caches = OverwriteCaches.Values.Where(x => x.AutoDestroyCache).ToList();

            foreach (var cache in caches)
            {
                if (!OverwriteCaches.Remove(cache.Id))
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

        public override string ToString()
        {
            return ToJson(this);
        }

        public override bool Equals(object obj)
        {
            NetworkPeer other = (NetworkPeer)obj;
            return other != null && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(NetworkPeer other)
        {
            return other != null && Id == other.Id;
        }
    }
}