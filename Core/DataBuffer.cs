using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Omni.Core
{
	// ref: https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs
	/// <summary>
	/// A high-performance buffer designed for efficient writing, reading, and management of network messages and other data.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <see cref="DataBuffer"/> class provides optimized operations for handling dynamic data, including methods for 
	/// writing, reading, resetting, and pooling. It supports span-based and memory-based APIs for maximum performance.
	/// </para>
	/// <para>
	/// Ideal for network communication and scenarios requiring compact, reusable, and efficient data management.
	/// </para>
	/// </remarks>
	public sealed partial class DataBuffer : IBufferWriter<byte>, IDisposable
	{
		private const int DefaultBufferSize = 32768;
		private readonly IBufferPooling<DataBuffer> _objectPooling;
		private readonly byte[] _buffer;

		private readonly Stream _stream;
		private int _position;
		private int _endPosition;
		private int _length;

		/// <summary>
		/// Represents an empty <see cref="DataBuffer"/> instance.
		/// </summary>
		/// <remarks>
		/// This instance can be used as a placeholder or default value where an empty buffer is required.
		/// </remarks>
		public static DataBuffer Empty { get; } = new DataBuffer();

		/// <summary>
		/// Retrieves the data written to the buffer so far as a <see cref="ReadOnlyMemory{T}"/>.
		/// </summary>
		/// <remarks>
		/// The memory spans only the portion of the buffer that contains written data, excluding any unused capacity.
		/// </remarks>
		public ReadOnlyMemory<byte> BufferAsMemory => _buffer.AsMemory(0, CurrentPos());

		/// <summary>
		/// Retrieves the data written to the buffer so far as a <see cref="ReadOnlySpan{T}"/>.
		/// </summary>
		/// <remarks>
		/// The span includes only the portion of the buffer that contains written data, excluding any unused capacity.
		/// </remarks>
		public ReadOnlySpan<byte> BufferAsSpan => _buffer.AsSpan(0, CurrentPos());

		/// <summary>
		/// Gets the current write position within the buffer.
		/// </summary>
		/// <remarks>
		/// This property indicates the index where the next write operation will occur.
		/// </remarks>
		public int Position
		{
			get => _position;
			internal set => _position = value;
		}

		/// <summary>
		/// Gets the position marking the end of the written data within the buffer.
		/// </summary>
		/// <remarks>
		/// This property represents the maximum point reached by write operations in the buffer.
		/// </remarks>
		public int EndPosition => _endPosition;

		/// <summary>
		/// Gets the total capacity of the buffer.
		/// </summary>
		/// <remarks>
		/// This property indicates the total size of the underlying buffer, including both used and unused space.
		/// </remarks>
		public int Capacity => _buffer.Length;

		/// <summary>
		/// Gets the available space in the buffer that can be written into without resizing.
		/// </summary>
		/// <remarks>
		/// This property helps track how much space remains before the buffer needs to grow.
		/// </remarks>
		public int FreeCapacity => _buffer.Length - _position;

		/// <summary>
		/// Gets the total length of the data written to the buffer.
		/// </summary>
		public int Length => _length;

		/// <summary>
		/// Gets the underlying <see cref="System.IO.Stream"/> associated with the buffer.
		/// </summary>
		/// <remarks>
		/// Use this property to interact with the underlying stream for operations not directly supported by the <see cref="DataBuffer"/> API.
		/// </remarks>
		public Stream Stream => _stream;

		private DataBuffer()
		{
			_buffer = Array.Empty<byte>();
		}

		/// <summary>
		/// Creates a new instance of a <see cref="DataBuffer"/> by copying the contents of another <see cref="DataBuffer"/>.
		/// </summary>
		/// <param name="fromBuffer">The <see cref="DataBuffer"/> to copy data from.</param>
		/// <param name="seekToBegin">
		/// If <c>true</c>, the position of the new buffer is reset to the beginning after copying the data.
		/// </param>
		/// <remarks>
		/// This constructor initializes a new buffer with a capacity large enough to hold the data from the source buffer.
		/// </remarks>
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
		/// Creates a new instance of a <see cref="DataBuffer"/> with the specified initial capacity.
		/// </summary>
		/// <param name="capacity">The minimum capacity to allocate for the underlying buffer.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="capacity"/> is less than or equal to 0.
		/// </exception>
		/// <remarks>
		/// If no capacity is specified, a default value of <see cref="DefaultBufferSize"/> bytes is used.
		/// </remarks>
		public DataBuffer(int capacity = DefaultBufferSize)
			: this(capacity, null) { }

		internal DataBuffer(int capacity = DefaultBufferSize, IBufferPooling<DataBuffer> pool = null)
		{
			if (capacity <= 0)
			{
				NetworkLogger.PrintHyperlink();
				throw new ArgumentException(null, nameof(capacity));
			}

			_objectPooling = pool;
			_buffer = new byte[capacity];
			_stream = new DataBufferStream(this);
		}

		/// <summary>
		/// Notifies the <see cref="IBufferWriter{T}"/> that a specified amount of data has been written to the output buffer.
		/// </summary>
		/// <param name="count">The number of bytes written to the buffer.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="count"/> is negative.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when attempting to advance past the end of the underlying buffer.
		/// </exception>
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
				NetworkLogger.PrintHyperlink();
				throw new ObjectDisposedException(
					nameof(DataBuffer),
					"Cannot advance the buffer because this DataBuffer instance has already been disposed. Ensure the instance is valid before reading from it."
				);
			}

			if (count < 0)
			{
				NetworkLogger.PrintHyperlink();
				throw new ArgumentException(
					"The 'count' parameter must be a non-negative integer. Advancing by a negative value is not allowed.",
					nameof(count)
				);
			}

			if (_position > _buffer.Length - count)
			{
				NetworkLogger.PrintHyperlink();
				throw new ArgumentException(
					$"The 'count' parameter exceeds the remaining buffer capacity. Current position: {_position}, Buffer length: {_buffer.Length}, Requested count: {count}.",
					nameof(count)
				);
			}

			if ((_position > _length - count) && ___ != nameof(Advance))
			{
				NetworkLogger.PrintHyperlink();
				throw new InvalidOperationException(
					$"Cannot advance past the end of the buffer. Insufficient data available for reading. Current position: {_position}, Buffer length: {_length}, Requested count: {count}."
				);
			}
#endif
			_position += count;
		}

		/// <summary>
		/// Converts the written data in the buffer to a new byte array.
		/// </summary>
		/// <returns>A byte array containing all the data written to the buffer so far.</returns>
		/// <remarks>
		/// This method creates a new array containing a copy of the written data. 
		/// It does not include unused capacity in the buffer.
		/// </remarks>
		public byte[] ToArray()
		{
			// Convert the written data to a byte array.
			return BufferAsSpan.ToArray();
		}

		/// <summary>
		/// Retrieves a writable <see cref="Memory{T}"/> segment from the buffer with at least the specified length.
		/// </summary>
		/// <param name="sizeHint">
		/// The minimum number of bytes required. If <c>0</c>, a non-empty buffer is returned.
		/// </param>
		/// <returns>
		/// A <see cref="Memory{T}"/> instance representing a segment of the buffer that can be written to.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="sizeHint"/> is negative.
		/// </exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Memory<byte> GetMemory(int sizeHint = 0) // interface!
		{
			CheckAndResizeBuffer(sizeHint);
			return _buffer.AsMemory(_position);
		}

		/// <summary>
		/// Retrieves a writable <see cref="Span{T}"/> segment from the buffer with at least the specified length.
		/// </summary>
		/// <param name="sizeHint">
		/// The minimum number of bytes required. If <c>0</c>, a non-empty buffer segment is returned.
		/// </param>
		/// <returns>
		/// A <see cref="Span{T}"/> instance representing a writable segment of the buffer.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="sizeHint"/> is negative.
		/// </exception>
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
		/// Clears all data written to the buffer and resets its state.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <see cref="Clear"/> method zeroes the content of the underlying buffer and resets the writer's position, length, and end position.
		/// </para>
		/// <para>
		/// Use <see cref="SeekToBegin"/> as a faster alternative if you only need to reset the writer's position without clearing the buffer's content.
		/// </para>
		/// </remarks>
		/// <seealso cref="SeekToBegin"/>
		public void Clear()
		{
			var span = _buffer.AsSpan();
			span.Clear();
			Reset();
		}

		/// <summary>
		/// Resets the writer's position, length, and end position without modifying the content of the buffer.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method effectively resets the state of the writer, allowing it to be reused without clearing the buffer's content.
		/// </para>
		/// <para>
		/// If you need to clear the content of the buffer, use the <see cref="Clear"/> method instead.
		/// </para>
		/// </remarks>
		public void Reset()
		{
			_endPosition = 0;
			_position = 0;
			_length = 0;
		}

		/// <summary>
		/// Resets the writer's position to the beginning of the buffer without modifying its content.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method resets the writer's position to the start of the buffer but retains the existing data.
		/// </para>
		/// <para>
		/// Use <see cref="Clear"/> to zero the buffer's content if needed.
		/// </para>
		/// </remarks>
		/// <seealso cref="Clear"/>
		public void SeekToBegin()
		{
			_endPosition = _position;
			_position = 0;
		}

		/// <summary>
		/// Moves the writer's position to the end of the buffer.
		/// </summary>
		/// <remarks>
		/// This method sets the writer's position to the end of the currently written data.
		/// </remarks>
		public void SeekToEnd()
		{
			_position = _endPosition;
		}

		/// <summary>
		/// Sets the writer's position to the specified position and saves the current position as the end position.
		/// </summary>
		/// <param name="pos">The new position to set for the writer.</param>
		/// <remarks>
		/// Use this method to manually adjust the writer's position within the buffer. Ensure the specified position is valid.
		/// </remarks>
		public void SetPosition(int pos)
		{
			_endPosition = _position;
			_position = pos;
		}

		/// <summary>
		/// Sets the logical length of the buffer to the specified value.
		/// </summary>
		/// <param name="length">The new length to set for the buffer.</param>
		/// <remarks>
		/// Use this method to explicitly adjust the length of the buffer. Ensure the value is valid relative to the buffer's capacity and position.
		/// </remarks>
		public void SetLength(int length)
		{
			_length = length;
		}

		/// <summary>
		/// Sets the end position of the buffer to the specified value.
		/// </summary>
		/// <param name="pos">The new end position to set for the buffer.</param>
		/// <remarks>
		/// This method adjusts the point marking the end of the written data in the buffer.
		/// Ensure the specified position does not exceed the buffer's length.
		/// </remarks>
		internal void SetEndPosition(int pos)
		{
			_endPosition = pos;
		}

		/// <summary>
		/// Copies the data from this buffer to another <see cref="DataBuffer"/>.
		/// </summary>
		/// <param name="toBuffer">The destination <see cref="DataBuffer"/> to copy the data to.</param>
		/// <param name="seekToBegin">
		/// If <c>true</c>, the destination buffer's position is reset to the beginning after copying.
		/// </param>
		/// <remarks>
		/// The method copies all the written data from the source buffer into the destination buffer. 
		/// The destination buffer's existing data may be overwritten depending on its current position.
		/// </remarks>
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
		/// Retrieves the underlying byte array of the buffer.
		/// </summary>
		/// <returns>The underlying buffer as a byte array.</returns>
		/// <remarks>
		/// This method exposes the internal storage of the buffer. 
		/// Modifying the returned array directly can lead to unexpected behavior and should be avoided.
		/// </remarks>
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
				NetworkLogger.PrintHyperlink();
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
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// Thrown if the buffer has already been disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the buffer was not acquired from a buffer pool and should not be disposed.
		/// </exception>
		/// <remarks>
		/// Once a buffer is disposed, it can no longer be used. 
		/// Ensure the buffer is valid and was acquired from a buffer pool before disposing of it.
		/// </remarks>
		public void Dispose()
		{
			if (_disposed == true)
			{
				NetworkLogger.PrintHyperlink();
				throw new ObjectDisposedException(
					"Cannot dispose this buffer because it has already been disposed. Ensure Dispose is called only once."
				);
			}

			if (_objectPooling == null)
			{
				NetworkLogger.PrintHyperlink();
				throw new ArgumentNullException("",
					"Cannot dispose this buffer because it was not acquired from a buffer pool. Only pooled buffers should be disposed."
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
		/// Enables or disables tracking for object disposal and return to the pool(Debug mode only).
		/// </summary>
		/// <param name="suppress">
		/// If <c>true</c>, suppresses tracking, disabling error messages about objects not being disposed. 
		/// If <c>false</c>, enables tracking. Default is <c>true</c>.
		/// </param>
		/// <remarks>
		/// Suppressing tracking is useful for long-running asynchronous operations, where tracking might incorrectly flag objects as not disposed.
		/// When tracking is suppressed, no warnings or errors will be generated for objects that are not properly disposed or returned to the pool.
		/// </remarks>
		/// <example>
		/// To suppress tracking:
		/// <code>
		/// buffer.SuppressTracking(); // Suppresses tracking (default is true)
		/// </code>
		/// To re-enable tracking:
		/// <code>
		/// buffer.SuppressTracking(false);
		/// </code>
		/// </example>
		[Conditional("OMNI_DEBUG")]
		public void SuppressTracking(bool suppress = true)
		{
			_enableTracking = !suppress;
		}
	}
}
