using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Omni.Core
{
	/// <summary>
	/// Provides a stream implementation that wraps a <see cref="DataBuffer"/>, 
	/// allowing read, write, and seek operations on its data.
	/// </summary>
	internal sealed class DataBufferStream : Stream
	{
		private readonly DataBuffer _buffer;

		/// <summary>
		/// Gets a value indicating whether the current stream supports reading.
		/// </summary>
		public override bool CanRead => true;

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		public override bool CanSeek => true;

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// </summary>
		public override bool CanWrite => true;

		/// <summary>
		/// Gets the length of the stream in bytes.
		/// </summary>
		public override long Length => _buffer.Length;

		/// <summary>
		/// Gets or sets the current position within the stream.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown if the specified position is less than 0 or greater than the stream's length.
		/// </exception>
		public override long Position
		{
			get => _buffer.Position;
			set => _buffer.Position = (int)value;
		}

		internal DataBufferStream(DataBuffer buffer)
		{
			_buffer = buffer;
		}

		// This implementation does nothing because the data is already in memory.
		public override void Flush()
		{
			// nothing
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream from a buffer.
		/// </summary>
		/// <param name="buffer">The buffer containing data to write.</param>
		/// <param name="offset">The zero-based byte offset in the buffer at which to begin writing bytes.</param>
		/// <param name="count">The number of bytes to write.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			_buffer.Write(buffer.AsSpan(offset, count));
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream from a span.
		/// </summary>
		/// <param name="buffer">A span of bytes to write to the stream.</param>
		public override void Write(ReadOnlySpan<byte> buffer)
		{
			_buffer.Write(buffer);
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream.
		/// </summary>
		/// <param name="buffer">The buffer to write the data to.</param>
		/// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data read from the stream.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			int n = _buffer.Length - _buffer.Position;
			if (n > count)
				n = count;

			if (n <= 0)
				return 0;

			Span<byte> span = _buffer.Internal_GetSpan(n);
			span.CopyTo(buffer.AsSpan(offset));

			// Advance position
			_buffer.Internal_Advance(n);
			return n;
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream into a span and advances the position within the stream.
		/// </summary>
		/// <param name="buffer">A span to write the data to.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public override int Read(Span<byte> buffer)
		{
			int n = Math.Min(_buffer.Length - _buffer.Position, buffer.Length);
			if (n <= 0)
				return 0;

			_buffer.Internal_GetSpan(n).CopyTo(buffer);

			// Advance position
			_buffer.Internal_Advance(n);
			return n;
		}

		/// <summary>
		/// Writes a single byte to the current stream.
		/// </summary>
		/// <param name="value">The byte to write to the stream.</param>
		public override void WriteByte(byte value)
		{
			_buffer.Write(value);
		}

		/// <summary>
		/// Reads a single byte from the stream and advances the position within the stream by one byte.
		/// </summary>
		/// <returns>The byte read from the stream.</returns>
		public override int ReadByte()
		{
			return _buffer.Read<byte>();
		}

		/// <summary>
		/// Sets the length of the current stream.
		/// </summary>
		/// <param name="value">The desired length of the stream in bytes.</param>
		public override void SetLength(long value)
		{
			_buffer.SetLength((int)value);
		}

		/// <summary>
		/// Sets the position within the current stream.
		/// </summary>
		/// <param name="offset">A byte offset relative to the <paramref name="origin"/>.</param>
		/// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
		/// <returns>The new position within the stream.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the resulting position is out of range.</exception>
		public override long Seek(long offset, SeekOrigin origin)
		{
			var newPosition = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => Position + offset,
				SeekOrigin.End => Length + offset,
				_ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null),
			};

			if (newPosition < 0 || newPosition > Length)
			{
				throw new ArgumentOutOfRangeException(
					nameof(offset),
					"The resulting position is out of range."
				);
			}

			Position = newPosition;
			return Position;
		}

		protected override void Dispose(bool disposing)
		{
			throw new NotSupportedException("The stream is not disposable.");
		}

		public override ValueTask DisposeAsync()
		{
			throw new NotSupportedException("The stream is not disposable.");
		}
	}
}
