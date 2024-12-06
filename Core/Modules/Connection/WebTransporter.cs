using Omni.Core.Attributes;
using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using WebClient = NativeWebSocket;
using WebServer = WebSocketSharp.Server;

#pragma warning disable

namespace Omni.Core.Modules.Connection
{
	[DefaultExecutionOrder(-1100)]
	[DisallowMultipleComponent]
	[AddComponentMenu("Omni/Transporters/Web Transporter")]
	internal class WebTransporter : TransporterBehaviour, ITransporter
	{
		private class WebServerListener : WebServer.WebSocketBehavior
		{
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
						NetworkLogger.__Log__($"Web Transporter: A secure connection was successfully established for {UserEndPoint}.");
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
						IManager.Internal_OnServerPeerConnected(
						UserEndPoint,
						new NativePeer(
							() => 0,
							() => 0
						));
					}
				});
			}

			protected override void OnClose(WebSocketSharp.CloseEventArgs e)
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

			protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
			{
				if (e.IsBinary && !e.IsPing)
				{
					NetworkHelper.RunOnMainThread(() =>
					{
						IManager.Internal_OnDataReceived(e.RawData, DeliveryMode.ReliableOrdered, UserEndPoint, 0, true, out _);
					});
				}
			}

			internal void Send(byte[] data)
			{
				base.Send(data);
			}

			internal void Disconnect(WebSocketSharp.CloseStatusCode statusCode, string reason)
			{
				base.Close(statusCode, reason);
			}
		}

		private bool isServer;
		private bool isRunning;

		private ITransporterReceive IManager;

		private WebServer.WebSocketServer webServer;
		private WebClient.WebSocket webClient;
		private readonly Dictionary<IPEndPoint, WebServerListener> _peers = new();

		[SerializeField]
		[ReadOnly]
		private string certificateConfig = "cert_conf.json";

		[SerializeField]
		private bool enableSsl = false;

		public void Initialize(ITransporterReceive IManager, bool isServer)
		{
			this.isServer = isServer;
			this.IManager = IManager;

			if (isRunning)
			{
				throw new Exception("Web Transporter is already initialized.");
			}

			if (enableSsl && isServer)
			{
				if (!File.Exists(certificateConfig))
				{
					using var fileStream = File.Create(certificateConfig);
					using StreamWriter sw = new(fileStream);
					sw.WriteLine("{\"cert\": \"cert.pfx\", \"password\": \"password for cert.pfx\"}");
				}
			}
		}

		public void Listen(int port)
		{
			if (isServer)
			{
				webServer = new WebServer.WebSocketServer($"{(enableSsl ? "wss" : "ws")}://{IPAddress.Any}:{port}");
				if (enableSsl)
				{
					if (File.Exists(certificateConfig))
					{
						try
						{
							var dict = NetworkManager.FromJson<Dictionary<string, string>>(File.ReadAllText(certificateConfig));
							webServer.SslConfiguration.ServerCertificate = new X509Certificate2(dict["cert"], dict["password"]);
						}
						catch (Exception ex)
						{
							NetworkLogger.__Log__("Web Transporter: Failed to load SSL certificate. Exception: " + ex.Message, NetworkLogger.LogType.Error);
						}
					}
					else
					{
						NetworkLogger.__Log__("Web Transporter: Certificate configuration file not found at path: " + "./" + certificateConfig, NetworkLogger.LogType.Error);
					}
				}

				webServer.AddWebSocketService<WebServerListener>("/", (listener) =>
				{
					listener.IManager = IManager;
					listener.Peers = _peers;
					listener.WebServer = webServer;
				});

				webServer.Start();
				// Set to 'true' to indicate that the server is running.
				isRunning = true;
				IManager.Internal_OnServerInitialized();
			}
		}

		private void Awake()
		{
			ITransporter = this;
		}

		void Update()
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
			if (enableSsl)
			{
				if (IPAddress.TryParse(address, out _))
				{
					throw new NotSupportedException("Web Transporter: SSL is not supported for IP addresses. Please use a hostname(domain name) instead.");
				}
			}

			IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, port);
			webClient = new WebClient.WebSocket($"{(enableSsl ? "wss" : "ws")}://{address}:{port}");
			webClient.OnOpen += () =>
			{
				// Set to 'true' to indicate that the client is running.
				isRunning = true;
				IManager.Internal_OnClientConnected(
					localEndPoint,
					new NativePeer(
						() => 0,
						() => 0
					)
				);
			};

			webClient.OnMessage += (data) =>
			{
				IManager.Internal_OnDataReceived(data, DeliveryMode.ReliableOrdered, localEndPoint, 0, false, out _);
			};

			webClient.OnClose += (e) =>
			{
				IManager.Internal_OnClientDisconnected(localEndPoint, $"code: {e}");
			};

			await webClient.Connect(); // While Loop -> 'Receive()' when is opened.
		}

		public void Disconnect(NetworkPeer peer)
		{
			if (isServer)
			{
				WebServerListener listener = _peers[peer.EndPoint];
				listener.Disconnect(WebSocketSharp.CloseStatusCode.Normal, "Normally closed.");
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

		public void SendP2P(ReadOnlySpan<byte> data, IPEndPoint target)
		{
			throw new NotSupportedException("The web transporter does not support P2P connections.");
		}

		public async void Stop()
		{
			if (isRunning)
			{
				if (isServer)
				{
					webServer.Stop();
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
			webTransporter.enableSsl = enableSsl;
			webTransporter.certificateConfig = certificateConfig;
		}
	}
}