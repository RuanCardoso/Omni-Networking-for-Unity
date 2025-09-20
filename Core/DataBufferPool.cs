using Omni.Core.Interfaces;
using Omni.Shared;
using Omni.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Omni.Core
{
    /// <summary>
    /// The DataBufferPool class provides a buffer pooling mechanism for managing instances of DataBuffer.
    /// It helps to minimize memory allocation overhead and reduce garbage collection pressure
    /// by reusing buffer instances from the pool.
    /// </summary>
    /// <remarks>
    /// This class is primarily used to manage the lifecycle of DataBuffer objects by providing rent and return operations.
    /// When a buffer is returned, it is reset to its initial state and made available for reuse.
    /// If the pool is empty when a buffer is requested, a new instance of DataBuffer will be created.
    /// </remarks>
    /// <threadsafety>
    /// This class is not thread-safe. Synchronization must be considered if accessed across multiple threads.
    /// </threadsafety>
    internal sealed class DataBufferPool : IBufferPooling<DataBuffer>
    {
        /// The maximum time in milliseconds that a buffer is being tracked before it is considered
        /// to have not been disposed or returned to the pool. <c>Debug mode only.</c>
        /// 500ms seems good to me, if there is an expensive operation that takes more than 500ms, it is recommended to call SupressTracking.
        private const int MAX_TRACKING_TIME = 500;

        private int DefaultCapacity { get; } = DataBuffer.DefaultBufferSize;
        private readonly Queue<DataBuffer> _pool;

        internal DataBufferPool(int capacity = DataBuffer.DefaultBufferSize, int poolSize = 32)
        {
            DefaultCapacity = capacity;
            _pool = new Queue<DataBuffer>();
            for (int i = 0; i < poolSize; i++)
                _pool.Enqueue(new DataBuffer(capacity, pool: this));
        }

        /// <inheritdoc />
        public DataBuffer Rent(bool enableTracking = true, [CallerMemberName] string methodName = "")
        {
            if (_pool.Count > 0)
            {
                var buffer = _pool.Dequeue();
                buffer._disposed = false;
#if UNITY_EDITOR // Obs: Disable tracking in the build for best performance tests
                if (enableTracking && NetworkManager.EnableDeepDebug)
                    CreateTrace(buffer, methodName);
#endif
                return buffer;
            }
            else
            {
                NetworkLogger.__Log__(
                    "BufferPool: created a new buffer. Increase the initial pool capacity to avoid extra allocations.\n" +
                    "Method: " + methodName,
                    NetworkLogger.LogType.Warning
                );

                return new DataBuffer(DefaultCapacity, pool: this);
            }
        }

        // Let's track the object and check if it's back in the pool.
        // Very slow operation, but useful for debugging. Debug mode only.
        [Conditional("OMNI_DEBUG")]
        private void CreateTrace(DataBuffer buffer, string methodName)
        {
            CancellationTokenSource cts = new();
            string hyperlink = NetworkLogger.GetStackFramesToHyperlink();

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
                            "Memory leak detected: DataBuffer was not disposed or returned to the pool. " +
                            "Ensure proper disposal (use 'using' or call Dispose) to avoid performance issues.\n" +
                            "Method: " + methodName,
                            NetworkLogger.LogType.Error
                        );

                        // Print the stack trace only in debug mode.
                        if (!string.IsNullOrEmpty(hyperlink))
                            NetworkLogger.Print(hyperlink, NetworkLogger.LogType.Error);

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

        /// <inheritdoc />
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