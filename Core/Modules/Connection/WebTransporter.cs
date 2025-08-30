using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Omni.Core.Web;
using Omni.Threading.Tasks;
using Omni.Inspector;
using UnityEngine;
using HttpListenerRequest = Omni.Core.Web.Net.HttpListenerRequest;
using HttpListenerResponse = Omni.Core.Web.Net.HttpListenerResponse;
using WebClient = NativeWebSocket;
using WebServer = Omni.Core.Web.Server;
using System.Net.Sockets;

#pragma warning disable

namespace Omni.Core.Web
{
    public class WebSocketService : WebServer.WebSocketBehavior
    {
        public WebSocketService()
        {
        }
    }

    public class HttpService : WebSocketService
    {

    }
}

namespace Omni.Core.Modules.Connection
{
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Omni/Transporters/Web Transporter")]
    [DeclareBoxGroup("Ssl Settings")]
    internal class WebTransporter : TransporterBehaviour, ITransporter
    {
        private class WebServerListener : WebSocketService
        {
            internal WebTransporter Transporter { get; set; }
            internal ITransporterReceive IManager { get; set; }
            internal Dictionary<IPEndPoint, WebServerListener> Peers { get; set; }
            internal WebServer.WebSocketServer WebServer { get; set; }

            protected override void OnOpen()
            {
                NetworkHelper.RunOnMainThread(() =>
                {
#if OMNI_DEBUG
                    if (WebServer.IsSecure)
                    {
                        NetworkLogger.__Log__(
                            $"[WebTransporter] Secure SSL connection established for endpoint {UserEndPoint}");
                    }
#endif
                    if (!Peers.TryAdd(UserEndPoint, this))
                    {
                        NetworkLogger.__Log__(
                            $"[WebTransporter] Connection rejected - Peer {UserEndPoint} is already registered",
                            NetworkLogger.LogType.Error
                        );
                    }
                    else
                    {
                        IManager.Internal_OnServerPeerConnected(UserEndPoint, new NativePeer());
                    }
                });
            }

            protected override void OnClose(Web.CloseEventArgs e)
            {
                NetworkHelper.RunOnMainThread(() =>
                {
                    if (!Peers.Remove(UserEndPoint))
                    {
                        NetworkLogger.__Log__(
                            $"[WebTransporter] Failed to remove peer {UserEndPoint} - Peer already disconnected",
                            NetworkLogger.LogType.Error
                        );
                    }
                    else
                    {
                        IManager.Internal_OnServerPeerDisconnected(UserEndPoint, $"Code: {e.Code} - {e.Reason}");
                    }
                });
            }

            protected override void OnMessage(Web.MessageEventArgs e)
            {
                if (e.IsBinary && !e.IsPing)
                {
                    NetworkHelper.RunOnMainThread(() =>
                    {
                        IManager.Internal_OnDataReceived(e.RawData, DeliveryMode.ReliableOrdered, UserEndPoint, 0, true, out _);
                    });
                }
                else if (e.IsText && !e.IsPing)
                {
                    Transporter.Internal_OnStringDataReceived(e.Data, UserEndPoint, true);
                }
            }

            internal void Send(byte[] data)
            {
                base.Send(data);
            }

            internal void Send(string data)
            {
                base.Send(data);
            }

            internal void Send(Stream stream, int length)
            {
                base.Send(stream, length);
            }

            internal void Disconnect(Web.CloseStatusCode statusCode, string reason)
            {
                base.Close(statusCode, reason);
            }
        }

        private bool isServer;
        private bool isRunning;

        private ITransporterReceive IManager;
        internal event Action<string, IPEndPoint, bool> OnStringDataReceived;
        internal event Action<kHttpRequest, kHttpResponse> OnGetRequest;
        internal event Action<kHttpRequest, kHttpResponse> OnPostRequest;

        private WebServer.HttpServer httpServer;
        private WebServer.WebSocketServer webSocketServer;
        private WebClient.WebSocket webClient;
        private readonly Dictionary<IPEndPoint, WebServerListener> _peers = new();

        [SerializeField]
        [Group("Ssl Settings")]
        private bool enableSsl = false;

        internal bool EnableWebSocket { get; set; } = true;
        internal bool EnableWebSocketSsl
        {
            get => enableSsl;
            set => enableSsl = value;
        }

        // Http Server Settings
        internal int HttpServerPort { get; set; } = 8080;
        internal bool EnableHttpServer { get; set; } = false;
        internal bool EnableHttpServerSsl { get; set; } = false;
        internal string HttpServerDocumentRootPath { get; set; } = "";

        public void Initialize(ITransporterReceive IManager, bool isServer)
        {
            this.isServer = isServer;
            this.IManager = IManager;

            if (isRunning)
            {
                throw new InvalidOperationException("[WebTransporter] Cannot initialize: Instance is already running. Call Stop() before reinitializing.");
            }
        }

        public void Listen(int port)
        {
            if (isServer)
            {
                httpServer = new WebServer.HttpServer(HttpServerPort, EnableHttpServerSsl);
                if (EnableHttpServer)
                {
                    httpServer.AddWebSocketService<HttpService>("/");
                    httpServer.DocumentRootPath = HttpServerDocumentRootPath;

                    httpServer.OnGet += (sender, e) => OnGetRequest?.Invoke(new kHttpRequest(e.Request), new kHttpResponse(e.Response));
                    httpServer.OnPost += (sender, e) => OnPostRequest?.Invoke(new kHttpRequest(e.Request), new kHttpResponse(e.Response));
                }

                webSocketServer =
                    new WebServer.WebSocketServer($"{(EnableWebSocketSsl ? "wss" : "ws")}://{IPAddress.Any}:{port}");

                if ((EnableWebSocket && EnableWebSocketSsl) || (EnableHttpServer && EnableHttpServerSsl))
                {
                    if (File.Exists(NetworkConstants.k_CertificateFile))
                    {
                        try
                        {
                            var dict = NetworkManager.FromJson<Dictionary<string, string>>(
                                File.ReadAllText(NetworkConstants.k_CertificateFile));

                            // Setup SSL(Secure Socket Layer)
                            if (EnableWebSocket && EnableWebSocketSsl)
                            {
                                webSocketServer.SslConfiguration.ServerCertificate =
                                    new X509Certificate2(dict["cert"], dict["password"]);
                            }

                            if (EnableHttpServer && EnableHttpServerSsl)
                            {
                                httpServer.SslConfiguration.ServerCertificate =
                                    new X509Certificate2(dict["cert"], dict["password"]);
                            }
                        }
                        catch (Exception ex)
                        {
                            NetworkLogger.__Log__(
                                $"[WebTransporter] SSL certificate loading failed - {ex.Message}",
                                NetworkLogger.LogType.Error);
                        }
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            $"[WebTransporter] Certificate configuration not found at: {Path.GetFullPath(NetworkConstants.k_CertificateFile)}", NetworkLogger.LogType.Error);
                    }
                }

                if (EnableWebSocket)
                {
                    webSocketServer.AddWebSocketService<WebServerListener>("/", (listener) =>
                    {
                        listener.Transporter = this;
                        listener.IManager = IManager;
                        listener.Peers = _peers;
                        listener.WebServer = webSocketServer;
                    });

                    try
                    {
                        if (!NetworkHelper.IsPortAvailable(port, ProtocolType.Tcp, false))
                        {
                            NetworkLogger.__Log__(
                                $"[WebTransporter] WebSocket server cannot start - Port {port} is unavailable.",
                                NetworkLogger.LogType.Log
                            );

                            return;
                        }

                        webSocketServer.Start();
                        if (webSocketServer.IsListening)
                        {
                            NetworkLogger.__Log__(
                                $"[WebTransporter] WebSocket server started successfully on port {port}{(webSocketServer.IsSecure ? " (SSL enabled)" : "")}",
                                NetworkLogger.LogType.Log
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.__Log__("[WebTransporter] Failed to start the WebSocket server -> Exception: " + ex.Message, NetworkLogger.LogType.Error);
                    }
                }

                if (EnableHttpServer)
                {
                    try
                    {
                        if (!NetworkHelper.IsPortAvailable(HttpServerPort, ProtocolType.Tcp, false))
                        {
                            NetworkLogger.__Log__(
                                $"[WebTransporter] HTTP server cannot start - Port {HttpServerPort} is unavailable.",
                                NetworkLogger.LogType.Log
                            );

                            return;
                        }

                        httpServer.Start();
                        if (httpServer.IsListening)
                        {
                            NetworkLogger.__Log__(
                                $"[WebTransporter] HTTP server started successfully on port {HttpServerPort}{(httpServer.IsSecure ? " (SSL enabled)" : "")}",
                                NetworkLogger.LogType.Log
                            );
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        {
                            NetworkLogger.__Log__($"[WebTransporter] Failed to start HTTP server - Port {HttpServerPort} is already in use", NetworkLogger.LogType.Error);
                        }
                        else
                        {
                            NetworkLogger.__Log__($"[WebTransporter] Failed to start HTTP server - {ex.Message}", NetworkLogger.LogType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.__Log__($"[WebTransporter] Failed to start HTTP server - {ex.Message}", NetworkLogger.LogType.Error);
                    }
                }

                // Set to 'true' to indicate that the server is running.
                isRunning = true;
                IManager.Internal_OnServerInitialized();
            }
        }

        internal void Awake()
        {
            ITransporter = this;
        }

        internal void Update()
        {
            if (!isServer && isRunning)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                webClient.DispatchMessageQueue();
#endif
            }
        }

        public async void Connect(string address, int port)
        {
            if (EnableWebSocketSsl)
            {
                if (IPAddress.TryParse(address, out _))
                {
                    throw new NotSupportedException("[WebTransporter] SSL connection failed - IP addresses are not supported for SSL connections. Use a hostname (e.g., 'example.com') instead of an IP address.");
                }
            }

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            webClient = new WebClient.WebSocket($"{(EnableWebSocketSsl ? "wss" : "ws")}://{address}:{port}");
            webClient.OnOpen += () =>
            {
                // Set to 'true' to indicate that the client is running.
                isRunning = true;
                IManager.Internal_OnClientConnected(localEndPoint, new NativePeer());
            };

            webClient.OnMessage += (data) =>
            {
                Internal_OnStringDataReceived(Encoding.UTF8.GetString(data), localEndPoint, false);
                IManager.Internal_OnDataReceived(data, DeliveryMode.ReliableOrdered, localEndPoint, 0, false, out _);
            };

            webClient.OnClose += (e) => { IManager.Internal_OnClientDisconnected(localEndPoint, $"code: {e}"); };
            await webClient.Connect(); // While Loop -> 'Receive()' when is opened.
        }

        public void Disconnect(NetworkPeer peer)
        {
            if (isServer)
            {
                WebServerListener listener = _peers[peer.EndPoint];
                listener.Disconnect(Web.CloseStatusCode.Normal, "[WebTransporter] Normally closed.");
            }
            else
            {
                if (webClient.State == WebClient.WebSocketState.Open)
                {
                    webClient.Close();
                }
            }
        }

        // Span.ToArray() is very fast
        public void Send(ReadOnlySpan<byte> data, IPEndPoint target, DeliveryMode deliveryMode, byte sequenceChannel)
        {
#if OMNI_DEBUG
            if (sequenceChannel > 0)
            {
                NetworkLogger.Print(
                    $"[WebTransporter] Sequence channel {sequenceChannel} is not supported - Channel will be ignored",
                    NetworkLogger.LogType.Warning
                );
            }

            if (deliveryMode != DeliveryMode.ReliableOrdered)
            {
                NetworkLogger.Print(
                    $"[WebTransporter] Unsupported delivery mode '{deliveryMode}' - Falling back to ReliableOrdered",
                    NetworkLogger.LogType.Warning
                );
            }
#endif
            if (isServer)
            {
                if (_peers.TryGetValue(target, out WebServerListener peer))
                {
                    byte[] webData = data.ToArray();
                    peer.Send(webData);
                }
            }
            else
            {
                if (webClient.State == WebClient.WebSocketState.Open)
                {
                    byte[] webData = data.ToArray();
                    webClient.Send(webData);
                }
            }
        }

        internal void Send(string data, IPEndPoint target)
        {
            if (isServer)
            {
                if (_peers.TryGetValue(target, out WebServerListener peer))
                {
                    peer.Send(data);
                }
            }
            else
            {
                if (webClient.State == WebClient.WebSocketState.Open)
                {
                    webClient.SendText(data);
                }
            }
        }

        internal void Disconnect(IPEndPoint endPoint)
        {
            if (isServer)
            {
                WebServerListener listener = _peers[endPoint];
                listener.Disconnect(Web.CloseStatusCode.Normal, "[WebTransporter] Normally closed.");
            }
            else
            {
                if (webClient.State == WebClient.WebSocketState.Open)
                {
                    webClient.Close();
                }
            }
        }

        internal WebSocketService GetDefaultWebSocketService(IPEndPoint endPoint)
        {
            if (isServer)
            {
                return _peers[endPoint];
            }

            throw new NullReferenceException();
        }

        public void SendP2P(ReadOnlySpan<byte> data, IPEndPoint target)
        {
            throw new NotSupportedException("[WebTransporter] P2P connections are not supported.");
        }

        internal async void AddWebSocketService<T>(string serviceName, Action<T> initializer)
            where T : WebSocketService, new()
        {
            if (!EnableWebSocket)
                return;

            await UniTask.WaitUntil(() => webSocketServer != null);
            webSocketServer.AddWebSocketService<T>(serviceName, initializer);
        }

        internal async void AddHttpService<T>(string serviceName, Action<T> initializer)
            where T : HttpService, new()
        {
            if (!EnableHttpServer)
                return;

            await UniTask.WaitUntil(() => httpServer != null);
            httpServer.AddWebSocketService<T>(serviceName, initializer);
        }

        internal void Internal_OnStringDataReceived(string data, IPEndPoint source, bool isServer)
        {
            OnStringDataReceived?.Invoke(data, source, isServer);
        }

        public async void Stop()
        {
            if (isRunning)
            {
                if (isServer)
                {
                    webSocketServer.Stop();
                    if (EnableHttpServer)
                    {
                        httpServer.Stop();
                    }
                }
                else
                {
                    await webClient.Close();
                }
            }
        }

        public void CopyTo(ITransporter ITransporter)
        {
            WebTransporter webTransporter = ITransporter as WebTransporter;
            if (webTransporter != null)
            {
                webTransporter.EnableWebSocketSsl = EnableWebSocketSsl;
            }
        }
    }
}