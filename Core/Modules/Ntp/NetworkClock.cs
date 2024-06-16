using Omni.Core.Attributes;
using UnityEngine;

namespace Omni.Core.Modules.Ntp
{
    public class NetworkClock : MonoBehaviour
    {
        internal const int DEFAULT_TIME_WINDOW = 60;
        internal const int DEFAULT_RTT_WINDOW = 25;
        internal const int DEFAULT_ACCURACY = 5;
        internal const float DEFAULT_QUERY_INTERVAL = 1f;

        [Header("Samples")]
        [SerializeField]
        [Min(1)]
        private int m_TimeWindow = DEFAULT_TIME_WINDOW;

        [SerializeField]
        [Min(1)]
        private int m_RttWindow = DEFAULT_RTT_WINDOW;

        [Header("Others")]
        [SerializeField]
        [Range(1, 1000)]
        private int m_Accuracy = DEFAULT_ACCURACY;

        [SerializeField]
        [Label("Query Interval(s)")]
        [Range(1f, 900f)]
        private float m_QueryInterval = DEFAULT_QUERY_INTERVAL;

        [SerializeField]
        private bool m_UseTickTiming = false;

        internal float QueryInterval => m_QueryInterval;
        internal int TimeWindow => m_TimeWindow;
        internal int RttWindow => m_RttWindow;
        internal int Accuracy => m_Accuracy;
        internal bool UseTickTiming => m_UseTickTiming;
    }
}
