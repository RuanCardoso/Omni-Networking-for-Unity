/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System.Collections.Generic;

namespace Omni.Shared
{
    public class SimpleMovingAverage : IMovingAverage
    {
        private readonly Queue<double> m_queue = new();
        private int windowSize;
        private double sum;

        public double Average
        {
            get
            {
                if (m_queue.Count == 0)
                {
                    return 0.0;
                }

                return sum / m_queue.Count;
            }
        }

        public SimpleMovingAverage() { }

        public SimpleMovingAverage(int windowSize)
        {
            SetPeriods(windowSize);
        }

        public void SetPeriods(int periods)
        {
            windowSize = periods;
        }

        public void Add(double value)
        {
            m_queue.Enqueue(value);
            sum += value;

            if (m_queue.Count > windowSize)
            {
                sum -= m_queue.Dequeue();
            }
        }
    }
}
