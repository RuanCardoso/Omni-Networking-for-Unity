using System.Collections.Generic;
using Omni.Core.Interfaces;
using Omni.Shared;

namespace Omni.Core
{
    public class NetworkBufferPool : IObjectPooling<NetworkBuffer>
    {
        private readonly Queue<NetworkBuffer> _pool;

        internal NetworkBufferPool()
        {
            _pool = new Queue<NetworkBuffer>();
            for (int i = 0; i < 10; i++)
            {
                _pool.Enqueue(new NetworkBuffer(pool: this));
            }
        }

        public NetworkBuffer Rent()
        {
            if (_pool.Count > 0)
            {
                var buffer = _pool.Dequeue();
                buffer._disposed = false;
                return buffer;
            }
            else
            {
                NetworkLogger.__Log__(
                    "Pool: A new network buffer was created. Consider increasing the initial capacity of the pool.",
                    NetworkLogger.LogType.Warning
                );

                return new NetworkBuffer(pool: this);
            }
        }

        public void Return(NetworkBuffer _buffer)
        {
            _buffer.ResetWrittenCount();
            _pool.Enqueue(_buffer);
        }
    }
}
