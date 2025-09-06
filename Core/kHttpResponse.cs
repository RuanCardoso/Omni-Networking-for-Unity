using System.Text;
using Omni.Core.Web.Net;

namespace Omni.Core.Web
{
    public class kHttpResponse
    {
        private readonly HttpListenerResponse listenerResponse;
        private readonly KestrelResponse kestrelResponse;

        public long ContentLength
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

        public bool KeepAlive
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

        public int StatusCode
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

        public Encoding ContentEncoding
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

        public string ContentType
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

        public void SetHeader(string name, string value)
        {
            if (kestrelResponse != null)
            {
                kestrelResponse.Headers.Add(new SerializableHeader(name, value));
                return;
            }

            listenerResponse.SetHeader(name, value);
        }

        public void SetCookie(Cookie cookie)
        {
            if (kestrelResponse != null)
            {
                kestrelResponse.Cookies.Add(new SerializableCookie(cookie));
                return;
            }

            listenerResponse.SetCookie(cookie);
        }

        public void Close(byte[] data)
        {
            if (data == null || data.Length == 0)
                data = Encoding.UTF8.GetBytes("The server did not provide any response.");

            if (kestrelResponse != null)
            {
                kestrelResponse.Data = data;
                kestrelResponse.KestrelLowLevel.AddKestrelResponse(kestrelResponse);
                return;
            }

            listenerResponse.Close(data, willBlock: true);
        }

        public void Close()
        {
            // Kestrel does not support Close(), so we do nothing.
            if (kestrelResponse == null)
                listenerResponse.Close();
        }
    }
}