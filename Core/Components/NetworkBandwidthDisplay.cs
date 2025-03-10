using Omni.Core;
using Omni.Inspector;
using UnityEngine;

[DisallowMultipleComponent]
[DeclareBoxGroup("GUI Settings")]
#pragma warning disable
public sealed class NetworkBandwidthDisplay : MonoBehaviour
{
    #region GUI
    private const float k_Padding = 10f;
    private const float k_ReferenceScreenWidth = 1920f;
    private const float k_ReferenceScreenHeight = 1080f;
    private const float k_HeaderHeightMultiplier = 1.5f;
    private const float k_HeaderFontSizeMultiplier = 1.2f;
    
    private const int k_StylePadding = 12;
    private const int k_ContentLeftPadding = 10;
    private const int k_ContentRightPadding = 20;

    private const float k_LabelYOffsetAddition = 1f;
    private const float k_DoubleSpacingHeight = 2f; // For the double line spacing in content

    [GroupNext("GUI Settings")]
    [SerializeField]
    private float m_Width = 310f;
    [SerializeField] private float m_Height = 210f;
    [SerializeField] private int m_FontSize = 24;
    #endregion

    #region Client
    private double m_ClientOutgoingBandwidth, m_ClientIncomingBandwidth;
    private int m_ClientOutgoingPacketsPerSecond, m_ClientIncomingPacketsPerSecond;
    #endregion

    #region Server
    private double m_ServerOutgoingBandwidth, m_ServerIncomingBandwidth;
    private int m_ServerOutgoingPacketsPerSecond, m_ServerIncomingPacketsPerSecond;
    #endregion

    private void Start()
    {
        #region Client
        NetworkManager.ClientSide.SentBandwidth.OnAverageChanged += (avg, pps) =>
        {
            m_ClientOutgoingBandwidth = avg;
            m_ClientOutgoingPacketsPerSecond = pps;
        };

        NetworkManager.ClientSide.ReceivedBandwidth.OnAverageChanged += (avg, pps) =>
        {
            m_ClientIncomingBandwidth = avg;
            m_ClientIncomingPacketsPerSecond = pps;
        };
        #endregion

        #region Server
        NetworkManager.ServerSide.SentBandwidth.OnAverageChanged += (avg, pps) =>
        {
            m_ServerOutgoingBandwidth = avg;
            m_ServerOutgoingPacketsPerSecond = pps;
        };

        NetworkManager.ServerSide.ReceivedBandwidth.OnAverageChanged += (avg, pps) =>
        {
            m_ServerIncomingBandwidth = avg;
            m_ServerIncomingPacketsPerSecond = pps;
        };
        #endregion
    }

#if OMNI_DEBUG
    private void OnGUI()
    {
        float widthScale = Screen.width / k_ReferenceScreenWidth;
        float heightScale = Screen.height / k_ReferenceScreenHeight;
        float scale = Mathf.Min(widthScale, heightScale);

        float scaledWidth = m_Width * scale;
        float scaledHeight = m_Height * scale;
        float scaledPadding = k_Padding * scale;
        int scaledFontSize = Mathf.RoundToInt(m_FontSize * scale);

        GUIStyle windowStyle = new(GUI.skin.box)
        {
            fontSize = scaledFontSize,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(Mathf.RoundToInt(k_StylePadding * scale), 0, Mathf.RoundToInt(k_StylePadding * scale), 0)
        };

        GUIStyle headerStyle = new(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(scaledFontSize * k_HeaderFontSizeMultiplier),
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

        string outgoingColorTag = "<color=#8aff8a>";
        string incomingColorTa = "<color=#8ae1ff>";

        if (NetworkManager.IsClientActive)
        {
            Rect windowRect = new(Screen.width - scaledWidth - scaledPadding, scaledPadding, scaledWidth, scaledHeight);

            GUI.BeginGroup(windowRect);
            GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), "", windowStyle);
            GUI.Box(new Rect(0, 0, windowRect.width, scaledFontSize * k_HeaderHeightMultiplier), "CLIENT", headerStyle);

            string clientContent = $"Sent: {outgoingColorTag}{m_ClientOutgoingBandwidth.ToSizeSuffix()}</color>\n" +
                                  $"Received: {incomingColorTa}{m_ClientIncomingBandwidth.ToSizeSuffix()}</color>\n\n" +
                                  $"Packets Sent: {outgoingColorTag}{m_ClientOutgoingPacketsPerSecond} p/s</color>\n" +
                                  $"Packets Received: {incomingColorTa}{m_ClientIncomingPacketsPerSecond} p/s</color>";

            float labelYOffset = scaledFontSize + k_LabelYOffsetAddition;
            GUI.Label(new Rect(k_ContentLeftPadding, scaledFontSize + labelYOffset, windowRect.width - k_ContentRightPadding, windowRect.height - scaledFontSize * k_HeaderHeightMultiplier - labelYOffset),
                      clientContent, contentStyle);

            GUI.EndGroup();
        }

        if (NetworkManager.IsServerActive)
        {
            float yPos = NetworkManager.IsClientActive ? scaledHeight + scaledPadding * k_DoubleSpacingHeight : scaledPadding;
            Rect windowRect = new(Screen.width - scaledWidth - scaledPadding, yPos, scaledWidth, scaledHeight);

            GUI.BeginGroup(windowRect);
            GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), "", windowStyle);
            GUI.Box(new Rect(0, 0, windowRect.width, scaledFontSize * k_HeaderHeightMultiplier), "SERVER", headerStyle);

            string serverContent = $"Sent: {outgoingColorTag}{m_ServerOutgoingBandwidth.ToSizeSuffix()}</color>\n" +
                                  $"Received: {incomingColorTa}{m_ServerIncomingBandwidth.ToSizeSuffix()}</color>\n\n" +
                                  $"Packets Sent: {outgoingColorTag}{m_ServerOutgoingPacketsPerSecond} p/s</color>\n" +
                                  $"Packets Received: {incomingColorTa}{m_ServerIncomingPacketsPerSecond} p/s</color>";

            float labelYOffset = scaledFontSize + k_LabelYOffsetAddition;
            GUI.Label(new Rect(k_ContentLeftPadding, scaledFontSize + labelYOffset, windowRect.width - k_ContentRightPadding, windowRect.height - scaledFontSize * k_HeaderHeightMultiplier - labelYOffset),
                      serverContent, contentStyle);

            GUI.EndGroup();
        }
    }
#endif
}