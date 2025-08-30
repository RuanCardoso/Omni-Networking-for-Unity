using System;
using System.IO;

namespace Omni.Core
{
    /// <summary>
    /// Contains extension methods for working with <see cref="Stream"/> instances.
    /// </summary>
    public static class StreamExtensions
    {
#if !NET7_0_OR_GREATER
        /// <summary>
        /// Reads a sequence of bytes from the current stream into a span and advances the position within the stream.
        /// This method reads until the span is filled or the end of the stream is reached.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The span to write the data to.</param>
        /// <returns>The total number of bytes read into the buffer. This will always be equal to the length of the span.</returns>
        /// <exception cref="EndOfStreamException">Thrown when the end of the stream is reached before the span is filled.</exception>
        public static int ReadExactly(this Stream stream, Span<byte> buffer)
        {
            return ReadExactly(stream, buffer, buffer.Length, throwOnEndOfStream: true);
        }

        private static int ReadExactly(this Stream stream, Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream)
        {
            int totalRead = 0;
            while (totalRead < minimumBytes)
            {
                int read = stream.Read(buffer[totalRead..]);
                if (read == 0)
                {
                    if (throwOnEndOfStream)
                    {
                        throw new EndOfStreamException("The stream has reached the end.");
                    }

                    return totalRead;
                }

                totalRead += read;
            }

            return totalRead;
        }
#endif
    }
}
