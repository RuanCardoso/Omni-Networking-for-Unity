using System;
using System.IO;
using MemoryPack;
using Omni.Inspector;

namespace Omni.Core.Web
{
    #region MemoryPackable

    [MemoryPackable]
    [Serializable]
    [DeclareFoldoutGroup("Network", Expanded = true)]
    [DeclareFoldoutGroup("Limits", Expanded = false)]
    public partial class KestrelOptions
    {
        [GroupNext("Network")]
        [LabelWidth(120)]
        [ReadOnly]
        public bool m_UseHttps;

        [ReadOnly]
        [ShowIf(nameof(m_UseHttps))]
        [LabelWidth(120)]
        public string m_CertificateFile = NetworkConstants.k_CertificateFile;

        [LabelWidth(120)]
        public int m_Port = 80;

        [GroupNext("Limits")]
        [LabelWidth(120)]
        public int m_KeepAliveTimeout = 130;

        [LabelWidth(120)]
        public int m_MaxConnections = 2000;

        [LabelWidth(120)]
        public int m_RequestTimeout = 30;
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