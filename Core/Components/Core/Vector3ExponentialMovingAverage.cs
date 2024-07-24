using Omni.Shared;
using UnityEngine;

public class Vector3MovingAverage<T> : MonoBehaviour
    where T : IMovingAverage, new()
{
    private readonly IMovingAverage x;
    private readonly IMovingAverage y;
    private readonly IMovingAverage z;

    public Vector3MovingAverage(int windowSize)
    {
        x = new T();
        y = new T();
        z = new T();

        x.SetPeriods(windowSize);
        y.SetPeriods(windowSize);
        z.SetPeriods(windowSize);
    }

    public void Add(Vector3 value)
    {
        x.Add(value.x);
        y.Add(value.y);
        z.Add(value.z);
    }

    public float GetAverageX()
    {
        return (float)x.Average;
    }

    public float GetAverageY()
    {
        return (float)y.Average;
    }

    public float GetAverageZ()
    {
        return (float)z.Average;
    }

    public Vector3 GetAverage()
    {
        return new Vector3((float)x.Average, (float)y.Average, (float)z.Average);
    }
}
