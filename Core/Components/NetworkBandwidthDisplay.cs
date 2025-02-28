using Omni.Core;
using Omni.Inspector;
using UnityEngine;

[DisallowMultipleComponent]
[DeclareBoxGroup("GUI Settings")]
#pragma warning disable
public sealed class NetworkBandwidthDisplay : MonoBehaviour
{
    private const float m_Padding = 10f;
    private const float m_ReferenceScreenWidth = 1920f;
    private const float m_ReferenceScreenHeight = 1080f;

    // Client
    private double clientSentBandwidth;
    private double clientReceivedBandwidth;
    private int clientSentPackets;
    private int clientReceivedPackets;

    // Server
    private double serverSentBandwidth;
    private double serverReceivedBandwidth;
    private int serverSentPackets;
    private int serverReceivedPackets;

    // GUI
    [SerializeField]
    [GroupNext("GUI Settings")]
    private float m_Width = 310f;

    [SerializeField] private float m_Height = 220f;

    [SerializeField] private int m_FontSize = 24;

    private void Start()
    {
        // Client
        NetworkManager.ClientSide.SentBandwidth.OnAverageChanged += (avg, pps) =>
        {
            clientSentBandwidth = avg;
            clientSentPackets = pps;
        };

        NetworkManager.ClientSide.ReceivedBandwidth.OnAverageChanged += (avg, pps) =>
        {
            clientReceivedBandwidth = avg;
            clientReceivedPackets = pps;
        };

        // Server
        NetworkManager.ServerSide.SentBandwidth.OnAverageChanged += (avg, pps) =>
        {
            serverSentBandwidth = avg;
            serverSentPackets = pps;
        };

        NetworkManager.ServerSide.ReceivedBandwidth.OnAverageChanged += (avg, pps) =>
        {
            serverReceivedBandwidth = avg;
            serverReceivedPackets = pps;
        };
    }

#if OMNI_DEBUG
    private void OnGUI()
    {
        float widthScale = Screen.width / m_ReferenceScreenWidth;
        float heightScale = Screen.height / m_ReferenceScreenHeight;
        float scale = Mathf.Min(widthScale, heightScale);

        float scaledWidth = m_Width * scale;
        float scaledHeight = m_Height * scale;
        float scaledPadding = m_Padding * scale;
        int scaledFontSize = Mathf.RoundToInt(m_FontSize * scale);

        GUIStyle style = new(GUI.skin.box)
        {
            fontSize = scaledFontSize,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(Mathf.RoundToInt(12 * scale), 0, Mathf.RoundToInt(12 * scale), 0)
        };

        if (NetworkManager.IsClientActive)
        {
            GUI.Box(new(Screen.width - scaledWidth - scaledPadding, scaledPadding, scaledWidth, scaledHeight),
                $"<b>Local</b>\r\n\r\nSent: {clientSentBandwidth.ToSizeSuffix()}\r\nReceived: {clientReceivedBandwidth.ToSizeSuffix()}\n\nPackets Sent: {clientSentPackets} p/s\nPackets Received: {clientReceivedPackets} p/s",
                style);
        }

        if (NetworkManager.IsServerActive)
        {
            GUI.Box(
                new(Screen.width - scaledWidth - scaledPadding,
                    NetworkManager.IsClientActive ? scaledHeight + scaledPadding * 2 : scaledPadding, scaledWidth, scaledHeight),
                $"<b>Server</b>\r\n\r\nSent: {serverSentBandwidth.ToSizeSuffix()}\r\nReceived: {serverReceivedBandwidth.ToSizeSuffix()}\n\nPackets Sent: {serverSentPackets} p/s\nPackets Received: {serverReceivedPackets} p/s",
                style);
        }
    }
#endif
}