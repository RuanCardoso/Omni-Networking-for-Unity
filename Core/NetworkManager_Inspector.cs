using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Omni.Core.Attributes;
using UnityEngine;
#if OMNI_DEBUG
using Omni.Shared;
#endif

#pragma warning disable

namespace Omni.Core
{
    public partial class NetworkManager
    {
        private int frameCount = 0;
        private float deltaTime = 0f;

        private static int p_UniqueId = 1; // 0 - is reserved for server
        private static bool _allowZeroGroupForInternalMessages = false;

        private static NetworkManager _manager;
        private static NetworkManager Manager
        {
            get
            {
                if (_manager == null)
                {
                    throw new Exception(
                        "Network Manager not initialized. Please add it to the scene."
                    );
                }

                return _manager;
            }
            set => _manager = value;
        }

        private static Dictionary<int, NetworkGroup> Groups { get; } = new();
        private static Dictionary<IPEndPoint, NetworkPeer> PeersByIp { get; } = new();
        private static Dictionary<int, NetworkPeer> PeersById { get; } = new();

        [SerializeField]
        [Label("Public IPv4")]
        [ReadOnly]
        private string PublicIPv4 = "127.0.0.1";

        [SerializeField]
        [Label("Public IPv6")]
        [ReadOnly]
        private string PublicIPv6 = "127.0.0.1";

        [Header("Scripting Backend")]
        [SerializeField]
#if OMNI_DEBUG
        [ReadOnly]
#endif
        [Label("Client Backend")]
        private ScriptingBackend m_ClientScriptingBackend = ScriptingBackend.Mono;

        [SerializeField]
#if OMNI_DEBUG
        [ReadOnly]
#endif
        [Label("Server Backend")]
        private ScriptingBackend m_ServerScriptingBackend = ScriptingBackend.Mono;

        [ReadOnly]
        [SerializeField]
        [Header("Modules")]
        private bool m_Connection = true;

        [SerializeField]
        private bool m_Matchmaking = false;

        [SerializeField]
        private bool m_TickSystem = false;

        [SerializeField]
        [Label("Server Console")]
        private bool m_Console = false;

        [SerializeField]
        [Label("Server Clock(Ntp)")]
        private bool m_NtpClock = false;

        [ReadOnly]
        [SerializeField]
        [Header("Transporters")]
        private TransporterBehaviour m_ServerTransporter;

        [ReadOnly]
        [SerializeField]
        private TransporterBehaviour m_ClientTransporter;

        [SerializeField]
        [Header("Listen")]
        [Label("Server Port")]
        private int m_ServerListenPort = 7777;

        [SerializeField]
        [Label("Client Port")]
        private int m_ClientListenPort = 7778;

        [Header("Connection")]
        [SerializeField]
        [Label("Host Address")]
        private string m_ConnectAddress = "127.0.0.1";

        [SerializeField]
        [Label("Port")]
        private int m_ConnectPort = 7777;

        [SerializeField]
        [Header("Misc")]
        [Min(1)]
        private int m_TickRate = 15;

        [SerializeField]
        [Min(1)]
        private int m_MaxFpsOnClient = 60;

        [SerializeField]
        [Label("Allow Across-Group Message")]
        private bool m_AcrossGroupMessage = false;

        [SerializeField]
        [Label("Allow Zero-Group Message")]
        private bool m_ZeroGroupMessage = true;

        [Header("Misc +")]
        [SerializeField]
#if OMNI_RELEASE
        [ReadOnly]
#endif
        private bool m_AutoStartClient = true;

        [SerializeField]
#if OMNI_RELEASE
        [ReadOnly]
#endif
        private bool m_AutoStartServer = true;

        [SerializeField]
        private bool m_RunInBackground = true;

        public static string ConnectAddress => Manager.m_ConnectAddress;

        internal static bool MatchmakingModuleEnabled => Manager.m_Matchmaking;
        internal static bool TickSystemModuleEnabled => Manager.m_TickSystem;

        public static int ServerListenPort => Manager.m_ServerListenPort;
        public static int ClientListenPort => Manager.m_ClientListenPort;
        public static int ConnectPort => Manager.m_ConnectPort;

        public static float Framerate { get; private set; }
        public static float CpuTimeMs { get; private set; }

        public virtual void Reset()
        {
            OnValidate();
        }

        public virtual void OnValidate()
        {
#if OMNI_DEBUG
            m_ClientScriptingBackend = ScriptingBackend.Mono;
            m_ServerScriptingBackend = ScriptingBackend.Mono;
#endif
            m_Connection = true;
            if (!Application.isPlaying)
            {
                if (m_ClientTransporter != null || m_ServerTransporter != null)
                {
                    m_ClientTransporter = m_ServerTransporter = null;
                    throw new Exception("Transporter cannot be set. Is automatically initialized.");
                }

                GetExternalIp();
                SetScriptingBackend();
                StripComponents();
            }

            Application.runInBackground = m_RunInBackground;
            m_ConnectAddress = m_ConnectAddress.Trim();
            DisableAutoStartIfHasHud();
        }

        [ContextMenu("Strip Components")]
        private void StripComponents()
        {
            try
            {
                // Strip the components.
                var serverObject = transform.GetChild(0);
                var clientObject = transform.GetChild(1);

#if OMNI_RELEASE
                UnityEngine.Debug.Log("Network Manager: Components stripped. Ready to build!");
                name = "Network Manager";

#if UNITY_SERVER
                SetTag(clientObject, "EditorOnly");
                SetTag(serverObject, "Untagged");
#else
                SetTag(serverObject, "EditorOnly");
                SetTag(clientObject, "Untagged");
#endif
#elif OMNI_DEBUG
                SetTag(clientObject, "Untagged");
                SetTag(serverObject, "Untagged");
#endif
            }
            catch { }
        }

        private void SetTag(Transform gameObject, string tagName)
        {
            if (!gameObject.CompareTag(tagName)) // disable warning!
            {
                gameObject.tag = tagName;
            }
        }

        [ContextMenu("Set Scripting Backend")]
        private void SetScriptingBackend()
        {
            ScriptingBackend[] scriptingBackends =
            {
                m_ServerScriptingBackend,
                m_ClientScriptingBackend
            };

            using StreamWriter writer = new("ScriptingBackend.txt");
            writer.Write(ToJson(scriptingBackends));
        }

        [ContextMenu("Get External IP")]
        private void ForceGetExternalIp()
        {
            PlayerPrefs.DeleteKey("IPLastReceiveDate");
            GetExternalIp();
        }

        [Conditional("UNITY_EDITOR")]
        private async void GetExternalIp()
        {
            string lastDateTime = PlayerPrefs.GetString(
                "IPLastReceiveDate",
                DateTime.UnixEpoch.ToString()
            );

            int minutes = 15;
            TimeSpan timeLeft = DateTime.Now - DateTime.Parse(lastDateTime);
            // Check if the last call was successful or if an {minutes} time has passed since the last call to avoid spamming.
            if (timeLeft.TotalMinutes >= minutes)
            {
                PublicIPv4 = (await NetworkHelper.GetExternalIp(useIPv6: false)).ToString();
                PublicIPv6 = (await NetworkHelper.GetExternalIp(useIPv6: true)).ToString();

                // Update the player preference with the current timestamp.
                PlayerPrefs.SetString("IPLastReceiveDate", DateTime.Now.ToString());
            }
            else
            {
#if OMNI_DEBUG
                timeLeft = TimeSpan.FromMinutes(minutes) - timeLeft;
                NetworkLogger.Log(
                    $"You should wait {minutes} minutes before you can get the external IP again. Go to the context menu and click \"Get External IP\" to force it. Remaining time: {timeLeft.Minutes:0} minutes and {timeLeft.Seconds} seconds.",
                    logType: NetworkLogger.LogType.Warning
                );
#endif
            }
        }

        private bool DisableAutoStartIfHasHud()
        {
            if (TryGetComponent<NetworkHud>(out _))
            {
                m_AutoStartClient = false;
                m_AutoStartServer = false;

                return true;
            }

            return false;
        }

        public virtual void OnApplicationQuit()
        {
            Connection.Server.Stop();
            Connection.Client.Stop();
        }
    }
}
