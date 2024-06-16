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
    public class NetworkTickSystem : ITickInfo
    {
        private readonly List<ITickSystem> handlers = new();

        private double elapsedDeltaTime;
        private double lastElapsedDeltaTime;

        public long CurrentTick { get; private set; }
        public long ElapsedTicks { get; private set; }

        public double DeltaTime { get; private set; }
        public double DeltaTick { get; private set; }

        public int TickRate { get; private set; }
        public double MsPerTick { get; private set; }
        public double FixedTimestep { get; private set; }

        internal void Initialize(int tickRate)
        {
            TickRate = tickRate;
            FixedTimestep = 1.0d / tickRate;
            MsPerTick = 1000d / TickRate;
        }

        public void Register(ITickSystem handler)
        {
#if OMNI_DEBUG || UNITY_EDITOR
            if (handlers.Contains(handler))
                return;
#endif
            handlers.Add(handler);
        }

        public void Unregister(ITickSystem handler)
        {
            handlers.Remove(handler);
        }

        internal void OnTick()
        {
            DeltaTime += Time.deltaTime;
            elapsedDeltaTime += Time.deltaTime;
            while (DeltaTime >= FixedTimestep)
            {
                // The interval in seconds from the last tick to the current one (Read Only).
                DeltaTick = elapsedDeltaTime - lastElapsedDeltaTime;
                lastElapsedDeltaTime = elapsedDeltaTime;

                // Add tick per frame(1 / tickrate)
                CurrentTick++;
                ElapsedTicks++;

                // Tick-tack..tick-tack..tick-tack..
                for (int i = 0; i < handlers.Count; i++)
                {
                    ITickSystem handler = handlers[i];
                    handler.OnTick(this);
                }

                if (CurrentTick == TickRate)
                {
                    CurrentTick -= TickRate;
                }

                DeltaTime -= FixedTimestep;
            }
        }
    }
}
