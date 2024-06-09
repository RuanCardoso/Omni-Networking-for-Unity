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

using System;
using System.Collections.Generic;

namespace Omni.Shared
{
	public class SimpleMovingAverage
	{
		private readonly Queue<double> m_queue = new Queue<double>();
		private readonly int windowSize;
		private double sum;

		public SimpleMovingAverage(int windowSize)
		{
			this.windowSize = windowSize;
		}

		public double Add(double value)
		{
			m_queue.Enqueue(value);
			sum += value;

			if (m_queue.Count > windowSize)
			{
				sum -= m_queue.Dequeue();
			}

			return GetAverage();
		}

		public double AddRounded(double value, int digits = 0)
		{
			return Add(Math.Round(value, digits));
		}

		public double GetAverage()
		{
			if (m_queue.Count == 0)
			{
				return 0.0;
			}

			return sum / m_queue.Count;
		}

		public double GetRoundedAverage(int digits = 0)
		{
			if (m_queue.Count == 0)
			{
				return 0.0;
			}

			double avg = sum / m_queue.Count;
			return Math.Round(avg, digits);
		}
	}
}