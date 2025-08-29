using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using MemoryPack;
using Omni.Inspector;
using Omni.Shared;

public static class extensionsStream
{
    public static int ReadExactly(this Stream stream, Span<byte> buffer)
    {
        return ReadExactly(stream, buffer, buffer.Length, throwOnEndOfStream: true);
    }

    private static int ReadExactly(this Stream stream, Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream)
    {
        int totalRead = 0;
        while (totalRead < minimumBytes)
        {
            int read = stream.Read(buffer.Slice(totalRead));
            if (read == 0)
            {
                if (throwOnEndOfStream)
                {
                    throw new EndOfStreamException();
                }

                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }
}

namespace Omni.Core.Web
{
    [MemoryPackable]
    [Serializable]
    public partial class KestrelOptions
    {
        [LabelWidth(120)]
        public int m_KeepAliveTimeout = 130;

        [LabelWidth(120)]
        public int m_Port = 80;
    }

    [MemoryPackable]
    internal partial class KestrelRoute
    {
        public string Route { get; set; }
        public string Method { get; set; }
    }

    [MemoryPackable]
    internal partial class KestrelRequest
    {
        public string UniqueId { get; set; }
        public KestrelRoute Route { get; set; }
        public string RawUrl { get; set; }
        public string HttpMethod { get; set; }
        public string ContentType { get; set; }
        public bool IsSecureConnection { get; set; }
        public string QueryString { get; set; }
        public string RemoteEndPoint { get; set; }
    }

    [MemoryPackable]
    internal partial class KestrelResponse
    {
        [MemoryPackIgnore]
        internal KestrelLowLevel KestrelLowLevel { get; set; }

        public string UniqueId { get; set; }
        public int StatusCode { get; set; }
        public string ContentType { get; set; }
        public bool KeepAlive { get; set; }
        public long ContentLength64 { get; set; }
        public byte[] Data { get; set; }
    }

    internal enum KestrelMessageType : byte
    {
        Initialize,
        AddRoutes,
        DispatchRequest,
        DispatchResponse,
    }

    internal class KestrelChannelMessage
    {
        public KestrelRequest request;
        public KestrelResponse response;
    }

    internal class KestrelLowLevel
    {
        private const int headerSize = 5;
        private readonly byte[] header = new byte[headerSize];
        private readonly byte[] data = new byte[4096 * 8 * 8]; // 32kb

        private System.Threading.Channels.Channel<KestrelChannelMessage> channel = System.Threading.Channels.Channel.CreateUnbounded<KestrelChannelMessage>();

        private NamedPipeClientStream pipeClient;
        private bool isConnected = false;
        private CancellationTokenSource cancellationTokenSource;

        internal Action<KestrelRequest, KestrelResponse> OnRequest;
        internal void Initialize(KestrelOptions options, List<KestrelRoute> routes)
        {
            new Thread(() =>
            {
                cancellationTokenSource = new();
                pipeClient = new("localhost", "HttpPipe", PipeDirection.InOut, PipeOptions.Asynchronous);
                pipeClient.Connect();
                isConnected = true;

                InternalInitialize(options, routes);
                NetworkLogger.__Log__("[Kestrel] Communication channel established.");
                Read();
            }
            )
            {
                Priority = ThreadPriority.Highest,
                Name = "KestrelLowLevel_Reader"
            }.Start();

            new Thread(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        var message = await channel.Reader.ReadAsync();
                        OnRequest?.Invoke(message.request, message.response);
                    }
                    catch
                    {
                        break;
                    }
                }
            })
            {
                Priority = ThreadPriority.Highest,
                Name = "KestrelLowLevel_Writer"
            }.Start();
        }

        private void Read()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    // read the header, exactly 4 bytes (int) + 1 (Kestrel Message)
                    pipeClient.ReadExactly(header);
                    int length = BitConverter.ToInt32(header);
                    if (length > 0)
                    {
                        KestrelMessageType kestrelMessage = (KestrelMessageType)header[^1];
                        // read the rest of the data
                        Span<byte> payload = data.AsSpan()[..length];
                        pipeClient.ReadExactly(payload);
                        switch (kestrelMessage)
                        {
                            case KestrelMessageType.DispatchRequest:
                                {
                                    var request = MemoryPackSerializer.Deserialize<KestrelRequest>(payload);
                                    var response = new KestrelResponse() { UniqueId = request.UniqueId, KestrelLowLevel = this };

                                    KestrelChannelMessage message = new()
                                    {
                                        request = request,
                                        response = response
                                    };

                                    //channel.Writer.TryWrite(message);
                                    OnRequest?.Invoke(request, response);
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

        static readonly object padLock = new();
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

                //lock (padLock)
                {
                    // write the packet to the pipe
                    pipeClient.Write(packet);
                }
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
            pipeClient.Dispose();
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            isConnected = false;
        }
    }
}