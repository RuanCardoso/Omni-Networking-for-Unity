using Omni.Shared;
using Omni.Threading.Tasks;
using System;
using UnityEngine;

/// <summary>
/// Monitors bandwidth by measuring and calculating averages for data usage over time.
/// </summary>
public sealed class BandwidthMonitor
{
    private const int windowSize = 5;

    private readonly IMovingAverage _bytesMeasurementMovingAverage;
    private readonly IMovingAverage _ppsMeasurementMovingAverage;

    private double _bytesMeasurementTotal;
    private int _ppsMeasurementTotal;

    /// <summary>
    /// Event triggered when the moving average of bandwidth usage changes.
    /// This event is invoked periodically with updated average bandwidth
    /// and packets-per-second (PPS) values.
    /// </summary>
    /// <remarks>
    /// The event provides two parameters:
    /// - The first parameter is the average bandwidth in bytes per second (rounded to the nearest whole number).
    /// - The second parameter is the average packets-per-second value (rounded to the nearest whole number).
    /// </remarks>
    public event Action<double, int> OnAverageChanged;

    internal BandwidthMonitor()
    {
        // Bytes Per Second
        _bytesMeasurementMovingAverage = new SimpleMovingAverage();
        _bytesMeasurementMovingAverage.SetPeriods(windowSize);

        // Packets Per Second
        _ppsMeasurementMovingAverage = new SimpleMovingAverage();
        _ppsMeasurementMovingAverage.SetPeriods(windowSize);
    }

    internal async void Start()
    {
        while (Application.isPlaying)
        {
            // Wait for 1 second to align the moving average calculations 
            // with the data collection period (e.g., 10 periods = 10 seconds of data).
            await UniTask.WaitForSeconds(1f);

            // Add the current measurements to the moving average calculations.
            // These measurements are accumulated over the 1-second interval.
            _bytesMeasurementMovingAverage.Add(_bytesMeasurementTotal);
            _ppsMeasurementMovingAverage.Add(_ppsMeasurementTotal);

            // Reset the total measurements for the next interval.
            _bytesMeasurementTotal = 0;
            _ppsMeasurementTotal = 0;

            // Invoke the callback with the rounded averages to estimate bandwidth usage.
            OnAverageChanged?.Invoke(Math.Round(_bytesMeasurementMovingAverage.Average, 0),
                (int)Math.Round(_ppsMeasurementMovingAverage.Average, 0));
        }
    }

    internal void Add(double value)
    {
        _bytesMeasurementTotal += value;
        _ppsMeasurementTotal++;
    }
}