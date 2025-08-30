using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MemoryPack;
using Omni.Shared;

namespace Omni.Core.Web
{
    internal class KestrelProcessor
    {
        private const int kPort = 60123;
        private const int headerSize = 5;

        private readonly byte[] header = new byte[headerSize];
        private readonly byte[] data = new byte[4096 * 8 * 2]; // 64KB Buffer

        private readonly BlockingCollection<KestrelChannelMessage> channel = new();
        private TcpClient tcpClient = new();

        private bool isConnected = false;
        private NetworkStream Stream => tcpClient.GetStream();
        private CancellationTokenSource cancellationTokenSource;

        internal Action<KestrelRequest, KestrelResponse> OnRequest;
        internal void Initialize(KestrelOptions options, List<KestrelRoute> routes)
        {
            new Thread(() =>
            {
                cancellationTokenSource = new();
                while (!isConnected && !cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        if (tcpClient.ConnectAsync("localhost", kPort).Wait(int.MaxValue, cancellationTokenSource.Token))
                        {
                            isConnected = true;
                            InternalInitialize(options, routes);
                            NetworkLogger.__Log__("[Kestrel] Communication channel established.");
                            Run();
                        }
                    }
                    catch
                    {
                        tcpClient.Dispose();
                        Thread.Sleep(1000);
                        tcpClient = new();
                        continue;
                    }
                }
            }
            )
            {
                Priority = ThreadPriority.Highest,
                Name = "Kestrel_Reader"
            }.Start();

            new Thread(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        break;

                    try
                    {
                        var msg = channel.Take();
                        Send(msg.MessageType, msg.Payload);
                    }
                    catch { break; }
                }
            })
            {
                Priority = ThreadPriority.Highest,
                Name = "Kestrel_Writer"
            }.Start();
        }

        private void Run()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    break;

                try
                {
                    // read the header, exactly 4 bytes (int) + 1 (Kestrel Message)
                    Stream.ReadExactly(header);
                    int length = BitConverter.ToInt32(header);
                    if (length > 0)
                    {
                        KestrelMessageType kestrelMessage = (KestrelMessageType)header[^1];
                        // read the rest of the data
                        Span<byte> payload = data.AsSpan()[..length];
                        Stream.ReadExactly(payload);
                        switch (kestrelMessage)
                        {
                            case KestrelMessageType.DispatchRequest:
                                {
                                    var request = MemoryPackSerializer.Deserialize<KestrelRequest>(payload);
                                    var response = new KestrelResponse() { UniqueId = request.UniqueId, KestrelLowLevel = this };
                                    Task.Run(() => { OnRequest?.Invoke(request, response); });
                                    break;
                                }
                            default:
                                Console.WriteLine($"Unknown Kestrel message type: {kestrelMessage}");
                                break;
                        }
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        internal void AddKestrelResponse(KestrelResponse response)
        {
            KestrelChannelMessage message = new()
            {
                MessageType = KestrelMessageType.DispatchResponse,
                Payload = MemoryPackSerializer.Serialize(response)
            };

            channel.Add(message);
        }

        internal void Send(KestrelMessageType kestrelMessage, ReadOnlySpan<byte> payload)
        {
            if (!isConnected)
            {
                NetworkLogger.__Log__(
                    "[Kestrel] Attempted to send a message before the communication channel was ready. " +
                    "Initialize Kestrel before dispatching requests.",
                    NetworkLogger.LogType.Error);

                return;
            }

            // write the header and payload to the pipe
            // header is 4 bytes for length and 1 byte for message type
            Span<byte> header = stackalloc byte[headerSize];
            if (BitConverter.TryWriteBytes(header, payload.Length))
            {
                header[^1] = (byte)kestrelMessage;

                // combine header and payload into one array
                Span<byte> packet = new byte[header.Length + payload.Length];
                header.CopyTo(packet);
                payload.CopyTo(packet[header.Length..]);

                // write the packet to the pipe
                Stream.Write(packet);
            }
        }

        private void InternalInitialize(KestrelOptions options, List<KestrelRoute> routes)
        {
            // Setup the kestrel server with the given options
            byte[] payload = MemoryPackSerializer.Serialize(options);
            Send(KestrelMessageType.Initialize, payload);
            // Add the routes to the kestrel server
            AddRoutes(routes);
        }

        private void AddRoutes(List<KestrelRoute> routes)
        {
            byte[] payload = MemoryPackSerializer.Serialize(routes);
            Send(KestrelMessageType.AddRoutes, payload);
        }

        internal void Close()
        {
            tcpClient.Dispose();
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            isConnected = false;
        }
    }
}