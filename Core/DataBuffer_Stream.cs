using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Omni.Core
{
    internal sealed class DataBufferStream : Stream
    {
        private readonly DataBuffer _buffer;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _buffer.Length;

        public override long Position
        {
            get => _buffer.Position;
            set => _buffer.Position = (int)value;
        }

        private DataBufferStream() { }

        internal DataBufferStream(DataBuffer buffer)
        {
            _buffer = buffer;
        }

        public override void Flush()
        {
            // Because any data written to a DataBuffer object is written into RAM, this method is redundant.
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffer.Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _buffer.Write(buffer);
        }

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

        public override void WriteByte(byte value)
        {
            _buffer.Write(value);
        }

        public override int ReadByte()
        {
            return _buffer.Read<byte>();
        }

        public override void SetLength(long value)
        {
            _buffer.SetLength((int)value);
        }

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
            throw new NotSupportedException();
        }

        public override ValueTask DisposeAsync()
        {
            throw new NotSupportedException();
        }
    }
}
