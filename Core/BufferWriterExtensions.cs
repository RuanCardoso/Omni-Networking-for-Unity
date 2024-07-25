using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;
using MemoryPack.Compression;
using Newtonsoft.Json;
using Omni.Core.Cryptography;
using UnityEngine;

namespace Omni.Core
{
    public enum BrotliCompressionLevel
    {
        UltraFast = 1, // Ultra Rápido
        VeryFast = 2, // Muito Rápido
        Fast = 3, // Rápido
        ModeratelyFast = 4, // Moderadamente Rápido
        Balanced = 5, // Balanceado
        ModeratelySlow = 6, // Moderadamente Lento
        Slow = 7, // Lento
        VerySlow = 8, // Muito Lento
        UltraSlow = 9, // Ultra Lento
        MaxCompression = 10, // Compressão Máxima
        ExtremeCompression = 11 // Compressão Extrema
    }

    // Global
    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// Compresses the data in the buffer using the Brotli compression algorithm.
        /// </summary>
        /// <param name="data">The buffer containing the data to compress.</param>
        /// <param name="window">The Brotli sliding window size, ranging from 10 to 24. Default is 22.</param>
        /// <returns>A new buffer containing the compressed data. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Compress(
            this DataBuffer data,
            BrotliCompressionLevel level = BrotliCompressionLevel.UltraFast,
            int window = 22
        )
        {
            try
            {
                data.SeekToEnd();
                using BrotliCompressor compressor = new((int)level, window);
                compressor.Write(data.BufferAsSpan);

                var compressedBuffer = NetworkManager.Pool.Rent(); // Disposed by the caller!
                compressor.CopyTo(compressedBuffer); // IBufferWriter<byte> implementation
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

        /// <summary>
        /// Compresses the data in the current buffer using the Brotli compression algorithm.
        /// </summary>
        /// <param name="data">The buffer containing the data to compress.</param>
        /// <param name="window">The Brotli sliding window size, ranging from 10 to 24. Default is 22.</param>
        public static void CompressRaw(
            this DataBuffer data,
            BrotliCompressionLevel level = BrotliCompressionLevel.UltraFast,
            int window = 22
        )
        {
            using var compressedBuffer = Compress(data, level, window);
            data.Reset();
            data.Write(compressedBuffer.BufferAsSpan);
        }

        /// <summary>
        /// Decompresses the data in the buffer using the Brotli decompression algorithm.
        /// </summary>
        /// <param name="data">The buffer containing the compressed data.</param>
        /// <returns>A new buffer containing the decompressed data. The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer Decompress(this DataBuffer data)
        {
            data.SeekToEnd();
            using BrotliDecompressor decompressor = new();
            var sequenceData = decompressor.Decompress(data.BufferAsSpan);

            var length = (int)sequenceData.Length;
            var decompressedBuffer = NetworkManager.Pool.Rent();

            sequenceData.CopyTo(decompressedBuffer.Internal_GetSpan(length));
            decompressedBuffer.SetLength(length);
            decompressedBuffer.SetEndPosition(length);
            return decompressedBuffer;
        }

        /// <summary>
        /// Decompresses the data in the current buffer using the Brotli decompression algorithm.
        /// </summary>
        /// <param name="data">The buffer containing the compressed data.</param>
        public static void DecompressRaw(this DataBuffer data)
        {
            using var decompressedBuffer = Decompress(data);
            data.Reset();
            data.Write(decompressedBuffer.Internal_GetSpan(decompressedBuffer.EndPosition));
            data.SeekToBegin();
        }

        /// <summary>
        /// Encrypts the data in the buffer using AES encryption.
        /// </summary>
        /// <param name="buffer">The data buffer to encrypt.</param>
        /// <param name="peer">The network peer used for encryption.</param>
        /// <returns>A new encrypted data buffer. The caller must ensure the buffer is disposed or used within a using statement</returns>
        public static DataBuffer Encrypt(this DataBuffer buffer, NetworkPeer peer)
        {
            peer ??= NetworkManager.LocalPeer;
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

        /// <summary>
        /// Encrypts the data in the current buffer using AES encryption.
        /// </summary>
        /// <param name="buffer">The data buffer to encrypt.</param>
        /// <param name="peer">The network peer used for encryption.</param>
        public static void EncryptRaw(this DataBuffer buffer, NetworkPeer peer)
        {
            using var encryptedBuffer = Encrypt(buffer, peer);
            buffer.Reset();
            buffer.Write(encryptedBuffer.BufferAsSpan);
        }

        /// <summary>
        /// Decrypts the data in the buffer using AES decryption.
        /// </summary>
        /// <param name="buffer">The data buffer to decrypt.</param>
        /// <param name="peer">The network peer used for decryption.</param>
        /// <returns>A new decrypted data buffer. The caller must ensure the buffer is disposed or used within a using statement</returns>
        public static DataBuffer Decrypt(this DataBuffer buffer, NetworkPeer peer)
        {
            peer ??= NetworkManager.LocalPeer;
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

        /// <summary>
        /// Decrypts the data in the current buffer using AES decryption.
        /// </summary>
        /// <param name="buffer">The data buffer to decrypt.</param>
        /// <param name="peer">The network peer used for decryption.</param>
        /// <returns>A new decrypted data buffer. The caller must ensure the buffer is disposed or used within a using statement</returns>
        public static void DecryptRaw(this DataBuffer buffer, NetworkPeer peer)
        {
            using var decryptedBuffer = Decrypt(buffer, peer);
            buffer.Reset();
            buffer.Write(decryptedBuffer.Internal_GetSpan(decryptedBuffer.EndPosition));
            buffer.SeekToBegin();
        }

        private static void SetNetworkSerializableOptions(
            ISerializable message,
            bool isServer,
            NetworkPeer peer
        )
        {
            if (message is ISerializableWithPeer withPeer)
            {
                withPeer.Peer = peer;
                withPeer.IsServer = isServer;
            }
        }

        /// <summary>
        /// Serializes the given network serializable object into a new data buffer.
        /// </summary>
        /// <returns>
        /// A new data buffer containing the serialized data. The caller must ensure the buffer is disposed or used within a using statement.
        /// </returns>
        public static DataBuffer Serialize(
            this ISerializable message,
            bool isServer = false,
            NetworkPeer peer = null
        )
        {
            var writer = NetworkManager.Pool.Rent();
            SetNetworkSerializableOptions(message, isServer, peer);
            message.Serialize(writer);
            return writer;
        }

        /// <summary>
        /// Deserializes the contents of the given data buffer into the given network serializable object.
        /// </summary>
        /// <param name="reader">The data buffer containing the serialized data to deserialize.</param>
        public static void Populate(
            this ISerializable message,
            DataBuffer reader,
            bool isServer = false,
            NetworkPeer peer = null
        )
        {
            SetNetworkSerializableOptions(message, isServer, peer);
            message.Deserialize(reader);
        }

        /// <summary>
        /// Deserializes the contents of the given data buffer into a new instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the message to deserialize. Must be a network serializable object.</typeparam>
        /// <returns>The deserialized message.</returns>
        public static T Deserialize<T>(
            this DataBuffer reader,
            bool isServer = false,
            NetworkPeer peer = null
        )
            where T : ISerializable, new()
        {
            T message = new();
            SetNetworkSerializableOptions(message, isServer, peer);
            message.Deserialize(reader);
            return message;
        }
    }

    // Writers
    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// The default encoding used when writing strings to the buffer.
        /// </summary>
        public static Encoding DefaultEncoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// The default JSON serializer settings used when serializing or deserializing objects.
        /// </summary>
        public static JsonSerializerSettings DefaultJsonSettings { get; set; } =
            new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new HalfJsonConverter() },
            };

        /// <summary>
        /// The default settings used when serializing or deserializing objects using MemoryPack.
        /// </summary>
        public static MemoryPackSerializerOptions DefaultMemoryPackSettings { get; set; } =
            MemoryPackSerializerOptions.Default;

        /// <summary>
        /// Use binary serialization for some types by default. Useful for types like as <see cref="ApiResponse"/>
        /// </summary>
        public static bool UseBinarySerialization = false;

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
            settings ??= DefaultJsonSettings;
            string json = JsonConvert.SerializeObject(value, settings);
            // The json string returned may be very large, so avoid using FastWrite which uses "stackalloc".
            // Could cause a stack overflow.
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
            settings ??= DefaultMemoryPackSettings;
            IBufferWriter<byte> writer = buffer;
            byte[] data = MemoryPackSerializer.Serialize(value, settings);
            Write7BitEncodedInt(buffer, data.Length);
            writer.Write(data);
        }

        /// <summary>
        /// Asynchronously converts an object to binary and writes it to the buffer.<br/>
        /// By default, MemoryPack(https://github.com/Cysharp/MemoryPack) is used for serialization.
        /// </summary>
        public static async ValueTask ToBinaryAsync<T>(
            this DataBuffer buffer,
            T value,
            MemoryPackSerializerOptions settings = null
        )
        {
            settings ??= DefaultMemoryPackSettings;
            IBufferWriter<byte> writer = buffer;

            using MemoryStream stream = new();
            await MemoryPackSerializer.SerializeAsync(stream, value, settings);

            int length = (int)stream.Length;
            Write7BitEncodedInt(buffer, length);
            writer.Write(stream.GetBuffer().AsSpan(0, length));
        }

        /// <summary>
        /// Writes a response to the buffer, used to response any request with status code, message and data(optional).
        /// </summary>
        public static void ToApiResponse(this DataBuffer buffer, ApiResponse response)
        {
            if (!UseBinarySerialization)
            {
                ToJson(buffer, response);
            }
            else
            {
                ToBinary(buffer, response);
            }
        }

        /// <summary>
        /// Writes a generic response to the buffer, used to response any request with status code, message and data(optional).
        /// </summary>
        public static void ToApiResponse<T>(this DataBuffer buffer, ApiResponse<T> response)
        {
            if (!UseBinarySerialization)
            {
                ToJson(buffer, response);
            }
            else
            {
                ToBinary(buffer, response);
            }
        }

        /// <summary>
        /// Writes the raw bytes to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="data">The raw bytes to write.</param>
        public static void RawWrite(this DataBuffer buffer, ReadOnlySpan<byte> data)
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
        public static void WriteIdentity(this DataBuffer buffer, NetworkIdentity identity)
        {
            FastWrite(buffer, identity.IdentityId);
            FastWrite(buffer, identity.Owner.Id);
        }

        /// <summary>
        /// Writes network identity data to the buffer, most used to instantiate network objects.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        public static void WriteIdentity(this DataBuffer buffer, int identityId, int peerId)
        {
            FastWrite(buffer, identityId);
            FastWrite(buffer, peerId);
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

        /// <summary>
        /// Writes a compressed representation of a Vector3 to the DataBuffer.
        /// This method reduces the size of the data by compressing the Vector3 before writing,
        /// resulting in significant bandwidth savings during data transmission.
        /// The compressed size of the Vector3 is 8 bytes (long).<br/><br/>
        /// Min/Max Values (X / Y / Z)<br/>
        /// Min Values: -9999.99f / -9999.99f / -9999.99f<br/>
        /// Max Values: 9999.99f / 9999.99f / 9999.99f<br/>
        /// </summary>
        public static void WritePacked(this DataBuffer buffer, Vector3 vector)
        {
            long packedValue = VectorCompressor.Compress(vector);
            FastWrite(buffer, packedValue);
        }

        /// <summary>
        /// Writes a compressed representation of a Quaternion to the DataBuffer.
        /// This method reduces the size of the data by compressing the Quaternion before writing,
        /// resulting in significant bandwidth savings during data transmission.
        /// The compressed size of the Quaternion is 4 bytes (uint).
        /// </summary>
        public static void WritePacked(this DataBuffer buffer, Quaternion quat)
        {
            uint packedValue = QuaternionCompressor.Compress(quat);
            FastWrite(buffer, packedValue);
        }
    }

    // Readers
    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// Reads and deserializes an API response from the given data buffer. This method is used to parse a response that includes a status code, a message, and optionally, data of type <see cref="ApiResponse"/>.
        /// </summary>
        /// <param name="buffer">The data buffer containing the serialized API response.</param>
        /// <returns>An instance of <see cref="ApiResponse"/> representing the deserialized response.</returns>
        public static ApiResponse FromApiResponse(this DataBuffer buffer)
        {
            if (!UseBinarySerialization)
                return FromJson<ApiResponse>(buffer);

            return FromBinary<ApiResponse>(buffer);
        }

        /// <summary>
        /// Reads and deserializes a generic API response from the given data buffer. This method is used to parse a response that includes a status code, a message, and optionally, data of type <typeparamref name="T"/>. The data is deserialized into an instance of <see cref="ApiResponse{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the data included in the API response.</typeparam>
        /// <param name="buffer">The data buffer containing the serialized API response.</param>
        /// <returns>An instance of <see cref="ApiResponse{T}"/> representing the deserialized response with data of type <typeparamref name="T"/>.</returns>
        public static ApiResponse<T> FromApiResponse<T>(this DataBuffer buffer)
        {
            if (!UseBinarySerialization)
                return FromJson<ApiResponse<T>>(buffer);

            return FromBinary<ApiResponse<T>>(buffer);
        }

        /// <summary>
        /// Reads a JSON string from the buffer and converts it to an object.<br/>
        /// By default, Newtonsoft.Json is used for deserialization.
        /// </summary>
        public static T FromJson<T>(this DataBuffer buffer, JsonSerializerSettings settings = null)
        {
            settings ??= DefaultJsonSettings;
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
            settings ??= DefaultJsonSettings;
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
            settings ??= DefaultMemoryPackSettings;
            int dataSize = Read7BitEncodedInt(buffer);
            Span<byte> data = buffer.Internal_GetSpan(dataSize);
            buffer.Internal_Advance(dataSize);
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
            buffer.Internal_Advance(byteCount);
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
            buffer.Internal_Advance(size_t);

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
            buffer.Internal_Advance(size_t);

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
            buffer.Internal_Advance(size_t);
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

        public static void ReadIdentity(this DataBuffer buffer, out int peerId, out int identityId)
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

        /// <summary>
        /// Reads and decompresses a Vector3 from the DataBuffer.
        /// This method expects the data to be in the compressed format written by WritePacked.
        /// </summary>
        public static Vector3 ReadPackedVector3(this DataBuffer buffer)
        {
            long packedValue = FastRead<long>(buffer);
            return VectorCompressor.Decompress(packedValue);
        }

        /// <summary>
        /// Reads and decompresses a Quaternion from the DataBuffer.
        /// This method expects the data to be in the compressed format written by WritePacked.
        /// </summary>
        public static Quaternion ReadPackedQuaternion(this DataBuffer buffer)
        {
            uint packedValue = FastRead<uint>(buffer);
            return QuaternionCompressor.Decompress(packedValue);
        }
    }

    // Syntactic sugar
    public static partial class BufferWriterExtensions
    {
        /// <returns>The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer ToApiResponse<T>(
            this ApiResponse<T> data,
            ResponseStatusCode statusCode,
            string statusMessage = ""
        )
        {
            var message = NetworkManager.Pool.Rent(); // disposed by the caller
            data.StatusCode = statusCode;
            data.StatusMessage = statusMessage;
            message.ToApiResponse(data);
            return message;
        }

        /// <returns>The caller must ensure the buffer is disposed or used within a using statement.</returns>
        public static DataBuffer ToApiResponse(
            this string statusMessage,
            ResponseStatusCode statusCode
        )
        {
            var message = NetworkManager.Pool.Rent(); // disposed by the caller
            message.ToApiResponse(
                new ApiResponse() { StatusCode = statusCode, StatusMessage = statusMessage }
            );

            return message;
        }
    }
}
