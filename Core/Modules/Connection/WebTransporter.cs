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
        protected override void OnOpen()
        {

        }

        protected override void OnMessage(Web.MessageEventArgs e)
        {
            Send(e.Data);
        }
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
                            $"Web Transporter: A secure connection(Ssl) was successfully established for {UserEndPoint}.");
                    }
#endif
                    if (!Peers.TryAdd(UserEndPoint, this))
                    {
                        NetworkLogger.__Log__(
                            $"Web Transporter: The peer: {UserEndPoint} is already connected.",
                            NetworkLogger.LogType.Error
                        );
                    }
                    else
                    {
                        IManager.Internal_OnServerPeerConnected(UserEndPoint, new NativePeer(() => 0, () => 0));
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
                            $"Web Transporter: The peer: {UserEndPoint} is already disconnected.",
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
                        IManager.Internal_OnDataReceived(e.RawData, DeliveryMode.ReliableOrdered, UserEndPoint, 0,
                            true, out _);
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
        internal event Action<HttpListenerRequest, HttpListenerResponse> OnGetRequest;
        internal event Action<HttpListenerRequest, HttpListenerResponse> OnPostRequest;

        private WebServer.HttpServer httpServer;
        private WebServer.WebSocketServer webSocketServer;
        private WebClient.WebSocket webClient;
        private readonly Dictionary<IPEndPoint, WebServerListener> _peers = new();

        [SerializeField]
        [Group("Ssl Settings")]
        [ReadOnly]
        private string certificateConfig = "cert_conf.json";

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
                throw new Exception("Web Transporter is already initialized.");
            }

            if ((EnableWebSocketSsl || EnableHttpServerSsl) && isServer)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
			return;
#endif
                try
                {
#if OMNI_DEBUG || UNITY_EDITOR || UNITY_SERVER
                    if (!NetworkHelper.CanHostServer())
                        return;

                    if (!File.Exists(certificateConfig))
                    {
                        using var fileStream = File.Create(certificateConfig);
                        using StreamWriter sw = new(fileStream);
                        sw.WriteLine("{\"cert\": \"cert.pfx\", \"password\": \"password for cert.pfx\"}");
                    }
#endif
                }
                catch (Exception ex)
                {
                    NetworkLogger.__Log__("Web Transporter: Failed to create the certificate configuration file. Exception: " + ex.Message, NetworkLogger.LogType.Error);
                }
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

                    httpServer.OnGet += (sender, e) => OnGetRequest?.Invoke(e.Request, e.Response);
                    httpServer.OnPost += (sender, e) => OnPostRequest?.Invoke(e.Request, e.Response);
                }

                webSocketServer =
                    new WebServer.WebSocketServer($"{(EnableWebSocketSsl ? "wss" : "ws")}://{IPAddress.Any}:{port}");

                if ((EnableWebSocket && EnableWebSocketSsl) || (EnableHttpServer && EnableHttpServerSsl))
                {
                    if (File.Exists(certificateConfig))
                    {
                        try
                        {
                            var dict = NetworkManager.FromJson<Dictionary<string, string>>(
                                File.ReadAllText(certificateConfig));

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
                                "Web Transporter: Failed to load SSL certificate. Exception: " + ex.Message,
                                NetworkLogger.LogType.Error);
                        }
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            "Web Transporter: Certificate configuration file not found at path: " + "./" +
                            certificateConfig, NetworkLogger.LogType.Error);
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
                        webSocketServer.Start();
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.__Log__("Web Transporter: Failed to start the WebSocket server. Exception: " + ex.Message, NetworkLogger.LogType.Error);
                    }
                }

                if (EnableHttpServer)
                {
                    try
                    {
                        httpServer.Start();
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        {
                            NetworkLogger.__Log__($"Web Transporter: Failed to start the HTTP server. The port {HttpServerPort} is already in use by another application.", NetworkLogger.LogType.Error);
                        }
                        else
                        {
                            NetworkLogger.__Log__("Web Transporter: Failed to start the HTTP server. Exception: " + ex.Message, NetworkLogger.LogType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.__Log__("Web Transporter: Failed to start the HTTP server. Exception: " + ex.Message, NetworkLogger.LogType.Error);
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
                    throw new NotSupportedException(
                        "Web Transporter: SSL is not supported for IP addresses. Please use a hostname(domain name) instead.");
                }
            }

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            webClient = new WebClient.WebSocket($"{(EnableWebSocketSsl ? "wss" : "ws")}://{address}:{port}");
            webClient.OnOpen += () =>
            {
                // Set to 'true' to indicate that the client is running.
                isRunning = true;
                IManager.Internal_OnClientConnected(localEndPoint, new NativePeer(() => 0, () => 0));
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
                listener.Disconnect(Web.CloseStatusCode.Normal, "Normally closed.");
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
                listener.Disconnect(Web.CloseStatusCode.Normal, "Normally closed.");
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
            throw new NotSupportedException("The web transporter does not support P2P connections.");
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
                webTransporter.certificateConfig = certificateConfig;
            }
        }
    }
}