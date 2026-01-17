#if UNITY_EDITOR
using ParrelSync;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Omni.Inspector;
using UnityEngine;
using Omni.Shared;
using Omni.Core.Web;

#pragma warning disable

namespace Omni.Core
{
    [DeclareFoldoutGroup("Infor", Expanded = true, Title = "Network")]
    [DeclareBoxGroup("Listen"), DeclareBoxGroup("Connection")]
    [DeclareTabGroup("MiscTabs")]
    public partial class NetworkManager
    {
        #region Obsolete
        private bool m_ConnectionModule = true;
        private bool m_ConsoleModule = true;
        private bool m_MatchModule = true;
        private bool m_TickModule = true;
        private bool m_SntpModule = true;
        #endregion

        private int frameCount = 0;
        private float deltaTime = 0f;

        [SerializeField]
        [ReadOnly]
        [Group("Infor")]
        private string m_CurrentVersion = NetworkLogger.Version;

        [SerializeField]
        [LabelText("Public IPv4")]
        [ReadOnly]
        [Group("Infor")]
        private string PublicIPv4 = "127.0.0.1";

        [SerializeField]
        [LabelText("Public IPv6")]
        [ReadOnly]
        [Group("Infor")]
        private string PublicIPv6 = "::1";

        [SerializeField]
        [Group("Connection")]
        [LabelText("Hosts")]
        private List<string> m_ConnectAddresses = new List<string>()
        {
            "127.0.0.1",
            "::1"
        };

        [SerializeField]
        [Group("Connection")]
        private int m_Port = 7777;

        [SerializeField]
        [Group("MiscTabs"), Tab("Basic")]
        [LabelWidth(140), DisableIf(nameof(m_HasConnectionDisplayHud))]
#if OMNI_RELEASE
        [HideInInspector]
#endif
        private bool m_StartClient = true;

        [SerializeField]
        [Group("MiscTabs"), Tab("Basic")]
        [LabelWidth(140), DisableIf(nameof(m_HasConnectionDisplayHud))]
#if OMNI_RELEASE
        [HideInInspector]
#endif
        private bool m_StartServer = true;

        [SerializeField]
        [Group("MiscTabs"), Tab("Basic")]
        [LabelText("Use UTF-8 Encoding")]
        [LabelWidth(140)]
        private bool m_UseUtf8 = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Basic")]
        [Range(10, 120)]
        private int m_TickRate = 15;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [Min(1)]
        private int m_PoolCapacity = DataBuffer.DefaultBufferSize;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [Min(1)]
        private int m_PoolSize = 32;

        [SerializeField]
        [Group("MiscTabs"), Tab("Basic")]
        [Min(0)]
        private int m_LockClientFps = 300;

        [SerializeField]
        [Group("MiscTabs"), Tab("Basic")]
        [Min(0)]
        private int m_LockServerFps = 300; // 0 = Unlocked -> But the CPU will be maxed out.

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [LabelWidth(190)]
        [InfoBox(
            "Dapper has limited 'IL2CPP' support. Disable for JSON mapping. Tip: Use 'Mono' for Server, 'IL2CPP' for Client for database operations.")]
        private bool m_UseDapper = true;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [LabelWidth(190)]
        private bool m_UseSecureRoutes = false;

        // [SerializeField]
        // [ReadOnly]
        private bool m_UseBinarySerialization = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [LabelWidth(190)]
        private bool m_UseUnalignedMemory = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [LabelWidth(190)]
        private bool m_EnableBandwidthOptimization = true;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [LabelWidth(190)]
        private bool m_RunInBackground = true;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
#if OMNI_DEBUG || UNITY_WEBGL
        [HideInInspector]
#endif
        [LabelText("Client Backend")]
        private ScriptingBackend m_ClientScriptingBackend = ScriptingBackend.Mono;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
#if OMNI_DEBUG || UNITY_WEBGL
        [HideInInspector]
#endif
        [LabelText("Server Backend")]
        private ScriptingBackend m_ServerScriptingBackend = ScriptingBackend.Mono;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [ReadOnly, LabelWidth(150)]
        private string certificateFile = NetworkConstants.k_CertificateFile;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [LabelWidth(150)]
        private bool m_TlsHandshake = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Advanced")]
        [LabelWidth(150), ShowIf("m_TlsHandshake")]
        private bool m_WarnIfCertInvalid = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Http Server")]
        [LabelWidth(120)]
        [ValidateInput("OnEnableHttpServer")]
        private bool m_EnableHttpServer = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Http Server")]
        [LabelWidth(120)]
        [Indent(1)]
        [ShowIf(nameof(m_EnableHttpServer))]
        private bool m_UseKestrel = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Http Server")]
        [LabelWidth(120)]
        [ShowIf(nameof(m_EnableHttpServer))]
        [ValidateInput("OnEnableHttpSsl")]
        private bool m_EnableHttpSsl = false;

        [SerializeField]
        [Group("MiscTabs"), Tab("Http Server")]
        [LabelWidth(120)]
        [ShowIf(nameof(m_EnableHttpServer))]
        [HideIf(nameof(m_UseKestrel))]
        private int m_HttpServerPort = 80;

        [SerializeField]
        [Group("MiscTabs"), Tab("Http Server")]
        [LabelWidth(120)]
        [ShowIf(nameof(m_EnableHttpServer)), ShowIf(nameof(m_UseKestrel))]
        [InlineProperty]
        [HideLabel]
        [Title("Kestrel Options", HorizontalLine = true)]
        private KestrelOptions m_KestrelOptions = new();

        [SerializeField]
        private List<NetworkIdentity> m_NetworkPrefabs = new();

        [SerializeField]
        [DisableInEditMode]
        private List<InspectorSerializableGroup> Groups = new();

#if OMNI_RELEASE
        [HideInInspector]
#endif
        [SerializeField] private bool m_HideDebugInfo = false;

#if OMNI_RELEASE
        [HideInInspector]
#endif
        [ValidateInput("OnEnableDeepDebug")]
        [SerializeField] private bool m_EnableDeepDebug = true;

        public static string ConnectAddress => Manager.m_ConnectAddresses[0];
        internal static bool EnableDeepDebug => Manager.m_EnableDeepDebug;
        internal static bool MatchmakingModuleEnabled => Manager.m_MatchModule;
        internal static bool TickSystemModuleEnabled => Manager.m_TickModule;
        internal static bool UseSecureRoutes => Manager.m_UseSecureRoutes;

        public static int Port => Manager.m_Port;
        public static int Framerate { get; private set; }
        public static int CpuTimeMs { get; private set; }

        public virtual void Reset()
        {
            PlayerPrefs.DeleteKey("lastIpUpdate");
            OnValidate();
        }

        public virtual void OnValidate()
        {
            try
            {
                m_CurrentVersion = NetworkLogger.Version;
                if (m_HttpServerPort == 80 && m_EnableHttpSsl)
                {
                    m_HttpServerPort = 443;
                    NetworkHelper.EditorSaveObject(gameObject);
                }
                else if (m_HttpServerPort == 443 && !m_EnableHttpSsl)
                {
                    m_HttpServerPort = 80;
                    NetworkHelper.EditorSaveObject(gameObject);
                }

#if OMNI_DEBUG
                m_ClientScriptingBackend = ScriptingBackend.Mono;
                m_ServerScriptingBackend = ScriptingBackend.Mono;
#endif
                m_ConnectionModule = true;
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

                DisableAutoStartIfHasHud();
                Application.runInBackground = m_RunInBackground;
                // Trim the list.
                for (int i = 0; i < m_ConnectAddresses.Count; i++)
                    m_ConnectAddresses[i] = m_ConnectAddresses[i].Trim();

                m_KestrelOptions.m_UseHttps = m_EnableHttpSsl;
            }
            catch { }
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
            catch
            {
                // Suppress any errors!
            }
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
            // WebGl only supports IL2CPP -> Wasm
#if UNITY_WEBGL
			return;
#endif
            ScriptingBackend[] scriptingBackends =
            {
                m_ServerScriptingBackend,
                m_ClientScriptingBackend
            };

            bool isClone = false;
#if UNITY_EDITOR
            if (ClonesManager.IsClone())
            {
                isClone = true;
            }

#if OMNI_VIRTUAL_PLAYER_ENABLED && UNITY_6000_3_OR_NEWER && UNITY_EDITOR
            if (MPPM.IsVirtualPlayer)
            {
                isClone = true;
            }
#endif
#endif
            if (!isClone)
            {
                try
                {
                    using StreamWriter writer = new("ScriptingBackend.txt");
                    writer.Write(ToJson(scriptingBackends));
                }
                catch
                {
                    // ignore shared violation
                }
            }
        }

        [ContextMenu("Force Get Public IP")]
        private void ForceGetExternalIp()
        {
            PlayerPrefs.DeleteKey("lastIpUpdate");
            GetExternalIp();
        }

        [Conditional("UNITY_EDITOR")]
        private async void GetExternalIp()
        {
            const int minutes = 5;
            string lastDateTime = PlayerPrefs.GetString("lastIpUpdate", DateTime.UnixEpoch.ToString());
            TimeSpan timeLeft = DateTime.Now - DateTime.Parse(lastDateTime);
            // Check if the last call was successful or if an {minutes} time has passed since the last call to avoid spamming.
            if (timeLeft.TotalMinutes >= minutes)
            {
                string publicIPv4 = (await NetworkHelper.GetExternalIpAsync(useIPv6: false)).ToString();
                string publicIPv6 = (await NetworkHelper.GetExternalIpAsync(useIPv6: true)).ToString();

                // Check if the IP has changed and update it.
                if (publicIPv4 != PublicIPv4)
                {
                    // Remove the old addresses.
                    if (m_ConnectAddresses.Contains(PublicIPv4) &&
                        (PublicIPv4.ToLowerInvariant() != "localhost" && PublicIPv4 != "127.0.0.1"))
                        m_ConnectAddresses.Remove(PublicIPv4);

                    PublicIPv4 = publicIPv4;
                    NetworkHelper.EditorSaveObject(gameObject);
                }

                if (publicIPv6 != PublicIPv6)
                {
                    // Remove the old addresses.
                    if (m_ConnectAddresses.Contains(PublicIPv6) &&
                        (PublicIPv6.ToLowerInvariant() != "localhost" && PublicIPv6 != "::1"))
                        m_ConnectAddresses.Remove(PublicIPv6);

                    PublicIPv6 = publicIPv6;
                    NetworkHelper.EditorSaveObject(gameObject);
                }

                // Add the new addresses.
                if (PublicIPv4.ToLowerInvariant() != "localhost" || PublicIPv4 != "127.0.0.1")
                {
                    if (!m_ConnectAddresses.Contains(PublicIPv4))
                    {
                        m_ConnectAddresses.Add(PublicIPv4);
                        NetworkHelper.EditorSaveObject(gameObject);
                    }
                }

                if (PublicIPv6.ToLowerInvariant() != "localhost" || PublicIPv6 != "::1")
                {
                    if (!m_ConnectAddresses.Contains(PublicIPv6))
                    {
                        m_ConnectAddresses.Add(PublicIPv6);
                        NetworkHelper.EditorSaveObject(gameObject);
                    }
                }

                // Update the player preference with the current timestamp.
                PlayerPrefs.SetString("lastIpUpdate", DateTime.Now.ToString());
            }
        }

        [SerializeField, HideInInspector]
        private bool m_HasConnectionDisplayHud = false;
        private void DisableAutoStartIfHasHud()
        {
#if OMNI_DEBUG
            m_HasConnectionDisplayHud = TryGetComponent<NetworkConnectionDisplay>(out _) || FindFirstObjectByType<NetworkConnectionDisplay>() != null;
            if (m_HasConnectionDisplayHud)
            {
                m_StartClient = false;
                m_StartServer = false;
                NetworkHelper.EditorSaveObject(gameObject);
            }
#else
            m_HasConnectionDisplayHud = false;
#endif
        }

        private TriValidationResult OnEnableHttpServer()
        {
            if (m_EnableHttpServer)
            {
                return TriValidationResult.Warning(
                    "The built-in HTTP server feature is experimental and may be unstable. Use with caution. " +
                    "For high-demand or production environments, it is strongly recommended to enable and configure Kestrel instead."
                );
            }

            return TriValidationResult.Valid;
        }

        private TriValidationResult OnEnableHttpSsl()
        {
            if (m_EnableHttpServer && m_EnableHttpSsl)
            {
                return TriValidationResult.Warning(
                    "Built-in HTTP SSL support is experimental. For production environments, it is strongly recommended to use a full-featured and professional proxy (e.g., Nginx, Caddy, HAProxy) in front of the server."
                );
            }

            return TriValidationResult.Valid;
        }

        private TriValidationResult OnEnableDeepDebug()
        {
            if (!m_EnableDeepDebug)
            {
                return TriValidationResult.Warning(
                    "Disabling this option may hinder debugging capabilities and make it more difficult to identify and resolve issues. However, it can significantly improve editor performance."
                );
            }

            return TriValidationResult.Valid;
        }
    }
}