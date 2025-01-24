using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Omni.Core.Interfaces;
using Omni.Core.Modules.Connection;
using Omni.Shared;
using UnityEngine;
using HttpListenerRequest = Omni.Core.Web.Net.HttpListenerRequest;
using HttpListenerResponse = Omni.Core.Web.Net.HttpListenerResponse;

#pragma warning disable

namespace Omni.Core.Web
{
    /// <summary>
    /// A component that manages WebSocket and HTTP communication, providing a unified interface 
    /// for server and client network operations in real-time applications.
    /// This component encapsulates the setup, configuration, and lifecycle management 
    /// of both WebSocket and HTTP transporters, ensuring efficient and reliable communication.
    /// </summary>
    [DisallowMultipleComponent]
    public class WebCommunicationManager : MonoBehaviour, ITransporterReceive
    {
        public class SocketConfiguration
        {
            internal readonly WebTransporter serverTransporter;
            internal readonly WebTransporter clientTransporter;

            internal SocketConfiguration(WebTransporter serverTransporter, WebTransporter clientTransporter)
            {
                this.serverTransporter = serverTransporter;
                this.clientTransporter = clientTransporter;
            }
        }

        public class WebSocketConfiguration : SocketConfiguration
        {
            public class WebSocketClient
            {
                internal SocketConfiguration configuration;

                public delegate void ReadOnlySpanAction(ReadOnlySpan<byte> span);

                /// <summary>
                /// Fired when the client connects to the server.
                /// </summary>
                public event Action OnClientConnected;

                /// <summary>
                /// Fired when the client disconnects from the server.
                /// </summary>
                /// <param name="reason">The reason for the disconnection.</param>
                public event Action<string> OnClientDisconnected;

                /// <summary>
                /// Fired when the client receives a string message from the server.
                /// </summary>
                /// <param name="data">The received string message.</param>
                public event Action<string> OnClientStringDataReceived;

                /// <summary>
                /// Fired when the client receives a raw byte array message from the server.
                /// </summary>
                /// <param name="span">The received byte array message.</param>
                public event ReadOnlySpanAction OnClientRawDataReceived;

                /// <summary>
                /// Fired when the client receives a data buffer from the server.
                /// </summary>
                /// <param name="data">The received data buffer.</param>
                public event Action<DataBuffer> OnClientDataReceived;

                /// <summary>
                /// Connects the client to the specified server address and port.
                /// </summary>
                /// <param name="address">The server address to connect to.</param>
                /// <param name="port">The server port to connect to.</param>
                public void Connect(string address, int port)
                {
                    configuration.clientTransporter.Connect(address, port);
                }

                /// <summary>
                /// Sends data to the server using a DataBuffer.
                /// </summary>
                /// <param name="data">The data buffer to send.</param>
                public void Send(DataBuffer data)
                {
                    configuration.clientTransporter.Send(data.BufferAsSpan, default, default, default);
                }

                /// <summary>
                /// Sends a string message to the server.
                /// </summary>
                /// <param name="data">The string message to send.</param>
                public void Send(string data)
                {
                    configuration.clientTransporter.Send(data, default);
                }

                /// <summary>
                /// Stops the client transporter.
                /// </summary>
                public void Stop()
                {
                    configuration.clientTransporter.Stop();
                }

                /// <summary>
                /// Disconnects the client from the server.
                /// </summary>
                public void Disconnect()
                {
                    configuration.clientTransporter.Disconnect(new IPEndPoint(IPAddress.Loopback, 0));
                }

                internal void FireClientConnected()
                {
                    OnClientConnected?.Invoke();
                }

                internal void FireClientDisconnected(string reason)
                {
                    OnClientDisconnected?.Invoke(reason);
                }

                internal void FireClientDataReceived(DataBuffer data)
                {
                    OnClientDataReceived?.Invoke(data);
                }

                internal void FireClientRawDataReceived(ReadOnlySpan<byte> data)
                {
                    OnClientRawDataReceived?.Invoke(data);
                }

                internal void FireClientStringDataReceived(string data)
                {
                    OnClientStringDataReceived?.Invoke(data);
                }
            }

            public class WebSocketServer
            {
                internal SocketConfiguration configuration;

                public delegate void ReadOnlySpanAction(ReadOnlySpan<byte> span, in IPEndPoint endPoint);

                public int Port { get; set; } = 8080;
                public event Action OnServerStarted;
                public event Action<IPEndPoint> OnServerPeerConnected;
                public event Action<IPEndPoint, string> OnServerPeerDisconnected;
                public event Action<string, IPEndPoint> OnServerStringDataReceived;
                public event ReadOnlySpanAction OnServerRawDataReceived;
                public event Action<DataBuffer, IPEndPoint> OnServerDataReceived;

                public void Stop()
                {
                    configuration.serverTransporter.Stop();
                }

                public void Disconnect(IPEndPoint endPoint)
                {
                    configuration.serverTransporter.Disconnect(endPoint);
                }

                public void Send(DataBuffer data, IPEndPoint endPoint)
                {
                    configuration.serverTransporter.Send(data.BufferAsSpan, endPoint, default, default);
                }

                public void Send(string data, IPEndPoint endPoint)
                {
                    configuration.serverTransporter.Send(data, endPoint);
                }

                public void AddWebSocketService<T>(string path) where T : WebSocketService, new()
                {
                    AddWebSocketService<T>(path, _ => { });
                }

                public void AddWebSocketService<T>(string path, Action<T> initializer) where T : WebSocketService, new()
                {
                    configuration.serverTransporter.AddWebSocketService(path, initializer);
                }

                public WebSocketService GetDefaultWebSocketService(IPEndPoint endPoint)
                {
                    return configuration.serverTransporter.GetDefaultWebSocketService(endPoint);
                }

                internal void FireServerStarted()
                {
                    OnServerStarted?.Invoke();
                }

                internal void FireServerPeerConnected(IPEndPoint endPoint)
                {
                    OnServerPeerConnected?.Invoke(endPoint);
                }

                internal void FireServerPeerDisconnected(IPEndPoint endPoint, string reason)
                {
                    OnServerPeerDisconnected?.Invoke(endPoint, reason);
                }

                internal void FireServerDataReceived(DataBuffer data, IPEndPoint endPoint)
                {
                    OnServerDataReceived?.Invoke(data, endPoint);
                }

                internal void FireServerRawDataReceived(ReadOnlySpan<byte> data, IPEndPoint endPoint)
                {
                    OnServerRawDataReceived?.Invoke(data, endPoint);
                }

                internal void FireServerStringDataReceived(string data, IPEndPoint endPoint)
                {
                    OnServerStringDataReceived?.Invoke(data, endPoint);
                }
            }

            public bool Enabled { get; set; } = true;
            public bool EnableSsl { get; set; } = false;

            public WebSocketClient Client { get; }
            public WebSocketServer Server { get; }

            internal WebSocketConfiguration(WebTransporter serverTransporter, WebTransporter clientTransporter) : base(
                serverTransporter, clientTransporter)
            {
                Client = new WebSocketClient
                {
                    configuration = this
                };

                Server = new WebSocketServer
                {
                    configuration = this
                };
            }
        }

        public class HttpServerConfiguration : SocketConfiguration
        {
            public bool Enabled { get; set; } = true;
            public bool EnableSsl { get; set; } = false;

            public int Port { get; set; } = 80;
            public string DocumentRootPath { get; set; } = Application.dataPath;
            public event Action<HttpListenerRequest, HttpListenerResponse, string> OnRequestHandled;

            internal Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>> GetRoutesAsync { get; } = new();
            internal Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>> PostRoutesAsync { get; } = new();

            internal HttpServerConfiguration(WebTransporter serverTransporter, WebTransporter clientTransporter) : base(
                serverTransporter, clientTransporter)
            {
                serverTransporter.OnGetRequest += OnGet;
                serverTransporter.OnPostRequest += OnPost;
            }

            public void AddHttpService<T>(string path) where T : HttpService, new()
            {
                AddHttpService<T>(path, _ => { });
            }

            public void AddHttpService<T>(string path, Action<T> initializer) where T : HttpService, new()
            {
                serverTransporter.AddHttpService(path, initializer);
            }

            /// <summary>
            /// Registers a route for HTTP GET requests with the given <paramref name="routeName"/>.
            /// The given callback is invoked when a GET request is received with a matching route.
            /// </summary>
            /// <param name="routeName">The route name to register the callback for.</param>
            /// <param name="callback">The callback to invoke when a GET request is received with a matching route.</param>
            public void GetAsync(string routeName, Func<HttpListenerRequest, HttpListenerResponse, Task> callback)
            {
                GetRoutesAsync[routeName] = callback;
            }

            /// <summary>
            /// Registers a route for HTTP GET requests with the given <paramref name="routeName"/>.
            /// The given callback is invoked when a GET request is received with a matching route.
            /// The callback is provided with the request and response objects, but does not have to return a value.
            /// </summary>
            /// <param name="routeName">The route name to register the callback for.</param>
            /// <param name="callback">The callback to invoke when a GET request is received with a matching route.</param>
            public void Get(string routeName, Action<HttpListenerRequest, HttpListenerResponse> callback)
            {
                GetRoutesAsync[routeName] = async (req, res) =>
                {
                    callback(req, res);
                };
            }

            /// <summary>
            /// Registers a route for HTTP POST requests with the given <paramref name="routeName"/>.
            /// The given callback is invoked when a POST request is received with a matching route.
            /// </summary>
            /// <param name="routeName">The route name to register the callback for.</param>
            /// <param name="callback">The callback to invoke when a POST request is received with a matching route.</param>
            public void PostAsync(string routeName, Func<HttpListenerRequest, HttpListenerResponse, Task> callback)
            {
                PostRoutesAsync[routeName] = callback;
            }

            /// <summary>
            /// Registers a route for HTTP POST requests with the given <paramref name="routeName"/>.
            /// The given callback is invoked when a POST request is received with a matching route.
            /// The callback is provided with the request and response objects, but does not have to return a value.
            /// </summary>
            /// <param name="routeName">The route name to register the callback for.</param>
            /// <param name="callback">The callback to invoke when a POST request is received with a matching route.</param>
            public void Post(string routeName, Action<HttpListenerRequest, HttpListenerResponse> callback)
            {
                PostRoutesAsync[routeName] = async (req, res) =>
                {
                    callback(req, res);
                };
            }

            private async void OnGet(HttpListenerRequest req, HttpListenerResponse res)
            {
                try
                {
                    SetDefaultOptions(req, res);
                    string path = req.RawUrl;
                    if (path.StartsWith("/"))
                    {
                        int parameterCount = req.QueryString.Count;
                        // If the path contains parameters (i.e. ?key=value), we need to strip them from the path.
                        // This is because the route name should not include the parameters.
                        if (parameterCount > 0)
                        {
                            int indexOf = path.IndexOf('?');
                            if (indexOf > -1)
                            {
                                path = path.Substring(0, indexOf);
                            }
                        }

                        if (GetRoutesAsync.TryGetValue(path, out var getCallback))
                        {
                            SetDefaultOptions(req, res, path);
                            await getCallback(req, res);
                        }
                        else
                        {
                            res.Reject($"The requested route(GET) '{path}' was not found. Please verify the URL and try again.");
                        }
                    }
                    else
                    {
                        res.Reject("The server only supports route-based requests. Static file serving is not available.");
                    }
                }
                catch (Exception ex)
                {
                    NetworkLogger.PrintHyperlink(ex);
                    NetworkLogger.__Log__(ex.Message, NetworkLogger.LogType.Error);
                    res.Reject("An unexpected error occurred while processing the request. Please try again later.");
                }
            }

            private async void OnPost(HttpListenerRequest req, HttpListenerResponse res)
            {
                try
                {
                    SetDefaultOptions(req, res);
                    string path = req.RawUrl;
                    if (path.StartsWith("/"))
                    {
                        if (PostRoutesAsync.TryGetValue(path, out var postCallback))
                        {
                            SetDefaultOptions(req, res, path);
                            await postCallback(req, res);
                        }
                        else
                        {
                            res.Reject($"The requested route(POST) '{path}' was not found. Please verify the URL and try again. Ensure the path does not include unexpected query parameters(QueryString) or invalid characters.");
                        }
                    }
                    else
                    {
                        res.Reject("Invalid path format. The server only supports routes starting with '/'.");
                    }
                }
                catch (Exception ex)
                {
                    NetworkLogger.PrintHyperlink(ex);
                    NetworkLogger.__Log__(ex.Message, NetworkLogger.LogType.Error);
                    res.Reject("An unexpected error occurred while processing the request. Please try again later.");
                }
            }

            private void SetDefaultOptions(HttpListenerRequest req, HttpListenerResponse res, string path = null)
            {
                res.SetHeader("server", "Omni Server");
                res.KeepAlive = true;
                // Allows the client to modify or override the default options if a path is specified.
                if (!string.IsNullOrEmpty(path))
                    OnRequestHandled?.Invoke(req, res, path);
            }
        }

        private WebTransporter serverTransporter;
        private WebTransporter clientTransporter;

        protected WebSocketConfiguration WebSocket { get; private set; }
        protected HttpServerConfiguration HttpServer { get; private set; }

        /// <summary>
        /// Initializes and starts the network services for both WebSocket and HTTP communication.
        /// </summary>
        /// <param name="onSetup">An optional setup action that allows customization of WebSocket and HTTP server configurations.</param>
        protected void StartServices(Action<WebSocketConfiguration, HttpServerConfiguration> onSetup = null)
        {
            // Create the transporters
            GameObject clientObject = new("WebClient Transporter");
            GameObject serverObject = new("WebServer Transporter");

            clientObject.hideFlags = serverObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            clientObject.transform.parent = serverObject.transform.parent = transform;

            clientTransporter = clientObject.AddComponent<WebTransporter>();
            serverTransporter = serverObject.AddComponent<WebTransporter>();

            WebSocket = new WebSocketConfiguration(serverTransporter, clientTransporter);
            HttpServer = new HttpServerConfiguration(serverTransporter, clientTransporter);

            onSetup?.Invoke(WebSocket, HttpServer);
            DontDestroyOnLoad(gameObject);

            if (HttpServer.EnableSsl && HttpServer.Port == 80)
            {
                HttpServer.Port = 443;
            }

            serverTransporter.EnableWebSocket = clientTransporter.EnableWebSocket = WebSocket.Enabled;
            serverTransporter.EnableWebSocketSsl = clientTransporter.EnableWebSocketSsl = WebSocket.EnableSsl;

            serverTransporter.EnableHttpServer = clientTransporter.EnableHttpServer = HttpServer.Enabled;
            serverTransporter.EnableHttpServerSsl = clientTransporter.EnableHttpServerSsl = HttpServer.EnableSsl;

            serverTransporter.HttpServerDocumentRootPath =
                clientTransporter.HttpServerDocumentRootPath = HttpServer.DocumentRootPath;
            serverTransporter.HttpServerPort = clientTransporter.HttpServerPort = HttpServer.Port;

            serverTransporter.OnStringDataReceived += OnStringDataReceived;
            clientTransporter.OnStringDataReceived += OnStringDataReceived;

            serverTransporter.Initialize(this, isServer: true);
            clientTransporter.Initialize(this, isServer: false);
            serverTransporter.Listen(WebSocket.Server.Port);
        }

        /// <summary>
        /// Stops all networking services.
        /// </summary>
        /// <remarks>
        /// This method should be called when the application is about to quit.
        /// </remarks>
        protected void StopServices()
        {
            serverTransporter.Stop();
            clientTransporter.Stop();
        }

        public void Internal_OnP2PDataReceived(ReadOnlySpan<byte> data, IPEndPoint source)
        {
            // Not supported
        }

        private void OnStringDataReceived(string data, IPEndPoint source, bool isServer)
        {
            if (isServer)
            {
                WebSocket.Server.FireServerStringDataReceived(data, source);
            }
            else
            {
                WebSocket.Client.FireClientStringDataReceived(data);
            }
        }

        public void Internal_OnDataReceived(ReadOnlySpan<byte> data, DeliveryMode deliveryMethod, IPEndPoint source,
            byte sequenceChannel,
            bool isServer, out byte msgType)
        {
            using DataBuffer message = NetworkManager.Pool.Rent();
            message.WriteRawBytes(data);
            message.SeekToBegin();

            if (isServer)
            {
                WebSocket.Server.FireServerDataReceived(message, source);
                WebSocket.Server.FireServerRawDataReceived(data, source);
            }
            else
            {
                WebSocket.Client.FireClientDataReceived(message);
                WebSocket.Client.FireClientRawDataReceived(data);
            }

            msgType = 0;
        }

        public void Internal_OnServerInitialized()
        {
            WebSocket.Server.FireServerStarted();
        }

        public void Internal_OnClientConnected(IPEndPoint peer, NativePeer nativePeer)
        {
            WebSocket.Client.FireClientConnected();
        }

        public void Internal_OnClientDisconnected(IPEndPoint peer, string reason)
        {
            WebSocket.Client.FireClientDisconnected(reason);
        }

        public void Internal_OnServerPeerConnected(IPEndPoint peer, NativePeer nativePeer)
        {
            WebSocket.Server.FireServerPeerConnected(peer);
        }

        public void Internal_OnServerPeerDisconnected(IPEndPoint peer, string reason)
        {
            WebSocket.Server.FireServerPeerDisconnected(peer, reason);
        }

        protected virtual void OnApplicationQuit()
        {
            StopServices();
        }
    }
}