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

namespace Omni.Shared
{
    // N-day EMA implementation from Mirror with a few changes (struct etc.)
    // it calculates an exponential moving average roughly equivalent to the last n observations
    // https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average
    public class ExponentialMovingAverage : IMovingAverage
    {
        private bool initialized;
        private float alpha;

        private double avg;
        private double variance;
        private double standardDeviation;

        public double Average => avg;
        public double Variance => variance;
        public double StandardDeviation => standardDeviation;

        public ExponentialMovingAverage() { }

        public ExponentialMovingAverage(int periods)
        {
            SetPeriods(periods);
        }

        public void SetPeriods(int periods)
        {
            // standard N-day EMA alpha calculation
            alpha = 2.0f / (periods + 1f);
            variance = 0;
            standardDeviation = 0;
            initialized = false;
            avg = 0;
        }

        // simple algorithm for EMA described here:
        // https://en.wikipedia.org/wiki/Moving_average#Exponentially_weighted_moving_variance_and_standard_deviation
        public void Add(double value)
        {
            if (initialized)
            {
                double delta = value - avg;
                variance = (1 - alpha) * (variance + alpha * delta * delta);
                standardDeviation = Math.Sqrt(variance);
                avg += alpha * delta;
            }
            else
            {
                avg = value;
                initialized = true;
            }
        }
    }
}
