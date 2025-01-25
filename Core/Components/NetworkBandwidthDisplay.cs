using Omni.Core;
using Omni.Inspector;
using UnityEngine;

[DisallowMultipleComponent]
[DeclareBoxGroup("GUI Settings")]
#pragma warning disable
public sealed class NetworkBandwidthDisplay : MonoBehaviour
{
    private const float m_Padding = 10f;

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
    [SerializeField] [GroupNext("GUI Settings")]
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
        GUIStyle style = new(GUI.skin.box)
            { fontSize = m_FontSize, alignment = TextAnchor.UpperLeft, padding = new RectOffset(12, 0, 12, 0) };

        if (NetworkManager.IsClientActive)
        {
            GUI.Box(new(Screen.width - m_Width - m_Padding, m_Padding, m_Width, m_Height),
                $"<b>Local</b>\r\n\r\nSent: {clientSentBandwidth.ToSizeSuffix()}\r\nReceived: {clientReceivedBandwidth.ToSizeSuffix()}\n\nPackets Sent: {clientSentPackets} p/s\nPackets Received: {clientReceivedPackets} p/s",
                style);
        }

        if (NetworkManager.IsServerActive)
        {
            GUI.Box(
                new(Screen.width - m_Width - m_Padding,
                    NetworkManager.IsClientActive ? m_Height + m_Padding * 2 : m_Padding, m_Width, m_Height),
                $"<b>Server</b>\r\n\r\nSent: {serverSentBandwidth.ToSizeSuffix()}\r\nReceived: {serverReceivedBandwidth.ToSizeSuffix()}\n\nPackets Sent: {serverSentPackets} p/s\nPackets Received: {serverReceivedPackets} p/s",
                style);
        }
    }
#endif
}