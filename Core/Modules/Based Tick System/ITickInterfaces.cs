namespace Omni.Core
{
    public interface ITickInfo
    {
        public int TickRate { get; }

        public long ElapsedTicks { get; }
        public long CurrentTick { get; }

        public double FixedTimestep { get; }
        public double MsPerTick { get; }
        public double DeltaTime { get; }
        public double DeltaTick { get; }
    }

    public interface ITickSystem
    {
        void OnTick(ITickInfo data);
    }
}
