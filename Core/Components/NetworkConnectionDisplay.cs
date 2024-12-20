using UnityEngine;
using TriInspector;
using Omni.Threading.Tasks;

#if UNITY_EDITOR
using ParrelSync;
#endif

#pragma warning disable

namespace Omni.Core
{
    [DisallowMultipleComponent]
    [DeclareBoxGroup("GUI Settings")]
    public sealed class NetworkConnectionDisplay : MonoBehaviour
    {
        private string host = "127.0.0.1";
        private string port = "7777";

        [SerializeField] [GroupNext("GUI Settings")]
        private float m_Width = 200;

        [SerializeField] private float m_Height = 35;

        [SerializeField] private int m_FontSize = 20;

        private void Start()
        {
            host = NetworkManager.ConnectAddress;
            port = NetworkManager.ConnectPort.ToString();
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
#endif
            var width = GUILayout.Width(m_Width);
            var height = GUILayout.Height(m_Height);

            var buttonFontSize = new GUIStyle(GUI.skin.button) { fontSize = m_FontSize };
            var labelFontSize = new GUIStyle(GUI.skin.label) { fontSize = m_FontSize };
            var textFieldFontSize = new GUIStyle(GUI.skin.textField) { fontSize = m_FontSize };

            float centerX = (Screen.width - m_Width) / 2;
            float centerY = (Screen.height - (m_Height * 8)) / 2;

            if (NetworkManager.IsClientActive || NetworkManager.IsServerActive)
            {
                GUILayout.BeginArea(new Rect(10, centerY, m_Width, m_Height * 8));
                if (NetworkManager.IsClientActive && NetworkManager.IsServerActive)
                {
                    if (GUILayout.Button("Stop Client & Server", buttonFontSize, width, height))
                    {
                        UniTask.Void(async () =>
                        {
                            NetworkManager.StopClient();
                            await UniTask.WaitForSeconds(0.5f);
                            NetworkManager.StopServer();
                        });
                    }
                }
                else
                {
                    if (NetworkManager.IsClientActive)
                    {
                        if (GUILayout.Button("Stop Client", buttonFontSize, width, height))
                        {
                            NetworkManager.StopClient();
                        }
                    }
                    else if (NetworkManager.IsServerActive)
                    {
                        if (GUILayout.Button("Stop Server", buttonFontSize, width, height))
                        {
                            NetworkManager.StopServer();
                        }
                    }
                }

                GUILayout.EndArea();
                return;
            }

            GUILayout.BeginArea(new Rect(centerX, centerY, m_Width, m_Height * 8));
            GUILayout.Label("Host:", labelFontSize);
            host = GUILayout.TextField(host, textFieldFontSize, width, height);
            GUILayout.Label("Port:", labelFontSize);
            port = GUILayout.TextField(port, textFieldFontSize, width, height);

            if (!int.TryParse(port, out int hostPort))
            {
                GUILayout.EndArea();
                return;
            }

            if (!isClone)
            {
                // WebGL can't start server.
#if !UNITY_WEBGL || UNITY_EDITOR
                if (!NetworkManager.IsServerActive)
                {
                    if (GUILayout.Button("Start Server", buttonFontSize, width, height))
                    {
                        NetworkManager.StartServer(hostPort);
                    }
                }
#endif
            }

            if (!NetworkManager.IsClientActive)
            {
                if (GUILayout.Button("Start Client", buttonFontSize, width, height))
                {
                    NetworkManager.Connect(host, hostPort);
                }
            }

            if (!isClone)
            {
                // WebGL can't start server.
#if !UNITY_WEBGL || UNITY_EDITOR
                if (GUILayout.Button("Start Server & Client", buttonFontSize, width, height))
                {
                    UniTask.Void(async () =>
                    {
                        NetworkManager.StartServer(hostPort);
                        await UniTask.WaitForSeconds(0.1f);
                        NetworkManager.Connect(host, hostPort);
                    });
                }
#endif
            }

            GUILayout.EndArea();
        }
#endif
    }
}