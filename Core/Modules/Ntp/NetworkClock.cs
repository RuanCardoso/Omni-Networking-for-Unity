using TriInspector;
using UnityEngine;

namespace Omni.Core.Modules.Ntp
{
    [DeclareBoxGroup("Samples")]
    [DeclareBoxGroup("Others")]
    public class NetworkClock : MonoBehaviour
    {
        internal const int DEFAULT_TIME_WINDOW = 60;
        internal const int DEFAULT_RTT_WINDOW = 25;
        internal const int DEFAULT_ACCURACY = 5;
        internal const float DEFAULT_QUERY_INTERVAL = 1f;

        [SerializeField] [Group("Samples")] [Min(1)]
        private int m_TimeWindow = DEFAULT_TIME_WINDOW;

        [SerializeField] [Group("Samples")] [Min(1)]
        private int m_RttWindow = DEFAULT_RTT_WINDOW;

        [SerializeField] [Group("Others")] [Range(1, 1000)]
        private int m_Accuracy = DEFAULT_ACCURACY;

        [SerializeField] [LabelText("Query Interval(s)")] [Group("Others")] [Range(1f, 900f)]
        private float m_QueryInterval = DEFAULT_QUERY_INTERVAL;

        [SerializeField] [Group("Others")] private bool m_UseTickTiming = false;

        internal float QueryInterval => m_QueryInterval;
        internal int TimeWindow => m_TimeWindow;
        internal int RttWindow => m_RttWindow;
        internal int Accuracy => m_Accuracy;
        internal bool UseTickTiming => m_UseTickTiming;
    }
}