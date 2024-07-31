using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Omni.Core.Interfaces;
using Omni.Shared;
using Omni.Threading.Tasks;

namespace Omni.Core
{
    public sealed class DataBufferPool : IObjectPooling<DataBuffer>
    {
        /// The maximum time in milliseconds that a buffer is being tracked before it is considered
        /// to have not been disposed or returned to the pool. <c>Debug mode only.</c>
        /// 500ms seems good to me, if there is an expensive operation that takes more than 500ms, it is recommended to call SupressTracking.
        private const int MAX_TRACKING_TIME = 500;
        private readonly Queue<DataBuffer> _pool;

        internal DataBufferPool(int capacity = 32768, int poolSize = 32)
        {
            _pool = new Queue<DataBuffer>();
            for (int i = 0; i < poolSize; i++)
            {
                _pool.Enqueue(new DataBuffer(capacity, pool: this));
            }
        }

        /// <summary>
        /// Rents a buffer from the pool. This operation may be slow in <c>Debug mode</c> due to additional diagnostic checks.
        /// In <c>Debug mode</c>, this method performs additional tracking to ensure the buffer is properly disposed of and returned to the pool.
        /// </summary>
        /// <returns>A <see cref="DataBuffer"/> object from the pool.</returns>
        public DataBuffer Rent()
        {
            if (_pool.Count > 0)
            {
                var buffer = _pool.Dequeue();
                buffer._disposed = false;
#if UNITY_EDITOR // disable tracking in the build for best testing performance
                CreateTrace(buffer);
#endif
                return buffer;
            }
            else
            {
                NetworkLogger.__Log__(
                    "Pool: A new buffer was created. Consider increasing the initial capacity of the pool, recommended to reduce pressure on the garbage collector.",
                    NetworkLogger.LogType.Error
                );

                return new DataBuffer(pool: this);
            }
        }

        // Let's track the object and check if it's back in the pool.
        // Very slow operation, but useful for debugging. Debug mode only.
        [Conditional("OMNI_DEBUG")]
        private void CreateTrace(DataBuffer buffer)
        {
            CancellationTokenSource cts = new();
            string message = NetworkLogger.GetStackTrace();

#if OMNI_DEBUG
            buffer._onDisposed = () =>
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            };
#endif

            UniTask.Void(
                async (token) =>
                {
                    await UniTask.Delay(MAX_TRACKING_TIME, cancellationToken: token);

                    if (
                        buffer._disposed == false
                        && buffer._enableTracking
                        && !token.IsCancellationRequested
                    )
                    {
                        NetworkLogger.Print(
                            "Memory Leak Fatal Error: The DataBuffer object has not been disposed or returned to the pool within the expected time frame. This could indicate a missing 'using' statement, a failure to call 'Dispose', prolonged processing time, or a persistent reference to the object. Ensure that the object is properly disposed or processing time is not too long."
                        );

                        // Print the stack trace only in debug mode.
                        NetworkLogger.Print(message);
                        cts.Cancel();
                        cts.Dispose();
                        return;
                    }

                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }
                },
                cts.Token
            );
        }

        /// <summary>
        /// Returns a DataBuffer to the pool.
        /// </summary>
        /// <param name="_buffer">The DataBuffer to return to the pool.</param>
        public void Return(DataBuffer _buffer)
        {
            _buffer.SetPosition(0);
            _buffer.SetLength(0);
            _buffer.SetEndPosition(0);
            _buffer.SuppressTracking(false);

            // Set the buffer as disposed so it can be returned to the pool.
            _buffer.SendEnabled = false;
            _buffer._disposed = true;

#if OMNI_DEBUG
            _buffer._onDisposed?.Invoke();
#endif
            _pool.Enqueue(_buffer);
        }
    }
}
