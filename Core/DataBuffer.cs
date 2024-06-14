using System;
using System.Buffers;
using Omni.Core.Interfaces;

namespace Omni.Core
{
    // ref: https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs
    public sealed partial class DataBuffer : IBufferWriter<byte>, IDisposable
    {
        internal int _reworkStart;
        internal int _reworkEnd;

        private readonly IObjectPooling<DataBuffer> _objectPooling;
        private readonly byte[] _buffer;
        private int _position;

        /// <summary>
        /// An empty <see cref="DataBuffer"/> instance.
        /// </summary>
        public static DataBuffer Empty { get; } = new DataBuffer();

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _position);

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _position);

        /// <summary>
        /// Returns the amount of data written to the underlying buffer so far.
        /// </summary>
        public int WrittenCount => _position;

        /// <summary>
        /// Returns the total amount of space within the underlying buffer.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
        /// </summary>
        public int FreeCapacity => _buffer.Length - _position;

        private DataBuffer()
        {
            _buffer = Array.Empty<byte>();
            _position = 0;
        }

        /// <summary>
        /// Creates an instance of an <see cref="DataBuffer"/>, in which data can be written to,
        /// with an initial capacity specified.
        /// </summary>
        /// <param name="capacity">The minimum capacity with which to initialize the underlying buffer.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="capacity"/> is not positive (i.e. less than or equal to 0).
        /// </exception>
        public DataBuffer(int capacity = 1024, IObjectPooling<DataBuffer> pool = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException(null, nameof(capacity));
            }

            _objectPooling = pool;
            _buffer = new byte[capacity];
            _position = 0;
        }

        /// <summary>
        /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to advance past the end of the underlying buffer.
        /// </exception>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentException(null, nameof(count));

            if (_position > _buffer.Length - count)
                throw new ArgumentException(null, nameof(count));

            _position += count;
        }

        /// <summary>
        /// Converts the written data to a byte array.
        /// </summary>
        /// <returns>A byte array containing the written data.</returns>
        public byte[] ToArray()
        {
            // Convert the written data to a byte array.
            return WrittenSpan.ToArray();
        }

        /// <summary>
        /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This will never return an empty <see cref="Memory{T}"/>.
        /// </para>
        /// <para>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </para>
        /// <para>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </para>
        /// <para>
        /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Memory{T}"/>.
        /// </para>
        /// <para>
        /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Memory{T}"/> with its content zeroed.
        /// </para>
        /// </remarks>
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsMemory(_position);
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This will never return an empty <see cref="Span{T}"/>.
        /// </para>
        /// <para>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </para>
        /// <para>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </para>
        /// <para>
        /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Span{T}"/>.
        /// </para>
        /// <para>
        /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Span{T}"/> with its content zeroed.
        /// </para>
        /// </remarks>
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_position);
        }

        internal Span<byte> Internal_GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_position, sizeHint);
        }

        /// <summary>
        /// Clears the data written to the underlying buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You must reset or clear the <see cref="DataBuffer"/> before trying to re-use it.
        /// </para>
        /// <para>
        /// The <see cref="ResetWrittenCount"/> method is faster since it only sets to zero the writer's index
        /// while the <see cref="Clear"/> method additionally zeroes the content of the underlying buffer.
        /// </para>
        /// </remarks>
        /// <seealso cref="ResetWrittenCount"/>
        public void Clear()
        {
            _buffer.AsSpan(0, _position).Clear();
            _position = 0;
        }

        /// <summary>
        /// Resets the data written to the underlying buffer without zeroing its content.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You must reset or clear the <see cref="DataBuffer"/> before trying to re-use it.
        /// </para>
        /// <para>
        /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, the underlying buffer will not be cleared.
        /// </para>
        /// </remarks>
        /// <seealso cref="Clear"/>
        public void ResetWrittenCount()
        {
            _position = 0;
        }

        /// <summary>
        /// Resets the read position to the start of the data, allowing the buffer to be read from the beginning again.
        /// </summary>
        public void ResetReadPosition()
        {
            _position = _reworkStart;
        }

        /// <summary>
        /// Creates a new <see cref="DataBuffer"/> containing the data from the underlying buffer
        /// without including the old header. This new <see cref="DataBuffer"/> is suitable for re-sending
        /// as it contains only the relevant data.
        /// </summary>
        /// <remarks>
        /// Ensure that the returned <see cref="DataBuffer"/> is used within a <c>using</c> statement
        /// to properly return it to the pool after use. The caller must ensure the buffer is disposed or used within a using statement.
        /// </remarks>
        /// <returns>
        /// A <see cref="DataBuffer"/> instance that contains the data from the original buffer,
        /// excluding the old header.
        /// </returns>
        public DataBuffer Rework()
        {
            DataBuffer buffer = NetworkManager.Pool.Rent();
            buffer.Write(_buffer.AsSpan(_reworkStart, _reworkEnd));
            return buffer;
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint > FreeCapacity)
            {
                throw new NotSupportedException(
                    $"Network Buffer: The buffer cannot be resized to accommodate the requested size ({sizeHint}) because it would exceed its capacity ({Capacity}).\nResizing the buffer is not supported due to performance considerations. Consider using a bigger initial capacity or a different data structure."
                );
            }
        }

        internal bool _disposed;

        public void Dispose()
        {
            if (_disposed == true)
            {
                throw new Exception(
                    "Network Buffer: Buffer already disposed. Cannot dispose again."
                );
            }

            if (_objectPooling == null)
            {
                throw new Exception(
                    "Network Buffer: You should not dispose a buffer that was not acquired from the buffer pool."
                );
            }

            _objectPooling.Return(this);
            _disposed = true;

            /// Used for Lite HTTP
            SendEnabled = false;
        }
    }

    /// Used for Lite HTTP
    public sealed partial class DataBuffer
    {
        internal bool SendEnabled { get; private set; }
        internal DeliveryMode DeliveryMode { get; private set; } = DeliveryMode.ReliableOrdered;
        internal Target Target { get; private set; } = Target.Self;
        internal int GroupId { get; private set; }
        internal int CacheId { get; private set; }
        internal CacheMode CacheMode { get; private set; } = CacheMode.None;
        internal byte SequenceChannel { get; private set; }

        public void Send(
            Target target = Target.Self,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            SendEnabled = true;
            Target = target;
            DeliveryMode = deliveryMode;
            GroupId = groupId;
            CacheId = cacheId;
            CacheMode = cacheMode;
            SequenceChannel = sequenceChannel;
        }
    }
}
