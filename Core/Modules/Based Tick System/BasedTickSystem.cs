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
using UnityEngine;

namespace Omni.Core
{
    [DefaultExecutionOrder(-5000)]
    public class BasedTickSystem : ITickData
    {
        private readonly List<IBasedTickSystem> handlers = new();
        private double deltaTimestep;
        private double elapsedDeltaTime;
        private double lastElapsedDeltaTime;

        /// <inheritdoc />
        public int TickRate { get; private set; }
        /// <inheritdoc />
        public int CurrentTick { get; private set; }
        /// <inheritdoc />
        public long ElapsedTicks { get; private set; }
        /// <inheritdoc />
        public double DeltaTime { get; private set; }
        /// <inheritdoc />
        public double MsPerTick { get; private set; }
        /// <inheritdoc />
        public double FixedTimestep { get; private set; }

        internal void Initialize(int tickRate)
        {
            TickRate = tickRate;
            FixedTimestep = 1.0d / tickRate;
            MsPerTick = 1000d / TickRate;
        }

        /// <summary>
        /// Registers a handler implementing the ITickSystem interface to the NetworkTickSystem, allowing it to receive periodic tick updates.
        /// </summary>
        /// <param name="handler">The handler implementing ITickSystem to be registered for tick updates.</param>
        public void Register(IBasedTickSystem handler)
        {
            if (handlers.Contains(handler))
            {
                handlers.Remove(handler);
            }

            handlers.Add(handler);
        }

        /// <summary>
        /// Unregisters a handler that implements the ITickSystem interface from the NetworkTickSystem,
        /// preventing it from receiving further periodic tick updates.
        /// </summary>
        /// <param name="handler">The handler implementing ITickSystem to be unregistered from tick updates.</param>
        public void Unregister(IBasedTickSystem handler)
        {
            handlers.Remove(handler);
        }

        internal void UpdateTick()
        {
            if (!NetworkManager.IsServerActive && !NetworkManager.IsClientActive)
                return;

            deltaTimestep += Time.deltaTime;
            elapsedDeltaTime += Time.deltaTime;
            while (deltaTimestep >= FixedTimestep)
            {
                // The interval in seconds from the last tick to the current one (Read Only).
                DeltaTime = elapsedDeltaTime - lastElapsedDeltaTime;
                lastElapsedDeltaTime = elapsedDeltaTime;

                // Add tick per frame(1 / tickrate)
                CurrentTick++;
                ElapsedTicks++;

                // Tick-tack..tick-tack..tick-tack..
                foreach (var handler in handlers)
                    handler.OnTick(this);

                if (CurrentTick == TickRate)
                    CurrentTick -= TickRate;

                deltaTimestep -= FixedTimestep;
            }
        }
    }
}