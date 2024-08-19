using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    [DisallowMultipleComponent]
    public sealed class NetworkConnectionDisplay : MonoBehaviour
    {
        private string host = "127.0.0.1";
        private string port = "7777";

        [SerializeField]
        private float m_Width = 200;

        [SerializeField]
        private float m_Height = 35;

        [SerializeField]
        private int m_FontSize = 20;

        private void Start()
        {
            host = NetworkManager.ConnectAddress;
            port = NetworkManager.ConnectPort.ToString();
        }

#if OMNI_DEBUG
        void OnGUI()
        {
            if (NetworkManager.IsClientActive || NetworkManager.IsServerActive)
                return;

            var width = GUILayout.Width(m_Width);
            var height = GUILayout.Height(m_Height);

            var buttonFontSize = new GUIStyle(GUI.skin.button) { fontSize = m_FontSize };
            var labelFontSize = new GUIStyle(GUI.skin.label) { fontSize = m_FontSize };
            var textFieldFontSize = new GUIStyle(GUI.skin.textField) { fontSize = m_FontSize };

            GUILayout.BeginArea(new Rect(10, 10, Screen.width, Screen.height));

            GUILayout.Label("Host:", labelFontSize);
            host = GUILayout.TextField(host, textFieldFontSize, width, height);
            GUILayout.Label("Port:", labelFontSize);
            port = GUILayout.TextField(port, textFieldFontSize, width, height);

            if (!int.TryParse(port, out int hostPort))
            {
                GUILayout.EndArea();
                return;
            }

            if (GUILayout.Button("Start Server", buttonFontSize, width, height))
            {
                NetworkManager.StartServer(hostPort);
            }

            if (GUILayout.Button("Start Client", buttonFontSize, width, height))
            {
                NetworkManager.Connect(host, hostPort);
            }

            if (GUILayout.Button("Start Server & Client", buttonFontSize, width, height))
            {
                NetworkManager.StartServer(hostPort);
                NetworkManager.Connect(host, hostPort);
            }

            GUILayout.EndArea();
        }
#endif
    }
}
