using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;
using Omni.Core.Cryptography;
using Omni.Core.Interfaces;
using Omni.Core.Modules.Connection;
using Omni.Core.Modules.Matchmaking;
using Omni.Core.Modules.Ntp;
using Omni.Core.Modules.UConsole;
using Omni.Shared;
using Omni.Shared.Collections;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable

namespace Omni.Core
{
	[DefaultExecutionOrder(-1000)]
	[DisallowMultipleComponent]
	[JsonObject(MemberSerialization.OptIn)]
	public partial class NetworkManager : MonoBehaviour, ITransporterReceive
	{
		private static Stopwatch _stopwatch = new Stopwatch();
		private static bool _allowLoadScene;

		public static bool UseTickTiming { get; private set; } = false;
		internal static float DeltaTime =>
			UseTickTiming ? (float)TickSystem.DeltaTick : UnityEngine.Time.deltaTime;

		public static double ClockTime =>
			UseTickTiming ? TickSystem.ElapsedTicks : _stopwatch.Elapsed.TotalSeconds; // does not depend on frame rate.

		public static int MainThreadId { get; private set; }
		public static IObjectPooling<DataBuffer> Pool { get; private set; }

		public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
		public static event Action<Scene> OnSceneUnloaded;
		public static event Action<Scene, SceneOperationMode> OnBeforeSceneLoad;

		public static event Action OnServerInitialized;
		public static event Action<NetworkPeer, Phase> OnServerPeerConnected;
		public static event Action<NetworkPeer, Phase> OnServerPeerDisconnected;
		public static event Action OnClientConnected;
		public static event Action<string> OnClientDisconnected;

		public static event Action<NetworkIdentity> OnClientIdentitySpawned;

		private static event Action<byte, DataBuffer, NetworkPeer, int> OnServerCustomMessage;
		private static event Action<byte, DataBuffer, int> OnClientCustomMessage;

		internal static event Action<string, DataBuffer> OnJoinedGroup; // for client
		internal static event Action<DataBuffer, NetworkGroup, NetworkPeer> OnPlayerJoinedGroup; // for server
		internal static event Action<NetworkPeer, string> OnPlayerFailedJoinGroup; // for server
		internal static event Action<string, string> OnLeftGroup; // for client
		internal static event Action<NetworkGroup, NetworkPeer, Phase, string> OnPlayerLeftGroup; // for server
		internal static event Action<NetworkPeer, string> OnPlayerFailedLeaveGroup;

		static NetworkConsole _console;
		public static NetworkConsole Console
		{
			get
			{
				if (_console == null)
				{
					throw new Exception(
						"Console module not initialized. Please call NetworkManager.InitializeModule(Module.Console) at least once before accessing the Console."
					);
				}

				return _console;
			}
			private set
			{
				if (_console != null)
				{
					throw new Exception(
						"Console module already initialized. Please call NetworkManager.InitializeModule(Module.Console) only once."
					);
				}

				_console = value;
			}
		}

		static NetworkConnection _connection;
		private static NetworkConnection Connection
		{
			get
			{
				if (_connection == null)
				{
					throw new Exception(
						"Connection module not initialized. Please call NetworkManager.InitializeModule(Module.Connection) at least once before accessing the Connection."
					);
				}

				return _connection;
			}
			set
			{
				if (_connection != null)
				{
					throw new Exception(
						"Connection module already initialized. Please call NetworkManager.InitializeModule(Module.Connection) only once."
					);
				}

				_connection = value;
			}
		}

		static NetworkMatchmaking _matchmaking;
		public static NetworkMatchmaking Matchmaking
		{
			get
			{
				if (_matchmaking == null)
				{
					throw new Exception(
						"Matchmaking module not initialized. Please call NetworkManager.InitializeModule(Module.Matchmaking) at least once before accessing the Matchmaking."
					);
				}

				return _matchmaking;
			}
			set
			{
				if (_matchmaking != null)
				{
					throw new Exception(
						"Matchmaking module already initialized. Please call NetworkManager.InitializeModule(Module.Matchmaking) only once."
					);
				}

				_matchmaking = value;
			}
		}

		static SimpleNtp _ntpClock;
		public static SimpleNtp Sntp
		{
			get
			{
				if (_ntpClock == null)
				{
					throw new Exception(
						"NtpClock module not initialized. Please call NetworkManager.InitializeModule(Module.NtpClock) at least once before accessing the NtpClock."
					);
				}

				return _ntpClock;
			}
			set
			{
				if (_ntpClock != null)
				{
					throw new Exception(
						"NtpClock module already initialized. Please call NetworkManager.InitializeModule(Module.NtpClock) only once."
					);
				}

				_ntpClock = value;
			}
		}

		static NetworkTickSystem _tickSystem;
		public static NetworkTickSystem TickSystem
		{
			get
			{
				if (_tickSystem == null)
				{
					throw new Exception(
						"TickSystem module not initialized. Please ensure that NetworkManager.InitializeModule(Module.TickSystem) is called at least once before accessing the TickSystem. If you are a client, wait until the connection and authentication processes are completed before accessing the TickSystem."
					);
				}

				return _tickSystem;
			}
			set
			{
				if (_tickSystem != null)
				{
					throw new Exception(
						"TickSystem module already initialized. Please call NetworkManager.InitializeModule(Module.TickSystem) only once."
					);
				}

				_tickSystem = value;
			}
		}

		/// <summary>
		/// Gets or sets the native peer.
		/// This property stores an instance of the NativePeer class, which represents a low-level peer in the network.
		/// It is used to manage native networking operations and interactions.
		/// </summary>
		static NativePeer LocalNativePeer { get; set; }
		static NetworkPeer _localPeer;

		/// <summary>
		/// Gets the local network peer.
		/// This property stores an instance of the NetworkPeer class, representing the local peer in the network.
		/// </summary>
		public static NetworkPeer LocalPeer
		{
			get
			{
				return _localPeer
					?? throw new Exception(
						"Client(LocalPeer) is neither active, nor authenticated. Please verify using NetworkManager.IsClientActive."
					);
			}
			private set => _localPeer = value;
		}

		/// <summary>
		/// Gets the shared peer, used to secure communication between peers and the server.
		/// Useful for encryption and authentication.
		/// </summary>
		public static NetworkPeer SharedPeer
		{
			get
			{
				if (IsClientActive && IsServerActive)
				{
					return Server.ServerPeer;
				}

				if (IsClientActive)
				{
					return Client.ServerPeer;
				}

				return Server.ServerPeer;
			}
		}

		/// <summary>
		/// Gets the local endpoint.
		/// This property stores an instance of the IPEndPoint class, representing the local network endpoint.
		/// It is used to specify the IP address and port number of the local peer.
		/// </summary>
		public static IPEndPoint LocalEndPoint { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the client is active.
		/// This property returns true if the client is currently active, authenticated and connected; otherwise, false.
		/// It is used to determine the client's connection status in the network.
		/// </summary>
		public static bool IsClientActive { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the server is active.
		/// This property returns true if the server is currently active; otherwise, false.
		/// It is used to determine the server's status in the network.
		/// </summary>
		public static bool IsServerActive { get; private set; }

		protected virtual void Awake()
		{
			NetworkHelper.LoadComponent(this, "setup.cfg");
			if (_manager != null)
			{
				gameObject.SetActive(false);
				Destroy(gameObject, 1f);
				return;
			}

			Pool = new DataBufferPool(m_PoolCapacity, m_PoolSize);
			BufferWriterExtensions.UseBinarySerialization = m_UseBinarySerialization;
			BufferWriterExtensions.EnableBandwidthOptimization = m_EnableBandwidthOptimization;
			BufferWriterExtensions.UseUnalignedMemory = m_UseUnalignedMemory;
			BufferWriterExtensions.DefaultEncoding = m_UseUtf8 ? Encoding.UTF8 : Encoding.ASCII;

			if (!UseTickTiming)
			{
				_stopwatch.Start();
			}

			_manager = this;
#if !UNITY_SERVER || UNITY_EDITOR
			if (m_MaxFpsOnClient > 0)
			{
				QualitySettings.vSyncCount = 0;
				Application.targetFrameRate = m_MaxFpsOnClient;
			}
#else
            NetworkLogger.__Log__(
                $"MaxFpsOnClient is set to {m_MaxFpsOnClient}. This setting is ignored on server build.",
                NetworkLogger.LogType.Warning
            );
#endif
			AotHelper.EnsureDictionary<string, object>();
			MainThreadId = Thread.CurrentThread.ManagedThreadId;
			DisableAutoStartIfHasHud();

			if (m_ConsoleModule)
			{
				InitializeModule(Module.Console);
			}

			if (m_SntpModule)
			{
				InitializeModule(Module.NtpClock);
			}

			if (m_TickModule)
			{
				InitializeModule(Module.TickSystem);
			}

			if (m_MatchModule)
			{
				InitializeModule(Module.Matchmaking);
			}

			// This module should be initialized last, as it needs the other modules to be initialized.
			if (m_Connection)
			{
				HttpLite.Initialize();
				InitializeModule(Module.Connection);
			}

			// Used to perform some operations before the scene is loaded.
			// for example: removing registered events. (:

			int sceneIndex = 0;
			SceneManager.sceneLoaded += (scene, mode) =>
			{
				if (!_allowLoadScene && sceneIndex > 0)
				{
					throw new NotSupportedException(
						"Use 'NetworkManager.LoadScene() or NetworkManager.LoadSceneAsync()' to load a scene instead of 'SceneManager.LoadScene().'"
					);
				}

				if (sceneIndex > 0)
				{
					_allowLoadScene = false;
					OnSceneLoaded?.Invoke(scene, mode);
				}

				sceneIndex++;
			};

			SceneManager.sceneUnloaded += (scene) => OnSceneUnloaded?.Invoke(scene);
		}

		protected virtual void Start()
		{
#if OMNI_SERVER && !UNITY_EDITOR
            SkipDefaultUnityLog();
            ShowDefaultOmniLog();
#endif
		}

		private void Update()
		{
			if (m_TickModule && _tickSystem != null)
			{
				TickSystem.OnTick();
			}

			UpdateFrameAndCpuMetrics();
		}

		private void UpdateFrameAndCpuMetrics()
		{
			deltaTime += UnityEngine.Time.unscaledDeltaTime;
			frameCount++;

			float rate = 1f;
			if (deltaTime >= rate)
			{
				Framerate = frameCount / deltaTime;
				CpuTimeMs = deltaTime / frameCount * 1000f;

				deltaTime -= rate;
				frameCount = 0;
			}
		}

		private void SkipDefaultUnityLog()
		{
			System.Console.Clear();
		}

		private void ShowDefaultOmniLog()
		{
			NetworkLogger.Log("Welcome to Omni Server Console.");
#if OMNI_DEBUG
			NetworkLogger.Log("You are in Debug Mode.");
#else
            NetworkLogger.Log("You are in Release Mode.");
#endif
			System.Console.Write("\n");
		}

		public static void InitializeModule(Module module)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			switch (module)
			{
				case Module.TickSystem:
					{
						if (_tickSystem == null)
						{
							TickSystem = new NetworkTickSystem();
							TickSystem.Initialize(Manager.m_TickRate);
						}
					}
					break;
				case Module.NtpClock:
					{
						NetworkClock nClock = Manager.GetComponent<NetworkClock>();
						if (nClock != null)
						{
							Manager.m_QueryInterval = nClock.QueryInterval;
							UseTickTiming = nClock.UseTickTiming;
						}

						Sntp = new SimpleNtp();
						Sntp.Initialize(nClock);
					}
					break;
				case Module.Console:
					{
						Console = new NetworkConsole();
						Console.Initialize();
					}
					break;
				case Module.Connection:
					{
						if (
							!Manager.TryGetComponent<TransporterBehaviour>(
								out var currentTransporter
							)
						)
						{
							throw new Exception(
								"No transporter found on NetworkManager. Please add one."
							);
						}

						GameObject clientTransporter = new("Client Transporter");
						GameObject serverTransporter = new("Server Transporter");

						clientTransporter.hideFlags =
							HideFlags.HideInHierarchy | HideFlags.HideInInspector;
						serverTransporter.hideFlags =
							HideFlags.HideInHierarchy | HideFlags.HideInInspector;

						clientTransporter.transform.parent = Manager.transform;
						serverTransporter.transform.parent = Manager.transform;

						clientTransporter.AddComponent(currentTransporter.GetType());
						serverTransporter.AddComponent(currentTransporter.GetType());

						Manager.m_ClientTransporter =
							clientTransporter.GetComponent<TransporterBehaviour>();

						Manager.m_ServerTransporter =
							serverTransporter.GetComponent<TransporterBehaviour>();

						currentTransporter.ITransporter.CopyTo(
							Manager.m_ClientTransporter.ITransporter
						);

						currentTransporter.ITransporter.CopyTo(
							Manager.m_ServerTransporter.ITransporter
						);

						// Don't destroy the manager!
						DontDestroyOnLoad(Manager);

						Connection = new NetworkConnection();
						Connection.Server.StartTransporter(
							Manager.m_ServerTransporter.ITransporter,
							Manager
						);

						Connection.Client.StartTransporter(
							Manager.m_ClientTransporter.ITransporter,
							Manager
						);

#if OMNI_RELEASE
                        Manager.m_AutoStartServer = true;
                        Manager.m_AutoStartClient = true;
#else
						if (
							ConnectAddress.ToLower() != "localhost"
							&& ConnectAddress != "127.0.0.1"
							&& ConnectAddress != Manager.PublicIPv4
							&& ConnectAddress != Manager.PublicIPv6
						)
						{
							Manager.m_AutoStartServer = false;
							NetworkLogger.__Log__(
								"Server auto-start has been disabled as the client address is not a recognized localhost or public IPv4/IPv6 address. "
									+ "Starting a server in this case does not make sense because the client cannot connect to it. But you can start it manually.",
								NetworkLogger.LogType.Warning
							);
						}
#endif

						if (Manager.m_AutoStartServer)
						{
							StartServer(Manager.m_ServerListenPort);
						}

						if (Manager.m_AutoStartClient)
						{
							Connect(
								Manager.m_ConnectAddress,
								Manager.m_ConnectPort,
								Manager.m_ClientListenPort
							);
						}
					}
					break;
				case Module.Matchmaking:
					{
						Matchmaking = new NetworkMatchmaking();
						Matchmaking.Initialize();
					}
					break;
			}
		}

		public static void StartServer()
		{
			StartServer(Manager.m_ServerListenPort);
		}

		public static void StartServer(int port)
		{
			if (!IsServerActive)
			{
#if OMNI_DEBUG
				Server.GenerateRsaKeys();
				Connection.Server.Listen(port);
				NetworkHelper.SaveComponent(_manager, "setup.cfg");
#else
#if UNITY_EDITOR
                Server.GenerateRsaKeys();
                Connection.Server.Listen(port);
                NetworkHelper.SaveComponent(_manager, "setup.cfg");
#elif !UNITY_SERVER
                NetworkLogger.LogToFile(
                    "Server is not available in 'release mode' on client build."
                );
#else
                Server.GenerateRsaKeys();
                Connection.Server.Listen(port);
                NetworkHelper.SaveComponent(_manager, "setup.cfg");
#endif
#endif
			}
			else
			{
				throw new Exception(
					"Server is already initialized. Ensure to call StopServer() before calling StartServer()."
				);
			}
		}

		public static void DisconnectPeer(NetworkPeer peer)
		{
			if (IsServerActive)
			{
				Connection.Server.Disconnect(peer);
			}
			else
			{
				throw new Exception("Server is not initialized. Ensure to call StartServer().");
			}
		}

		public static void StopServer()
		{
			if (IsServerActive)
			{
				Connection.Server.Stop();
			}
			else
			{
				throw new Exception(
					"Server is not initialized. Ensure to call StartServer() before calling StopServer()."
				);
			}
		}

		public static void Connect(string address, int port)
		{
			Connect(address, port, Manager.m_ClientListenPort);
		}

		public static void Connect(string address, int port, int listenPort)
		{
			if (!IsClientActive)
			{
#if !UNITY_SERVER || UNITY_EDITOR // Don't connect to the server in server build!
				Connection.Client.Listen(listenPort);
				Connection.Client.Connect(address, port);
#elif UNITY_SERVER && !UNITY_EDITOR
                NetworkLogger.__Log__("Client is not available in a server build.");
#endif
			}
			else
			{
				throw new Exception(
					"Client is already initialized. Ensure to call StopClient() before calling Connect()."
				);
			}
		}

		public static void Disconnect()
		{
			if (IsClientActive)
			{
				Connection.Client.Disconnect(LocalPeer);
			}
			else
			{
				throw new Exception("Client is not initialized. Ensure to call Connect().");
			}
		}

		public static void StopClient()
		{
			if (IsClientActive)
			{
				Connection.Client.Stop();
			}
			else
			{
				throw new Exception(
					"Client is not initialized. Ensure to call Connect() before calling StopClient()."
				);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void SendToClient(
			byte msgType,
			DataBuffer buffer,
			NetworkPeer sender,
			Target target,
			DeliveryMode deliveryMode,
			int groupId,
			DataCache dataCache,
			byte sequenceChannel
		)
		{
			Manager.Internal_SendToClient(
				msgType,
				buffer.BufferAsSpan,
				sender,
				target,
				deliveryMode,
				groupId,
				dataCache,
				sequenceChannel
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void SendToServer(
			byte msgType,
			DataBuffer buffer,
			DeliveryMode deliveryMode,
			byte sequenceChannel
		)
		{
			Manager.Internal_SendToServer(
				msgType,
				buffer.BufferAsSpan,
				deliveryMode,
				sequenceChannel
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected virtual ReadOnlySpan<byte> PrepareClientMessageForSending(
			byte msgType,
			ReadOnlySpan<byte> message
		)
		{
			using DataBuffer header = Pool.Rent();
			header.Write(msgType);
			header.Write(message);
			return header.BufferAsSpan;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected virtual ReadOnlySpan<byte> PrepareServerMessageForSending(
			byte msgType,
			ReadOnlySpan<byte> message
		)
		{
			using DataBuffer header = Pool.Rent();
			header.Write(msgType);
			header.Write(message);
			return header.BufferAsSpan;
		}

		protected virtual void Internal_SendToClient(
			byte msgType,
			ReadOnlySpan<byte> _data,
			NetworkPeer sender,
			Target target,
			DeliveryMode deliveryMode,
			int groupId,
			DataCache dataCache,
			byte sequenceChannel
		)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			void Send(ReadOnlySpan<byte> message, NetworkPeer sender)
			{
				Connection.Server.Send(message, sender.EndPoint, deliveryMode, sequenceChannel);
			}

			NetworkCache GetCache(ReadOnlySpan<byte> message)
			{
				return new NetworkCache(
					dataCache.Id,
					dataCache.Mode,
					message.ToArray(),
					sender,
					deliveryMode,
					target,
					sequenceChannel,
					destroyOnDisconnect: dataCache.Mode.HasFlag(CacheMode.AutoDestroy)
				);
			}

			void CreateCache(ReadOnlySpan<byte> message, NetworkGroup _group)
			{
				if (dataCache.Mode != CacheMode.None || dataCache.Id != 0)
				{
					if (
						(dataCache.Id != 0 && dataCache.Mode == CacheMode.None)
						|| (dataCache.Mode != CacheMode.None && dataCache.Id == 0)
					)
					{
						throw new Exception(
							"Cache Error: Both dataCache.Id and dataCache.Mode must be set together."
						);
					}
					else
					{
						if (
							dataCache.Mode == (CacheMode.Global | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							Server.CACHES_APPEND_GLOBAL.Add(GetCache(message));
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.New)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							if (_group != null)
							{
								_group.CACHES_APPEND.Add(GetCache(message));
							}
							else
							{
								NetworkLogger.__Log__(
									"Cache Error: The specified group was not found. Please verify the existence of the group and ensure the groupId is correct.",
									NetworkLogger.LogType.Error
								);
							}
						}
						else if (
							dataCache.Mode == (CacheMode.Global | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Global | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							NetworkCache newCache = GetCache(message);
							if (Server.CACHES_OVERWRITE_GLOBAL.ContainsKey(dataCache.Id))
							{
								Server.CACHES_OVERWRITE_GLOBAL[dataCache.Id] = newCache;
							}
							else
							{
								Server.CACHES_OVERWRITE_GLOBAL.Add(dataCache.Id, newCache);
							}
						}
						else if (
							dataCache.Mode == (CacheMode.Group | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Group | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							if (_group != null)
							{
								NetworkCache newCache = GetCache(message);
								if (_group.CACHES_OVERWRITE.ContainsKey(dataCache.Id))
								{
									_group.CACHES_OVERWRITE[dataCache.Id] = newCache;
								}
								else
								{
									_group.CACHES_OVERWRITE.Add(dataCache.Id, newCache);
								}
							}
							else
							{
								NetworkLogger.__Log__(
									"Cache Error: The specified group was not found. Please verify the existence of the group and ensure the groupId is correct.",
									NetworkLogger.LogType.Error
								);
							}
						}
						else if (
							dataCache.Mode == (CacheMode.Peer | CacheMode.Overwrite)
							|| dataCache.Mode
								== (CacheMode.Peer | CacheMode.Overwrite | CacheMode.AutoDestroy)
						)
						{
							NetworkCache newCache = GetCache(message);
							if (sender.CACHES_OVERWRITE.ContainsKey(dataCache.Id))
							{
								sender.CACHES_OVERWRITE[dataCache.Id] = newCache;
							}
							else
							{
								sender.CACHES_OVERWRITE.Add(dataCache.Id, newCache);
							}
						}
						else if (
							dataCache.Mode == (CacheMode.Peer | CacheMode.New)
							|| dataCache.Mode == (CacheMode.Peer | CacheMode.New | CacheMode.AutoDestroy)
						)
						{
							sender.CACHES_APPEND.Add(GetCache(message));
						}
						else
						{
							NetworkLogger.__Log__(
								"Cache Error: Unsupported cache mode set.",
								NetworkLogger.LogType.Error
							);
						}
					}
				}
			}

			ReadOnlySpan<byte> message = PrepareServerMessageForSending(msgType, _data);
			bool cacheIsEnabled = dataCache.Mode != CacheMode.None || dataCache.Id != 0;

			if (IsServerActive)
			{
				if (!_allowZeroGroupForInternalMessages && !m_AllowZeroGroupMessage && groupId == 0)
				{
					NetworkLogger.__Log__(
						"Send: Access denied: Zero-group message not allowed. Join a group first or set 'AllowZeroGroupMessage' to true.",
						NetworkLogger.LogType.Error
					);

					return;
				}

				if (target == Target.Self && groupId != 0 && !cacheIsEnabled)
				{
					NetworkLogger.__Log__(
						"Target.Self cannot be used with groups. Note that this is not a limitation, it just doesn't make sense.",
						NetworkLogger.LogType.Warning
					);
				}

				var peersById = PeersById;
				NetworkGroup _group = null;

				if (groupId != 0)
				{
					if (GroupsById.TryGetValue(groupId, out _group))
					{
						if (!m_AllowAcrossGroupMessage || !_group.AllowAcrossGroupMessage)
						{
							if (!_group._peersById.ContainsKey(sender.Id) && sender.Id != 0)
							{
								NetworkLogger.__Log__(
									"Send: Access denied: Across-group message not allowed. Or set 'AllowAcrossGroupMessage' to true.",
									NetworkLogger.LogType.Error
								);

								return;
							}
						}

						peersById = _group._peersById; // Filter: peers by group.
					}
					else
					{
						NetworkLogger.__Log__(
							$"Cache Error: Group with ID '{groupId}' not found. Please verify that the group exists and that the provided groupId is correct.",
							NetworkLogger.LogType.Error
						);

						return;
					}
				}

				CreateCache(message, _group);

				// Authentication
				if (msgType == MessageType.BeginHandshake || msgType == MessageType.EndHandshake)
				{
					Send(message, sender);
					return;
				}

				// Send message to peers
				switch (target)
				{
					case Target.NonGroupMembers:
					case Target.NonGroupMembersExceptSelf:
						{
							var peers = peersById.Values.Where(p => p._groups.Count == 0);
							foreach (var peer in peers)
							{
								if (!peer.IsAuthenticated)
								{
									NetworkLogger.__Log__(
										"Server is trying to send a message to a peer that is not authenticated. This warning can sometimes be ignored.",
										NetworkLogger.LogType.Warning
									);

									continue;
								}

								if (peer.Id == Server.ServerPeer.Id)
									continue;

								if (
									peer.Equals(sender)
									&& target == Target.NonGroupMembersExceptSelf
								)
									continue;

								Send(message, peer);
							}
						}
						break;
					case Target.GroupMembersExceptSelf:
					case Target.GroupMembers:
						{
							if (groupId != 0 && !cacheIsEnabled)
							{
								NetworkLogger.__Log__(
									"Send: Target.GroupMembers cannot be used with specified groups(groupId is not 0). Note that this is not a limitation, it just doesn't make sense.",
									NetworkLogger.LogType.Warning
								);
							}

							if (sender.Id == 0)
							{
								NetworkLogger.__Log__(
									"Send: The server(id: 0) cannot use Target.GroupMembers. Because he's not in any group.",
									NetworkLogger.LogType.Error
								);

								return;
							}

							if (sender._groups.Count == 0)
							{
								NetworkLogger.__Log__(
									"Send: You are not in any groups. Please join a group first.",
									NetworkLogger.LogType.Error
								);

								return;
							}

							foreach (var (_, group) in sender._groups)
							{
								if (group.IsSubGroup)
									continue;

								foreach (var (_, peer) in group._peersById)
								{
									if (!peer.IsAuthenticated)
									{
										NetworkLogger.__Log__(
											"Server is trying to send a message to a peer that is not authenticated. This warning can sometimes be ignored.",
											NetworkLogger.LogType.Warning
										);

										continue;
									}

									if (peer.Id == Server.ServerPeer.Id)
										continue;

									if (
										peer.Equals(sender)
										&& target == Target.GroupMembersExceptSelf
									)
										continue;

									Send(message, peer);
								}
							}
						}
						break;
					case Target.All:
						{
							foreach (var (_, peer) in peersById)
							{
								if (!peer.IsAuthenticated)
								{
									NetworkLogger.__Log__(
										"Server is trying to send a message to a peer that is not authenticated. This warning can sometimes be ignored.",
										NetworkLogger.LogType.Warning
									);

									continue;
								}

								if (peer.Id == Server.ServerPeer.Id)
									continue;

								Send(message, peer);
							}
						}
						break;
					case Target.AllExceptSelf:
						{
							foreach (var (_, peer) in peersById)
							{
								if (!peer.IsAuthenticated)
								{
									NetworkLogger.__Log__(
										"Server is trying to send a message to a peer that is not authenticated. This warning can sometimes be ignored.",
										NetworkLogger.LogType.Warning
									);

									continue;
								}

								if (peer.Id == Server.ServerPeer.Id)
									continue;

								if (peer.Equals(sender))
									continue;

								Send(message, peer);
							}
						}
						break;
					case Target.Self:
						{
							if (!sender.IsAuthenticated)
								return;

							// group id doesn't make sense here, because peersById is not used for target.Self.
							Send(message, sender);
						}
						break;
				}

				_allowZeroGroupForInternalMessages = false;
			}
		}

		protected virtual void Internal_SendToServer(
			byte msgType,
			ReadOnlySpan<byte> data,
			DeliveryMode deliveryMode,
			byte sequenceChannel
		)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			if (IsClientActive)
			{
				Connection.Client.Send(
					PrepareClientMessageForSending(msgType, data),
					LocalEndPoint,
					deliveryMode,
					sequenceChannel
				);
			}
			else
			{
				NetworkLogger.__Log__(
					"Your are trying to send a message to the server while not connected. Please connect first.",
					NetworkLogger.LogType.Error
				);
			}
		}

		private float m_QueryInterval = NetworkClock.DEFAULT_QUERY_INTERVAL;

		private IEnumerator QueryNtp()
		{
			// The purpose of these calls before the while loop may be to ensure that the system clock is initially synchronized before entering the continuous query cycle.
			// This can be helpful to prevent situations where the system clock is not immediately synchronized when the application starts.
			//
			// Furthermore, the introduction of these initial pauses may serve as a startup measure to allow the system time to stabilize before initiating the repetitive querying of the NTP server.
			// This can be particularly useful if there are other startup or configuration operations that need to occur before the system is fully ready to synchronize the clock continuously.

			if (IsClientActive && m_SntpModule)
			{
				Sntp.Client.Query();
				yield return new WaitForSeconds(0.5f);
				Sntp.Client.Query();
				yield return new WaitForSeconds(0.5f);
				Sntp.Client.Query();
				yield return new WaitForSeconds(0.5f);

				while (IsClientActive && m_SntpModule)
				{
					// Continuously query the NTP server to ensure that the system clock is continuously synchronized with the NTP server.
					Sntp.Client.Query();
					yield return new WaitForSeconds(m_QueryInterval);
				}
			}
		}

		public virtual void Internal_OnServerInitialized()
		{
			NetworkHelper.EnsureRunningOnMainThread();
			// Set the default peer, used when the server sends to nothing(peerId = 0).
			NetworkPeer serverPeer = Server.ServerPeer;
			serverPeer._aesKey = AesCryptography.GenerateKey();
			serverPeer.IsConnected = true;
			serverPeer.IsAuthenticated = true;

			// Add the server to the list of peers.
			PeersByIp.Add(serverPeer.EndPoint, serverPeer);
			PeersById.Add(serverPeer.Id, serverPeer);

			// Set the server as active.
			IsServerActive = true;
			OnServerInitialized?.Invoke();
		}

		public virtual void Internal_OnClientConnected(IPEndPoint peer, NativePeer nativePeer)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			LocalEndPoint = peer;
			LocalNativePeer = nativePeer;
		}

		public virtual void Internal_OnClientDisconnected(IPEndPoint peer, string reason)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			IsClientActive = false;
			OnClientDisconnected?.Invoke(reason);
		}

		public virtual async void Internal_OnServerPeerConnected(
			IPEndPoint peer,
			NativePeer nativePeer
		)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			NetworkPeer newPeer = new(peer, p_UniqueId++);
			newPeer._nativePeer = nativePeer;

			if (!PeersByIp.TryAdd(peer, newPeer))
			{
				NetworkLogger.__Log__(
					$"Connection Error: Failed to add peer '{peer}' because it already exists.",
					NetworkLogger.LogType.Error
				);
			}
			else
			{
				if (!PeersById.TryAdd(newPeer.Id, newPeer))
				{
					NetworkLogger.__Log__(
						$"Connection Error: Failed to add peer by ID '{newPeer.Id}' because it already exists.",
						NetworkLogger.LogType.Error
					);
				}
				else
				{
					OnServerPeerConnected?.Invoke(newPeer, Phase.Begin);
					newPeer.IsConnected = true;
					using var message = Pool.Rent();
					message.Write(newPeer.Id);
					// Write the server's RSA public key to the buffer
					message.WriteString(Server.RsaPublicKey);

					SendToClient(
						MessageType.BeginHandshake,
						message,
						newPeer,
						Target.Self,
						DeliveryMode.ReliableOrdered,
						0,
						DataCache.None,
						0
					);

					NetworkLogger.__Log__(
						$"Connection Info: Peer '{peer}' added to the server successfully."
					);

					OnServerPeerConnected?.Invoke(newPeer, Phase.Normal);
				}
			}
		}

		public virtual void Internal_OnServerPeerDisconnected(IPEndPoint peer, string reason)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			OnServerPeerDisconnected?.Invoke(PeersByIp[peer], Phase.Begin);
			if (!PeersByIp.Remove(peer, out NetworkPeer currentPeer))
			{
				NetworkLogger.__Log__(
					$"Disconnection Error: Failed to remove peer '{peer}' from the server.",
					NetworkLogger.LogType.Error
				);
			}
			else
			{
				if (!PeersById.Remove(currentPeer.Id, out NetworkPeer _))
				{
					NetworkLogger.__Log__(
						$"Disconnection Error: Failed to remove peer by ID '{currentPeer.Id}' from the server.",
						NetworkLogger.LogType.Error
					);
				}
				else
				{
					foreach (var (_, group) in currentPeer._groups)
					{
						if (group._peersById.Remove(currentPeer.Id, out _))
						{
							NetworkLogger.__Log__(
								$"Disconnection Info: Peer '{peer}' removed from group '{group.Identifier}'."
							);

							// Dereferencing to allow for GC(Garbage Collector).
							// All resources should be released at this point.
							group.DestroyAllCaches(currentPeer);

							OnPlayerLeftGroup?.Invoke(
								group,
								currentPeer,
								Phase.End,
								"Leave event called by disconnect event."
							);

							// Change the current master client if the disconnected client was the master client
							if (currentPeer.Id == group.MasterClientId)
							{
								var nextPeer = group._peersById.Values.FirstOrDefault();
								if (nextPeer != null)
								{
									group.SetMasterClient(nextPeer);
								}
							}

							if (group.DestroyWhenEmpty)
							{
								Server.DestroyGroup(group);
							}
						}
						else
						{
							NetworkLogger.__Log__(
								$"Disconnection Error: Failed to remove peer '{peer}' from group '{group.Identifier}'.",
								NetworkLogger.LogType.Error
							);
						}
					}

					NetworkLogger.__Log__(
						$"Disconnection Info: Peer '{peer}' removed from the server. Reason: {reason}."
					);

					OnServerPeerDisconnected?.Invoke(currentPeer, Phase.Normal);

					// Dereferencing to allow for GC(Garbage Collector).
					currentPeer.ClearGroups();
					currentPeer.ClearData();

					// All resources should be released at this point.
					Server.DestroyAllCaches(currentPeer);
					currentPeer.DestroyAllCaches();
					currentPeer.IsConnected = false;

					// Finished disconnection
					OnServerPeerDisconnected?.Invoke(currentPeer, Phase.End);
				}
			}
		}

		public virtual void Internal_OnDataReceived(
			ReadOnlySpan<byte> _data,
			DeliveryMode deliveryMethod,
			IPEndPoint endPoint,
			byte sequenceChannel,
			bool isServer
		)
		{
			NetworkHelper.EnsureRunningOnMainThread();
			if (PeersByIp.TryGetValue(endPoint, out NetworkPeer peer) || !isServer)
			{
				using DataBuffer header = Pool.Rent();
				header.Write(_data);
				header.SeekToBegin();

				// Note: On Message event
				byte msgType = header.Read<byte>();
#if OMNI_DEBUG || (UNITY_SERVER && !UNITY_EDITOR)
				if (isServer)
				{
					Connection.Server.ReceivedBandwidth.Add(_data.Length);
				}
				else
				{
					Connection.Client.ReceivedBandwidth.Add(_data.Length);
				}
#endif
				int length = _data.Length;
				if (!isServer)
				{
					if (
						msgType != MessageType.BeginHandshake
						&& msgType != MessageType.EndHandshake
					)
					{
						if (!IsClientActive)
						{
							throw new Exception(
								"The client received a message while not yet authenticated. Wait until the handshake is completed."
							);
						}
					}
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				DataBuffer EndOfHeader() // Disposed by the caller!
				{
					DataBuffer message = Pool.Rent();
					message.Write(header.Internal_GetSpan(length));
					message.SeekToBegin();
					return message;
				}

				switch (msgType)
				{
					case MessageType.SyncGroupSerializedData:
						{
							int groupId = header.Read<int>();
							ImmutableKeyValuePair keyValuePair =
								header.ReadAsJson<ImmutableKeyValuePair>();

							var groups = Client.Groups;
							if (!groups.ContainsKey(groupId))
							{
								groups.Add(groupId, new NetworkGroup(groupId, "NOT SERIALIZED!")); // This group is invalid!, Used only for data sync.
							}

							NetworkGroup fGroup = groups[groupId];
							if (keyValuePair.Key != "_AllKeys_")
							{
								if (
									!fGroup.SerializedData.TryAdd(
										keyValuePair.Key,
										keyValuePair.Value
									)
								)
								{
									fGroup.SerializedData[keyValuePair.Key] = keyValuePair.Value;
								}
							}
							else
							{
								JObject jObject = (JObject)keyValuePair.Value;
								fGroup.SerializedData = jObject.ToObject<
									ObservableDictionary<string, object>
								>();
							}
						}
						break;
					case MessageType.SyncPeerSerializedData:
						{
							if (!isServer)
							{
								int peerId = header.Read<int>();
								ImmutableKeyValuePair keyValuePair =
									header.ReadAsJson<ImmutableKeyValuePair>();

								var peers = Client.Peers;
								if (!peers.ContainsKey(peerId))
								{
									peers.Add(peerId, new NetworkPeer(endPoint, peerId)); // _peer is not valid endpoint in this case!
								}

								NetworkPeer fPeer = peers[peerId];
								if (keyValuePair.Key != "_AllKeys_")
								{
									if (
										!fPeer.SerializedData.TryAdd(
											keyValuePair.Key,
											keyValuePair.Value
										)
									)
									{
										fPeer.SerializedData[keyValuePair.Key] = keyValuePair.Value;
									}
								}
								else
								{
									JObject jObject = (JObject)keyValuePair.Value;
									fPeer.SerializedData = jObject.ToObject<
										ObservableDictionary<string, object>
									>();
								}
							}
						}
						break;
					case MessageType.NtpQuery:
						{
							if (isServer)
							{
								double time = header.Read<double>();
								float t = header.Read<float>();
								Sntp.Server.SendNtpResponse(time, peer, t);
							}
							else
							{
								double a = header.Read<double>();
								double x = header.Read<double>();
								double y = header.Read<double>();
								float t = header.Read<float>();
								Sntp.Client.Evaluate(a, x, y, t);
							}
						}
						break;
					case MessageType.BeginHandshake:
						{
							if (!isServer)
							{
								// Client side!

								int localPeerId = header.Read<int>();
								string rsaServerPublicKey = header.ReadString();

								// Initialize the local peer
								LocalPeer = new NetworkPeer(LocalEndPoint, localPeerId);
								LocalPeer._nativePeer = LocalNativePeer;
								IsClientActive = true; // true: to allow send the aes key to the server.
								Client.Peers.Add(localPeerId, LocalPeer);

								// Generate AES Key and send it to the server(Encrypted by RSA public key).
								Client.RsaServerPublicKey = rsaServerPublicKey;
								byte[] aesKey = AesCryptography.GenerateKey();
								LocalPeer._aesKey = aesKey;

								// Crypt the AES Key with the server's RSA public key
								byte[] encryptedAesKey = RsaCryptography.Encrypt(
									aesKey,
									Client.RsaServerPublicKey
								);

								// Send the AES Key to the server
								using DataBuffer authMessage = Pool.Rent();
								authMessage.WriteAsBinary(encryptedAesKey);
								SendToServer(
									MessageType.BeginHandshake,
									authMessage,
									DeliveryMode.ReliableOrdered,
									0
								);

								IsClientActive = false; // Waiting for server's authorization response.
							}
							else
							{
								// Server side!

								byte[] aesKey = header.ReadAsBinary<byte[]>();

								// Decrypt the AES Key with the server's RSA private key
								peer._aesKey = RsaCryptography.Decrypt(
									aesKey,
									Server.RsaPrivateKey
								);

								byte[] serverAesKey = Server.ServerPeer._aesKey;
								byte[] cryptedServerAesKey = AesCryptography.Encrypt(
									serverAesKey,
									0,
									serverAesKey.Length,
									peer._aesKey,
									out byte[] iv
								);

								using var message = Pool.Rent();
								message.WriteAsBinary(iv);
								message.WriteAsBinary(cryptedServerAesKey);
								// Send Ok to the client!
								SendToClient(
									MessageType.EndHandshake,
									message,
									peer,
									Target.Self,
									DeliveryMode.ReliableOrdered,
									0,
									DataCache.None,
									0
								);
							}
						}
						break;
					case MessageType.EndHandshake:
						{
							if (!isServer)
							{
								// if (_tickSystem == null)
								// {
								//     TickSystem = new NetworkTickSystem();
								//     TickSystem.Initialize(m_TickRate);
								// }

								// Read server aes key
								byte[] iv = header.ReadAsBinary<byte[]>();
								byte[] serverAesKeyCrypted = header.ReadAsBinary<byte[]>();

								// decrypt server aes key
								Client.ServerPeer._aesKey = AesCryptography.Decrypt(
									serverAesKeyCrypted,
									0,
									serverAesKeyCrypted.Length,
									LocalPeer._aesKey,
									iv
								);

								// Connection end & authorized.
								LocalPeer.IsConnected = true;
								LocalPeer.IsAuthenticated = true;
								IsClientActive = true; // connected and authorized(authenticated).
								StartCoroutine(QueryNtp());
								OnClientConnected?.Invoke();

								// Send Ok to the server!
								SendToServer(
									MessageType.EndHandshake,
									DataBuffer.Empty,
									DeliveryMode.ReliableOrdered,
									0
								);
							}
							else
							{
								peer.IsAuthenticated = true;
								OnServerPeerConnected?.Invoke(peer, Phase.End);
							}
						}
						break;
					case MessageType.LocalInvoke:
						{
							int identityId = header.Internal_Read();
							byte instanceId = header.Read<byte>();
							byte invokeId = header.Read<byte>();

							if (
								isServer
								&& invokeId == NetworkConstants.NET_VAR_RPC_ID
								&& !m_AllowNetworkVariablesFromClients
							) // 255 is reserved for NetVar
							{
								// NetVar exclusively
								NetworkLogger.__Log__(
									"The client does not have permission to send Network Variables.",
									NetworkLogger.LogType.Error
								);
#if OMNI_RELEASE
                                peer.Disconnect();
#endif
								return;
							}

							using var message = EndOfHeader();
							var key = (identityId, instanceId);
							var eventBehavious = isServer
								? Server.LocalEventBehaviours
								: Client.LocalEventBehaviours;

							if (eventBehavious.TryGetValue(key, out IInvokeMessage behaviour))
							{
								behaviour.OnMessageInvoked(
									invokeId,
									message,
									peer, // peer is null on client.
									isServer,
									sequenceChannel
								);
							}
							else
							{
								NetworkLogger.__Log__(
									$"Local Invoke Error: Did you spawn the identity? -> Failed to find 'Local Event Behaviour' with Identity Id: [{identityId}] and instance Id: [{instanceId}] on the {(isServer ? "Server" : "Client")} side. "
										+ $"This function exists on the {(!isServer ? "Server" : "Client")} side, but is missing on the {(!isServer ? "Client" : "Server")} side. "
										+ "Ensure it is registered first or ignore it if intended.",
									NetworkLogger.LogType.Error
								);
							}
						}
						break;
					case MessageType.GlobalInvoke:
						{
							int identityId = header.Read<int>();
							byte invokeId = header.Read<byte>();

							if (
								isServer
								&& invokeId == NetworkConstants.NET_VAR_RPC_ID
								&& !m_AllowNetworkVariablesFromClients
							)
							{
								// NetVar exclusively
								NetworkLogger.__Log__(
									"The Client does not have permission to send Network Variables. Server refuses it.",
									NetworkLogger.LogType.Error
								);

#if OMNI_RELEASE
                                peer.Disconnect();
#endif
								return;
							}

							using var message = EndOfHeader();
							var eventBehavious = isServer
								? Server.GlobalEventBehaviours
								: Client.GlobalEventBehaviours;

							if (
								eventBehavious.TryGetValue(identityId, out IInvokeMessage behaviour)
							)
							{
								behaviour.OnMessageInvoked(
									invokeId,
									message,
									peer, // peer is null on client.
									isServer,
									sequenceChannel
								);
							}
							else
							{
								NetworkLogger.__Log__(
									$"Global Invoke Error: Failed to find 'Global Event Behaviour' with Id: [{identityId}] on the {(isServer ? "Server" : "Client")} side. "
										+ $"This function exists on the {(!isServer ? "Server" : "Client")} side, but is missing on the {(!isServer ? "Client" : "Server")} side. "
										+ "Ensure it is registered first or ignore it if intended.",
									NetworkLogger.LogType.Error
								);
							}
						}
						break;
					case MessageType.LeaveGroup:
						{
							string groupName = header.ReadString();
							string reason = header.ReadString();

							if (isServer)
							{
								Server.LeaveGroup(groupName, reason, peer);
							}
							else
							{
								OnLeftGroup?.Invoke(groupName, reason);
							}
						}
						break;
					case MessageType.JoinGroup:
						{
							string groupName = header.ReadString();
							using var message = EndOfHeader();

							if (isServer)
							{
								if (string.IsNullOrEmpty(groupName))
								{
									NetworkLogger.__Log__(
										"JoinGroup: Group name cannot be null or empty.",
										NetworkLogger.LogType.Error
									);
								}

								if (groupName.Length > 256)
								{
									NetworkLogger.__Log__(
										"JoinGroup: Group name cannot be longer than 256 characters.",
										NetworkLogger.LogType.Error
									);
								}

								Server.JoinGroup(groupName, message, peer, false);
							}
							else
							{
								OnJoinedGroup?.Invoke(groupName, message);
							}
						}
						break;
					case MessageType.Spawn:
						{
							if (!isServer)
							{
								string prefabName = header.ReadString();
								header.ReadIdentity(out int peerId, out int identityId);
								NetworkIdentity prefab = GetPrefab(prefabName);
								prefab.SpawnOnClient(peerId, identityId);
							}
							else
							{
								int identityId = header.Read<int>();
								if (
									NetworkManager.Server.TryGetIdentity(
										identityId,
										out var identity
									))
								{
									identity.OnSpawn?.Invoke();
								}
							}
						}
						break;
					case MessageType.SetOwner:
						{
							int identityId = header.Read<int>();
							int peerId = header.Read<int>();
							if (!isServer)
							{
								if (
									NetworkManager.Client.TryGetIdentity(
										identityId,
										out var identity
									)
								)
								{
									identity.IsLocalPlayer = LocalPeer.Id == peerId;
								}
								else
								{
									NetworkLogger.__Log__(
										$"SetOwner: Identity with ID: [{identityId}] not found. Please ensure it is spawned.",
										NetworkLogger.LogType.Error
									);
								}
							}
						}
						break;
					case MessageType.Destroy:
						{
							if (!isServer)
							{
								int identityId = header.Read<int>();
								NetworkHelper.Destroy(identityId, isServer);
							}
						}
						break;
					case MessageType.RequestEntityAction:
						{
							if (isServer)
							{
								int identityId = header.Read<int>();
								using var message = EndOfHeader();

								if (
									NetworkManager.Server.TryGetIdentity(
										identityId,
										out var identity
									))
								{
									identity.OnRequestAction?.Invoke(message);
								}
							}
						}
						break;
					default:
						{
							if (isServer)
							{
								using var message = EndOfHeader();
								OnServerCustomMessage?.Invoke(
									msgType,
									message,
									peer,
									sequenceChannel
								);
							}
							else
							{
								using var message = EndOfHeader();
								OnClientCustomMessage?.Invoke(msgType, message, sequenceChannel);
							}
						}
						break;
				}
			}
		}

		private static void DestroyScene(LoadSceneMode mode, Scene scene, SceneOperationMode op)
		{
			if (_allowLoadScene)
			{
				throw new Exception(
					"Load Scene: Wait for scene load to complete before loading another scene."
				);
			}

			_allowLoadScene = true;
			if (mode == LoadSceneMode.Single)
			{
				// This event is used to perform some operations before the scene is loaded.
				// for example: removing registered events, destroying objects, etc.
				// Only single mode, because the additive does not destroy/unregister anything.
				OnBeforeSceneLoad?.Invoke(scene, op);
			}
		}

		public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
		{
			DestroyScene(mode, SceneManager.GetSceneByName(sceneName), SceneOperationMode.Load);
			SceneManager.LoadScene(sceneName, mode);
		}

		public static AsyncOperation LoadSceneAsync(
			string sceneName,
			LoadSceneMode mode = LoadSceneMode.Single
		)
		{
			DestroyScene(mode, SceneManager.GetSceneByName(sceneName), SceneOperationMode.Load);
			return SceneManager.LoadSceneAsync(sceneName, mode);
		}

		public static void LoadScene(int index, LoadSceneMode mode = LoadSceneMode.Single)
		{
			DestroyScene(mode, SceneManager.GetSceneByBuildIndex(index), SceneOperationMode.Load);
			SceneManager.LoadScene(index, mode);
		}

		public static AsyncOperation LoadSceneAsync(
			int index,
			LoadSceneMode mode = LoadSceneMode.Single
		)
		{
			DestroyScene(mode, SceneManager.GetSceneByBuildIndex(index), SceneOperationMode.Load);
			return SceneManager.LoadSceneAsync(index, mode);
		}

		public static AsyncOperation UnloadSceneAsync(
			string sceneName,
			UnloadSceneOptions options = UnloadSceneOptions.None
		)
		{
			DestroyScene(LoadSceneMode.Single, SceneManager.GetSceneByName(sceneName), SceneOperationMode.Unload);
			return SceneManager.UnloadSceneAsync(sceneName, options);
		}

		public static AsyncOperation UnloadSceneAsync(
			int index,
			bool useBuildIndex = false,
			UnloadSceneOptions options = UnloadSceneOptions.None
		)
		{
			DestroyScene(
				LoadSceneMode.Single,
				useBuildIndex
					? SceneManager.GetSceneByBuildIndex(index)
					: SceneManager.GetSceneAt(index), SceneOperationMode.Unload
			);

			return SceneManager.UnloadSceneAsync(index, options);
		}

		/// <summary>
		/// Adds a prefab to the registration list.
		/// </summary>
		/// <param name="prefab"></param>
		public static void AddPrefab(NetworkIdentity prefab)
		{
			if (Manager.m_Prefabs.Any(x => x != null && x.name == prefab.name))
				return;

			Manager.m_Prefabs.Add(prefab);
		}

		/// <summary>
		/// Retrieves a prefab by its name.
		/// </summary>
		/// <param name="prefabName">The name of the prefab to retrieve.</param>
		/// <returns>The prefab with the specified name.</returns>
		public static NetworkIdentity GetPrefab(string prefabName)
		{
			return Manager.m_Prefabs.FirstOrDefault(x => x != null && x.name == prefabName)
				?? throw new Exception(
					$"Could not find prefab with name: \"{prefabName}\". Ensure the prefab is added to the registration list."
				);
		}

		/// <summary>
		/// Retrieves a prefab by its index in the list.
		/// </summary>
		/// <param name="index">The index of the prefab to retrieve.</param>
		/// <returns>The prefab at the specified index or null if index is out of bounds.</returns>
		public static NetworkIdentity GetPrefab(int index)
		{
			if (index >= 0 && index < Manager.m_Prefabs.Count)
			{
				return Manager.m_Prefabs[index];
			}

			throw new IndexOutOfRangeException(
				"Prefab index out of bounds. Ensure the prefab is added to the registration list."
			);
		}

		/// <summary>
		/// Converts an object to JSON format.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="obj">The object to be converted.</param>
		/// <param name="settings">Optional settings for JSON serialization (default is null).</param>
		/// <returns>A string representing the JSON serialization of the object.</returns>
		public static string ToJson<T>(T obj, JsonSerializerSettings settings = null)
		{
			settings ??= BufferWriterExtensions.DefaultJsonSettings;
			return JsonConvert.SerializeObject(obj, settings);
		}

		/// <summary>
		/// Converts an object to binary format using MemoryPackSerializer.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="obj">The object to be converted.</param>
		/// <param name="settings">Optional settings for serialization (default is null).</param>
		/// <returns>A byte array representing the binary serialization of the object.</returns>
		public static byte[] ToBinary<T>(T obj, MemoryPackSerializerOptions settings = null)
		{
			settings ??= BufferWriterExtensions.DefaultMemoryPackSettings;
			return MemoryPackSerializer.Serialize(obj, settings);
		}

		/// <summary>
		/// Deserializes an object from JSON format.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="json">The JSON string to deserialize.</param>
		/// <param name="settings">Optional settings for JSON deserialization (default is null).</param>
		/// <returns>The deserialized object.</returns>
		public static T FromJson<T>(string json, JsonSerializerSettings settings = null)
		{
			settings ??= BufferWriterExtensions.DefaultJsonSettings;
			return JsonConvert.DeserializeObject<T>(json, settings);
		}

		/// <summary>
		/// Deserializes an object from binary format using MemoryPackSerializer.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="data">The byte array containing the binary data to deserialize.</param>
		/// <param name="settings">Optional settings for deserialization (default is null).</param>
		/// <returns>The deserialized object.</returns>
		public static T FromBinary<T>(byte[] data, MemoryPackSerializerOptions settings = null)
		{
			settings ??= BufferWriterExtensions.DefaultMemoryPackSettings;
			return MemoryPackSerializer.Deserialize<T>(data, settings);
		}

		public static List<byte[]> Split(ReadOnlySpan<byte> data, int blockSize = 128)
		{
			if (data.Length <= blockSize)
			{
				throw new Exception("The specified data must be longer than block size");
			}

			int x = blockSize;
			if (!((x != 0) && ((x & (x - 1)) == 0)))
			{
				throw new Exception("Block size must be a power of 2.");
			}

			int offset = 0;
			List<byte[]> blocks = new();
			while (offset < data.Length)
			{
				int end = Math.Min(data.Length - offset, blockSize);
				ReadOnlySpan<byte> block = data.Slice(offset, end);
				offset += block.Length;

				// Add the block to the final result list.
				blocks.Add(block.ToArray());
			}

			// Return the splitted data in order.
			return blocks;
		}

		void OnGUI()
		{
#if UNITY_EDITOR && OMNI_DEBUG
			GUI.Label(
				new Rect(10, Screen.height - 60, 350, 30),
				"Debug Mode (Very Slow on Editor)\r\nUse in development only. For heavy testing or performance, use release mode.",
				new GUIStyle()
				{
					fontSize = 22,
					normal = new GUIStyleState() { textColor = Color.red }
				}
			);
#endif

#if OMNI_DEBUG
			GUI.Label(
				new Rect(10, 10, 100, 30),
				m_SntpModule
					? $"FPS: {Framerate:F0}\r\nCpu: {CpuTimeMs:F0} ms\r\nPing: {(IsClientActive ? LocalPeer.Ping : 0):F0}({Sntp.Client.Ping}:F0)"
					: $"FPS: {Framerate:F0}\r\nCpu: {CpuTimeMs:F0} ms\r\nPing: {(IsClientActive ? LocalPeer.Ping : 0):F0}",
				new GUIStyle()
				{
					fontSize = 23,
					normal = new GUIStyleState() { textColor = Color.white }
				}
			);
#endif
		}
	}
}
