using System;
using System.Net;
using Omni.Core.Interfaces;

namespace Omni.Core.Modules.Connection
{
    internal class NetworkConnection
    {
        internal NetworkServer Server { get; } = new NetworkServer();
        internal NetworkClient Client { get; } = new NetworkClient();

        internal class NetworkSocket
        {
            protected ITransporter Transporter { get; private set; }
            protected virtual bool IsServer { get; }

            internal void StartTransporter(ITransporter ITransporter, ITransporterReceive IReceive)
            {
                Transporter = ITransporter;
                Transporter.Initialize(IReceive, IsServer);
            }

            internal void Send(
                ReadOnlySpan<byte> data,
                IPEndPoint target,
                DeliveryMode deliveryMode,
                byte channel
            )
            {
                Transporter.Send(data, target, deliveryMode, channel);
            }

            internal void Listen(int port)
            {
                Transporter.Listen(port);
            }

            internal void Stop()
            {
                Transporter.Stop();
            }
        }

        internal class NetworkClient : NetworkSocket
        {
            protected override bool IsServer => false;

            internal void Connect(string address, int port)
            {
                Transporter.Connect(address, port);
            }

            internal void Disconnect()
            {
                Transporter.Disconnect();
            }
        }

        internal class NetworkServer : NetworkSocket
        {
            protected override bool IsServer => true;
        }
    }
}
