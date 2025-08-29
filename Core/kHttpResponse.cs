using System.Text;
using System.Threading.Tasks;
using MemoryPack;
using Omni.Core.Web.Net;
using Omni.Shared;

namespace Omni.Core.Web
{
    public class kHttpResponse
    {
        private readonly HttpListenerResponse listenerResponse;
        private readonly KestrelResponse kestrelResponse;

        internal long ContentLength
        {
            get
            {
                if (kestrelResponse != null)
                    return kestrelResponse.ContentLength64;

                return listenerResponse.ContentLength64;
            }

            set
            {
                if (kestrelResponse != null)
                {
                    kestrelResponse.ContentLength64 = value;
                    return;
                }

                listenerResponse.ContentLength64 = value;
            }
        }

        internal bool KeepAlive
        {
            get
            {
                if (kestrelResponse != null)
                    return kestrelResponse.KeepAlive;

                return listenerResponse.KeepAlive;
            }

            set
            {
                if (kestrelResponse != null)
                {
                    kestrelResponse.KeepAlive = value;
                    return;
                }

                listenerResponse.KeepAlive = value;
            }
        }

        internal int StatusCode
        {
            get
            {
                if (kestrelResponse != null)
                    return kestrelResponse.StatusCode;

                return listenerResponse.StatusCode;
            }

            set
            {
                if (kestrelResponse != null)
                {
                    kestrelResponse.StatusCode = value;
                    return;
                }

                listenerResponse.StatusCode = value;
            }
        }

        internal Encoding ContentEncoding
        {
            get
            {
                if (kestrelResponse == null) // UTF-8 is default
                    return listenerResponse.ContentEncoding;

                return Encoding.UTF8;
            }

            set
            {
                if (kestrelResponse == null) // UTF-8 is default
                    listenerResponse.ContentEncoding = value;

                // so we do nothing
            }
        }

        internal string ContentType
        {
            get
            {
                if (kestrelResponse != null)
                    return kestrelResponse.ContentType;

                return listenerResponse.ContentType;
            }

            set
            {
                if (kestrelResponse != null)
                {
                    kestrelResponse.ContentType = value;
                    return;
                }

                listenerResponse.ContentType = value;
            }
        }

        internal kHttpResponse(HttpListenerResponse response)
        {
            listenerResponse = response;
        }

        internal kHttpResponse(KestrelResponse response)
        {
            kestrelResponse = response;
        }

        internal void SetHeader(string name, string value)
        {
            if (kestrelResponse == null) // Coming Soon, Kestrel does not support headers.
                listenerResponse.SetHeader(name, value);
        }

        internal void SetCookie(Cookie cookie)
        {
            if (kestrelResponse == null) // Coming Soon, Kestrel does not support cookies.
                listenerResponse.SetCookie(cookie);
        }

        internal void Close(byte[] data)
        {
            if (data == null || data.Length == 0)
                data = Encoding.UTF8.GetBytes("The server did not provide any response.");

            if (kestrelResponse != null)
            {
                kestrelResponse.Data = data;
                byte[] response = MemoryPackSerializer.Serialize(kestrelResponse);
                kestrelResponse.KestrelLowLevel.Send(KestrelMessageType.DispatchResponse, response);
                return;
            }

            listenerResponse.Close(data, willBlock: true);
        }

        internal void Close()
        {
            // Kestrel does not support Close(), so we do nothing.
            if (kestrelResponse == null)
                listenerResponse.Close();
        }
    }
}