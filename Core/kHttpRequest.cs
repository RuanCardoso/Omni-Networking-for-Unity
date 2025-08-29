using System.Collections.Specialized;
using System.IO;
using System.Text;
using Omni.Core.Web.Net;
using kNet = System.Net;

namespace Omni.Core.Web
{
    public class kHttpRequest
    {
        private readonly HttpListenerRequest listenerRequest;
        private readonly KestrelRequest kestrelRequest;

        internal string RawUrl => listenerRequest?.RawUrl ?? kestrelRequest.RawUrl;
        internal string HttpMethod => listenerRequest?.HttpMethod ?? kestrelRequest.HttpMethod;
        internal string ContentType => listenerRequest?.ContentType ?? kestrelRequest.ContentType;
        internal bool IsSecureConnection => listenerRequest?.IsSecureConnection ?? kestrelRequest.IsSecureConnection;
        internal CookieCollection Cookies => listenerRequest?.Cookies ?? null;
        internal Encoding ContentEncoding => listenerRequest?.ContentEncoding ?? Encoding.UTF8;
        internal Stream InputStream => listenerRequest?.InputStream ?? null;
        internal NameValueCollection QueryString => listenerRequest?.QueryString ?? CreateQueryString(kestrelRequest.QueryString);
        public kNet.IPEndPoint RemoteEndPoint => listenerRequest?.RemoteEndPoint ?? CreateRemoteEndPoint(kestrelRequest.RemoteEndPoint);

        internal kHttpRequest(HttpListenerRequest request)
        {
            listenerRequest = request;
        }

        internal kHttpRequest(KestrelRequest request)
        {
            kestrelRequest = request;
        }

        private NameValueCollection CreateQueryString(string queryString)
        {
            return System.Web.HttpUtility.ParseQueryString(queryString);
        }

        private kNet.IPEndPoint CreateRemoteEndPoint(string remoteEndPoint)
        {
            string[] parts = remoteEndPoint.Split(':');
            string address = parts[0];
            int port = int.Parse(parts[^1]);
            if (kNet.IPAddress.TryParse(address, out kNet.IPAddress ipAddress))
            {
                return new kNet.IPEndPoint(ipAddress, port);
            }

            return new kNet.IPEndPoint(kNet.IPAddress.Loopback, port);
        }
    }
}
