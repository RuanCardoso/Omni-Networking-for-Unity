using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Omni.Core.Interfaces;
using UnityEngine;

namespace Omni.Core
{
    // ref: https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs
    public sealed partial class DataBuffer : IBufferWriter<byte>, IDisposable
    {
        private readonly IObjectPooling<DataBuffer> _objectPooling;
        private readonly byte[] _buffer;

        private readonly Stream _stream;
        private int _position;
        private int _endPosition;
        private int _length;

        /// <summary>
        /// An empty <see cref="DataBuffer"/> instance.
        /// </summary>
        public static DataBuffer Empty { get; } = new DataBuffer();

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        public ReadOnlyMemory<byte> BufferAsMemory => _buffer.AsMemory(0, CurrentPos());

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        public ReadOnlySpan<byte> BufferAsSpan => _buffer.AsSpan(0, CurrentPos());

        /// <summary>
        /// Returns the amount of data written to the underlying buffer so far.
        /// </summary>
        public int Position
        {
            get => _position;
            internal set => _position = value;
        }

        /// <summary>
        /// Returns the amount of data written to the underlying buffer.
        /// </summary>
        public int EndPosition => _endPosition;

        /// <summary>
        /// Returns the total amount of space within the underlying buffer.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
        /// </summary>
        public int FreeCapacity => _buffer.Length - _position;

        /// <summary>
        /// Returns the amount of data written to the underlying buffer.
        /// Call <see cref="Clear"/> to clear the buffer and reset the length to 0.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Returns the underlying <see cref="System.IO.Stream"/> instance.
        /// </summary>
        /// <remarks>
        /// This property is useful for interacting with the underlying stream
        /// when you need to do something that isn't supported by the
        /// <see cref="DataBuffer"/> API itself.
        /// </remarks>
        public Stream Stream => _stream;

        private DataBuffer()
        {
            _buffer = Array.Empty<byte>();
        }

        /// <summary>
        /// Creates a new instance of an <see cref="DataBuffer"/> that is a copy of another <see cref="DataBuffer"/>.
        /// </summary>
        /// <param name="fromBuffer">The <see cref="DataBuffer"/> to copy from.</param>
        /// <param name="seekToBegin">
        /// If <c>true</c>, the position of the new buffer is set to the beginning after the copy.
        /// </param>
        public DataBuffer(DataBuffer fromBuffer, bool seekToBegin = false)
            : this(fromBuffer._length + 1)
        {
            // Copy the buffer.
            this.Write(fromBuffer.BufferAsSpan);
            if (seekToBegin)
            {
                // Reset the position to the beginning.
                SeekToBegin();
            }
        }

        /// <summary>
        /// Creates an instance of an <see cref="DataBuffer"/>, in which data can be written to,
        /// with an initial capacity specified.
        /// </summary>
        /// <param name="capacity">The minimum capacity with which to initialize the underlying buffer.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="capacity"/> is not positive (i.e. less than or equal to 0).
        /// </exception>
        public DataBuffer(int capacity = 32768)
            : this(capacity, null) { }

        internal DataBuffer(int capacity = 32768, IObjectPooling<DataBuffer> pool = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException(null, nameof(capacity));
            }

            _objectPooling = pool;
            _buffer = new byte[capacity];
            _stream = new DataBufferStream(this);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) // interface!
        {
            Internal_Advance(count);
            _length += count;
            _endPosition += count;
        }

        // Advances when read, read-only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Internal_Advance(int count, [CallerMemberName] string ___ = "")
        {
#if OMNI_DEBUG
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(DataBuffer),
                    "Cannot advance as the DataBuffer instance has already been disposed."
                );
            }

            if (count < 0)
            {
                throw new ArgumentException(
                    "The count parameter must be a non-negative integer.",
                    nameof(count)
                );
            }

            if (_position > _buffer.Length - count)
            {
                throw new ArgumentException(
                    $"The count parameter exceeds the buffer length. Current position: {_position}, Buffer length: {_buffer.Length}, Count: {count}",
                    nameof(count)
                );
            }

            if ((_position > _length - count) && ___ != nameof(Advance))
            {
                throw new InvalidOperationException(
                    $"Cannot advance past the end of the buffer. Not enough data to read. Current position: {_position}, Length: {_length}, Count: {count}"
                );
            }
#endif
            _position += count;
        }

        /// <summary>
        /// Converts the written data to a byte array.
        /// </summary>
        /// <returns>A byte array containing the written data.</returns>
        public byte[] ToArray()
        {
            // Convert the written data to a byte array.
            return BufferAsSpan.ToArray();
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
        /// If you reset the writer using the <see cref="SeekToBegin"/> method, this method may return a non-cleared <see cref="Memory{T}"/>.
        /// </para>
        /// <para>
        /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Memory{T}"/> with its content zeroed.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<byte> GetMemory(int sizeHint = 0) // interface!
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsMemory(_position);
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <c>0</c>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// <para>
        /// If you reset the writer using the <see cref="SeekToBegin"/> method, this method may return a non-cleared <see cref="Span{T}"/>.
        /// </para>
        /// <para>
        /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Span{T}"/> with its content zeroed.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int sizeHint = 0) // interface!
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<byte> Internal_GetSpan(int length)
        {
            CheckAndResizeBuffer(length);
            return _buffer.AsSpan(_position, length);
        }

        /// <summary>
        /// Clears the data written to the underlying buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You must reset or clear the <see cref="DataBuffer"/> before trying to re-use it.
        /// </para>
        /// <para>
        /// The <see cref="SeekToBegin"/> method is faster since it only sets to zero the writer's index
        /// while the <see cref="Clear"/> method additionally zeroes the content of the underlying buffer.
        /// </para>
        /// </remarks>
        /// <seealso cref="SeekToBegin"/>
        public void Clear()
        {
            _buffer.AsSpan().Clear();
            _endPosition = _position;
            _position = 0;
            _length = 0;
        }

        /// <summary>
        /// Resets the data writer's position and length to their initial values.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method sets the writer's position and length to their initial values,
        /// effectively resetting the data writer's state without clearing the underlying buffer.
        /// </para>
        /// <para>
        /// You must reset or clear the <see cref="DataBuffer"/> before trying to re-use it.
        /// </para>
        /// </remarks>
        public void Reset()
        {
            _endPosition = 0;
            _position = 0;
            _length = 0;
        }

        /// <summary>
        /// Resets the data writer's position to the beginning without clearing the buffer's content.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You must reset or clear the <see cref="DataBuffer"/> before trying to re-use it.
        /// </para>
        /// <para>
        /// If you reset the writer using the <see cref="SeekToBegin"/> method, the underlying buffer will not be cleared.
        /// </para>
        /// </remarks>
        /// <seealso cref="Clear"/>
        public void SeekToBegin()
        {
            _endPosition = _position;
            _position = 0;
        }

        /// <summary>
        /// Sets the writer's position to the end of the buffer.
        /// </summary>
        public void SeekToEnd()
        {
            _position = _endPosition;
        }

        /// <summary>
        /// Sets the writer's position to the specified position and saves the current position as the end position.
        /// </summary>
        /// <param name="pos">The new position to set the writer's position to.</param>
        public void SetPosition(int pos)
        {
            _endPosition = _position;
            _position = pos;
        }

        /// <summary>
        /// Sets the length of the buffer to the specified position.
        /// </summary>
        /// <param name="length">The new length to set.</param>
        public void SetLength(int length)
        {
            _length = length;
        }

        /// <summary>
        /// Sets the end position of the buffer to the specified position.
        /// </summary>
        /// <param name="pos">The new end position to set.</param>
        internal void SetEndPosition(int pos)
        {
            _endPosition = pos;
        }

        /// <summary>
        /// Copies the data from this buffer to another buffer, and optionally seeks to the beginning of the destination buffer.
        /// </summary>
        /// <param name="toBuffer">The buffer to copy the data to.</param>
        /// <param name="seekToBegin">Whether to seek to the beginning of the destination buffer after copying.</param>
        public void CopyTo(DataBuffer toBuffer, bool seekToBegin = false)
        {
            // Copy the buffer.
            toBuffer.Write(BufferAsSpan);
            // Reset the position to the beginning.
            if (seekToBegin)
            {
                toBuffer.SeekToBegin();
            }
        }

        /// <summary>
        /// Gets the underlying buffer.
        /// </summary>
        /// <returns>The underlying buffer.</returns>
        public byte[] GetBuffer()
        {
            // Returns the underlying buffer.
            return _buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CurrentPos()
        {
            return _position > 0 ? _position : _endPosition;
        }

        [Conditional("OMNI_DEBUG")]
        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint > FreeCapacity)
            {
                throw new NotSupportedException(
                    $"The buffer cannot be resized to the requested size ({sizeHint}) because it exceeds the maximum capacity ({Capacity}). "
                        + "Resizing the buffer is not supported due to performance reasons. "
                        + "Consider using a larger initial capacity or a different data structure."
                );
            }
        }

#if OMNI_DEBUG
        internal Action _onDisposed;
#endif
        internal bool _disposed;
        internal bool _enableTracking = true;

        /// <summary>
        /// Disposes the buffer, returning it to the pool if it was acquired from one.
        /// Throws exceptions if the buffer is already disposed or was not acquired from a buffer pool.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when the buffer is already disposed or was not acquired from the buffer pool.
        /// </exception>
        public void Dispose()
        {
            if (_disposed == true)
            {
                throw new ObjectDisposedException(
                    "buffer: Buffer already disposed. Cannot dispose again."
                );
            }

            if (_objectPooling == null)
            {
                throw new ArgumentNullException(
                    "buffer: You should not dispose a buffer that was not acquired from the buffer pool."
                );
            }

            _objectPooling.Return(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(DataBuffer buffer)
        {
            return buffer.Read<bool>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator char(DataBuffer buffer)
        {
            return buffer.Read<char>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte(DataBuffer buffer)
        {
            return buffer.Read<byte>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator sbyte(DataBuffer buffer)
        {
            return buffer.Read<sbyte>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(DataBuffer buffer)
        {
            return buffer.Read<int>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(DataBuffer buffer)
        {
            return buffer.Read<uint>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(DataBuffer buffer)
        {
            return buffer.Read<short>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(DataBuffer buffer)
        {
            return buffer.Read<ushort>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(DataBuffer buffer)
        {
            return buffer.Read<float>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(DataBuffer buffer)
        {
            return buffer.Read<double>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(DataBuffer buffer)
        {
            return buffer.ReadString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(DataBuffer buffer)
        {
            return buffer.Read<long>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(DataBuffer buffer)
        {
            return buffer.Read<ulong>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator decimal(DataBuffer buffer)
        {
            return buffer.Read<decimal>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(DataBuffer buffer)
        {
            return buffer.Read<Vector3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(DataBuffer buffer)
        {
            return buffer.Read<Vector2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quaternion(DataBuffer buffer)
        {
            return buffer.Read<Quaternion>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HalfVector3(DataBuffer buffer)
        {
            return buffer.Read<HalfVector3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HalfVector2(DataBuffer buffer)
        {
            return buffer.Read<HalfVector2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator HalfQuaternion(DataBuffer buffer)
        {
            return buffer.Read<HalfQuaternion>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color(DataBuffer buffer)
        {
            return buffer.Read<Color>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color32(DataBuffer buffer)
        {
            return buffer.Read<Color32>();
        }

        /// <summary>
        /// Toggles the suppression of tracking for object disposal and return to the pool.
        /// When tracking is suppressed, error messages about objects not being disposed of are disabled.
        /// This is helpful for long-running asynchronous operations where tracking might mistakenly flag objects as not disposed.
        /// </summary>
        /// <param name="suppress">If true, suppresses tracking; if false, enables tracking. Default is true.</param>
        [Conditional("OMNI_DEBUG")]
        public void SuppressTracking(bool suppress = true)
        {
            _enableTracking = !suppress;
        }
    }
}
