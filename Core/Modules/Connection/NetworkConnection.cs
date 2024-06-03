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
                NetworkHelper.EnsureRunningOnMainThread();
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
                NetworkHelper.EnsureRunningOnMainThread();
                if (target.Port == 0)
                {
                    throw new NotSupportedException(
                        "Operation not supported: The server cannot send messages to itself. Please check the target configuration or specify a 'peerId' different from 0."
                    );
                }

                Transporter.Send(data, target, deliveryMode, channel);
            }

            internal void Listen(int port)
            {
                NetworkHelper.EnsureRunningOnMainThread();
                Transporter.Listen(port);
            }

            internal void Stop()
            {
                NetworkHelper.EnsureRunningOnMainThread();
                Transporter.Stop();
            }
        }

        internal class NetworkClient : NetworkSocket
        {
            protected override bool IsServer => false;

            internal void Connect(string address, int port)
            {
                NetworkHelper.EnsureRunningOnMainThread();
                Transporter.Connect(address, port);
            }

            internal void Disconnect()
            {
                NetworkHelper.EnsureRunningOnMainThread();
                Transporter.Disconnect();
            }
        }

        internal class NetworkServer : NetworkSocket
        {
            protected override bool IsServer => true;
        }
    }
}
