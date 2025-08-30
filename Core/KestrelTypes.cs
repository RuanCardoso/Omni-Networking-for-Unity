using System;
using MemoryPack;
using Omni.Inspector;

namespace Omni.Core.Web
{
    #region MemoryPackable

    [MemoryPackable]
    [Serializable]
    public partial class KestrelOptions
    {
        [LabelWidth(120)]
        public int m_KeepAliveTimeout = 130;

        [LabelWidth(120)]
        public int m_Port = 80;
    }

    [MemoryPackable]
    internal partial class KestrelRoute
    {
        public string Route { get; set; }
        public string Method { get; set; }
    }

    [MemoryPackable]
    internal partial class KestrelRequest
    {
        public string UniqueId { get; set; }
        public KestrelRoute Route { get; set; }
        public string RawUrl { get; set; }
        public string HttpMethod { get; set; }
        public string ContentType { get; set; }
        public bool IsSecureConnection { get; set; }
        public string QueryString { get; set; }
        public string RemoteEndPoint { get; set; }
    }

    [MemoryPackable]
    internal partial class KestrelResponse
    {
        [MemoryPackIgnore]
        internal KestrelProcessor KestrelLowLevel { get; set; }

        public string UniqueId { get; set; }
        public int StatusCode { get; set; }
        public string ContentType { get; set; }
        public bool KeepAlive { get; set; }
        public long ContentLength64 { get; set; }
        public byte[] Data { get; set; }
    }

    #endregion

    #region All

    internal enum KestrelMessageType : byte
    {
        Initialize,
        AddRoutes,
        DispatchRequest,
        DispatchResponse,
    }

    internal class KestrelChannelMessage
    {
        public KestrelMessageType MessageType;
        public byte[] Payload;
    }

    #endregion
}