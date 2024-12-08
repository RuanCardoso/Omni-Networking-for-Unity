#if UNITY_EDITOR
using ParrelSync;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using TriInspector;
using UnityEngine;

#if OMNI_DEBUG
using Omni.Shared;
#endif

#pragma warning disable

namespace Omni.Core
{
	[DeclareFoldoutGroup("Modules")]
	[DeclareFoldoutGroup("Infor", Expanded = true)]
	[DeclareFoldoutGroup("Permissions")]
	[DeclareTabGroup("MiscTabs")]
	[DeclareBoxGroup("Listen")]
	[DeclareBoxGroup("Connection")]
	public partial class NetworkManager
	{
		private TransporterBehaviour m_ServerTransporter;
		private TransporterBehaviour m_ClientTransporter;

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

		private static Dictionary<int, NetworkGroup> GroupsById { get; } = new();
		private static Dictionary<IPEndPoint, NetworkPeer> PeersByIp { get; } = new();
		private static Dictionary<int, NetworkPeer> PeersById { get; } = new();

		[SerializeField]
		[ReadOnly]
		[Group("Infor")]
		private string m_CurrentVersion = "v2.0.9";

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

		private bool m_ConnectionModule = true;
		private bool m_ConsoleModule = true;
		private bool m_MatchModule = true;

		[SerializeField]
		[Group("Modules")]
		private bool m_TickModule = false;

		[SerializeField]
		[Group("Modules")]
		private bool m_SntpModule = false;

		[SerializeField]
		[Group("Listen")]
		[LabelText("Server Port")]
		private int m_ServerListenPort = 7777;

		[SerializeField]
		[Group("Listen")]
		[LabelText("Client Port")]
		private int m_ClientListenPort = 7778;

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
		[LabelText("Port")]
		private int m_ConnectPort = 7777;

		[SerializeField]
		[Group("MiscTabs"), Tab("Basic")]
		[LabelWidth(140)]
#if OMNI_RELEASE
		[HideInInspector]
#endif
		private bool m_AutoStartClient = true;

		[SerializeField]
		[Group("MiscTabs"), Tab("Basic")]
		[LabelWidth(140)]
#if OMNI_RELEASE
		[HideInInspector]
#endif
		private bool m_AutoStartServer = true;

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
		private int m_PoolCapacity = 32768;

		[SerializeField]
		[Group("MiscTabs"), Tab("Advanced")]
		[Min(1)]
		private int m_PoolSize = 32;

		[SerializeField]
		[Group("MiscTabs"), Tab("Basic")]
		[Min(0)]
		private int m_LockClientFps = 60;

		[SerializeField]
		[Group("MiscTabs"), Tab("Advanced")]
		[LabelWidth(190)]
		private bool m_UseSecureRouteX = false;

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
		[Group("Permissions")]
		[LabelWidth(230)]
		private bool m_AllowNetworkVariablesFromClients = false;

		[SerializeField]
		[Group("Permissions")]
		[LabelWidth(230)]
		private bool m_AllowAcrossGroupMessage = false;

		// [SerializeField]
		// [ReadOnly]
		// [LabelText("Allow Zero-Group Message")]
		private bool m_AllowZeroGroupMessage = true;

		[SerializeField]
		private List<NetworkIdentity> m_NetworkPrefabs = new();

		public static string ConnectAddress => Manager.m_ConnectAddresses[0];

		internal static bool MatchmakingModuleEnabled => Manager.m_MatchModule;
		internal static bool TickSystemModuleEnabled => Manager.m_TickModule;
		internal static bool UseSecureRouteX => Manager.m_UseSecureRouteX;

		public static int ServerListenPort => Manager.m_ServerListenPort;
		public static int ClientListenPort => Manager.m_ClientListenPort;
		public static int ConnectPort => Manager.m_ConnectPort;

		public static float Framerate { get; private set; }
		public static float CpuTimeMs { get; private set; }

		public virtual void Reset()
		{
			PlayerPrefs.DeleteKey("IPLastReceiveDate");
			OnValidate();
		}

		public virtual void OnValidate()
		{
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
#endif
			if (!isClone)
			{
				try
				{
					using StreamWriter writer = new("ScriptingBackend.txt");
					writer.Write(ToJson(scriptingBackends));
				}
				catch { }
			}
		}

		[ContextMenu("Force Get Public IP")]
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
				string publicIPv4 = (await NetworkHelper.GetExternalIpAsync(useIPv6: false)).ToString();
				string publicIPv6 = (await NetworkHelper.GetExternalIpAsync(useIPv6: true)).ToString();

				// Check if the IP has changed and update it.
				if (publicIPv4 != PublicIPv4)
				{
					// Remove the old addresses.
					if (m_ConnectAddresses.Contains(PublicIPv4) && (PublicIPv4.ToLower() != "localhost" && PublicIPv4 != "127.0.0.1"))
						m_ConnectAddresses.Remove(PublicIPv4);

					PublicIPv4 = publicIPv4;
					NetworkHelper.EditorSaveObject(gameObject);
				}

				if (publicIPv6 != PublicIPv6)
				{
					// Remove the old addresses.
					if (m_ConnectAddresses.Contains(PublicIPv6) && (PublicIPv6.ToLower() != "localhost" && PublicIPv6 != "::1"))
						m_ConnectAddresses.Remove(PublicIPv6);

					PublicIPv6 = publicIPv6;
					NetworkHelper.EditorSaveObject(gameObject);
				}

				// Add the new addresses.
				if (PublicIPv4.ToLower() != "localhost" || PublicIPv4 != "127.0.0.1")
				{
					if (!m_ConnectAddresses.Contains(PublicIPv4))
					{
						m_ConnectAddresses.Add(PublicIPv4);
						NetworkHelper.EditorSaveObject(gameObject);
					}
				}

				if (PublicIPv6.ToLower() != "localhost" || PublicIPv6 != "::1")
				{
					if (!m_ConnectAddresses.Contains(PublicIPv6))
					{
						m_ConnectAddresses.Add(PublicIPv6);
						NetworkHelper.EditorSaveObject(gameObject);
					}
				}

				// Update the player preference with the current timestamp.
				PlayerPrefs.SetString("IPLastReceiveDate", DateTime.Now.ToString());
			}
			else
			{
#if OMNI_DEBUG
				timeLeft = TimeSpan.FromMinutes(minutes) - timeLeft;
				NetworkLogger.Log(
					$"You should wait {minutes} minutes before you can get the external IP again. Go to the context menu and click \"Force Get Public IP\" to force it. Remaining time: {timeLeft.Minutes:0} minutes and {timeLeft.Seconds} seconds.",
					logType: NetworkLogger.LogType.Warning
				);
#endif
			}
		}

		private void DisableAutoStartIfHasHud()
		{
#if OMNI_DEBUG
			if (TryGetComponent<NetworkConnectionDisplay>(out _))
			{
				m_AutoStartClient = false;
				m_AutoStartServer = false;
				NetworkHelper.EditorSaveObject(gameObject);
			}
#endif
		}
	}
}
