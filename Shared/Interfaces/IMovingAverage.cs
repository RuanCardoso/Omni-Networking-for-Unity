namespace Omni.Shared
{
    public interface IMovingAverage
    {
        void Add(double value);
        void SetPeriods(int periods);
        double Average { get; }
    }
}
