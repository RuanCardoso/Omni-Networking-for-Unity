using System;
using System.Net;

namespace Omni.Core.Interfaces
{
    internal interface ITransporter
    {
        void Initialize(ITransporterReceive IReceive, bool isServer);
        void Listen(int port);
        void Send(
            ReadOnlySpan<byte> data,
            IPEndPoint target,
            DeliveryMode deliveryMode,
            byte sequenceChannel
        );
        void Connect(string address, int port);
        void Disconnect(NetworkPeer peer);
        void Stop();
        void CopyTo(ITransporter ITransporter);
    }
}
