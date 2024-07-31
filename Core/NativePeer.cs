using System;

namespace Omni.Core
{
    public sealed class NativePeer
    {
        private readonly Func<double> _onTime;
        private readonly Func<int> _onPing;

        internal double Time => _onTime();
        internal int Ping => _onPing();

        internal NativePeer(Func<double> onTime, Func<int> onPing)
        {
            _onTime = onTime;
            _onPing = onPing;
        }
    }
}
