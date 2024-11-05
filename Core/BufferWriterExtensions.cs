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
            encryptedBuffer.WriteAsBinary(Iv);
            encryptedBuffer.WriteAsBinary(encryptedData);
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
            byte[] iv = buffer.ReadAsBinary<byte[]>();
            byte[] encryptedData = buffer.ReadAsBinary<byte[]>();
            byte[] decryptedData = AesCryptography.Decrypt(
                encryptedData,
                0,
                encryptedData.Length,
                peer._aesKey,
                iv
            );

            var decryptedBuffer = NetworkManager.Pool.Rent();
            BuffersExtensions.Write(decryptedBuffer, decryptedData);
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

        /// <summary>
        /// Serializes the given network serializable object into a new data buffer.
        /// </summary>
        /// <returns>
        /// A new data buffer containing the serialized data. The caller must ensure the buffer is disposed or used within a using statement.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer Serialize(this IMessage message)
        {
            var writer = NetworkManager.Pool.Rent();
            message.Serialize(writer);
            return writer;
        }

        /// <summary>
        /// Deserializes the contents of the given data buffer into the given network serializable object.
        /// </summary>
        /// <param name="reader">The data buffer containing the serialized data to deserialize.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Populate(this IMessage message, DataBuffer reader)
        {
            message.Deserialize(reader);
        }

        /// <summary>
        /// Deserializes the contents of the given data buffer into a new instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the message to deserialize. Must be a network serializable object.</typeparam>
        /// <returns>The deserialized message.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(this DataBuffer reader)
            where T : IMessage, new()
        {
            T message = new();
            message.Deserialize(reader);
            return message;
        }

        /// <summary>
        /// Deserializes the contents of the given data buffer into a new instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the message to deserialize. Must be a network serializable object.</typeparam>
        /// <returns>The deserialized message.</returns>
        public static T Deserialize<T>(this DataBuffer reader, NetworkPeer peer, bool isServer)
            where T : IMessageWithPeer, new()
        {
            T message = new() { SharedPeer = peer, IsServer = isServer };
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
        /// Use binary serialization for some types by default. Useful for types like as <see cref="HttpResponse"/>
        /// </summary>
        public static bool UseBinarySerialization = false;

        /// <summary>
        /// Enable bandwidth optimization for data sent over the network.
        /// </summary>
        public static bool EnableBandwidthOptimization { get; set; } = true;

        /// <summary>
        /// Determines whether to use unaligned memory access when writing primitive types to the buffer.
        /// </summary>
        /// <remarks>
        /// This property is set to true by default.
        /// When set to true, the buffer writer will use unaligned memory access, which can improve performance
        /// on some platforms. However, it may be slower on others.
        /// </remarks>
        public static bool UseUnalignedMemory { get; set; } = false;

        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/MemoryMarshal.cs#L507
        /// <summary>
        /// Writes a structure of type T into a span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Internal_UnsafeWrite<T>(DataBuffer buffer, in T value)
            where T : unmanaged
        {
            int size_t = sizeof(T);
            Span<byte> destination = buffer.Internal_GetSpan(size_t);
#if OMNI_DEBUG
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                throw new InvalidOperationException(
                    "The type T is either a reference type or contains references, which is not supported."
                );
            }

            if (size_t > (uint)destination.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(destination),
                    "The size of type T exceeds the length of the destination span."
                );
            }
#endif
            fixed (byte* p = destination)
            {
                // When useUnalignedMemory is true, write the value to memory without assuming alignment.
                if (UseUnalignedMemory)
                {
                    // Unsafe.WriteUnaligned is used to write the value to an unaligned memory location.
                    // Advantages:
                    // - Flexibility: Allows writing data to memory locations that are not aligned to the natural alignment of the data type.
                    //   Useful in scenarios where data alignment cannot be guaranteed, such as certain network protocols or binary file formats.
                    // - Compatibility: Ensures the code works correctly on systems where memory might not be aligned, preventing crashes or unexpected results.
                    // Disadvantages:
                    // - Performance: Unaligned memory operations can be slower, especially on CPU architectures that penalize unaligned accesses.
                    //   May result in multiple memory reads/writes to handle the misalignment.
                    // - Complexity: Increases code complexity when dealing with data that doesn't follow normal alignment rules.
                    Unsafe.WriteUnaligned(p, value);
                }
                else
                {
                    // When useUnalignedMemory is false, write the value assuming it is aligned in memory.
                    // Unsafe.Write is used to write the value to an aligned memory location.
                    // Advantages:
                    // - Performance: Aligned memory operations are generally faster, as most CPU architectures optimize for aligned accesses.
                    //   Results in faster and more efficient memory reads/writes.
                    // - Simplicity: Assumes memory is aligned, which can simplify code reasoning and maintenance as long as the memory is actually aligned.
                    // Disadvantages:
                    // - Alignment Restrictions: Requires memory to be aligned to the natural boundaries of the data type.
                    //   Misaligned memory can cause exceptions or undefined behavior.
                    // - Incompatibility: May not be suitable for scenarios where alignment cannot be guaranteed.
                    Unsafe.Write(p, value);
                }
            }

            buffer.Advance(size_t);
        }

        // Internal use for bandwidth optimization
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Internal_Write(this DataBuffer buffer, int value)
        {
            if (!EnableBandwidthOptimization)
            {
                Write(buffer, value);
            }
            else
            {
                Write7BitEncodedInt(buffer, value);
            }
        }

        /// <summary>
        /// Converts an object to JSON and writes it to the buffer.<br/>
        /// By default, Newtonsoft.Json is used for serialization.
        /// </summary>
        public static string WriteAsJson<T>(
            this DataBuffer buffer,
            T value,
            JsonSerializerSettings settings = null
        )
        {
            settings ??= DefaultJsonSettings;
            string objectAsJson = JsonConvert.SerializeObject(value, settings);
            WriteString(buffer, objectAsJson);
            return objectAsJson;
        }

        /// <summary>
        /// Converts an object to binary and writes it to the buffer.<br/>
        /// By default, MemoryPack(https://github.com/Cysharp/MemoryPack) is used for serialization.
        /// </summary>
        public static void WriteAsBinary<T>(
            this DataBuffer buffer,
            T value,
            MemoryPackSerializerOptions settings = null
        )
        {
            settings ??= DefaultMemoryPackSettings;
            ReadOnlySpan<byte> data = MemoryPackSerializer.Serialize(value, settings);
            Write7BitEncodedInt(buffer, data.Length);
            buffer.Write(data);
        }

        /// <summary>
        /// Asynchronously converts an object to binary and writes it to the buffer.<br/>
        /// By default, MemoryPack(https://github.com/Cysharp/MemoryPack) is used for serialization.
        /// </summary>
        public static async ValueTask WriteAsBinaryAsync<T>(
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
        /// Writes an HTTP response to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="response">The HTTP response to write.</param>
        public static void WriteHttpResponse(this DataBuffer buffer, HttpResponse response)
        {
            if (!UseBinarySerialization)
            {
                WriteAsJson(buffer, response);
                return;
            }

            WriteAsBinary(buffer, response);
        }

        /// <summary>
        /// Writes an HTTP response with a generic payload to the buffer.
        /// </summary>
        /// <typeparam name="T">The type of the payload.</typeparam>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="response">The HTTP response with the generic payload to write.</param>
        public static void WriteHttpResponse<T>(this DataBuffer buffer, HttpResponse<T> response)
        {
            if (!UseBinarySerialization)
            {
                WriteAsJson(buffer, response);
                return;
            }

            WriteAsBinary(buffer, response);
        }

        /// <summary>
        /// Writes the raw bytes to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="data">The raw bytes to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RawWrite(this DataBuffer buffer, ReadOnlySpan<byte> data)
        {
            BuffersExtensions.Write(buffer, data);
        }

        /// <summary>
        /// Writes an primitive array to the buffer.<br/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(this DataBuffer buffer, T[] array)
            where T : unmanaged
        {
            int size_t = sizeof(T) * array.Length;
            Write7BitEncodedInt(buffer, size_t);

            ReadOnlySpan<T> data = array.AsSpan();
            buffer.Write(MemoryMarshal.AsBytes(data));
        }

        /// <summary>
        /// Writes a string to the buffer.
        /// </summary>
        public static void WriteString(
            this DataBuffer buffer,
            ReadOnlySpan<char> input,
            Encoding encoding = null
        )
        {
            encoding ??= DefaultEncoding;
            // Write a header with the length of the string.
            int byteCount = encoding.GetByteCount(input);
            Write7BitEncodedInt(buffer, byteCount);

            // Write the string data.
            Span<byte> data = buffer.Internal_GetSpan(byteCount);
            int length = encoding.GetBytes(input, data);
            buffer.Advance(length);
        }

        /// <summary>
        /// Writes a primitive value to the buffer.<br/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this DataBuffer buffer, in T value)
            where T : unmanaged
        {
            Internal_UnsafeWrite(buffer, in value);
        }

        /// <summary>
        /// Writes network identity data to the buffer, most used to instantiate network objects.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        /// <param name="identity">The network identity to write.</param>
        public static void WriteIdentity(this DataBuffer buffer, NetworkIdentity identity)
        {
            Write(buffer, identity.IdentityId);
            Write(buffer, identity.Owner.Id);
        }

        /// <summary>
        /// Writes network identity data to the buffer, most used to instantiate network objects.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        public static void WriteIdentity(this DataBuffer buffer, int identityId, int peerId)
        {
            Write(buffer, identityId);
            Write(buffer, peerId);
        }

        /// <summary>
        /// Writes an integer in a compact 7-bit encoded format to the buffer.
        /// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs#L473
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                Write(buffer, (byte)(uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write(buffer, (byte)uValue);
        }

        /// <summary>
        /// Writes an long in a compact 7-bit encoded format to the buffer.
        /// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs#L492
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                Write(buffer, (byte)((uint)uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write(buffer, (byte)uValue);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WritePacked(this DataBuffer buffer, Vector3 vector)
        {
            long packedValue = VectorCompressor.Compress(vector);
            Write(buffer, packedValue);
        }

        /// <summary>
        /// Writes a compressed representation of a Quaternion to the DataBuffer.
        /// This method reduces the size of the data by compressing the Quaternion before writing,
        /// resulting in significant bandwidth savings during data transmission.
        /// The compressed size of the Quaternion is 4 bytes (uint).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WritePacked(this DataBuffer buffer, Quaternion quat)
        {
            uint packedValue = QuaternionCompressor.Compress(quat);
            Write(buffer, packedValue);
        }
    }

    // Readers
    public static partial class BufferWriterExtensions
    {
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/MemoryMarshal.cs#L468
        /// <summary>
        /// Reads a structure of type T out of a read-only span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe T Internal_UnsafeRead<T>(DataBuffer buffer)
            where T : unmanaged
        {
            int size_t = sizeof(T);
            Span<byte> source = buffer.Internal_GetSpan(size_t);
#if OMNI_DEBUG
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                throw new InvalidOperationException(
                    "The type T is either a reference type or contains references, which is not supported."
                );
            }
            if (size_t > source.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(source),
                    "The size of type T exceeds the length of the source span."
                );
            }
#endif
            buffer.Internal_Advance(size_t);
            fixed (byte* p = source)
            {
                // If useUnalignedMemory is true, read the value from memory without assuming alignment.
                if (UseUnalignedMemory)
                {
                    // Unsafe.ReadUnaligned is used to read the value from an unaligned memory location.
                    // Advantages:
                    // - Flexibility: Allows reading data from memory locations that are not aligned to the natural alignment of the data type.
                    //   Useful in scenarios where data alignment cannot be guaranteed, such as certain network protocols or binary file formats.
                    // - Compatibility: Ensures the code works correctly on systems where memory might not be aligned, preventing crashes or unexpected results.
                    // Disadvantages:
                    // - Performance: Unaligned memory operations can be slower, especially on CPU architectures that penalize unaligned accesses.
                    //   May result in multiple memory reads/writes to handle the misalignment.
                    // - Complexity: Increases code complexity when dealing with data that doesn't follow normal alignment rules.
                    return Unsafe.ReadUnaligned<T>(p);
                }
                else
                {
                    // If useUnalignedMemory is false, read the value assuming it is aligned in memory.
                    // Unsafe.Read is used to read the value from an aligned memory location.
                    // Advantages:
                    // - Performance: Aligned memory operations are generally faster, as most CPU architectures optimize for aligned accesses.
                    //   Results in faster and more efficient memory reads/writes.
                    // - Simplicity: Assumes memory is aligned, which can simplify code reasoning and maintenance as long as the memory is actually aligned.
                    // Disadvantages:
                    // - Alignment Restrictions: Requires memory to be aligned to the natural boundaries of the data type.
                    //   Misaligned memory can cause exceptions or undefined behavior.
                    // - Incompatibility: May not be suitable for scenarios where alignment cannot be guaranteed.
                    return Unsafe.Read<T>(p);
                }
            }
        }

        // Internal use for bandwidth optimization
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Internal_Read(this DataBuffer buffer)
        {
            if (!EnableBandwidthOptimization)
            {
                return Read<int>(buffer);
            }
            else
            {
                return Read7BitEncodedInt(buffer);
            }
        }

        /// <summary>
        /// Reads an HTTP response from the DataBuffer.
        /// </summary>
        /// <returns>The deserialized HTTP response.</returns>
        public static HttpResponse ReadHttpResponse(this DataBuffer buffer)
        {
            if (!UseBinarySerialization)
            {
                return ReadAsJson<HttpResponse>(buffer);
            }

            return ReadAsBinary<HttpResponse>(buffer);
        }

        /// <summary>
        /// Reads an HTTP response with a generic payload from the DataBuffer.
        /// </summary>
        /// <typeparam name="T">The type of the payload.</typeparam>
        /// <returns>The deserialized HTTP response.</returns>
        public static HttpResponse<T> ReadHttpResponse<T>(this DataBuffer buffer)
        {
            if (!UseBinarySerialization)
            {
                return ReadAsJson<HttpResponse<T>>(buffer);
            }

            return ReadAsBinary<HttpResponse<T>>(buffer);
        }

        /// <summary>
        /// Reads a JSON string from the buffer and converts it to an object.<br/>
        /// By default, Newtonsoft.Json is used for deserialization.
        /// </summary>
        public static T ReadAsJson<T>(
            this DataBuffer buffer,
            JsonSerializerSettings settings = null
        )
        {
            settings ??= DefaultJsonSettings;
            string json = ReadString(buffer);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Reads a JSON string from the buffer and converts it to an object.<br/>
        /// By default, Newtonsoft.Json is used for deserialization.
        /// </summary>
        public static T ReadAsJson<T>(
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
        public static T ReadAsBinary<T>(
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
        public static string ReadString(this DataBuffer buffer, Encoding encoding = null)
        {
            encoding ??= DefaultEncoding;
            int byteCount = Read7BitEncodedInt(buffer);
            Span<byte> data = buffer.Internal_GetSpan(byteCount);
            buffer.Internal_Advance(byteCount);
            return encoding.GetString(data);
        }

        /// <summary>
        /// Reads a primitive array from the buffer, allocating a new array each time.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array (must be unmanaged).</typeparam>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns>A new array containing the read data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T[] ReadArray<T>(this DataBuffer buffer)
            where T : unmanaged
        {
            int size_t = Read7BitEncodedInt(buffer);
            ReadOnlySpan<byte> data = buffer.Internal_GetSpan(size_t);
            buffer.Internal_Advance(size_t);

            // Allocate a new array.
            int elementCount = size_t / sizeof(T);
            T[] array = new T[elementCount];

            // Fastest way to cast a byte[] to a T[].
            ReadOnlySpan<T> castedArray = MemoryMarshal.Cast<byte, T>(data);
            castedArray.CopyTo(array);
            return array;
        }

        /// <summary>
        /// Reads a primitive array from the buffer without allocating a new array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array (must be unmanaged).</typeparam>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="array">The array to store the read data. It must be preallocated with the correct size.</param>
        /// <returns>The size of the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ReadArray<T>(this DataBuffer buffer, T[] array)
            where T : unmanaged
        {
            int size_t = Read7BitEncodedInt(buffer);
            ReadOnlySpan<byte> data = buffer.Internal_GetSpan(size_t);
            buffer.Internal_Advance(size_t);

            // Fastest way to cast a byte[] to a T[].
            ReadOnlySpan<T> castedArray = MemoryMarshal.Cast<byte, T>(data);
            castedArray.CopyTo(array);
            return size_t;
        }

        /// <summary>
        /// Reads a primitive value from the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(this DataBuffer buffer)
            where T : unmanaged
        {
            return Internal_UnsafeRead<T>(buffer);
        }

        public static void ReadIdentity(this DataBuffer buffer, out int peerId, out int identityId)
        {
            identityId = Read<int>(buffer);
            peerId = Read<int>(buffer);
        }

        /// <summary>
        /// Reads a 32-bit signed integer in a compressed format(7-bit encoded) from the buffer.
        /// </summary>
        /// <returns>The 32-bit signed integer read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                byteReadJustNow = Read<byte>(buffer);
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int)result; // early exit
                }
            }

            // Read the 5th byte. Since we already read 28 bits,
            // the value of this byte must fit within 4 bits (32 - 28),
            // and it must not have the high bit set.

            byteReadJustNow = Read<byte>(buffer);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                byteReadJustNow = Read<byte>(buffer);
                result |= (byteReadJustNow & 0x7Ful) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (long)result; // early exit
                }
            }

            // Read the 10th byte. Since we already read 63 bits,
            // the value of this byte must fit within 1 bit (64 - 63),
            // and it must not have the high bit set.

            byteReadJustNow = Read<byte>(buffer);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadPackedVector3(this DataBuffer buffer)
        {
            long packedValue = Read<long>(buffer);
            return VectorCompressor.Decompress(packedValue);
        }

        /// <summary>
        /// Reads and decompresses a Quaternion from the DataBuffer.
        /// This method expects the data to be in the compressed format written by WritePacked.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ReadPackedQuaternion(this DataBuffer buffer)
        {
            uint packedValue = Read<uint>(buffer);
            return QuaternionCompressor.Decompress(packedValue);
        }
    }

    // Syntactic sugar
    public static partial class BufferWriterExtensions
    {
        // nothing, i finish this
    }
}
