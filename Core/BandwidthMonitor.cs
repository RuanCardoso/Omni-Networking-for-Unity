using System;
using Omni.Shared;
using Omni.Threading.Tasks;
using UnityEngine;

public sealed class BandwidthMonitor
{
    private readonly IMovingAverage _movingAverage;
    private double _total;
    public event Action<double> OnAverageChanged;

    public BandwidthMonitor()
        : this(new SimpleMovingAverage(), 5) { } // SMA -> because we want to get a stable average over time.

    public BandwidthMonitor(IMovingAverage movingAverage, int windowSize)
    {
        _movingAverage = movingAverage;
        _movingAverage.SetPeriods(windowSize);
    }

    public async void Start(float seconds = 1f, int decimalPlaces = 0)
    {
        while (Application.isPlaying)
        {
            // wait for 1 second to best match the moving average
            // eg: 10 periods = 10 seconds of data.
            await UniTask.WaitForSeconds(seconds);

            _movingAverage.Add(_total);
            _total = 0;

            // Get the rounded average to aproximately value the bandwidth!
            OnAverageChanged?.Invoke(Math.Round(_movingAverage.Average, decimalPlaces));
        }
    }

    public void Add(double value)
    {
        _total += value;
    }
}
