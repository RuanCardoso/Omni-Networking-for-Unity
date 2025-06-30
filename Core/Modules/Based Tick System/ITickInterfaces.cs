namespace Omni.Core
{
    /// <summary>
    /// Represents information about a tick process, providing details necessary for managing and processing game or application ticks.
    /// </summary>
    public interface ITickData
    {
        /// <summary>
        /// Gets the current tick rate.
        /// </summary>
        /// <remarks>
        /// The tick rate represents the number of ticks or updates that occur per second.
        /// It defines how frequently the system updates or processes ticks.
        /// </remarks>
        public int TickRate { get; }

        /// <summary>
        /// Gets the total number of ticks that have elapsed since the start of the tick system.
        /// </summary>
        /// <remarks>
        /// This property represents the accumulated count of ticks since the tick system began running.
        /// Each tick corresponds to a fixed interval determined by the tick rate.
        /// It is useful in scenarios where tracking the total progression over time is necessary.
        /// </remarks>
        public long ElapsedTicks { get; }

        /// <summary>
        /// Gets the current tick count since the initialization or last reset. This value is incremented
        /// on each successful tick operation during the networked system's update loop. It resets after
        /// reaching the defined tick rate.
        /// </summary>
        public int CurrentTick { get; }

        /// <summary>
        /// Represents the fixed time step duration for each tick in seconds.
        /// This property is calculated as the reciprocal of the tick rate and determines
        /// the interval at which the system updates or processes tick-related events.
        /// </summary>
        public double FixedTimestep { get; }

        /// <summary>
        /// Represents the number of milliseconds that pass for each tick in the tick system.
        /// </summary>
        /// <remarks>
        /// This property is calculated based on the tick rate and is used to determine
        /// the duration of each tick interval in milliseconds. It is configured during
        /// the initialization of the tick system.
        /// </remarks>
        public double MsPerTick { get; }

        /// <summary>
        /// Represents the interval in seconds between the last tick and the current one.
        /// </summary>
        /// <remarks>
        /// This is a read-only property that captures the time difference since the last
        /// tick event within the network tick system.
        /// </remarks>
        public double DeltaTime { get; }
    }

    /// <summary>
    /// Defines a system that responds to periodic tick updates. This interface is crucial for managing timed operations
    /// within the application, ensuring that components implementing this interface can effectively handle updates
    /// provided by a tick source.
    /// </summary>
    public interface IBasedTickSystem
    {
        /// <summary>
        /// Called on each update tick.
        /// </summary>
        /// <param name="data">The data associated with the current tick.</param>
        /// <remarks>
        /// Override this method to perform per-tick processing during the object's active state.
        /// This method is called at regular intervals defined by the system's tick rate, making it suitable
        /// for time-sensitive logic such as physics updates or state synchronization.
        /// </remarks>
        void OnTick(ITickData data);
    }
}