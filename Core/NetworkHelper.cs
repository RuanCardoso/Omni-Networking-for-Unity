using System;
using System.Net;
using System.Net.Sockets;

namespace Omni.Core
{
    internal static class NetworkHelper
    {
        // the chances of collision are low, so it's fine to use hashcode.
        // because... do you have billions of network objects in the scene?
        // scene objects are different from dynamic objects(instantiated);
        internal static int GenerateUniqueId()
        {
            Guid guid = Guid.NewGuid();
            return guid.GetHashCode();
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
    }
}
