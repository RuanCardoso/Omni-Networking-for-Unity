using Omni.Core;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NetworkBandwidthDisplay : MonoBehaviour
{
    private const float m_Padding = 10f;

    // Client
    private double clientSentBandwidth;
    private double clientReceivedBandwidth;

    // Server
    private double serverSentBandwidth;
    private double serverReceivedBandwidth;

    // GUI
    [SerializeField]
    private float m_Width = 300f;

    [SerializeField]
    private float m_Height = 130f;

    [SerializeField]
    private int m_FontSize = 24;

    private void Start()
    {
        // Client
        NetworkManager.Client.SentBandwidth.OnAverageChanged += (avg) => clientSentBandwidth = avg;
        NetworkManager.Client.ReceivedBandwidth.OnAverageChanged += (avg) =>
            clientReceivedBandwidth = avg;

        // Server
        NetworkManager.Server.SentBandwidth.OnAverageChanged += (avg) => serverSentBandwidth = avg;
        NetworkManager.Server.ReceivedBandwidth.OnAverageChanged += (avg) =>
            serverReceivedBandwidth = avg;
    }

    private void OnGUI()
    {
#if OMNI_DEBUG
        GUIStyle style =
            new(GUI.skin.box)
            {
                fontSize = m_FontSize,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 0, 10, 0)
            };

        if (NetworkManager.IsClientActive)
        {
            GUI.Box(
                new(Screen.width - m_Width - m_Padding, m_Padding, m_Width, m_Height),
                $"<b>Local</b>\r\n\r\nSent: {clientSentBandwidth.ToSizeSuffix()}\r\nReceived: {clientReceivedBandwidth.ToSizeSuffix()}",
                style
            );
        }

        if (NetworkManager.IsServerActive)
        {
            GUI.Box(
                new(
                    Screen.width - m_Width - m_Padding,
                    NetworkManager.IsClientActive ? m_Height + m_Padding * 2 : m_Padding,
                    m_Width,
                    m_Height
                ),
                $"<b>Server</b>\r\n\r\nSent: {serverSentBandwidth.ToSizeSuffix()}\r\nReceived: {serverReceivedBandwidth.ToSizeSuffix()}",
                style
            );
        }
#endif
    }
}
