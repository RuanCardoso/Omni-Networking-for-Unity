using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Omni.Core
{
    public static class NetworkHelper
    {
        private static int d_UniqueId = int.MinValue;

        // the chances of collision are low, so it's fine to use hashcode.
        // because... do you have billions of network objects in the scene?
        // scene objects are different from dynamic objects(instantiated);
        internal static int GenerateSceneUniqueId()
        {
            Guid newGuid = Guid.NewGuid();
            return newGuid.GetHashCode();
        }

        // Used for dynamic objects (instantiated).
        // The chances of collision is zero(0) because each ID is unique(incremental).
        internal static int GenerateDynamicUniqueId()
        {
            if (d_UniqueId == 0)
            {
                d_UniqueId = 1;
            }

            return d_UniqueId++;
        }

        internal static bool IsPortAvailable(int port, ProtocolType protocolType, bool useIPv6)
        {
            try
            {
                if (protocolType == ProtocolType.Udp)
                {
                    using Socket socket = new Socket(
                        useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                        SocketType.Dgram,
                        ProtocolType.Udp
                    );

                    if (useIPv6)
                    {
                        socket.DualMode = true;
                    }

                    socket.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
                    socket.Close();
                }
                else if (protocolType == ProtocolType.Tcp)
                {
                    using Socket socket = new Socket(
                        useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp
                    );

                    if (useIPv6)
                    {
                        socket.DualMode = true;
                    }

                    socket.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
                    socket.Close();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static int GetAvailablePort(int port, bool useIPv6)
        {
            while (!IsPortAvailable(port, ProtocolType.Udp, useIPv6))
            {
                port++;
                if (port > 65535)
                {
                    port = 7777;
                }
            }

            return port;
        }

        public static async Task<IPAddress> GetExternalIp(bool useIPv6)
        {
            try
            {
                using var httpClient = new HttpClient();
                string externalIp = (
                    await httpClient.GetStringAsync(
                        useIPv6 ? "http://ipv6.icanhazip.com/" : "http://ipv4.icanhazip.com/"
                    )
                );

                externalIp = externalIp.Replace("\\r\\n", "");
                externalIp = externalIp.Replace("\\n", "");
                externalIp = externalIp.Trim();

                if (!IPAddress.TryParse(externalIp, out var ipAddress))
                {
                    return IPAddress.Loopback;
                }

                return ipAddress;
            }
            catch
            {
                return IPAddress.Loopback;
            }
        }

        [Conditional("OMNI_DEBUG")]
        internal static void EnsureRunningOnMainThread()
        {
            if (NetworkManager.MainThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new Exception(
                    "This operation must be performed on the main thread. Omni does not support multithreaded operations. Tip: Dispatch the events to the main thread."
                );
            }
        }
    }
}
