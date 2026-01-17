using UnityEngine;
using Omni.Inspector;
using Omni.Threading.Tasks;

#if UNITY_EDITOR
using ParrelSync;
#endif

#pragma warning disable

namespace Omni.Core
{
    [DisallowMultipleComponent]
    [DeclareBoxGroup("GUI Settings")]
    public sealed class NetworkConnectionDisplay : OmniBehaviour
    {
        private string host = "127.0.0.1";
        private string port = "7777";

        [SerializeField]
        [GroupNext("GUI Settings")]
        private float m_Width = 395;

        [SerializeField] private float m_Height = 240;
        [SerializeField] private int m_FontSize = 30;
        [SerializeField] private bool m_HideStopButton = false;

        private void Start()
        {
            host = NetworkManager.ConnectAddress;
            port = NetworkManager.Port.ToString();
#if UNITY_SERVER && !UNITY_EDITOR
			Destroy(this);
#endif
        }

#if OMNI_DEBUG
        private void OnGUI()
        {
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
            float widthScale = Screen.width / 1920f;
            float heightScale = Screen.height / 1080f;
            float scale = Mathf.Min(widthScale, heightScale);

            float scaledWidth = m_Width * scale;
            float scaledHeight = m_Height * scale;
            float scaledPadding = 10f * scale;
            int scaledFontSize = Mathf.RoundToInt(m_FontSize * scale);

            GUIStyle windowStyle = new(GUI.skin.box)
            {
                fontSize = scaledFontSize,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(Mathf.RoundToInt(12 * scale), 0, Mathf.RoundToInt(12 * scale), 0)
            };

            GUIStyle headerStyle = new(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(scaledFontSize * 1f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 1f) }
            };

            var buttonFontSize = new GUIStyle(GUI.skin.button) { fontSize = scaledFontSize };
            var labelFontSize = new GUIStyle(GUI.skin.label) { fontSize = scaledFontSize };
            var textFieldFontSize = new GUIStyle(GUI.skin.textField) { fontSize = scaledFontSize };

            var widthOption = GUILayout.Width(scaledWidth - (scaledPadding * 2));
            var heightOption = GUILayout.Height(scaledFontSize * 2);

            float centerX = (Screen.width - scaledWidth) / 2;
            float centerY = (Screen.height - (scaledHeight * 2)) / 2;

            Rect windowRect = new Rect(centerX, centerY, scaledWidth, scaledHeight * 2);

            float stopButtonWidth = scaledWidth * 0.7f;
            float stopButtonHeight = scaledFontSize * 2f;
            float buttonPadding = 24f * scale;
            float leftAreaX = buttonPadding;
            float leftAreaY = (Screen.height - stopButtonHeight * 1.2f) / 2f;
            float leftAreaWidth = stopButtonWidth;
            float leftAreaHeight = stopButtonHeight * 1.2f;

            bool showStopButton = !m_HideStopButton && (NetworkManager.IsClientActive || NetworkManager.IsServerActive);

            if (showStopButton)
            {
                GUILayout.BeginArea(new Rect(leftAreaX, leftAreaY, leftAreaWidth, leftAreaHeight));
                if (NetworkManager.IsHost)
                {
                    if (GUILayout.Button("Stop Host", buttonFontSize, GUILayout.Height(stopButtonHeight)))
                    {
                        UniTask.Void(async () =>
                        {
                            NetworkManager.StopClient();
                            await UniTask.WaitForSeconds(0.5f);
                            if (NetworkManager.IsServerActive)
                            {
                                NetworkManager.StopServer();
                            }
                        });
                    }
                }
                else
                {
                    if (NetworkManager.IsClientActive)
                    {
                        if (GUILayout.Button("Stop Client", buttonFontSize, GUILayout.Height(stopButtonHeight)))
                        {
                            NetworkManager.StopClient();
                        }
                    }
                    else if (NetworkManager.IsServerActive)
                    {
                        if (GUILayout.Button("Stop Server", buttonFontSize, GUILayout.Height(stopButtonHeight)))
                        {
                            NetworkManager.StopServer();
                        }
                    }
                }

                GUILayout.EndArea();
                return;
            }

            GUI.Box(windowRect, GUIContent.none, windowStyle);
            GUI.Box(new Rect(windowRect.x, windowRect.y, windowRect.width, scaledFontSize * 1.5f), "NETWORK CONNECTION", headerStyle);

            GUILayout.BeginArea(new Rect(windowRect.x + scaledPadding, windowRect.y + (scaledFontSize * 1.5f) + scaledPadding, windowRect.width - (scaledPadding * 2), windowRect.height - (scaledFontSize * 1.5f) - (scaledPadding * 2)));
            GUILayout.Label("Host:", labelFontSize);
            host = GUILayout.TextField(host, textFieldFontSize, widthOption, heightOption);
            GUILayout.Label("Port:", labelFontSize);
            port = GUILayout.TextField(port, textFieldFontSize, widthOption, heightOption);

            if (!int.TryParse(port, out int hostPort))
            {
                GUILayout.EndArea();
                return;
            }

            if (!isClone)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                if (!NetworkManager.IsServerActive)
                {
                    if (GUILayout.Button("Start Server", buttonFontSize, widthOption, heightOption))
                    {
                        NetworkManager.StartServer(hostPort);
                    }
                }
#endif
            }

            if (!NetworkManager.IsClientActive)
            {
                if (GUILayout.Button("Start Client", buttonFontSize, widthOption, heightOption))
                {
                    NetworkManager.Connect(host, hostPort);
                }
            }

            if (!isClone)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                if (GUILayout.Button("Start Host", buttonFontSize, widthOption, heightOption))
                {
                    NetworkManager.StartHost(host, hostPort);
                }
#endif
            }

            GUILayout.EndArea();
        }
#endif
    }
}
