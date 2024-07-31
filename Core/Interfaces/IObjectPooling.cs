namespace Omni.Core.Interfaces
{
    public interface IObjectPooling<T>
    {
        /// <summary>
        /// Rents a buffer from the pool. This operation may be slow in <c>Debug mode</c> due to additional diagnostic checks.
        /// In <c>Debug mode</c>, this method performs additional tracking to ensure the buffer is properly disposed of and returned to the pool.
        /// </summary>
        /// <returns>A <see cref="DataBuffer"/> object from the pool.</returns>
        T Rent();

        /// <summary>
        /// Returns a DataBuffer to the pool.
        /// </summary>
        /// <param name="_buffer">The DataBuffer to return to the pool.</param>
        void Return(T item);
    }
}
