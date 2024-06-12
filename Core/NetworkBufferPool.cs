using System.Collections.Generic;
using Omni.Core.Interfaces;
using Omni.Shared;

namespace Omni.Core
{
    public class NetworkBufferPool : IObjectPooling<DataBuffer>
    {
        private readonly Queue<DataBuffer> _pool;

        internal NetworkBufferPool()
        {
            _pool = new Queue<DataBuffer>();
            for (int i = 0; i < 10; i++)
            {
                _pool.Enqueue(new DataBuffer(pool: this));
            }
        }

        public DataBuffer Rent()
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
                    "Pool: A new network buffer was created. Consider increasing the initial capacity of the pool, recommended to reduce pressure on the garbage collector.",
                    NetworkLogger.LogType.Warning
                );

                return new DataBuffer(pool: this);
            }
        }

        public void Return(DataBuffer _buffer)
        {
            _buffer.ResetWrittenCount();
            _pool.Enqueue(_buffer);
        }
    }
}
