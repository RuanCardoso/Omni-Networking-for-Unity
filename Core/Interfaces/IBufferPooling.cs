namespace Omni.Core.Interfaces
{
    /// <summary>
    /// Defines a pooling mechanism for renting and returning buffers of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of buffer managed by the pool.</typeparam>
    /// <remarks>
    /// Buffer pooling is a technique to reuse buffers, reducing memory allocations and improving performance.
    /// </remarks>
    public interface IBufferPooling<T>
    {
        /// <summary>
        /// Rents a buffer from the pool, providing a reusable instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In <c>Debug mode</c>, this method performs additional diagnostic checks to ensure proper usage and disposal of buffers.
        /// If <paramref name="enableTracking"/> is <c>false</c>, the buffer will not be tracked in the Unity Editor or Debug mode.
        /// This is particularly useful for performance testing within the editor, as it eliminates the diagnostic overhead
        /// associated with buffer tracking, allowing for more accurate performance measurements.
        /// Note that tracking is always disabled in release builds, regardless of this parameter's value.
        /// </para>
        /// <para>
        /// Diagnostic checks may introduce a slight performance overhead compared to <c>Release mode</c>.
        /// </para>
        /// </remarks>
        /// <returns>A reusable buffer of type <typeparamref name="T"/> from the pool.</returns>
        T Rent(bool enableTracking = true);

        /// <summary>
        /// Returns a buffer to the pool for reuse.
        /// </summary>
        /// <param name="item">The buffer to return to the pool.</param>
        /// <remarks>
        /// Returning a buffer allows it to be reused, minimizing memory allocations and reducing garbage collection overhead.
        /// Ensure the buffer is no longer in use before returning it to the pool.
        /// </remarks>
        void Return(T item);
    }
}