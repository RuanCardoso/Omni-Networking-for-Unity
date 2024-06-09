using System;
using System.Net;

namespace Omni.Core.Interfaces
{
    internal interface ITransporterReceive
    {
        void Internal_OnDataReceived(
            ReadOnlySpan<byte> data,
            DeliveryMode deliveryMethod,
            IPEndPoint source,
            byte sequenceChannel,
            bool isServer
        );

        void Internal_OnServerInitialized();
        void Internal_OnClientConnected(IPEndPoint peer, NativePeer nativePeer);
        void Internal_OnClientDisconnected(IPEndPoint peer, string reason);
        void Internal_OnServerPeerConnected(IPEndPoint peer, NativePeer nativePeer);
        void Internal_OnServerPeerDisconnected(IPEndPoint peer, string reason);
    }
}
