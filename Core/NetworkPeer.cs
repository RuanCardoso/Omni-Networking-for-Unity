using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using MemoryPack;
using Newtonsoft.Json;
using Omni.Core.Modules.Matchmaking;
using Omni.Shared.Collections;

namespace Omni.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    [MemoryPackable]
    public partial class NetworkPeer
    {
        [MemoryPackIgnore]
        internal byte[] AesKey { get; set; }

        [MemoryPackIgnore]
        internal NativePeer _nativePeer;

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
        public Dictionary<int, NetworkGroup> Groups { get; } = new();

        [MemoryPackIgnore]
        public ObservableDictionary<string, object> Data { get; } = new();

        [MemoryPackIgnore, JsonProperty("Data")]
        public ObservableDictionary<string, object> SerializedData { get; } = new();

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
            Groups.Clear();
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
            NetworkManager.DisconnectPeer(this);
        }

        [Conditional("OMNI_DEBUG")]
        private void EnsureServerActive()
        {
            if (!NetworkManager.IsServerActive)
            {
                throw new Exception("Can't use this method on client.");
            }
        }

        public override string ToString()
        {
            return NetworkManager.ToJson(this);
        }
    }
}
