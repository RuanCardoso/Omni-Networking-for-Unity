using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MemoryPack;
using MemoryPack.Compression;
using Newtonsoft.Json;
using Omni.Core.Cryptography;

namespace Omni.Core
{
    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// Instantiates a network identity on the server for a specific network peer and serializes its data to the buffer.
        /// </summary>
        /// <param name="prefab">The prefab of the network identity to instantiate.</param>
        /// <param name="peer">The network peer for which the identity is instantiated.</param>
        /// <param name="buffer">The buffer to write identity data.</param>
        /// <param name="OnBeforeStart">An action to execute before the network identity starts, but after it has been registered.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity InstantiateOnServer(
            this DataBuffer buffer,
            NetworkIdentity prefab,
            NetworkPeer peer,
            Action<NetworkIdentity> OnBeforeStart = null
        )
        {
            return prefab.InstantiateOnServer(peer, buffer, OnBeforeStart);
        }

        /// <summary>
        /// Instantiates a network identity on the client from serialized data in the buffer.
        /// </summary>
        /// <param name="prefab">The prefab of the network identity to instantiate.</param>
        /// <param name="buffer">The buffer containing serialized identity data.</param>
        /// <returns>The instantiated network identity.</returns>
        public static NetworkIdentity InstantiateOnClient(
            this DataBuffer buffer,
            NetworkIdentity prefab,
            Action<NetworkIdentity> OnBeforeStart = null
        )
        {
            return prefab.InstantiateOnClient(buffer, OnBeforeStart);
        }

        /// <summary>
        /// Destroys a network identity on the server and serializes its destruction to the buffer.
        /// </summary>
        /// <param name="identity">The network identity to destroy.</param>
        public static void DestroyOnServer(this DataBuffer buffer, NetworkIdentity identity)
        {
            identity.DestroyOnServer(buffer);
        }

        /// <summary>
        /// Destroys a network identity on the client from serialized data in the buffer.
        /// </summary>
        public static void DestroyOnClient(this DataBuffer buffer)
        {
            buffer.Internal_DestroyOnClient();
        }

        /// <summary>
        /// Compresses the data in the buffer using the Brotli compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer containing the data to compress.</param>
        /// <param name="quality">The compression quality, ranging from 0 (fastest) to 11 (slowest). Default is 1.</param>
        /// <param name="window">The Brotli sliding window size, ranging from 10 to 24. Default is 22.</param>
        /// <returns>A new buffer containing the compressed data. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        /// <exception cref="Exception">
        /// Thrown when there is no space available in the buffer acquired from the pool,
        /// or if an error occurs during compression.
        /// </exception>
        public static DataBuffer ToBrotli(this DataBuffer buffer, int quality = 1, int window = 22)
        {
            try
            {
                using BrotliCompressor compressor = new(quality, window);
                buffer.SeekToEnd();
                compressor.Write(buffer.BufferAsSpan);

                var compressedBuffer = NetworkManager.Pool.Rent(); // Disposed by the caller!
                compressor.CopyTo(compressedBuffer);
                return compressedBuffer;
            }
            catch (NotSupportedException ex)
            {
                throw new Exception(
                    $"{ex.Message} - There is no space available in the buffer acquired from the pool. Consider increasing the initial capacity of the pool."
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static void ToBrotliRaw(this DataBuffer buffer, int quality = 1, int window = 22)
        {
            using var compressedBuffer = ToBrotli(buffer, quality, window);
            buffer.SeekToBegin();
            WriteRaw(buffer, compressedBuffer.BufferAsSpan);
        }

        /// <summary>
        /// Decompresses the data in the buffer using the Brotli decompression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer containing the compressed data.</param>
        /// <returns>A new buffer containing the decompressed data. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        /// <exception cref="Exception">
        /// Thrown if an error occurs during decompression.
        /// </exception>
        public static DataBuffer FromBrotli(this DataBuffer buffer)
        {
            using BrotliDecompressor decompressor = new();
            buffer.SeekToEnd();
            var data = decompressor.Decompress(buffer.BufferAsSpan);

            var decompressedBuffer = NetworkManager.Pool.Rent();
            data.CopyTo(decompressedBuffer.GetSpan());
            decompressedBuffer.SetEndPosition((int)data.Length);
            return decompressedBuffer;
        }

        public static void FromBrotliRaw(this DataBuffer buffer)
        {
            using var decompressedBuffer = FromBrotli(buffer);
            buffer.SeekToBegin();
            WriteRaw(buffer, decompressedBuffer.Internal_GetSpan(decompressedBuffer.EndPosition));
            buffer.SeekToBegin();
        }

        /// <summary>
        /// Encrypts the data buffer using AES encryption.
        /// </summary>
        /// <param name="buffer">The data buffer to encrypt.</param>
        /// <param name="peer">The network peer used for encryption.</param>
        /// <returns>A new encrypted data buffer. The caller must ensure the buffer is disposed or used within a using statement</returns>
        public static DataBuffer Encrypt(this DataBuffer buffer, NetworkPeer peer)
        {
            buffer.SeekToEnd();
            byte[] data = buffer.ToArray();
            byte[] encryptedData = AesCryptography.Encrypt(
                data,
                0,
                data.Length,
                peer._aesKey,
                out byte[] Iv
            );

            var encryptedBuffer = NetworkManager.Pool.Rent();
            encryptedBuffer.ToBinary(Iv);
            encryptedBuffer.ToBinary(encryptedData);
            return encryptedBuffer;
        }

        public static void EncryptRaw(this DataBuffer buffer, NetworkPeer peer)
        {
            using var encryptedBuffer = Encrypt(buffer, peer);
            buffer.SeekToBegin();
            WriteRaw(buffer, encryptedBuffer.BufferAsSpan);
        }

        /// <summary>
        /// Decrypts the data buffer using AES decryption.
        /// </summary>
        /// <param name="buffer">The data buffer to decrypt.</param>
        /// <param name="peer">The network peer used for decryption.</param>
        /// <returns>A new decrypted data buffer. The caller must ensure the buffer is disposed or used within a using statement</returns>
        public static DataBuffer Decrypt(this DataBuffer buffer, NetworkPeer peer)
        {
            byte[] iv = buffer.FromBinary<byte[]>();
            byte[] encryptedData = buffer.FromBinary<byte[]>();
            byte[] decryptedData = AesCryptography.Decrypt(
                encryptedData,
                0,
                encryptedData.Length,
                peer._aesKey,
                iv
            );

            var decryptedBuffer = NetworkManager.Pool.Rent();
            decryptedBuffer.Write(decryptedData);
            decryptedBuffer.SeekToBegin();
            return decryptedBuffer;
        }

        public static void DecryptRaw(this DataBuffer buffer, NetworkPeer peer)
        {
            using var decryptedBuffer = Decrypt(buffer, peer);
            buffer.SeekToBegin();
            WriteRaw(buffer, decryptedBuffer.Internal_GetSpan(decryptedBuffer.EndPosition));
            buffer.SeekToBegin();
        }
    }

    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// The default encoding used when writing strings to the buffer.
        /// </summary>
        public static Encoding DefaultEncoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Converts an object to JSON and writes it to the buffer.<br/>
        /// By default, Newtonsoft.Json is used for serialization.
        /// </summary>
        public static string ToJson<T>(
            this DataBuffer buffer,
            T value,
            JsonSerializerSettings settings = null
        )
        {
            string json = JsonConvert.SerializeObject(value, settings);
            Write(buffer, json);
            return json;
        }

        /// <summary>
        /// Converts an object to binary and writes it to the buffer.<br/>
        /// By default, MemoryPack(https://github.com/Cysharp/MemoryPack) is used for serialization.
        /// </summary>
        public static void ToBinary<T>(
            this DataBuffer buffer,
            T value,
            MemoryPackSerializerOptions settings = null
        )
        {
            IBufferWriter<byte> writer = buffer;
            byte[] data = MemoryPackSerializer.Serialize(value, settings);
            Write7BitEncodedInt(buffer, data.Length);
            writer.Write(data);
        }

        /// <summary>
        /// Asynchronously converts an object to binary and writes it to the buffer.<br/>
        /// By default, MemoryPack(https://github.com/Cysharp/MemoryPack) is used for serialization.
        /// </summary>
        public static async void ToBinaryAsync<T>(
            this DataBuffer buffer,
            T value,
            MemoryPackSerializerOptions settings = null
        )
        {
            IBufferWriter<byte> writer = buffer;
            using MemoryStream stream = new();
            await MemoryPackSerializer.SerializeAsync(stream, value, settings);
            Write7BitEncodedInt(buffer, (int)stream.Length);
            writer.Write(stream.GetBuffer().AsSpan(0, (int)stream.Length));
        }

        /// <summary>
        /// Writes a response to the buffer, used to response any request with status code, message and data(optional).
        /// </summary>
        public static string ToResponse(this DataBuffer buffer, Response response)
        {
            return ToJson(buffer, response);
        }

        /// <summary>
        /// Writes a generic response to the buffer, used to response any request with status code, message and data(optional).
        /// </summary>
        public static string ToResponse<T>(this DataBuffer buffer, Response<T> response)
        {
            return ToJson(buffer, response);
        }

        /// <summary>
        /// Writes the raw bytes to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="data">The raw bytes to write.</param>
        public static void WriteRaw(this DataBuffer buffer, ReadOnlySpan<byte> data)
        {
            BuffersExtensions.Write(buffer, data);
        }

        /// <summary>
        /// Writes an primitive array to the buffer.<br/>
        /// </summary>
        public static void FastWrite<T>(this DataBuffer buffer, T[] array)
            where T : unmanaged
        {
            IBufferWriter<byte> writer = buffer;
            int size_t = Unsafe.SizeOf<T>() * array.Length;
            Write7BitEncodedInt(buffer, size_t);

            ReadOnlySpan<T> data = array.AsSpan();
            writer.Write(MemoryMarshal.AsBytes(data));
        }

        /// <summary>
        /// Writes a string to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// Note: May result in a StackOverflowException if the string is excessively long.
        /// </summary>
        public static int FastWrite(
            this DataBuffer buffer,
            ReadOnlySpan<char> input,
            Encoding encoding = null
        )
        {
            encoding ??= DefaultEncoding;
            IBufferWriter<byte> writer = buffer;

            // Write a header with the length of the string.
            int byteCount = encoding.GetByteCount(input);
            Write7BitEncodedInt(buffer, byteCount);

            // Write the string data.
            Span<byte> data = stackalloc byte[byteCount];
            int encodedBytesCount = encoding.GetBytes(input, data);
            writer.Write(data);
            return encodedBytesCount;
        }

        /// <summary>
        /// Writes a string to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        public static int Write(
            this DataBuffer buffer,
            ReadOnlySpan<char> input,
            Encoding encoding = null
        )
        {
            encoding ??= DefaultEncoding;
            IBufferWriter<byte> writer = buffer;

            // Write a header with the length of the string.
            int byteCount = encoding.GetByteCount(input);
            Write7BitEncodedInt(buffer, byteCount);

            // rent an array from the pool to avoid allocations.
            byte[] rentedArray = ArrayPool<byte>.Shared.Rent(byteCount);

            // Write the string data.
            Span<byte> data = rentedArray;
            int encodedBytesCount = encoding.GetBytes(input, data);
            writer.Write(data[..encodedBytesCount]);

            ArrayPool<byte>.Shared.Return(rentedArray);
            return encodedBytesCount;
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Utilizes stackalloc to avoid allocations, offering high performance.
        /// </summary>
        public static void FastWrite<T>(this DataBuffer buffer, T value)
            where T : unmanaged
        {
            IBufferWriter<byte> writer = buffer;
            int size_t = Unsafe.SizeOf<T>();

            Span<byte> data = stackalloc byte[size_t];
            MemoryMarshal.Write(data, ref value);
            writer.Write(data);
        }

        /// <summary>
        /// Writes network identity data to the buffer, most used to instantiate network objects.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="identity">The network identity to write.</param>
        internal static void Write(this DataBuffer buffer, NetworkIdentity identity)
        {
            FastWrite(buffer, identity.IdentityId);
            FastWrite(buffer, identity.Owner.Id);
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// Allocates an array from the pool to avoid allocations.
        /// </summary>
        public static void Write<T>(this DataBuffer buffer, T value)
            where T : unmanaged
        {
            IBufferWriter<byte> writer = buffer;
            int size_t = Unsafe.SizeOf<T>();

            byte[] rentedArray = ArrayPool<byte>.Shared.Rent(size_t);
            Span<byte> data = rentedArray;

            MemoryMarshal.Write(data, ref value);
            writer.Write(data[..size_t]); // sliced, because the length of the rented array is not equal to the size_t, it may be larger.

            ArrayPool<byte>.Shared.Return(rentedArray);
        }

        /// <summary>
        /// Writes an integer in a compact 7-bit encoded format to the buffer.
        /// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs#L473
        /// </summary>
        public static void Write7BitEncodedInt(this DataBuffer buffer, int value)
        {
            uint uValue = (uint)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                FastWrite(buffer, (byte)(uValue | ~0x7Fu));
                uValue >>= 7;
            }

            FastWrite(buffer, (byte)uValue);
        }

        /// <summary>
        /// Writes an long in a compact 7-bit encoded format to the buffer.
        /// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs#L492
        /// </summary>
        public static void Write7BitEncodedInt64(this DataBuffer buffer, long value)
        {
            ulong uValue = (ulong)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                FastWrite(buffer, (byte)((uint)uValue | ~0x7Fu));
                uValue >>= 7;
            }

            FastWrite(buffer, (byte)uValue);
        }
    }

    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// Reads a response to the buffer, used to response any request with status code, message and data(optional). The position is set to <c>0</c>
        /// </summary>
        public static Response FromResponse(this DataBuffer buffer, bool seekToBegin = false)
        {
            if (seekToBegin)
            {
                buffer.SeekToBegin();
            }

            return FromJson<Response>(buffer);
        }

        /// <summary>
        /// Reads a generic response to the buffer, used to response any request with status code, message and data(optional). The position is set to <c>0</c>
        /// </summary>
        public static Response<T> FromResponse<T>(this DataBuffer buffer, bool seekToBegin = false)
        {
            if (seekToBegin)
            {
                buffer.SeekToBegin();
            }

            return FromJson<Response<T>>(buffer);
        }

        /// <summary>
        /// Reads a JSON string from the buffer and converts it to an object.<br/>
        /// By default, Newtonsoft.Json is used for deserialization.
        /// </summary>
        public static T FromJson<T>(this DataBuffer buffer, JsonSerializerSettings settings = null)
        {
            string json = ReadString(buffer);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Reads a JSON string from the buffer and converts it to an object.<br/>
        /// By default, Newtonsoft.Json is used for deserialization.
        /// </summary>
        public static T FromJson<T>(
            this DataBuffer buffer,
            out string json,
            JsonSerializerSettings settings = null
        )
        {
            json = ReadString(buffer);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Reads binary data from the buffer and converts it to an object.<br/>
        /// By default, MemoryPack(https://github.com/Cysharp/MemoryPack) is used for deserialization.
        /// </summary>
        public static T FromBinary<T>(
            this DataBuffer buffer,
            MemoryPackSerializerOptions settings = null
        )
        {
            int dataSize = Read7BitEncodedInt(buffer);
            Span<byte> data = buffer.Internal_GetSpan(dataSize);
            buffer.Advance(dataSize);
            return MemoryPackSerializer.Deserialize<T>(data, settings);
        }

        /// <summary>
        /// Reads a string from the buffer.<br/>
        /// </summary>
        public static string FastReadString(this DataBuffer buffer, Encoding encoding = null)
        {
            encoding ??= DefaultEncoding;
            int byteCount = Read7BitEncodedInt(buffer);
            Span<byte> data = buffer.Internal_GetSpan(byteCount);
            buffer.Advance(byteCount);
            return encoding.GetString(data);
        }

        /// <summary>
        /// Reads a string from the buffer.<br/>
        /// Syntactic sugar for <see cref="FastReadString(DataBuffer, Encoding)"/>
        /// </summary>
        public static string ReadString(this DataBuffer buffer, Encoding encoding = null)
        {
            return FastReadString(buffer, encoding);
        }

        /// <summary>
        /// Reads a primitive array from the buffer, allocating a new array each time.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array (must be unmanaged).</typeparam>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns>A new array containing the read data.</returns>
        public static unsafe T[] ReadArray<T>(this DataBuffer buffer)
            where T : unmanaged
        {
            int size_t = Read7BitEncodedInt(buffer);
            ReadOnlySpan<byte> data = buffer.Internal_GetSpan(size_t);
            buffer.Advance(size_t);

            // Allocate a new array.
            int elementCount = size_t / Unsafe.SizeOf<T>();
            T[] array = new T[elementCount];

            // Fastest way to cast a byte[] to a T[].
            ReadOnlySpan<T> arraySpan = MemoryMarshal.Cast<byte, T>(data);
            arraySpan.CopyTo(array);
            return array;
        }

        /// <summary>
        /// Reads a primitive array from the buffer without allocating a new array(ArrayPool).
        /// </summary>
        /// <typeparam name="T">The type of elements in the array (must be unmanaged).</typeparam>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="array">The array to store the read data. It must be preallocated with the correct size.</param>
        /// <returns>The size of the array.</returns>
        public static unsafe int FastReadArray<T>(this DataBuffer buffer, T[] array)
            where T : unmanaged
        {
            int size_t = Read7BitEncodedInt(buffer);
            ReadOnlySpan<byte> data = buffer.Internal_GetSpan(size_t);
            buffer.Advance(size_t);

            // Fastest way to cast a byte[] to a T[].
            ReadOnlySpan<T> arraySpan = MemoryMarshal.Cast<byte, T>(data);
            arraySpan.CopyTo(array);
            return size_t;
        }

        /// <summary>
        /// Reads a primitive value from the buffer.
        /// </summary>
        public static T FastRead<T>(this DataBuffer buffer)
            where T : unmanaged
        {
            int size_t = Unsafe.SizeOf<T>();
            ReadOnlySpan<byte> data = buffer.Internal_GetSpan(size_t);
            buffer.Advance(size_t);
            return MemoryMarshal.Read<T>(data);
        }

        /// <summary>
        /// Reads a primitive value from the buffer.
        /// Syntactic sugar for <see cref="FastRead{T}(DataBuffer)"/>
        /// </summary>
        public static T Read<T>(this DataBuffer buffer)
            where T : unmanaged
        {
            return FastRead<T>(buffer);
        }

        internal static void ReadIdentityData(
            this DataBuffer buffer,
            out int identityId,
            out int peerId
        )
        {
            identityId = FastRead<int>(buffer);
            peerId = FastRead<int>(buffer);
        }

        /// <summary>
        /// Reads a 32-bit signed integer in a compressed format(7-bit encoded) from the buffer.
        /// </summary>
        /// <returns>The 32-bit signed integer read from the stream.</returns>
        public static int Read7BitEncodedInt(this DataBuffer buffer)
        {
            // Unlike writing, we can't delegate to the 64-bit read on
            // 64-bit platforms. The reason for this is that we want to
            // stop consuming bytes if we encounter an integer overflow.

            uint result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 5 bytes,
            // or the fifth byte is about to cause integer overflow.
            // This means that we can read the first 4 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 4;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = FastRead<byte>(buffer);
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int)result; // early exit
                }
            }

            // Read the 5th byte. Since we already read 28 bits,
            // the value of this byte must fit within 4 bits (32 - 28),
            // and it must not have the high bit set.

            byteReadJustNow = FastRead<byte>(buffer);
            if (byteReadJustNow > 0b_1111u)
            {
                throw new FormatException("SR.Format_Bad7BitInt");
            }

            result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (int)result;
        }

        /// <summary>
        /// Reads a 64-bit signed integer in a compressed format(7-bit encoded) from the buffer.
        /// </summary>
        /// <returns>The 64-bit signed integer read from the stream.</returns>
        public static long Read7BitEncodedInt64(this DataBuffer buffer)
        {
            ulong result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 10 bytes,
            // or the tenth byte is about to cause integer overflow.
            // This means that we can read the first 9 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 9;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = FastRead<byte>(buffer);
                result |= (byteReadJustNow & 0x7Ful) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (long)result; // early exit
                }
            }

            // Read the 10th byte. Since we already read 63 bits,
            // the value of this byte must fit within 1 bit (64 - 63),
            // and it must not have the high bit set.

            byteReadJustNow = FastRead<byte>(buffer);
            if (byteReadJustNow > 0b_1u)
            {
                throw new FormatException("SR.Format_Bad7BitInt");
            }

            result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (long)result;
        }
    }
}
