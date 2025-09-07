#if UNITY_EDITOR
using ParrelSync;
#endif
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
using Omni.Collections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Omni.Core.Web;
using Omni.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Omni.Core.Attributes;
using UnityEngine.Profiling;
using System.IO;
using Omni.Inspector;

#pragma warning disable

public enum Transporter
{
    None,
    Lite,
    Kcp,
    Web,
}

namespace Omni.Core
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [JsonObject(MemberSerialization.OptIn)]
    [GenerateSecureKeys]
    public partial class NetworkManager : OmniBehaviour, ITransporterReceive
    {
        public static Transporter UnderlyingTransporter { get; internal set; }
#if OMNI_DEBUG
        internal static byte[] SharedKey = new byte[]
        {
            36, 209, 27, 118, 198, 198, 212, 154, 35, 189, 161, 45, 109, 26, 6, 153,
            100, 137, 209, 89, 45, 170, 119, 124, 83, 164, 87, 97, 225, 206, 3, 13,
            230, 241, 150, 251, 99, 186, 103, 31, 190, 34, 150, 116, 99, 52, 80, 15,
            243, 197, 248, 227, 240, 191, 130, 200, 153, 92, 61, 176, 144, 205, 79, 27,
            235, 111, 246, 162, 98, 217, 199, 241, 137, 4, 91, 126, 44, 87, 250, 3,
            70, 201, 55, 189, 98, 146, 108, 182, 1, 59, 124, 66, 231, 18, 213, 156,
            248, 85, 196, 168, 94, 105, 58, 163, 200, 33, 72, 8, 135, 224, 30, 87,
            160, 191, 177, 135, 193, 252, 17, 24, 146, 137, 73, 186, 20, 235, 202, 48
        };

        internal static byte[] SecretKey = new byte[]
        {
            248, 70, 170, 87, 200, 85, 189, 137, 186, 225, 99, 15, 153, 109, 118, 176,
            27, 105, 241, 212, 154, 55, 252, 213, 3, 98, 6, 193, 4, 17, 31, 214,
            83, 161, 200, 230, 36, 45, 18, 189, 146, 137, 124, 58, 144, 137, 59, 250,
            198, 33, 243, 3, 198, 162, 241, 235, 176, 164, 79, 119, 87, 225, 252, 248,
            100, 27, 209, 116, 135, 154, 124, 206, 135, 108, 105, 241, 213, 248, 91, 66,
            98, 31, 191, 153, 18, 212, 83, 3, 186, 98, 193, 45, 109, 200, 205, 146,
            6, 17, 241, 144, 176, 15, 119, 189, 252, 198, 83, 162, 124, 4, 70, 212,
            225, 248, 31, 193, 154, 186, 191, 91, 135, 59, 87, 243, 17, 87, 252, 241,
            230, 18, 225, 116, 36, 105, 164, 176, 100, 6, 99, 3, 198, 144, 212, 135,
            213, 124, 200, 146, 154, 119, 191, 193, 91, 243, 79, 17, 83, 252, 212, 225
        };

#else
        internal static byte[] SharedKey => __Internal__Key__;
#if UNITY_SERVER || UNITY_EDITOR
        internal static byte[] SecretKey => __Internal__SecretKey__;
#else
        internal static byte[] SecretKey => new byte[0];
#endif
#endif

        internal readonly static TransporterRouteManager _transporterRouteManager = new();
        private static bool _allowLoadScene;

        /// <summary>
        /// If enabled, the bandwidth monitor will account for only the Omni framework's payload bytes,
        /// excluding all transport and protocol overhead (such as transport headers, framing, and network-layer metadata).
        ///
        /// When set to <c>true</c>, bandwidth statistics will reflect only the raw payload data sent and received by the framework.
        /// When set to <c>false</c>, all bytes transmitted—including transport and network headers—will be included in the bandwidth calculation;
        /// in this mode, the reported value is an approximation and may not be exact, due to variable overhead introduced by the underlying network layers.
        ///
        /// <para>
        /// This option is useful for developers who want to measure the pure efficiency of their application's network logic,
        /// without the variability introduced by underlying transport protocols.
        /// </para>
        /// </summary>
        public static bool BandwidthPayloadOnly { get; set; } = true;

        /// <summary>
        /// Represents the managed thread ID of the main thread running the network operations.
        /// </summary>
        public static int UnityMainThreadId { get; private set; }

        /// <summary>
        /// Indicates whether this instance is running in host mode (both server and client active in the same process).
        /// Returns <c>true</c> if the application is acting simultaneously as server and client,
        /// which is typical for local multiplayer testing or single-player with network features.
        /// </summary>
        public static bool IsHost => IsServerActive && IsClientActive;

        static IBufferPooling<DataBuffer> m_Pool;

        /// <summary>
        /// Provides access to the buffer pooling mechanism used within the network manager for buffering data operations.
        /// </summary>
        public static IBufferPooling<DataBuffer> Pool
        {
            get
            {
                if (m_Pool == null)
                {
                    throw new NullReferenceException(
                        "The object pool has not been initialized and is not ready for use. This may occur if it is being accessed in Awake or OnAwake. Obtain the object pool in the Start method instead.");
                }

                return m_Pool;
            }
            private set => m_Pool = value;
        }

        /// <summary>
        /// An event that is triggered after a scene has been successfully loaded.
        /// It provides the loaded scene and the mode in which the scene was loaded.
        /// </summary>
        public static event Action<Scene, LoadSceneMode> OnSceneLoaded;

        /// <summary>
        /// Event triggered when a scene is unloaded. Subscribers can perform operations
        /// related to the unloading process, such as cleanup of scene-specific resources or
        /// states. It provides an opportunity to handle tasks that should occur immediately
        /// when a scene is no longer active or present in memory.
        /// </summary>
        public static event Action<Scene> OnSceneUnloaded;

        /// <summary>
        /// Event triggered before a scene is loaded, providing the opportunity to perform any pre-load operations.
        /// Triggered only in single mode.
        /// </summary>
        public static event Action<Scene, SceneOperationMode> OnBeforeSceneLoad;

        /// <summary>
        /// Event triggered when the server is fully initialized and ready to handle network connections.
        /// </summary>
        public static event Action OnServerInitialized;

        /// <summary>
        /// Event that is triggered when a peer successfully connects to the server.
        /// </summary>
        public static event Action<NetworkPeer, Phase> OnServerPeerConnected;

        /// <summary>
        /// Event that triggers when a server-side network peer is disconnected.
        /// </summary>
        public static event Action<NetworkPeer, Phase> OnServerPeerDisconnected;

        /// <summary>
        /// An event that is triggered when a client successfully connects to the network.
        /// </summary>
        public static event Action OnClientConnected;

        /// <summary>
        /// Event triggered when a client disconnects from the network.
        /// Provides the disconnected client's identifier as a parameter.
        /// </summary>
        public static event Action<string> OnClientDisconnected;

        /// <summary>
        /// Event triggered when an identity is successfully spawned on the client. 
        /// </summary>
        public static event Action<NetworkIdentity> OnClientIdentitySpawned;

        /// <summary>
        /// Event triggered on the client when the server modifies a specific key 
        /// in the shared data of a network peer.
        /// </summary>
        /// <param name="peer">The <see cref="NetworkPeer"/> whose shared data was modified.</param>
        /// <param name="key">The key in the shared data that was changed.</param>
        public static event Action<NetworkPeer, string> OnPeerSharedDataChanged;

        /// <summary>
        /// Event triggered on the client when the server modifies a specific key 
        /// in the shared data of a network group.
        /// </summary>
        /// <param name="group">The <see cref="NetworkGroup"/> whose shared data was modified.</param>
        /// <param name="key">The key in the shared data that was changed.</param>
        public static event Action<NetworkGroup, string> OnGroupSharedDataChanged;

        private static event Action<byte, DataBuffer, NetworkPeer, int> OnServerCustomMessage;
        private static event Action<byte, DataBuffer, int> OnClientCustomMessage;

        internal static event Action<string, DataBuffer> OnJoinedGroup; // for client
        internal static event Action<DataBuffer, NetworkGroup, NetworkPeer> OnPlayerJoinedGroup; // for server
        internal static event Action<NetworkPeer, string> OnPlayerFailedJoinGroup; // for server
        internal static event Action<string, string> OnLeftGroup; // for client
        internal static event Action<NetworkGroup, NetworkPeer, Phase, string> OnPlayerLeftGroup; // for server
        internal static event Action<NetworkPeer, string> OnPlayerFailedLeaveGroup;

        private static NetworkConsole _console;

        /// <summary>
        /// Provides access to the console module for network-related operations
        /// </summary>
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

        private static NetworkConnection _connection;

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

        private static NetworkMatchmaking _matchmaking;

        /// <summary>
        /// Provides access to the matchmaking functionalities within the network framework.
        /// </summary>
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

        private static SimpleNtp _ntpClock;

        /// <summary>
        /// Provides access to the SimpleNtp instance used by the NetworkManager for network time synchronization.
        /// </summary>
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

        private static BasedTickSystem _tickSystem;

        public static BasedTickSystem TickSystem
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
        private static NativePeer LocalNativePeer { get; set; }

        private static NetworkPeer _localPeer;

        /// <summary>
        /// Gets the local network peer.
        /// </summary>
        public static NetworkPeer LocalPeer
        {
            get
            {
                return _localPeer ??
                       throw new Exception(
                           "Client(LocalPeer) is neither active, nor authenticated. Please verify using NetworkManager.IsClientActive.");
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
                if (IsHost)
                {
                    return ServerSide.ServerPeer;
                }

                if (IsClientActive)
                {
                    return ClientSide.ServerPeer;
                }

                return ServerSide.ServerPeer;
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

        internal static void ShowUnsupportedPlataformWarning()
        {
#if !UNITY_2022_3_OR_NEWER
            throw new System.Exception("Omni Networking is not supported on this platform. Please upgrade to Unity 2022.3 or higher.");
#endif
        }

        bool _isInitialized = false;
        protected virtual void Awake()
        {
            ShowUnsupportedPlataformWarning();
#if !UNITY_EDITOR // log file in release mode, but without generating new stack trace
            Application.logMessageReceived += (string logString, string stackTrace, LogType type) =>
            {
                if (type == LogType.Error)
                {
                    if (logString.StartsWith("Full Log"))
                        return;

                    NetworkLogger.LogToFile($"[Re-throw]: {logString}\nStack Trace:\n{stackTrace}", NetworkLogger.LogType.Error);
                }
            };
#endif
            MemoryPackFormatterProvider.Register(new DataBufferFormatter());
            if (SharedKey == null)
            {
                throw new NotSupportedException(
                    "Unable to start server: Private key not found. " +
                    "This typically occurs when the source generator has not executed properly. " +
                    "To resolve, please verify:\n" +
                    "1. The project has been built correctly\n" +
                    "2. The source generator is properly configured in your project\n" +
                    "3. There are no compilation errors preventing key generation\n\n" +
                    "If the issue persists, please refer to the documentation or contact support."
                );
            }

#if OMNI_VIRTUAL_PLAYER_ENABLED
            if (MPPM.Player == VirtualPlayer.None)
            {
                NetworkLogger.__Log__("To use virtual player mode, you need to: Configure appropriate tags (Main, Player2, Player3, or Player4) and verify that Unity Multiplayer Playmode is correctly configured. Refer to the documentation for more information on setting up virtual players.", NetworkLogger.LogType.Warning);
                throw new NotSupportedException(
                    "To use virtual player mode, you need to: Configure appropriate tags (Main, Player2, Player3, or Player4) and verify that Unity Multiplayer Playmode is correctly configured. Refer to the documentation for more information on setting up virtual players."
                );
            }
#endif

#if UNITY_EDITOR
            NetworkLogger.Initialize("EditorLog");
#if OMNI_RELEASE
            NetworkLogger.Log("The production key is: " + BitConverter.ToString(SharedKey));
#endif
#else
            string _uniqueLogId = Guid.NewGuid().ToString();
            NetworkLogger.Log($"The log ID for this session is: {_uniqueLogId}");
            NetworkLogger.Initialize(_uniqueLogId);
#endif
            // NetworkHelper.LoadComponent(this, "setup.cfg");
            if (_manager != null)
            {
                gameObject.SetActive(false);
                Destroy(gameObject, 1f);
                return;
            }

            _manager = this;
#if OMNI_SERVER && !UNITY_EDITOR
            ShowDefaultOmniLog();
#endif
            // Start http server
            if (m_EnableHttpServer)
            {
                var webServer = gameObject.AddComponent<HttpRouteManager>();
                webServer.StartServices(m_EnableHttpSsl, m_HttpServerPort, m_UseKestrel ? m_KestrelOptions : null);
                NetworkService.Register(webServer);
            }

            Pool = new DataBufferPool(m_PoolCapacity, m_PoolSize);
            BufferWriterExtensions.UseBinarySerialization = m_UseBinarySerialization;
            BufferWriterExtensions.EnableBandwidthOptimization = m_EnableBandwidthOptimization;
            BufferWriterExtensions.UseUnalignedMemory = m_UseUnalignedMemory;
            BufferWriterExtensions.DefaultEncoding = m_UseUtf8 ? Encoding.UTF8 : Encoding.ASCII;
            Bridge.UseDapper = m_UseDapper;

            QualitySettings.vSyncCount = 0;
#if !UNITY_SERVER || UNITY_EDITOR
            Application.targetFrameRate = m_LockClientFps > 0 ? m_LockClientFps : -1;
#else
            Application.targetFrameRate = m_LockServerFps > 0 ? m_LockServerFps : -1;
#endif
            AotHelper.EnsureDictionary<string, object>(); // Add IL2CPP Support to Dictionary for AOT
            UnityMainThreadId = Thread.CurrentThread.ManagedThreadId;
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
            if (m_ConnectionModule)
            {
                InitializeModule(Module.Connection);
                // Register transporter route manager
                ClientSide.OnMessage += _transporterRouteManager.OnClientMessage;
                ServerSide.OnMessage += _transporterRouteManager.OnServerMessage;
                NetworkService.Register(_transporterRouteManager);
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
            _isInitialized = true;
        }

        private async void Start()
        {
#if UNITY_EDITOR
            while (Application.isPlaying)
            {
                var groups = IsHost ? ServerSide.Groups.Values : IsServerActive ? ServerSide.Groups.Values : ClientSide.Groups.Values;
                int count = groups.Count(g => !g.IsSubGroup);
                if (Groups.Count != count)
                {
                    Groups = Enumerable.ToList(groups.Where(g => !g.IsSubGroup).Select(g => new InspectorSerializableGroup()
                    {
                        m_Id = g.Id,
                        m_Name = g.Name,
                        m_Data = g.Data.ToObservableDictionary(x => x.Key, x => x.Value.ToString()),
                    }));
                }

                await UniTask.Delay(1000);
            }
#endif
#if OMNI_DEBUG && !UNITY_EDITOR
            while (Application.isPlaying)
            {
                if (!IsHost && IsServerActive)
                {
                    // Unlock frame rate if is server and not host
                    Application.targetFrameRate = 0;
                }

                await UniTask.Delay(1000);
            }
#endif
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            if (m_TickModule && _tickSystem != null)
                TickSystem.UpdateTick();

            InspectorBridge.ForceRepaint = m_EnableDeepDebug;
            Bridge.EnableDeepDebug = m_EnableDeepDebug;
            UpdateFrameAndCpuMetrics();
        }

        private void UpdateFrameAndCpuMetrics()
        {
            deltaTime += UnityEngine.Time.unscaledDeltaTime;
            frameCount++;

            float rate = 1f;
            if (deltaTime >= rate)
            {
                Framerate = (int)(frameCount / deltaTime);
                CpuTimeMs = (int)(deltaTime / frameCount * 1000f);

                deltaTime -= rate;
                frameCount = 0;
            }
        }

#if OMNI_SERVER && !UNITY_EDITOR // Referenced! don't remove
        private void ShowDefaultOmniLog()
        {
            NetworkLogger.Log("Welcome to Omni Server Console. The server is now ready to handle connections and process requests.");
            NetworkLogger.Log($"The production key is: {BitConverter.ToString(SharedKey)}");
#if OMNI_DEBUG
            NetworkLogger.Log("Debug Mode Enabled: Detailed logs and debug features are active.");
#else
            NetworkLogger.Log("Release Mode Active: Optimized for production with minimal logging.");
            if (!Manager.m_TlsHandshake)
            {
                NetworkLogger.Log("TLS Handshake is disabled. AES key exchange will still occur, but the server certificate is not verified against a trusted authority. This may expose connections to MITM attacks. See Omni Documentation for more information.", false, NetworkLogger.LogType.Warning);
            }
            else
            {
                NetworkLogger.Log("Certificate validation is disabled. The server will not warn about expired, revoked, or untrusted certificates. It is strongly recommended to enable certificate validation in production environments. See Omni Documentation for more information.", false, NetworkLogger.LogType.Warning);
            }

            if (!m_UseSecureRoutes)
            {
                NetworkLogger.Log("Secure Routes are disabled. Transport data is not automatically encrypted. You must explicitly enable encryption for each route that requires protection. See Omni Documentation for more information.", false, NetworkLogger.LogType.Warning);
            }
#endif
        }
#endif

        /// <summary>
        /// Initializes a network module based on the provided module type.
        /// </summary>
        /// <param name="module">The type of module to initialize.</param>
        /// <returns>None</returns>
        public static void InitializeModule(Module module)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            switch (module)
            {
                case Module.TickSystem:
                    {
                        if (_tickSystem == null)
                        {
                            TickSystem = new BasedTickSystem();
                            TickSystem.Initialize(Manager.m_TickRate);
                            NetworkService.Register(TickSystem);
                        }
                    }
                    break;
                case Module.NtpClock:
                    {
                        Sntp = new SimpleNtp();
                        Sntp.Initialize();
                        NetworkService.Register(Sntp);
                    }
                    break;
                case Module.Console:
                    {
                        Console = new NetworkConsole();
                        Console.Initialize();
                        NetworkService.Register(Console);
                    }
                    break;
                case Module.Connection:
                    {
                        if (!Manager.TryGetComponent<TransporterBehaviour>(out var currentTransporter))
                        {
                            throw new NullReferenceException(
                                "Transporter component is missing in NetworkManager. Please add and configure a transporter component to ensure proper functioning of the network system."
                            );
                        }

                        switch (currentTransporter)
                        {
                            case LiteTransporter:
                                UnderlyingTransporter = Transporter.Lite;
                                break;
                            case KcpTransporter:
                                UnderlyingTransporter = Transporter.Kcp;
                                break;
                            case WebTransporter:
                                UnderlyingTransporter = Transporter.Web;
                                break;
                            default:
                                UnderlyingTransporter = Transporter.None;
                                break;
                        }

                        GameObject clientTransporter = new("Client Transporter");
                        GameObject serverTransporter = new("Server Transporter");

                        clientTransporter.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                        serverTransporter.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

                        clientTransporter.transform.parent = Manager.transform;
                        serverTransporter.transform.parent = Manager.transform;

                        Manager.m_ClientTransporter =
                            clientTransporter.AddComponent(currentTransporter.GetType()) as TransporterBehaviour;

                        Manager.m_ServerTransporter =
                            serverTransporter.AddComponent(currentTransporter.GetType()) as TransporterBehaviour;

                        if (Manager.m_ClientTransporter == null || Manager.m_ServerTransporter == null)
                        {
                            throw new NullReferenceException(
                                "Transporter component is missing in NetworkManager. Please add and configure a transporter component to ensure proper functioning of the network system."
                            );
                        }

                        currentTransporter.ITransporter.CopyTo(Manager.m_ClientTransporter.ITransporter);
                        currentTransporter.ITransporter.CopyTo(Manager.m_ServerTransporter.ITransporter);

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

#if OMNI_RELEASE || (UNITY_SERVER && !UNITY_EDITOR)
                        Manager.m_StartServer = true;
                        Manager.m_StartClient = true;
#else
                        if (Manager.m_StartServer)
                        {
                            if (
                                ConnectAddress.ToLowerInvariant() != "localhost"
                                && ConnectAddress.ToLowerInvariant() != "::1" // IPv6 localhost
                                && ConnectAddress != "127.0.0.1" // IPv4 localhost
                                && ConnectAddress != Manager.PublicIPv4
                                && ConnectAddress != Manager.PublicIPv6
                            )
                            {
                                bool isMe = false;
                                if (!IPAddress.TryParse(ConnectAddress, out _))
                                {
                                    IPAddress[] addresses = Dns.GetHostAddresses(ConnectAddress);
                                    isMe = addresses.Any(x => x.ToString() == Manager.PublicIPv4 || x.ToString() == Manager.PublicIPv6);
                                }

                                if (!isMe)
                                {
                                    Manager.m_StartServer = false;
                                    NetworkLogger.__Log__(
                                        "Server auto-start disabled: Client address '" + ConnectAddress + "' is not localhost or this machine's public IP. " +
                                        "When connecting to a remote server, starting a local server is unnecessary. " +
                                        "If you need to run a server on this machine, use NetworkManager.StartServer() manually.",
                                        NetworkLogger.LogType.Warning
                                    );
                                }
                            }
                        }
#endif

                        if (Manager.m_StartServer)
                        {
                            bool isClone = false;
#if UNITY_EDITOR
                            if (ClonesManager.IsClone())
                            {
                                isClone = true;
                            }

#if OMNI_VIRTUAL_PLAYER_ENABLED
                            if (MPPM.IsVirtualPlayer)
                            {
                                isClone = true;
                            }
#endif
#endif
                            if (!isClone)
                            {
                                StartServer();
                            }
                        }

                        if (Manager.m_StartClient)
                        {
                            Connect();
                        }
                    }
                    break;
                case Module.Matchmaking:
                    {
                        Matchmaking = new NetworkMatchmaking();
                        Matchmaking.Initialize();
                        NetworkService.Register(Matchmaking);
                    }
                    break;
            }
        }

        public static void StartHost(string host, int port)
        {
            UniTask.Void(async () =>
            {
                NetworkManager.StartServer(port);
                await UniTask.WaitForSeconds(0.1f);
                NetworkManager.Connect(host, port);
            });
        }

        /// <summary>
        /// Starts the network server using the default server listen port.
        /// </summary>
        /// <returns>None</returns>
        public static void StartServer()
        {
            StartServer(Manager.m_Port);
        }

        /// <summary>
        /// Starts the server on the specified port and initializes necessary services.
        /// </summary>
        /// <param name="port">The port number on which the server will listen for connections.</param>
        public static void StartServer(int port)
        {
            // WebGl Client does not support server
#if UNITY_WEBGL && !UNITY_EDITOR
			return;
#endif
            if (!IsServerActive)
            {
#if OMNI_DEBUG || UNITY_EDITOR || UNITY_SERVER
                if (!NetworkHelper.CanHostServer()) // Eg: OMNI_DEBUG defined with Android platform will return false.
                    return;

                try
                {
                    if (!File.Exists(NetworkConstants.k_CertificateFile))
                    {
                        // Create certificate file if it doesn't exist
                        using var fileStream = File.Create(NetworkConstants.k_CertificateFile);
                        using StreamWriter sw = new(fileStream);
                        sw.WriteLine("{\"cert\": \"cert.pfx\", \"password\": \"password for cert.pfx\"}");
                    }
                }
                catch (Exception ex)
                {
                    NetworkLogger.__Log__("Failed to create certificate file: " + ex.Message, NetworkLogger.LogType.Error);
                }

                if (Manager.m_TlsHandshake) ServerSide.LoadRsaKeysFromCert(NetworkConstants.k_CertificateFile, Manager.m_WarnIfCertInvalid);
                else ServerSide.GenerateRsaKeys();
                Connection.Server.Listen(port);
#else
                NetworkLogger.LogToFile(
                    "Server functionality is disabled in 'Release Mode' on client builds. To enable the server, run the application in 'Server Mode' or check the build configuration."
                );
#endif
            }
            else
            {
                throw new Exception(
                    "Error: The server is already running. Please call StopServer() to stop the current instance before attempting to start a new one using StartServer()."
                );
            }
        }

        /// <summary>
        /// Disconnects the specified network peer from the server.
        /// </summary>
        /// <param name="peer">The network peer to disconnect.</param>
        /// <exception cref="Exception">Thrown if the server is not initialized.</exception>
        public static void DisconnectPeer(NetworkPeer peer)
        {
            if (IsServerActive)
            {
                Connection.Server.Disconnect(peer);
            }
            else
            {
                throw new Exception(
                    "Error: The server is not initialized. Please ensure the StartServer() method has been successfully called before performing this operation.");
            }
        }

        internal static void ClearServerState()
        {
            foreach (var (id, identity) in ServerSide.Identities)
            {
                if (identity != null)
                    Destroy(identity.gameObject);
            }

            // Clear peers references
            PeersByIp.Clear();
            PeersById.Clear();

            // Clear group references
            GroupsById.Clear();
            ServerSide.Groups.Clear();

            // Clear RPC references
            ServerSide.StaticRpcHandlers.Clear();
            ServerSide.LocalRpcHandlers.Clear();

            // Clear other references
            ServerSide.Identities.Clear();
            ServerSide.Peers.Clear();
        }

        internal static void ClearClientState()
        {
            foreach (var (id, identity) in ClientSide.Identities)
            {
                if (identity != null)
                    Destroy(identity.gameObject);
            }

            // Clear group references
            ClientSide.Groups.Clear();

            // Clear RPC references
            ClientSide.StaticRpcHandlers.Clear();
            ClientSide.LocalRpcHandlers.Clear();

            // Clear other references
            ClientSide.Identities.Clear();
            ClientSide.Peers.Clear();
        }

        /// <summary>
        /// Stops the server if it is currently active.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown if the server is not initialized prior to calling this method.
        /// Ensure that StartServer() is called before attempting to stop the server.
        /// </exception>
        public static void StopServer()
        {
            if (IsServerActive)
            {
                ClearServerState();
                // Stop server: Shutdown and release all server resources.
                Connection.Server.Stop();
                IsServerActive = false;
            }
            else
            {
                throw new Exception(
                    "Error: The server is not currently running. Please ensure that StartServer() is called successfully before attempting to invoke StopServer()."
                );
            }
        }

        /// <summary>
        /// Initiates a network connection using the default connection address, port.
        /// </summary>
        /// <returns>None</returns>
        public static void Connect()
        {
            Connect(ConnectAddress, Port);
        }

        /// <summary>
        /// Establishes a connection to a network using the specified address, port.
        /// </summary>
        /// <param name="address">The address to connect to.</param>
        /// <param name="port">The port number to connect on the target address.</param>
        /// <param name="listenPort">The local port to listen on for incoming data.</param>
        public static void Connect(string address, int port)
        {
            if (!IsClientActive)
            {
#if !UNITY_SERVER || UNITY_EDITOR // Don't connect to the server in server build!
                if (Manager.m_TlsHandshake)
                {
                    if (IPAddress.TryParse(ConnectAddress, out _))
                    {
                        NetworkLogger.__Log__("[NetworkManager] SSL connection failed - IP addresses are not supported for SSL connections. Use a hostname (e.g., 'example.com' or 'omni.com.br') instead of an IP address.", NetworkLogger.LogType.Error);
                        return;
                    }
                }

                Connection.Client.Listen(0); // The OS will assign a random available port.
                Connection.Client.Connect(address, port);
#if OMNI_DEBUG
                if (
                    ConnectAddress.ToLowerInvariant() != "localhost"
                    && ConnectAddress.ToLowerInvariant() != "::1" // IPv6 localhost
                    && ConnectAddress != "127.0.0.1" // IPv4 localhost
                )
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(ConnectAddress);

                    NetworkLogger.Print(
                        $"[Network] Resolving host '{ConnectAddress}' → Found {addresses.Length} IP(s):",
                        NetworkLogger.LogType.Log);

                    for (int i = 0; i < addresses.Length; i++)
                    {
                        string family = addresses[i].AddressFamily.ToString();
                        NetworkLogger.Print(
                            $"({i + 1}/{addresses.Length}) {addresses[i]} [{family}]",
                            NetworkLogger.LogType.Log);
                    }

                    NetworkLogger.Print(
                        $"[Network] Attempting connection to primary IP: {addresses[0]}",
                        NetworkLogger.LogType.Log);
                }
#endif
#elif UNITY_SERVER && !UNITY_EDITOR
                NetworkLogger.__Log__("Client functionality is not available in a server build.");
#endif
            }
            else
            {
                throw new Exception(
                    "The client is already initialized. Please call StopClient() to disconnect the current session before attempting to establish a new one with Connect()."
                );
            }
        }

        /// <summary>
        /// Disconnects the client from the current network session.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when the client is not initialized and a disconnect attempt is made without a successful connection.
        /// </exception>
        public static void Disconnect()
        {
            if (IsClientActive)
            {
                ClearClientState();
                Connection.Client.Disconnect(LocalPeer);
                IsClientActive = false;
            }
            else
            {
                throw new Exception(
                    "Error: The client is not currently initialized. Please ensure you call the Connect() method before attempting to disconnect.");
            }
        }

        /// <summary>
        /// Stops the client if it is currently active.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when the client is not initialized. Ensure to call Connect() before attempting to stop the client.
        /// </exception>
        public static void StopClient()
        {
            if (IsClientActive)
            {
                ClearClientState();
                Connection.Client.Stop();
                IsClientActive = false;
            }
            else
            {
                throw new Exception(
                    "Error: The client is not currently initialized. Please ensure Connect() is called successfully before attempting to stop the client using StopClient()."
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SendToClient(byte msgType, NetworkPeer sender, DataBuffer message)
        {
            message ??= DataBuffer.Empty;
            Manager.Internal_SendToClient(msgType, message.BufferAsSpan, sender, ServerSide.DeliveryMode, ServerSide.Target, ServerSide.Group, ServerSide.SequenceChannel);
            ServerSide.RestoreDefaultNetworkConfiguration();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SendToServer(byte msgType, DataBuffer message)
        {
            message ??= DataBuffer.Empty;
            Manager.Internal_SendToServer(msgType, message.BufferAsSpan, ClientSide.DeliveryMode, ClientSide.SequenceChannel);
            ClientSide.RestoreDefaultNetworkConfiguration();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ReadOnlySpan<byte> PrepareClientMessageForSending(byte msgType, ReadOnlySpan<byte> message, byte channel)
        {
            byte packed = (byte)(msgType | channel << 4);
            using DataBuffer header = Pool.Rent(enableTracking: false);
            header.Write(packed);
            header.Write(message);
            return header.BufferAsSpan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual ReadOnlySpan<byte> PrepareServerMessageForSending(byte msgType, ReadOnlySpan<byte> message, byte channel)
        {
            byte packed = (byte)(msgType | channel << 4);
            using DataBuffer header = Pool.Rent(enableTracking: false);
            header.Write(packed);
            header.Write(message);
            return header.BufferAsSpan;
        }

        protected virtual void Internal_SendToClient(byte msgType, ReadOnlySpan<byte> _data, NetworkPeer sender, DeliveryMode deliveryMode, Target target, NetworkGroup toGroup, byte sequenceChannel)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            toGroup ??= NetworkGroup.None;
            bool isExplicitGroup = toGroup.Id != 0;
            if (target == Target.Auto)
            {
                if (!isExplicitGroup)
                {
                    if (sender.IsInAnyGroup)
                    {
                        target = Target.Group;
                        toGroup = sender.MainGroup;
                    }
                    else
                    {
                        target = Target.Everyone;
                    }
                }
                else
                {
                    target = Target.Everyone;
                }
            }

            void Send(ReadOnlySpan<byte> message, NetworkPeer sender) => Connection.Server.Send(message, sender.EndPoint, deliveryMode, sequenceChannel);
            ReadOnlySpan<byte> message = PrepareServerMessageForSending(msgType, _data, sequenceChannel);
            if (IsServerActive)
            {
                if (target == Target.Self && isExplicitGroup)
                {
                    NetworkLogger.__Log__(
                        $"Warning: Target.SelfOnly with group ID {toGroup.Id} is logically inconsistent. When sending to self only, the group ID is irrelevant as messages are routed directly to the sender. Consider using group ID 0 for self-targeted messages.",
                        NetworkLogger.LogType.Warning
                    );
                }

                var peersById = PeersById;
                if (isExplicitGroup)
                {
                    if (!toGroup.AllowAcrossGroupMessage)
                    {
                        if (!toGroup._peersById.ContainsKey(sender.Id) && sender.Id != 0)
                        {
                            NetworkLogger.__Log__(
                                "Access Denied: Across-group messaging is currently disabled. To enable this functionality, set 'AllowAcrossGroupMessage' to true in the group settings or ensure the sender belongs to the target group.",
                                NetworkLogger.LogType.Error
                            );

                            return;
                        }
                    }

                    peersById = toGroup._peersById; // Filter: peers by group.
                }

                // Authentication step!
                if (msgType is NetworkPacketType.k_BeginHandshake or NetworkPacketType.k_EndHandshake)
                {
                    Send(message, sender);
                    return;
                }

                // Send message to peers
                switch (target)
                {
                    case Target.GroupOthers:
                    case Target.Group:
                        {
                            if (isExplicitGroup)
                            {
                                NetworkLogger.__Log__(
                                     $"Error: Cannot use Target.GroupOnly with group ID {toGroup.Id}. When using Target.GroupOnly, the system automatically broadcasts to all groups the sender belongs to. Specifying a specific group ID conflicts with this behavior. Use Target.AllPlayers instead if you need to target a specific group.",
                                     NetworkLogger.LogType.Error
                                );

                                return;
                            }

                            if (sender.Id == 0)
                            {
                                NetworkLogger.__Log__(
                                    "Error: The server peer (ID 0) cannot use group targeting. Server peers must specify an explicit group ID when broadcasting messages.",
                                    NetworkLogger.LogType.Error
                                );

                                return;
                            }

                            if (!sender.IsInAnyGroup)
                            {
                                NetworkLogger.__Log__(
                                     "Error: Failed to send group message. The sender is not a member of any groups. Join at least one group before attempting to send group-targeted messages.",
                                     NetworkLogger.LogType.Error
                                );

                                return;
                            }

                            foreach (var (_, peer) in sender.MainGroup._peersById)
                            {
                                if (!peer.IsAuthenticated)
                                {
                                    NetworkLogger.__Log__(
                                        "Warning: The server attempted to send a message to an unauthenticated peer. This may occasionally be acceptable, but verify the connection state if unexpected.",
                                        NetworkLogger.LogType.Warning
                                    );

                                    continue;
                                }

                                if (peer.Id == ServerSide.ServerPeer.Id)
                                    continue;

                                if (peer.Equals(sender) && target == Target.GroupOthers)
                                    continue;

                                Send(message, peer);
                            }
                        }
                        break;
                    case Target.Everyone:
                    case Target.Others:
                        {
                            foreach (var (_, peer) in peersById)
                            {
                                if (!peer.IsAuthenticated)
                                {
                                    NetworkLogger.__Log__(
                                        "Warning: The server attempted to send a message to an unauthenticated peer. This may occasionally be acceptable, but verify the connection state if unexpected.",
                                        NetworkLogger.LogType.Warning
                                    );

                                    continue;
                                }

                                if (peer.Id == ServerSide.ServerPeer.Id)
                                    continue;

                                if (peer.Equals(sender) && target == Target.Others)
                                    continue;

                                Send(message, peer);
                            }
                        }
                        break;
                    case Target.Self:
                        {
                            if (sender.Id == ServerSide.ServerPeer.Id)
                                return;

                            if (!sender.IsAuthenticated)
                            {
                                NetworkLogger.__Log__(
                                    "Warning: The server attempted to send a message to an unauthenticated peer. This may occasionally be acceptable, but verify the connection state if unexpected.",
                                    NetworkLogger.LogType.Warning
                                );

                                return;
                            }

                            // group id doesn't make sense here, because peersById is not used for target.Self.
                            Send(message, sender);
                        }
                        break;
                }
            }
        }

        private void SendClientAuthenticationMessage(byte msgType, DataBuffer authMessage)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            Connection.Client.Send(PrepareClientMessageForSending(msgType, authMessage.BufferAsSpan, 0), LocalEndPoint,
                DeliveryMode.ReliableOrdered, 0);
        }

        protected virtual void Internal_SendToServer(byte msgType, ReadOnlySpan<byte> _data, DeliveryMode deliveryMode, byte sequenceChannel)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            if (_localPeer != null && _localPeer.IsConnected)
            {
                Connection.Client.Send(PrepareClientMessageForSending(msgType, _data, sequenceChannel), LocalEndPoint, deliveryMode, sequenceChannel);
            }
            else
            {
                NetworkLogger.__Log__(
                    "Error: Attempted to send a message to the server while not connected. Please establish a connection before sending any messages.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        private async void QueryNtpPeriodically()
        {
            // The purpose of these calls before the while loop may be to ensure that the system clock is initially synchronized before entering the continuous query cycle.
            // This can be helpful to prevent situations where the system clock is not immediately synchronized when the application starts.
            //
            // Furthermore, the introduction of these initial pauses may serve as a startup measure to allow the system time to stabilize before initiating the repetitive querying of the NTP server.
            // This can be particularly useful if there are other startup or configuration operations that need to occur before the system is fully ready to synchronize the clock continuously.

            if (!IsClientActive || !m_SntpModule)
                return;

            Sntp.Client.Query();
            await UniTask.WaitForSeconds(0.5f);
            Sntp.Client.Query();
            await UniTask.WaitForSeconds(0.5f);
            Sntp.Client.Query();
            await UniTask.WaitForSeconds(0.5f);

            while (IsClientActive && m_SntpModule)
            {
                // Continuously query the NTP server to ensure that the system clock is continuously synchronized with the NTP server.
                Sntp.Client.Query();
                await UniTask.WaitForSeconds(SimpleNtp.k_DefaultQueryInterval);
            }
        }

        public virtual void Internal_OnServerInitialized()
        {
            NetworkHelper.EnsureRunningOnMainThread();
            // Set the default peer, used when the server sends to nothing(peerId = 0).
            NetworkPeer serverPeer = ServerSide.ServerPeer;
            serverPeer._aesKey = AesProvider.GenerateKey();
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
            if (_localPeer != null)
            {
                _localPeer.IsConnected = false;
                _localPeer.IsAuthenticated = false;
            }

            IsClientActive = false;
            OnClientDisconnected?.Invoke(reason);
        }

        public virtual async void Internal_OnServerPeerConnected(IPEndPoint peer, NativePeer nativePeer)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            NetworkPeer newPeer = new(peer, p_UniqueId++, isServer: true)
            {
                _nativePeer = nativePeer
            };

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
                    newPeer.IsConnected = true;
                    OnServerPeerConnected?.Invoke(newPeer, Phase.Started);
                    using var message = Pool.Rent(enableTracking: false);
                    message.Write(newPeer.Id);
                    // Generate a unique challenge token. 
                    // This token serves as proof of authenticity for the server's public key during the handshake process.
                    // The token is signed using the server's RSA private key, creating a digital signature.
                    // Upon receiving the token and its signature, the client will verify the signature using the provided RSA public key.
                    // If the public key has been tampered with (e.g., a Man-in-the-Middle attack), the signature verification will fail,
                    // ensuring the integrity and authenticity of the server's public key and protecting the connection.
                    string token = NetworkHelper.GenerateRandomToken();
                    byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
                    byte[] tokenSignature = RsaProvider.Compute(tokenBytes, ServerSide.PEMPrivateKey);
                    message.WriteAsBinary(tokenBytes);
                    // Writes the encrypted server's RSA public key to the buffer
                    // If the public key were modified (MITM) the connection would fail because the server public key is validated by the server and the client.
                    byte[] encryptedRsaPublicKey = AesDerivedProvider.Encrypt(ServerSide.PEMPublicKey, SharedKey);
                    message.WriteAsBinary(encryptedRsaPublicKey);
                    message.WriteAsBinary(tokenSignature);

                    ServerSide.SetTarget(Target.Self);
                    SendToClient(NetworkPacketType.k_BeginHandshake, newPeer, message);
                }
            }
        }

        public virtual void Internal_OnServerPeerDisconnected(IPEndPoint peer, string reason)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            OnServerPeerDisconnected?.Invoke(PeersByIp[peer], Phase.Started);
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
                            OnPlayerLeftGroup?.Invoke(group, currentPeer, Phase.Ended,
                                "Leave event called by disconnect event.");

                            // Change the current master client if the disconnected client was the master client
                            if (currentPeer.Id == group.MasterClient.Id)
                            {
                                var nextPeer = group._peersById.Values.FirstOrDefault();
                                if (nextPeer != null)
                                {
                                    group.SetMasterClient(nextPeer);
                                }
                            }

                            if (group.DestroyWhenEmpty)
                            {
                                ServerSide.DestroyGroupWhenEmpty(group);
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

                    OnServerPeerDisconnected?.Invoke(currentPeer, Phase.Active);

                    // Dereferencing to allow for GC(Garbage Collector).
                    currentPeer.ClearAllGroups();
                    currentPeer.ResetDataCollections();

                    // All resources should be released at this point.
                    currentPeer.IsConnected = false;

                    // Finished disconnection
                    OnServerPeerDisconnected?.Invoke(currentPeer, Phase.Ended);
                    NetworkLogger.__Log__($"[NetworkManager] Client disconnected from {peer.Address}:{peer.Port} - Reason: {reason}");
                }
            }
        }

        public virtual void Internal_OnP2PDataReceived(ReadOnlySpan<byte> data, IPEndPoint source)
        {
        }

        public virtual void Internal_OnDataReceived(ReadOnlySpan<byte> _data, DeliveryMode deliveryMethod,
            IPEndPoint endPoint, byte _channel_, bool isServer, out byte msgType)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            if (PeersByIp.TryGetValue(endPoint, out NetworkPeer peer) || !isServer)
            {
                try
                {
                    if (isServer) Connection.Server.ReceivedBandwidth.Add(_data.Length);
                    else Connection.Client.ReceivedBandwidth.Add(_data.Length);
#if OMNI_DEBUG && UNITY_EDITOR
                    Profiler.BeginSample(isServer ? "Omni_OnServerDataReceived" : "Omni_OnClientDataReceived");
#endif
                    int _length = _data.Length;
                    using DataBuffer header = Pool.Rent(enableTracking: false);
                    header.Write(_data);
                    header.SeekToBegin();

                    byte packed = header.Read<byte>();
                    msgType = (byte)(packed & 0xF);
                    byte channel = (byte)((packed >> 4) & 0xF);
                    if (!isServer)
                    {
                        if (msgType != NetworkPacketType.k_BeginHandshake && msgType != NetworkPacketType.k_EndHandshake)
                        {
                            if (_localPeer != null && !_localPeer.IsAuthenticated)
                            {
                                throw new Exception(
                                    "The client received a message while not yet authenticated. Wait until the handshake is completed."
                                );
                            }
                        }
                    }

                    if (peer == null)
                        peer = _localPeer;

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    DataBuffer EndOfHeader() // Disposed by the caller(not user)!
                    {
                        DataBuffer message = Pool.Rent(enableTracking: false);
                        message.Write(header.Internal_GetSpan(_length));
                        message.SeekToBegin();
                        return message;
                    }

                    switch (msgType)
                    {
                        //case NetworkPacketType.KCP_PING_REQUEST_RESPONSE:
                        //    {
                        //        // Implemented By KCP Transporter.
                        //    }
                        //    break;
                        case NetworkPacketType.k_SyncGroupSharedData:
                            {
                                if (!isServer)
                                {
                                    int groupId = header.Read<int>();
                                    ImmutableKeyValuePair keyValuePair = header.ReadAsJson<ImmutableKeyValuePair>();

                                    var groups = ClientSide.Groups;
                                    if (!groups.ContainsKey(groupId))
                                    {
                                        // This group is invalid!, Used only for data sync.
                                        groups.Add(groupId, new NetworkGroup(groupId, "NOT SERIALIZED!", isServer: false));
                                    }

                                    NetworkGroup fGroup = groups[groupId];
                                    if (keyValuePair.Key != NetworkConstants.k_ShareAllKeys)
                                    {
                                        if (!fGroup.SharedData.TryAdd(keyValuePair.Key, keyValuePair.Value))
                                        {
                                            fGroup.SharedData[keyValuePair.Key] = keyValuePair.Value;
                                        }
                                    }
                                    else
                                    {
                                        JObject jObject = (JObject)keyValuePair.Value;
                                        fGroup.SharedData = jObject.ToObject<ObservableDictionary<string, object>>();
                                    }

                                    OnGroupSharedDataChanged?.Invoke(fGroup, keyValuePair.Key);
                                }
                            }
                            break;
                        case NetworkPacketType.k_SyncPeerSharedData:
                            {
                                if (!isServer)
                                {
                                    int peerId = header.Read<int>();
                                    ImmutableKeyValuePair keyValuePair = header.ReadAsJson<ImmutableKeyValuePair>();

                                    var peers = ClientSide.Peers;
                                    if (!peers.ContainsKey(peerId))
                                    {
                                        // _peer is not valid endpoint in this case!
                                        peers.Add(peerId, new NetworkPeer(endPoint, peerId, isServer: false));
                                    }

                                    NetworkPeer fPeer = peers[peerId];
                                    if (keyValuePair.Key != NetworkConstants.k_ShareAllKeys)
                                    {
                                        if (!fPeer.SharedData.TryAdd(keyValuePair.Key, keyValuePair.Value))
                                        {
                                            fPeer.SharedData[keyValuePair.Key] = keyValuePair.Value;
                                        }
                                    }
                                    else
                                    {
                                        JObject jObject = (JObject)keyValuePair.Value;
                                        fPeer.SharedData = jObject.ToObject<ObservableDictionary<string, object>>();
                                    }

                                    OnPeerSharedDataChanged?.Invoke(fPeer, keyValuePair.Key);
                                }
                            }
                            break;
                        case NetworkPacketType.k_NtpQuery:
                            {
                                if (isServer)
                                {
                                    double time = header.Read<double>();
                                    float realtimeSinceStartup = header.Read<float>();
                                    Sntp.Server.SendResponse(time, peer, realtimeSinceStartup);
                                }
                                else
                                {
                                    double a = header.Read<double>();
                                    double x = header.Read<double>();
                                    double y = header.Read<double>();
                                    float realtimeSinceStartup = header.Read<float>();
                                    Sntp.Client.Evaluate(a, x, y, realtimeSinceStartup);
                                }
                            }
                            break;
                        case NetworkPacketType.k_BeginHandshake:
                            {
                                if (!isServer)
                                {
                                    // Read the peer ID and RSA public key from the server.
                                    int localPeerId = header.Read<int>();
                                    byte[] tokenBytes = header.ReadAsBinary<byte[]>();
                                    byte[] rsaServerPublicKey = header.ReadAsBinary<byte[]>();
                                    byte[] tokenSignature = header.ReadAsBinary<byte[]>();

                                    // Validate server public key
                                    string publicKey = AesDerivedProvider.DecryptToString(rsaServerPublicKey, SharedKey);
                                    if (!RsaProvider.Validate(tokenBytes, tokenSignature, publicKey))
                                        throw new CryptographicException("The server's public key could not be verified.");
                                    if (m_TlsHandshake)
                                    {
                                        if (publicKey != ClientSide.GetRsaPublicKeyFromResources())
                                        {
                                            throw new CryptographicException("The server's public key does not match the one stored in the certificate.");
                                        }
                                    }

                                    // Initialize the local peer with the provided ID and endpoint.
                                    LocalPeer = new NetworkPeer(LocalEndPoint, localPeerId, isServer: false)
                                    {
                                        _nativePeer = LocalNativePeer
                                    };

                                    ClientSide.Peers.Add(localPeerId, LocalPeer);

                                    // Generate an AES session key for encryption.
                                    ClientSide.PEMServerPublicKey = publicKey;
                                    byte[] aesKey = AesProvider.GenerateKey();
                                    LocalPeer._aesKey = aesKey;

                                    // Encrypt the AES session key using the server's RSA public key.
                                    byte[] encryptedAesKey = RsaProvider.Encrypt(aesKey, ClientSide.PEMServerPublicKey);
                                    // Send the encrypted AES key to the server to begin the handshake.
                                    using DataBuffer authMessage = Pool.Rent(enableTracking: false);
                                    authMessage.WriteAsBinary(encryptedAesKey);
                                    SendClientAuthenticationMessage(NetworkPacketType.k_BeginHandshake, authMessage);
                                }
                                else
                                {
                                    // Read and decrypt the AES key sent by the client using the server's RSA private key.
                                    byte[] aesKey = header.ReadAsBinary<byte[]>();
                                    peer._aesKey = RsaProvider.Decrypt(aesKey, ServerSide.PEMPrivateKey);

                                    // Encrypt the server's AES key using the client's decrypted AES key.
                                    byte[] serverAesKey = ServerSide.ServerPeer._aesKey;
                                    byte[] encryptedServerAesKey = AesProvider.Encrypt(serverAesKey, 0, serverAesKey.Length, peer._aesKey, out byte[] iv);

                                    // Send the encrypted server AES key and initialization vector (IV) to the client.
                                    using var message = Pool.Rent(enableTracking: false);
                                    message.WriteAsBinary(iv);
                                    message.WriteAsBinary(encryptedServerAesKey);

                                    ServerSide.SetTarget(Target.Self);
                                    SendToClient(NetworkPacketType.k_EndHandshake, peer, message);
                                    OnServerPeerConnected?.Invoke(peer, Phase.Active);
                                }
                            }
                            break;
                        case NetworkPacketType.k_EndHandshake:
                            {
                                if (!isServer)
                                {
                                    // Read the server's AES key and IV for final decryption.
                                    byte[] iv = header.ReadAsBinary<byte[]>();
                                    byte[] serverAesKeyEncrypted = header.ReadAsBinary<byte[]>();

                                    NetworkPeer serverPeerInClientSide = ClientSide.ServerPeer;
                                    // Decrypt the server's AES key using the client's AES key and IV.
                                    serverPeerInClientSide._aesKey = AesProvider.Decrypt(serverAesKeyEncrypted, 0, serverAesKeyEncrypted.Length, LocalPeer._aesKey, iv);

                                    // Mark the peer as connected and authenticated.
                                    LocalPeer.IsConnected = true;
                                    LocalPeer.IsAuthenticated = true;
                                    serverPeerInClientSide.IsConnected = true;
                                    serverPeerInClientSide.IsAuthenticated = true;
                                    // Notify the server that the handshake is complete.
                                    SendClientAuthenticationMessage(NetworkPacketType.k_EndHandshake, DataBuffer.Empty);
                                }
                                else
                                {
                                    NetworkLogger.__Log__(
                                        $"[NetworkManager] Server accepted connection from client at {endPoint.Address}:{endPoint.Port} {(m_TlsHandshake ? "(TLS)" : "")}",
                                        NetworkLogger.LogType.Log
                                    );

                                    peer.IsAuthenticated = true;
                                    OnServerPeerConnected?.Invoke(peer, Phase.Active);
                                    // Send confirmation to the client that the handshake is complete.
                                    ServerSide.SetTarget(Target.Self);
                                    SendToClient(NetworkPacketType.k_Authenticate, peer, DataBuffer.Empty);
                                }
                            }
                            break;
                        case NetworkPacketType.k_Authenticate:
                            {
                                if (!isServer)
                                {
                                    if (_localPeer != null && _localPeer.IsAuthenticated)
                                    {
                                        NetworkLogger.__Log__(
                                             $"[NetworkManager] Successfully connected to server at {ConnectAddress}:{Port}",
                                             NetworkLogger.LogType.Log
                                        );

                                        // If the peer is already authenticated, mark the client as active.
                                        // Notify the server that the authentication is complete.
                                        IsClientActive = true;
                                        OnClientConnected?.Invoke();
                                        QueryNtpPeriodically();
                                        SendClientAuthenticationMessage(NetworkPacketType.k_Authenticate, DataBuffer.Empty);
                                    }
                                }
                                else OnServerPeerConnected?.Invoke(peer, Phase.Ended);
                            }
                            break;
                        case NetworkPacketType.k_LocalRpc:
                            {
                                int identityId = header.Internal_Read();
                                byte instanceId = header.Read<byte>();
                                byte rpcId = header.Read<byte>();

                                using var message = EndOfHeader();
                                var key = (identityId, instanceId);
                                var rpcHandlers = isServer ? ServerSide.LocalRpcHandlers : ClientSide.LocalRpcHandlers;
                                if (rpcHandlers.TryGetValue(key, out IRpcMessage behaviour))
                                {
                                    behaviour.OnRpcReceived(rpcId, message, peer, isServer, channel);
                                }
                            }
                            break;
                        case NetworkPacketType.k_StaticRpc:
                            {
                                int identityId = header.Internal_Read();
                                byte rpcId = header.Read<byte>();

                                using var message = EndOfHeader();
                                var rpcHandlers = isServer ? ServerSide.StaticRpcHandlers : ClientSide.StaticRpcHandlers;
                                if (rpcHandlers.TryGetValue(identityId, out IRpcMessage behaviour))
                                {
                                    if (rpcId == NetworkConstants.k_NetworkVariableRpcId)
                                    {
                                        if (behaviour is DualBehaviour)
                                        {
                                            if (!isServer && IsHost)
                                                return;
                                        }
                                    }

                                    behaviour.OnRpcReceived(rpcId, message, peer, isServer, channel);
                                }
                            }
                            break;
                        case NetworkPacketType.k_LeaveGroup:
                            {
                                string groupName = header.ReadString();
                                string reason = header.ReadString();

                                if (isServer)
                                {
                                    ServerSide.LeaveGroup(groupName, reason, peer);
                                }
                                else
                                {
                                    OnLeftGroup?.Invoke(groupName, reason);
                                }
                            }
                            break;
                        case NetworkPacketType.k_JoinGroup:
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

                                        return;
                                    }

                                    if (groupName.Length > 256)
                                    {
                                        NetworkLogger.__Log__(
                                            "JoinGroup: Group name cannot be longer than 256 characters.",
                                            NetworkLogger.LogType.Error
                                        );

                                        return;
                                    }

                                    ServerSide.JoinGroup(groupName, message, peer, includeBufferInResponse: true);
                                }
                                else
                                {
                                    OnJoinedGroup?.Invoke(groupName, message);
                                }
                            }
                            break;
                        case NetworkPacketType.k_RequestEntityAction:
                            {
                                int identityId = header.Read<int>();
                                byte actionId = header.Read<byte>();
                                using var message = EndOfHeader();

                                if (isServer)
                                {
                                    if (ServerSide.TryGetIdentity(identityId, out var identity))
                                        identity.OnRequestAction?.Invoke(actionId, message, peer);
                                }
                                else
                                {
                                    if (ClientSide.TryGetIdentity(identityId, out var identity))
                                        identity.OnRequestAction?.Invoke(actionId, message, peer);
                                }
                            }
                            break;
                        default:
                            {
                                using var message = EndOfHeader();
                                if (isServer) OnServerCustomMessage?.Invoke(msgType, message, peer, channel);
                                else OnClientCustomMessage?.Invoke(msgType, message, channel);
                            }
                            break;
                    }
                }
                catch (CryptographicException ex)
                {
                    NetworkLogger.PrintHyperlink(ex);
                    NetworkLogger.__Log__("A cryptographic error has occurred: " + ex.Message, NetworkLogger.LogType.Error);
                    msgType = 0;
#if OMNI_DEBUG
                    throw;
#endif
                }
                catch (Exception ex)
                {
                    NetworkLogger.PrintHyperlink(ex);
                    NetworkLogger.__Log__("A general error has occurred: " + ex.Message, NetworkLogger.LogType.Error);
                    msgType = 0;
#if OMNI_DEBUG
                    throw;
#endif
                }
                finally
                {
#if OMNI_DEBUG && UNITY_EDITOR
                    Profiler.EndSample();
#endif
                }
            }
            else
            {
                msgType = 0;
            }
        }

        private static void DestroyScene(LoadSceneMode mode, Scene scene, SceneOperationMode op)
        {
            if (_allowLoadScene)
            {
                throw new Exception(
                    "Load Scene Error: The scene loading process is already in progress. Please wait for the current scene to fully load before attempting to load another scene."
                );
            }

            _allowLoadScene = true;
            // OnBeforeSceneLoad is invoked only in Single mode because:
            // - In Single mode, the current scene will be unloaded automatically. 
            //   Therefore, it's necessary to perform cleanup operations, such as removing registered events,
            //   destroying objects, and preparing for the next scene.
            // - In Additive mode, existing scenes are not unloaded, so global cleanup is usually unnecessary.
            //   The focus is on loading the new scene alongside the existing ones.
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

        public static AsyncOperation LoadSceneAsync(int index, LoadSceneMode mode = LoadSceneMode.Single)
        {
            DestroyScene(mode, SceneManager.GetSceneByBuildIndex(index), SceneOperationMode.Load);
            return SceneManager.LoadSceneAsync(index, mode);
        }

        public static AsyncOperation UnloadSceneAsync(string sceneName,
            UnloadSceneOptions options = UnloadSceneOptions.None)
        {
            DestroyScene(LoadSceneMode.Single, SceneManager.GetSceneByName(sceneName), SceneOperationMode.Unload);
            return SceneManager.UnloadSceneAsync(sceneName, options);
        }

        public static AsyncOperation UnloadSceneAsync(int index, bool useBuildIndex = false,
            UnloadSceneOptions options = UnloadSceneOptions.None)
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
            if (Manager.m_NetworkPrefabs.Any(x => x != null && x.name == prefab.name))
                return;

            Manager.m_NetworkPrefabs.Add(prefab);
        }

        /// <summary>
        /// Retrieves a prefab by its name.
        /// </summary>
        /// <param name="prefabName">The name of the prefab to retrieve.</param>
        /// <returns>The prefab with the specified name.</returns>
        public static NetworkIdentity GetPrefab(string prefabName)
        {
            return Manager.m_NetworkPrefabs.FirstOrDefault(x => x != null && x.name == prefabName)
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
            if (index >= 0 && index < Manager.m_NetworkPrefabs.Count)
            {
                return Manager.m_NetworkPrefabs[index];
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
        /// Populates the properties of the target object from the provided JSON string.
        /// </summary>
        /// <param name="json">The JSON string to populate the target object from.</param>
        /// <param name="target">The object to populate with the JSON data.</param>
        /// <param name="settings">Optional settings for JSON deserialization (default is null).</param>
        public static void FromJson(string json, object target, JsonSerializerSettings settings = null)
        {
            settings ??= BufferWriterExtensions.DefaultJsonSettings;
            JsonConvert.PopulateObject(json, target, settings);
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

        /// <summary>
        /// Splits the provided data into blocks of a specified size.
        /// </summary>
        /// <param name="data">The data to be split.</param>
        /// <param name="blockSize">The size of each block. Defaults to 128.</param>
        /// <returns>A list of byte arrays, each representing a block of the original data.</returns>
        public static List<byte[]> Split(ReadOnlySpan<byte> data, int blockSize = 128)
        {
            if (data.Length <= blockSize)
            {
                throw new Exception("The specified data must be longer than the block size");
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

        public virtual void OnDestroy()
        {
            try
            {
                if (Application.isPlaying)
                {
                    UniTask.Void(async () =>
                    {
                        await UniTask.Delay(500);
                        if (Application.isPlaying)
                        {
                            NetworkLogger.__Log__("NetworkManager is being destroyed. This component is required to be present in the scene at all times."
                            + " Network functionality will cease to work without it. All connections will be terminated.", NetworkLogger.LogType.Error);
                        }
                    });

                    OnApplicationQuit();
                }
            }
            catch { }
        }

        public virtual void OnApplicationQuit()
        {
            Connection.Server.Stop();
            Connection.Client.Stop();

            // Dispose the log file stream
            if (NetworkLogger.fileStream != null)
            {
                NetworkLogger.fileStream.Close();
                NetworkLogger.fileStream.Dispose();
                NetworkLogger.fileStream = null; // Bug fix
            }

            IsClientActive = false;
            IsServerActive = false;
        }

        void OnGUI()
        {
            if (m_HideDebugInfo)
                return;

            float widthScale = Screen.width / 1920f;
            float heightScale = Screen.height / 1080f;
            float scale = Mathf.Min(widthScale, heightScale);

            float scaledWidth = 400f * scale;
            float scaledHeight = 210f * scale;
            float scaledPadding = 10f * scale;
            int scaledFontSize = Mathf.RoundToInt(24 * scale);

            GUIStyle windowStyle = new(GUI.skin.box)
            {
                fontSize = scaledFontSize,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(Mathf.RoundToInt(12 * scale), 0, Mathf.RoundToInt(12 * scale), 0)
            };

            GUIStyle headerStyle = new(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(scaledFontSize * 1.2f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 1f) }
            };

            GUIStyle contentStyle = new(GUI.skin.label)
            {
                fontSize = scaledFontSize,
                alignment = TextAnchor.UpperLeft,
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            bool isClone = false;
#if UNITY_EDITOR
            if (ClonesManager.IsClone())
            {
                isClone = true;
            }

#if OMNI_VIRTUAL_PLAYER_ENABLED
            if (MPPM.IsVirtualPlayer)
            {
                isClone = true;
            }
#endif
#endif
#if UNITY_EDITOR && OMNI_DEBUG
            float warningWidth = 710f * scale;
            float warningHeight = 110f * scale;
            Rect warningRect = new(scaledPadding, Screen.height - scaledPadding - warningHeight, warningWidth, warningHeight);

            GUI.BeginGroup(warningRect);
            GUI.Box(new Rect(0, 0, warningRect.width, warningRect.height), "", windowStyle);

            GUIStyle warningTextStyle = new(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(scaledFontSize * 0.8f),
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.red }
            };

            string warningText = !isClone
                ? "<b>⚠ DEBUG MODE</b>\nVery Slow in Editor\nUse in development only. For performance testing, use Release Mode."
                : "<b>⚠ DEBUG MODE</b>\nVery Slow in Editor\nUse in development only. For performance testing, use Release Mode [ParrelSync Clone]";

            GUI.Label(new Rect(10, 10, warningRect.width - 20, warningRect.height - 20), warningText, warningTextStyle);
            GUI.EndGroup();
#endif

#if OMNI_DEBUG
            Rect windowRect = new(scaledPadding, scaledPadding, scaledWidth, scaledHeight);

            GUI.BeginGroup(windowRect);
            GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), "", windowStyle);
            GUI.Box(new Rect(0, 0, windowRect.width, scaledFontSize * 1.5f), "NETWORK STATS", headerStyle);

            string valueColor = "<color=#8ae1ff>";
            string timeColor = "<color=#8aff8a>";
            string pingColor = "<color=#ffcf8a>";

            string statsContent = $"{valueColor}FPS:</color> {Framerate} | {valueColor}CPU:</color> {CpuTimeMs} ms\n";

            if (IsClientActive)
            {
                statsContent += $"{pingColor}Ping:</color> {Sntp.Client.Ping} ms / Tick Ping: {Sntp.Client.Ping2}\n";
                statsContent += $"{timeColor}Local Time:</color> {TickSystem.ElapsedTicks} sec\n";
                if (m_SntpModule) statsContent += $"{timeColor}Server Time:</color> {Sntp.Client.SyncedTime}";
            }
            else if (m_SntpModule)
            {
                statsContent += $"{timeColor}Server Time:</color> {Sntp.Server.LocalTime}";
            }

            float labelYOffset = scaledFontSize * 2f;
            GUI.Label(new Rect(10, labelYOffset, windowRect.width - 20, windowRect.height - scaledFontSize * 1.5f - labelYOffset),
                      statsContent, contentStyle);

            GUI.EndGroup();
#endif
        }
    }
}