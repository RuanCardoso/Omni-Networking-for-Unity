using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class NetworkPeer
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

        public void SyncSerializedData(
            Target target = Target.Self,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            SyncSerializedData(
                "_AllKeys_",
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }

        public void SyncSerializedData(
            string key,
            Target target = Target.Self,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            Internal_SyncSerializedData(
                key,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }

        private void Internal_SyncSerializedData(
            string key = "_AllKeys_",
            Target target = Target.Self,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            if (!IsServerActive)
            {
                throw new Exception("Can't use this method on client.");
            }

            if (SerializedData.TryGetValue(key, out object value) || key == "_AllKeys_")
            {
                value = key != "_AllKeys_" ? value : SerializedData;
                ImmutableKeyValuePair keyValuePair = new(key, value);
                using var message = Pool.Rent();
                message.FastWrite(Id);
                message.ToJson(keyValuePair);
                Server.SendMessage(
                    MessageType.SyncPeerSerializedData,
                    this,
                    message,
                    target,
                    deliveryMode,
                    groupId,
                    cacheId,
                    cacheMode,
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
    }
}
