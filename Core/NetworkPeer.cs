using System.Collections.Generic;
using System.Net;
using MemoryPack;
using Newtonsoft.Json;
using Omni.Core.Interfaces;
using Omni.Core.Modules.Matchmaking;
using Omni.Shared.Collections;

namespace Omni.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    [MemoryPackable]
    public partial class NetworkPeer
    {
#pragma warning disable IDE0052
        [MemoryPackIgnore]
        readonly string __endpoint__;
#pragma warning restore IDE0052
        [MemoryPackIgnore]
        public IPEndPoint EndPoint { get; }

        [MemoryPackIgnore]
        public int Id { get; }

        [MemoryPackIgnore]
        public bool IsConnected => EndPoint != null;

        [MemoryPackIgnore]
        public Dictionary<int, NetworkGroup> Groups { get; } = new();

        [MemoryPackIgnore]
        public ObservableDictionary<string, object> Data { get; } = new();

        [MemoryPackIgnore, JsonProperty("Data")]
        public ObservableDictionary<string, object> SerializedData { get; } = new();

        [MemoryPackConstructor]
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
            Groups.Clear();
        }

        public void ClearData()
        {
            Data.Clear();
            SerializedData.Clear();
        }

        public override string ToString()
        {
            return NetworkManager.ToJson(this);
        }
    }
}
