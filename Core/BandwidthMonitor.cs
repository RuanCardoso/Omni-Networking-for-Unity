using Omni.Core;
using Omni.Shared;
using Omni.Threading.Tasks;
using System;
using UnityEngine;

/// <summary>
/// Monitors bandwidth by measuring and calculating averages for data usage over time.
/// </summary>
public sealed class BandwidthMonitor
{
    private const int MovingAveragePeriod = 5;

    private readonly IMovingAverage _bytesMeasurementMovingAverage;
    private readonly IMovingAverage _ppsMeasurementMovingAverage;

    private double _accumulatedBytes;
    private int _accumulatedPps;

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
        _bytesMeasurementMovingAverage = new SimpleMovingAverage();
        _bytesMeasurementMovingAverage.SetPeriods(MovingAveragePeriod);

        _ppsMeasurementMovingAverage = new SimpleMovingAverage();
        _ppsMeasurementMovingAverage.SetPeriods(MovingAveragePeriod);
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
            _bytesMeasurementMovingAverage.Add(_accumulatedBytes);
            _ppsMeasurementMovingAverage.Add(_accumulatedPps);

            // Reset the total measurements for the next interval.
            _accumulatedBytes = _accumulatedPps = 0;

            // Invoke the callback with the truncated averages to estimate bandwidth usage.
            OnAverageChanged?.Invoke((int)Math.Round(_bytesMeasurementMovingAverage.Average), (int)Math.Round(_ppsMeasurementMovingAverage.Average));
        }
    }

    internal void Add(double value)
    {
        _accumulatedPps++;
        _accumulatedBytes += value + GetTransporterHeaderSize();
    }

    private int GetTransporterHeaderSize()
    {
        if (NetworkManager.BandwidthPayloadOnly)
            return 0;

        switch (NetworkManager.UnderlyingTransporter)
        {
            case Transporter.Lite:
                NetworkLogger.__Log__("Lite Transporter is not supported. Comming soon.", NetworkLogger.LogType.Warning);
                break;
            case Transporter.Kcp:
                NetworkLogger.__Log__("Kcp Transporter is not supported. Comming soon.", NetworkLogger.LogType.Warning);
                break;
            case Transporter.Web:
                NetworkLogger.__Log__("Web Transporter is not supported. Comming soon.", NetworkLogger.LogType.Warning);
                break;
        }

        return 0;
    }
}