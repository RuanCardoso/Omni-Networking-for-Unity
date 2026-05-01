using System;
using System.Collections.Generic;
using MemoryPack;
using Omni.Core.Web.Net;
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
        public string m_Domain = "*";

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
        public List<SerializableCookie> Cookies { get; set; }
        public List<SerializableHeader> Headers { get; set; }
        public byte[] InputStream { get; set; }
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
        public List<SerializableCookie> Cookies { get; set; } = new(); // empty list
        public List<SerializableHeader> Headers { get; set; } = new(); // empty list
    }

    [MemoryPackable]
    internal partial class SerializableCookie
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public DateTime Expires { get; set; }
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }
        public int Version { get; set; }
        public string Comment { get; set; }
        public Uri CommentUri { get; set; }
        public bool Discard { get; set; }

        [MemoryPackConstructor]
        public SerializableCookie() { }

        public SerializableCookie(Cookie cookie)
        {
            Name = cookie.Name;
            Value = cookie.Value;
            Domain = cookie.Domain;
            Path = cookie.Path;
            Expires = cookie.Expires;
            Secure = cookie.Secure;
            HttpOnly = cookie.HttpOnly;
            Version = cookie.Version;
            Comment = cookie.Comment;
            CommentUri = cookie.CommentUri;
            Discard = cookie.Discard;
        }

        public SerializableCookie(string key, string value)
        {
            Name = key;
            Value = value;
            Path = "/";
            Domain = string.Empty;
            Expires = DateTime.MinValue;
            Secure = false;
            HttpOnly = false;
            Version = 0;
        }

        public Cookie ToCookie()
        {
            var c = new Cookie(Name, Value, Path, Domain)
            {
                Expires = Expires,
                Secure = Secure,
                HttpOnly = HttpOnly,
                Version = Version,
                Comment = Comment,
                CommentUri = CommentUri,
                Discard = Discard
            };
            return c;
        }
    }

    [MemoryPackable]
    internal partial class SerializableHeader
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();

        [MemoryPackConstructor]
        public SerializableHeader() { }

        public SerializableHeader(string name, IEnumerable<string> values)
        {
            Name = name;
            Values = values.ToList();
        }

        public SerializableHeader(string name, string value)
        {
            Name = name;
            Values = new List<string> { value };
        }
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