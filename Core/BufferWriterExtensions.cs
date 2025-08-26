using MemoryPack;
using MemoryPack.Compression;
using Newtonsoft.Json;
using Omni.Core.Cryptography;
using Omni.Shared;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Omni.Core
{
    /// <summary>
    /// Represents the various levels of Brotli compression that can be applied.
    /// </summary>
    public enum BrotliCompressionLevel
    {
        /// <summary>
        /// Ultra-fast compression level. Prioritizes speed over compression efficiency, achieving minimal compression.
        /// </summary>
        UltraFast = 1,

        /// <summary>
        /// Very fast compression level. Offers a good balance between speed and compression efficiency.
        /// </summary>
        VeryFast = 2,

        /// <summary>
        /// Fast compression level. Offers a balance between speed and compression ratio, favoring quick execution.
        /// </summary>
        Fast = 3,

        /// <summary>
        /// Moderately fast compression level. Provides a balance between speed and compression efficiency.
        /// </summary>
        ModeratelyFast = 4,

        /// <summary>
        /// Balanced compression level. Offers a compromise between compression ratio and speed.
        /// </summary>
        Balanced = 5,

        /// <summary>
        /// Moderately slow compression level. Provides a balanced trade-off between speed and compression ratio, leaning towards better compression.
        /// </summary>
        ModeratelySlow = 6,

        /// <summary>
        /// Slow compression level. Achieves a balance favoring compression ratio over speed.
        /// </summary>
        Slow = 7,

        /// <summary>
        /// Very slow compression level. Prioritizes compression ratio significantly over speed.
        /// </summary>
        VerySlow = 8,

        /// <summary>
        /// Ultra-slow compression level. Achieves maximum compression ratio at the cost of speed.
        /// </summary>
        UltraSlow = 9,

        /// <summary>
        /// Maximum compression level available. Prioritizes highest compression ratio over speed.
        /// </summary>
        MaxCompression = 10,

        /// <summary>
        /// Extreme compression level. Provides the highest compression ratio at the cost of speed.
        /// </summary>
        ExtremeCompression = 11
    }

    // Global
    /// <summary>
    /// Provides extension methods for performing common operations on <see cref="DataBuffer"/> objects.
    /// </summary>
    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// Compresses the data in the provided buffer using the Brotli compression algorithm
        /// and returns a new buffer containing the compressed data.
        /// </summary>
        /// <param name="data">The buffer containing the data to be compressed.</param>
        /// <param name="level">The Brotli compression level, ranging from UltraFast to ExtremeCompression. Default is UltraFast.</param>
        /// <param name="window">The Brotli sliding window size, ranging from 10 to 24. Default is 22.</param>
        /// <returns>
        /// A new <see cref="DataBuffer"/> containing the compressed data.
        /// The caller is responsible for disposing of the returned buffer or managing its lifecycle appropriately.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when an error occurs during compression, such as insufficient buffer space or invalid settings.
        /// </exception>
        public static DataBuffer CompressWithBrotliToNewBuffer(this DataBuffer data,
            BrotliCompressionLevel level = BrotliCompressionLevel.UltraFast, int window = 22)
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
                NetworkLogger.PrintHyperlink();
                throw new Exception(
                    $"{ex.Message} - The buffer acquired from the pool does not have sufficient space. " +
                    "Consider increasing the initial capacity of the pool to handle larger data."
                );
            }
            catch (Exception ex)
            {
                NetworkLogger.PrintHyperlink();
                throw new Exception(
                    $"An error occurred during compression: {ex.Message}. Please verify the data and compression settings."
                );
            }
        }

        /// <summary>
        /// Compresses the data in the current buffer using the Brotli compression algorithm, replacing the original data with the compressed data.
        /// </summary>
        /// <param name="data">The buffer containing the data to compress. The buffer will be modified in place.</param>
        /// <param name="level">The Brotli compression level, ranging from UltraFast to ExtremeCompression. Default is UltraFast.</param>
        /// <param name="window">The Brotli sliding window size, ranging from 10 to 24. Default is 22.</param>
        /// <remarks>
        /// This method modifies the original buffer. Use with caution when the original data is still needed.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown when an error occurs during compression, such as insufficient buffer space or invalid settings.
        /// </exception>
        public static void CompressWithBrotliInPlace(this DataBuffer data,
            BrotliCompressionLevel level = BrotliCompressionLevel.UltraFast, int window = 22)
        {
            using var compressedBuffer = CompressWithBrotliToNewBuffer(data, level, window);
            data.Reset();
            data.Write(compressedBuffer.BufferAsSpan);
        }

        /// <summary>
        /// Decompresses the data in the provided buffer using the Brotli decompression algorithm 
        /// and returns a new buffer containing the decompressed data.
        /// </summary>
        /// <param name="data">The buffer containing the compressed data to decompress.</param>
        /// <returns>
        /// A new <see cref="DataBuffer"/> containing the decompressed data. 
        /// The caller is responsible for disposing of the returned buffer or managing its lifecycle appropriately.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when an error occurs during decompression, such as invalid or corrupted compressed data.
        /// </exception>
        public static DataBuffer DecompressWithBrotliToNewBuffer(this DataBuffer data)
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
        /// Decompresses the data in the current buffer using the Brotli decompression algorithm, replacing the original data with the decompressed data.
        /// </summary>
        /// <param name="data">The buffer containing the compressed data. The buffer will be replaced with the decompressed data.</param>
        /// <remarks>
        /// This method modifies the original buffer. Use with caution when the original compressed data is still needed.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown when an error occurs during decompression, such as invalid or corrupted compressed data.
        /// </exception>
        public static void DecompressWithBrotliInPlace(this DataBuffer data)
        {
            using var decompressedBuffer = DecompressWithBrotliToNewBuffer(data);
            data.Reset();
            data.Write(decompressedBuffer.Internal_GetSpan(decompressedBuffer.EndPosition));
            data.SeekToBegin();
        }

        /// <summary>
        /// Encrypts the data in the buffer using AES encryption and returns a new buffer containing the encrypted data.
        /// </summary>
        /// <param name="buffer">The data buffer to encrypt.</param>
        /// <param name="peer">The network peer providing the AES encryption key.</param>
        /// <returns>
        /// A new <see cref="DataBuffer"/> containing the encrypted data. 
        /// The caller is responsible for disposing of the returned buffer or managing its lifecycle appropriately.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if encryption fails, such as due to an invalid key or corrupted data.
        /// </exception>
        public static DataBuffer EncryptToNewBuffer(this DataBuffer buffer, NetworkPeer peer)
        {
            if (peer == null)
            {
                NetworkLogger.PrintHyperlink();
                throw new ArgumentNullException(nameof(peer), "The peer cannot be null.");
            }

            buffer.SeekToEnd();
            byte[] data = buffer.ToArray();
            byte[] encryptedData = AesProvider.Encrypt(data, 0, data.Length, peer._aesKey, out byte[] Iv);

            // Encrypt
            var encryptedBuffer = NetworkManager.Pool.Rent();
            encryptedBuffer.WriteAsBinary(Iv);
            encryptedBuffer.WriteAsBinary(encryptedData);
            return encryptedBuffer;
        }

        /// <summary>
        /// Encrypts the data in the current buffer using AES encryption, replacing the original data with the encrypted data.
        /// </summary>
        /// <param name="buffer">The data buffer to encrypt. The buffer will be replaced with the encrypted data.</param>
        /// <param name="peer">The network peer providing the AES encryption key.</param>
        /// <remarks>
        /// This method modifies the original buffer. Use with caution when the original unencrypted data is still needed.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if encryption fails, such as due to an invalid key or corrupted data.
        /// </exception>
        public static void EncryptInPlace(this DataBuffer buffer, NetworkPeer peer)
        {
            using var encryptedBuffer = EncryptToNewBuffer(buffer, peer);
            buffer.Reset();
            buffer.Write(encryptedBuffer.BufferAsSpan);
        }

        /// <summary>
        /// Decrypts the data in the buffer using AES decryption and returns a new buffer containing the decrypted data.
        /// </summary>
        /// <param name="buffer">The data buffer to decrypt.</param>
        /// <param name="peer">The network peer providing the AES decryption key.</param>
        /// <returns>
        /// A new <see cref="DataBuffer"/> containing the decrypted data. 
        /// The caller is responsible for disposing of the returned buffer or managing its lifecycle appropriately.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if decryption fails, such as due to an invalid key, or corrupted data.
        /// </exception>
        public static DataBuffer DecryptToNewBuffer(this DataBuffer buffer, NetworkPeer peer)
        {
            if (peer == null)
            {
                NetworkLogger.PrintHyperlink();
                throw new ArgumentNullException(nameof(peer), "The peer cannot be null.");
            }

            byte[] iv = buffer.ReadAsBinary<byte[]>();
            byte[] encryptedData = buffer.ReadAsBinary<byte[]>();
            byte[] decryptedData = AesProvider.Decrypt(encryptedData, 0, encryptedData.Length, peer._aesKey, iv);

            // Decrypt
            var decryptedBuffer = NetworkManager.Pool.Rent();
            BuffersExtensions.Write(decryptedBuffer, decryptedData);
            decryptedBuffer.SeekToBegin();
            return decryptedBuffer;
        }

        /// <summary>
        /// Decrypts the data in the current buffer using AES decryption, replacing the original data with the decrypted data.
        /// </summary>
        /// <param name="buffer">The data buffer to decrypt. The buffer will be replaced with the decrypted data.</param>
        /// <param name="peer">The network peer providing the AES decryption key.</param>
        /// <remarks>
        /// This method modifies the original buffer. Use with caution when the original encrypted data is still needed.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if decryption fails, such as due to an invalid key, IV, or corrupted data.
        /// </exception>
        public static void DecryptInPlace(this DataBuffer buffer, NetworkPeer peer)
        {
            using var decryptedBuffer = DecryptToNewBuffer(buffer, peer);
            buffer.Reset();
            buffer.Write(decryptedBuffer.Internal_GetSpan(decryptedBuffer.EndPosition));
            buffer.SeekToBegin();
        }

        /// <summary>
        /// Serializes the specified network message into a new <see cref="DataBuffer"/>.
        /// </summary>
        /// <param name="message">The message object implementing <see cref="IMessage"/> to serialize.</param>
        /// <returns>
        /// A new <see cref="DataBuffer"/> containing the serialized representation of the message. 
        /// The caller is responsible for disposing of the returned buffer or managing its lifecycle appropriately.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataBuffer Serialize(this IMessage message)
        {
            var writer = NetworkManager.Pool.Rent();
            message.Serialize(writer);
            return writer;
        }

        /// <summary>
        /// Deserializes the contents of the specified <see cref="DataBuffer"/> into the provided <see cref="IMessage"/> object.
        /// </summary>
        /// <param name="message">The <see cref="IMessage"/> object to populate with the deserialized data.</param>
        /// <param name="reader">The <see cref="DataBuffer"/> containing the serialized data to deserialize.</param>
        /// <remarks>
        /// This method reads from the given data buffer and populates the properties of the message object.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Populate(this IMessage message, DataBuffer reader)
        {
            message.Deserialize(reader);
        }

        /// <summary>
        /// Deserializes the contents of the specified <see cref="DataBuffer"/> into a new instance of the specified type.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the message to deserialize. Must implement <see cref="IMessage"/> and have a parameterless constructor.
        /// </typeparam>
        /// <param name="reader">The <see cref="DataBuffer"/> containing the serialized data to deserialize.</param>
        /// <returns>A new instance of <typeparamref name="T"/> populated with the deserialized data.</returns>
        /// <remarks>
        /// This method creates a new instance of the specified type, deserializes the data from the buffer,
        /// and populates the instance.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Deserialize<T>(this DataBuffer reader) where T : IMessage, new()
        {
            T message = new();
            message.Deserialize(reader);
            return message;
        }

        /// <summary>
        /// Deserializes the contents of the specified <see cref="DataBuffer"/> into a new instance of the specified type,
        /// while associating it with a network peer and server context.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the message to deserialize. Must implement <see cref="IMessageWithPeer"/> and have a parameterless constructor.
        /// </typeparam>
        /// <param name="reader">The <see cref="DataBuffer"/> containing the serialized data to deserialize.</param>
        /// <param name="peer">The <see cref="NetworkPeer"/> associated with the deserialized message.</param>
        /// <param name="isServer">
        /// A boolean value indicating whether the deserialized message is being handled in a server context.
        /// </param>
        /// <returns>A new instance of <typeparamref name="T"/> populated with the deserialized data.</returns>
        /// <remarks>
        /// This method creates a new instance of the specified type, deserializes the data from the buffer,
        /// and sets its network peer and server context.
        /// </remarks>
        public static T Deserialize<T>(this DataBuffer reader, NetworkPeer peer, bool isServer)
            where T : IMessageWithPeer, new()
        {
            T message = new()
            {
                SharedPeer = peer,
                IsServer = isServer
            };

            message.Deserialize(reader);
            return message;
        }
    }

    // Writers
    /// <summary>
    /// Provides extension methods for writing various data types to a <see cref="DataBuffer"/>.
    /// </summary>
    public static partial class BufferWriterExtensions
    {
        /// <summary>
        /// Specifies the default encoding used when writing strings to the buffer.
        /// </summary>
        public static Encoding DefaultEncoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Specifies the default JSON serializer settings used for serializing and deserializing objects.
        /// </summary>
        /// <remarks>
        /// By default, reference loops are ignored, and a custom converter for `Half` types is included.
        /// </remarks>
        public static JsonSerializerSettings DefaultJsonSettings { get; set; } = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = { new HalfJsonConverter() },
        };

        /// <summary>
        /// Specifies the default settings for serializing and deserializing objects using MemoryPack.
        /// </summary>
        /// <remarks>
        /// By default, this uses the global <see cref="MemoryPackSerializerOptions.Default"/> settings.
        /// </remarks>
        public static MemoryPackSerializerOptions DefaultMemoryPackSettings { get; set; } =
            MemoryPackSerializerOptions.Default;

        /// <summary>
        /// Indicates whether binary serialization is enabled by default for certain types.
        /// </summary>
        /// <remarks>
        /// This is particularly useful for types such as <see cref="Response"/>.
        /// </remarks>
        public static bool UseBinarySerialization = false;

        /// <summary>
        /// Enables or disables bandwidth optimization for data sent over the network.
        /// </summary>
        /// <remarks>
        /// When enabled, this optimizes the size of serialized data for network transmission.
        /// </remarks>
        public static bool EnableBandwidthOptimization { get; set; } = true;

        /// <summary>
        /// Determines whether unaligned memory access is used when writing primitive types to the buffer.
        /// </summary>
        /// <remarks>
        /// Default value is false. 
        /// Enabling this can improve performance on certain platforms but may degrade performance on others.
        /// </remarks>
        public static bool UseUnalignedMemory { get; set; } = false;

        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/MemoryMarshal.cs#L507
        /// <summary>
        /// Writes a structure of type T into a span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Internal_UnsafeWrite<T>(DataBuffer buffer, in T value) where T : unmanaged
        {
            int size_t = sizeof(T);
            Span<byte> destination = buffer.Internal_GetSpan(size_t);
#if OMNI_DEBUG
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                NetworkLogger.PrintHyperlink();
                throw new InvalidOperationException(
                    "Operation not supported: The type parameter T is a reference type or contains references, which violates the constraints for this operation.");
            }

            if (size_t > (uint)destination.Length)
            {
                NetworkLogger.PrintHyperlink();
                throw new ArgumentOutOfRangeException(
                    nameof(destination),
                    "The size of the type T exceeds the available length of the destination span. Ensure the destination span has sufficient capacity."
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
        /// <summary>
        /// Writes an integer value to the specified <see cref="DataBuffer"/> instance, utilizing
        /// bandwidth optimization techniques if enabled.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> into which the value will be written.</param>
        /// <param name="value">The integer value to be written to the buffer.</param>
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
        /// Serializes the specified object to JSON and writes it to the buffer.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> where the JSON data will be written.</param>
        /// <param name="value">The object to serialize to JSON.</param>
        /// <param name="settings">
        /// Optional <see cref="JsonSerializerSettings"/> to customize serialization behavior.
        /// If not provided, <see cref="DefaultJsonSettings"/> will be used.
        /// </param>
        /// <returns>
        /// A string containing the serialized JSON representation of the object.
        /// </returns>
        /// <remarks>
        /// This method uses Newtonsoft.Json for serialization by default.
        /// </remarks>
        public static string WriteAsJson<T>(this DataBuffer buffer, T value, JsonSerializerSettings settings = null)
        {
            settings ??= DefaultJsonSettings;
            string objectAsJson = JsonConvert.SerializeObject(value, settings);
            WriteString(buffer, objectAsJson);
            return objectAsJson;
        }

        /// <summary>
        /// Serializes the specified object to binary and writes it to the buffer.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> where the binary data will be written.</param>
        /// <param name="value">The object to serialize to binary.</param>
        /// <param name="settings">
        /// Optional <see cref="MemoryPackSerializerOptions"/> to customize serialization behavior.
        /// If not provided, <see cref="DefaultMemoryPackSettings"/> will be used.
        /// </param>
        /// <remarks>
        /// This method uses MemoryPack for serialization by default.
        /// </remarks>
        public static void WriteAsBinary<T>(this DataBuffer buffer, T value, MemoryPackSerializerOptions settings = null)
        {
            try
            {
                settings ??= DefaultMemoryPackSettings;
                ReadOnlySpan<byte> data = MemoryPackSerializer.Serialize(value, settings);
                Write7BitEncodedInt(buffer, data.Length);
                buffer.Write(data);
            }
            catch (MemoryPackSerializationException ex)
            {
                NetworkLogger.PrintHyperlink();
                // Show extra info for MemoryPackSerializationException
                NetworkLogger.PrintHyperlink(ex);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously serializes the specified object to binary format and writes it to the buffer.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> where the binary data will be written.</param>
        /// <param name="value">The object to serialize to binary format.</param>
        /// <param name="settings">
        /// Optional <see cref="MemoryPackSerializerOptions"/> to customize serialization behavior.
        /// If not provided, <see cref="DefaultMemoryPackSettings"/> will be used.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTask"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// This method uses MemoryPack for serialization by default.
        /// </remarks>
        public static async ValueTask WriteAsBinaryAsync<T>(this DataBuffer buffer, T value, MemoryPackSerializerOptions settings = null)
        {
            settings ??= DefaultMemoryPackSettings;
            using MemoryStream stream = new();
            await MemoryPackSerializer.SerializeAsync(stream, value, settings);

            int length = (int)stream.Length;
            Write7BitEncodedInt(buffer, length);
            buffer.Write(stream.GetBuffer().AsSpan(0, length));
        }

        /// <summary>
        /// Writes an response to the buffer using the configured serialization format.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the response to.</param>
        /// <param name="response">The <see cref="Response"/> to write.</param>
        /// <remarks>
        /// The serialization format is determined by the value of <see cref="UseBinarySerialization"/>.
        /// If set to true, binary serialization is used; otherwise, JSON serialization is used.
        /// </remarks>
        public static void WriteResponse(this DataBuffer buffer, Response response)
        {
            if (!UseBinarySerialization)
            {
                WriteAsJson(buffer, response);
                return;
            }

            WriteAsBinary(buffer, response);
        }

        /// <summary>
        /// Writes an response with a generic payload to the buffer using the configured serialization format.
        /// </summary>
        /// <typeparam name="T">The type of the payload in the response.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the response to.</param>
        /// <param name="response">The <see cref="Response{T}"/> containing the generic payload to write.</param>
        /// <remarks>
        /// The serialization format is determined by the value of <see cref="UseBinarySerialization"/>.
        /// If set to true, binary serialization is used; otherwise, JSON serialization is used.
        /// </remarks>
        public static void WriteResponse<T>(this DataBuffer buffer, Response<T> response)
        {
            if (!UseBinarySerialization)
            {
                WriteAsJson(buffer, response);
                return;
            }

            WriteAsBinary(buffer, response);
        }

        /// <summary>
        /// Writes a primitive array of unmanaged types to the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array. Must be an unmanaged type.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> where the array will be written.</param>
        /// <param name="array">The array of primitive values to write to the buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(this DataBuffer buffer, T[] array) where T : unmanaged
        {
            int size_t = sizeof(T) * array.Length;
            Write7BitEncodedInt(buffer, size_t);

            ReadOnlySpan<T> data = array.AsSpan();
            buffer.Write(MemoryMarshal.AsBytes(data));
        }

        /// <summary>
        /// Writes a string to the specified <see cref="DataBuffer"/> using the given encoding.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the string to.</param>
        /// <param name="input">The <see cref="ReadOnlySpan{T}"/> of characters representing the string to write.</param>
        /// <param name="encoding">
        /// The <see cref="Encoding"/> to use for converting the string to bytes. 
        /// If not specified, the default encoding (<see cref="DefaultEncoding"/>) is used.
        /// </param>
        public static void WriteString(this DataBuffer buffer, ReadOnlySpan<char> input, Encoding encoding = null)
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
        /// Writes a primitive value of an unmanaged type directly to the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value to write. Must be an unmanaged type.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> where the value will be written.</param>
        /// <param name="value">The primitive value to write to the buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this DataBuffer buffer, in T value) where T : unmanaged
        {
            Internal_UnsafeWrite(buffer, in value);
        }

        /// <summary>
        /// Writes network identity data to the specified <see cref="DataBuffer"/>, typically used for instantiating network objects.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the identity data to.</param>
        /// <param name="peerId">The unique identifier of the network peer owning the identity.</param>
        /// <param name="identityId">The unique identifier of the network identity.</param>
        /// <remarks>
        /// This method serializes the provided identity and peer IDs directly into the buffer.
        /// </remarks>
        public static void WriteIdentity(this DataBuffer buffer, int peerId, int identityId)
        {
            Write(buffer, peerId);
            Write(buffer, identityId);
        }

        /// <summary>
        /// Writes an integer to the specified <see cref="DataBuffer"/> in a compact 7-bit encoded format.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the encoded integer to.</param>
        /// <param name="value">The integer value to encode and write.</param>
        /// <remarks>
        /// This method encodes the integer value using a compact 7-bit format, where the high bit of each byte 
        /// indicates whether additional bytes follow. This is useful for minimizing storage space for smaller values.
        /// </remarks>
        /// <seealso href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs#L473">
        /// Reference implementation in .NET Runtime.
        /// </seealso>
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
        /// Writes a long integer to the specified <see cref="DataBuffer"/> in a compact 7-bit encoded format.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the encoded long integer to.</param>
        /// <param name="value">The long integer value to encode and write.</param>
        /// <remarks>
        /// This method encodes the long integer value using a compact 7-bit format, where the high bit of each byte 
        /// indicates whether additional bytes follow. This is useful for minimizing storage space for smaller values.
        /// </remarks>
        /// <seealso href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs#L492">
        /// Reference implementation in .NET Runtime.
        /// </seealso>
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
        /// Writes a compressed representation of a <see cref="Vector3"/> to the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the compressed <see cref="Vector3"/> to.</param>
        /// <param name="vector">The <see cref="Vector3"/> to compress and write.</param>
        /// <remarks>
        /// This method compresses the <see cref="Vector3"/> to reduce its size to 8 bytes (a <see cref="long"/>), 
        /// achieving significant bandwidth savings during data transmission.
        /// <br/><br/>
        /// <b>Value Ranges:</b><br/>
        /// <b>X:</b> -9999.99f to 9999.99f<br/>
        /// <b>Y:</b> -9999.99f to 9999.99f<br/>
        /// <b>Z:</b> -9999.99f to 9999.99f<br/>
        /// Ensure the vector values fall within these ranges to avoid compression errors.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WritePacked(this DataBuffer buffer, Vector3 vector)
        {
            long packedValue = VectorCompressor.Compress(vector);
            Write(buffer, packedValue);
        }

        /// <summary>
        /// Writes a compressed representation of a <see cref="Quaternion"/> to the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to write the compressed <see cref="Quaternion"/> to.</param>
        /// <param name="quat">The <see cref="Quaternion"/> to compress and write.</param>
        /// <remarks>
        /// This method compresses the <see cref="Quaternion"/> to reduce its size to 4 bytes (a <see cref="uint"/>),
        /// achieving significant bandwidth savings during data transmission.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WritePacked(this DataBuffer buffer, Quaternion quat)
        {
            uint packedValue = QuaternionCompressor.Compress(quat);
            Write(buffer, packedValue);
        }

        /// <summary>
        /// Writes two boolean values to the buffer, packing them into a single byte.
        /// </summary>
        /// <param name="buffer">The buffer where the packed boolean values will be written.</param>
        /// <param name="v1">The first boolean value to pack.</param>
        /// <param name="v2">The second boolean value to pack.</param>
        /// <remarks>
        /// The boolean values are packed into the least significant bits of the byte,
        /// with the first boolean occupying the least significant bit and the second boolean occupying the next bit.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WritePacked(this DataBuffer buffer, bool v1, bool v2) =>
            buffer.Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1));

        /// <summary>
        /// Writes three boolean values to the underlying buffer, packed into a single byte.
        /// </summary>
        /// <param name="buffer">The target buffer where the packed boolean values will be written.</param>
        /// <param name="v1">The first boolean value.</param>
        /// <param name="v2">The second boolean value.</param>
        /// <param name="v3">The third boolean value.</param>
        /// <remarks>
        /// The three boolean values are packed into the three least significant bits of a single byte,
        /// with each value occupying one bit in sequence.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WritePacked(this DataBuffer buffer, bool v1, bool v2, bool v3) =>
            buffer.Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2));

        /// <summary>
        /// Writes four boolean values to the buffer, efficiently packed into one byte.
        /// </summary>
        /// <param name="buffer">The DataBuffer to which the values will be written.</param>
        /// <param name="v1">The first boolean value to be packed.</param>
        /// <param name="v2">The second boolean value to be packed.</param>
        /// <param name="v3">The third boolean value to be packed.</param>
        /// <param name="v4">The fourth boolean value to be packed.</param>
        /// <remarks>
        /// The boolean values are compacted into the least significant bits of a byte, facilitating memory-efficient storage.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WritePacked(this DataBuffer buffer, bool v1, bool v2, bool v3, bool v4) =>
            buffer.Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3));

        /// <summary>
        /// Writes five boolean values to the underlying buffer, packed into a single byte.
        /// </summary>
        /// <param name="buffer">The buffer to write the packed boolean values into.</param>
        /// <param name="v1">The first boolean value.</param>
        /// <param name="v2">The second boolean value.</param>
        /// <param name="v3">The third boolean value.</param>
        /// <param name="v4">The fourth boolean value.</param>
        /// <param name="v5">The fifth boolean value.</param>
        /// <remarks>
        /// The five boolean values are packed into the five least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WritePacked(this DataBuffer buffer, bool v1, bool v2, bool v3, bool v4, bool v5) =>
            buffer.Write(
                (byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4));

        /// <summary>
        /// Writes six boolean values to the underlying buffer, packed into a single byte.
        /// </summary>
        /// <param name="buffer">The buffer to which the boolean values will be written.</param>
        /// <param name="v1">The first boolean value.</param>
        /// <param name="v2">The second boolean value.</param>
        /// <param name="v3">The third boolean value.</param>
        /// <param name="v4">The fourth boolean value.</param>
        /// <param name="v5">The fifth boolean value.</param>
        /// <param name="v6">The sixth boolean value.</param>
        /// <remarks>
        /// The six boolean values are packed into the six least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void
            WritePacked(this DataBuffer buffer, bool v1, bool v2, bool v3, bool v4, bool v5, bool v6) => buffer.Write(
            (byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 | *(byte*)&v4 << 3 | *(byte*)&v5 << 4 |
                   *(byte*)&v6 << 5));

        /// <summary>
        /// Writes seven boolean values to the underlying buffer, packed into a single byte.
        /// </summary>
        /// <param name="buffer">The buffer to which the packed boolean values will be written.</param>
        /// <param name="v1">The first boolean value.</param>
        /// <param name="v2">The second boolean value.</param>
        /// <param name="v3">The third boolean value.</param>
        /// <param name="v4">The fourth boolean value.</param>
        /// <param name="v5">The fifth boolean value.</param>
        /// <param name="v6">The sixth boolean value.</param>
        /// <param name="v7">The seventh boolean value.</param>
        /// <remarks>
        /// The seven boolean values are packed into the seven least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WritePacked(this DataBuffer buffer, bool v1, bool v2, bool v3, bool v4, bool v5,
            bool v6, bool v7) => buffer.Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 |
                                                     *(byte*)&v4 << 3 | *(byte*)&v5 << 4 | *(byte*)&v6 << 5 |
                                                     *(byte*)&v7 << 6));

        /// <summary>
        /// Writes eight boolean values to the underlying buffer, packed into a single byte.
        /// </summary>
        /// <param name="buffer">The buffer to which the packed byte will be written.</param>
        /// <param name="v1">The first boolean value.</param>
        /// <param name="v2">The second boolean value.</param>
        /// <param name="v3">The third boolean value.</param>
        /// <param name="v4">The fourth boolean value.</param>
        /// <param name="v5">The fifth boolean value.</param>
        /// <param name="v6">The sixth boolean value.</param>
        /// <param name="v7">The seventh boolean value.</param>
        /// <param name="v8">The eighth boolean value.</param>
        /// <remarks>
        /// The eight boolean values are packed into all eight bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WritePacked(this DataBuffer buffer, bool v1, bool v2, bool v3, bool v4, bool v5,
            bool v6, bool v7, bool v8) => buffer.Write((byte)(*(byte*)&v1 | *(byte*)&v2 << 1 | *(byte*)&v3 << 2 |
                                                              *(byte*)&v4 << 3 | *(byte*)&v5 << 4 | *(byte*)&v6 << 5 |
                                                              *(byte*)&v7 << 6 | *(byte*)&v8 << 7));
    }

    // Readers
    /// <summary>
    /// Provides extension methods for reading various data types from a <see cref="DataBuffer"/> object.
    /// </summary>
    public static partial class BufferWriterExtensions
    {
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/MemoryMarshal.cs#L468
        /// <summary>
        /// Reads a structure of type T out of a read-only span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe T Internal_UnsafeRead<T>(DataBuffer buffer) where T : unmanaged
        {
            int size_t = sizeof(T);
            Span<byte> source = buffer.Internal_GetSpan(size_t);
#if OMNI_DEBUG
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                NetworkLogger.PrintHyperlink();
                throw new InvalidOperationException(
                    "Operation not supported: The type parameter T is a reference type or contains references, which violates the constraints for this operation."
                );
            }

            if (size_t > source.Length)
            {
                NetworkLogger.PrintHyperlink();
                throw new ArgumentOutOfRangeException(
                    nameof(source),
                    "The size of the type T exceeds the available length of the source span. Ensure the source span has sufficient capacity."
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
        /// Reads an response from the specified <see cref="DataBuffer"/> using the configured deserialization format.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the response from.</param>
        /// <returns>
        /// A deserialized <see cref="Response"/> object.
        /// </returns>
        /// <remarks>
        /// The deserialization format is determined by the value of <see cref="UseBinarySerialization"/>.
        /// If set to true, binary deserialization is used; otherwise, JSON deserialization is used.
        /// </remarks>
        public static Response ReadResponse(this DataBuffer buffer)
        {
            if (!UseBinarySerialization)
            {
                return ReadAsJson<Response>(buffer);
            }

            return ReadAsBinary<Response>(buffer);
        }

        /// <summary>
        /// Reads an response with a generic payload from the specified <see cref="DataBuffer"/> using the configured deserialization format.
        /// </summary>
        /// <typeparam name="T">The type of the payload in the response.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the response from.</param>
        /// <returns>
        /// A deserialized <see cref="Response{T}"/> object containing the generic payload.
        /// </returns>
        /// <remarks>
        /// The deserialization format is determined by the value of <see cref="UseBinarySerialization"/>.
        /// If set to true, binary deserialization is used; otherwise, JSON deserialization is used.
        /// </remarks>
        public static Response<T> ReadResponse<T>(this DataBuffer buffer)
        {
            if (!UseBinarySerialization)
            {
                return ReadAsJson<Response<T>>(buffer);
            }

            return ReadAsBinary<Response<T>>(buffer);
        }

        /// <summary>
        /// Deserializes a JSON string from the specified <see cref="DataBuffer"/> and populates the target object with the deserialized data.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> containing the JSON string to read.</param>
        /// <param name="target">The object to populate with the deserialized data.</param>
        /// <param name="settings">Optional <see cref="JsonSerializerSettings"/> to customize the deserialization behavior.</param>
        public static void ReadAsJson(this DataBuffer buffer, object target, JsonSerializerSettings settings = null)
        {
            settings ??= DefaultJsonSettings;
            string json = ReadString(buffer);
            JsonConvert.PopulateObject(json, target, settings);
        }

        /// <summary>
        /// Reads a JSON string from the specified <see cref="DataBuffer"/> and deserializes it into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> containing the JSON string to read.</param>
        /// <param name="settings">
        /// Optional <see cref="JsonSerializerSettings"/> to customize the deserialization behavior.
        /// If not provided, <see cref="DefaultJsonSettings"/> is used.
        /// </param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method uses Newtonsoft.Json by default to deserialize the JSON string.
        /// </remarks>
        public static T ReadAsJson<T>(this DataBuffer buffer, JsonSerializerSettings settings = null)
        {
            settings ??= DefaultJsonSettings;
            string json = ReadString(buffer);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Reads a JSON string from the specified <see cref="DataBuffer"/>, deserializes it into an object of type <typeparamref name="T"/>, 
        /// and returns the raw JSON string as an output parameter.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> containing the JSON string to read.</param>
        /// <param name="json">The raw JSON string read from the buffer.</param>
        /// <param name="settings">
        /// Optional <see cref="JsonSerializerSettings"/> to customize the deserialization behavior.
        /// If not provided, <see cref="DefaultJsonSettings"/> is used.
        /// </param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method uses Newtonsoft.Json by default to deserialize the JSON string.
        /// </remarks>
        public static T ReadAsJson<T>(this DataBuffer buffer, out string json, JsonSerializerSettings settings = null)
        {
            settings ??= DefaultJsonSettings;
            json = ReadString(buffer);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Reads binary data from the specified <see cref="DataBuffer"/> and deserializes it into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> containing the binary data to read.</param>
        /// <param name="settings">
        /// Optional <see cref="MemoryPackSerializerOptions"/> to customize the deserialization behavior.
        /// If not provided, <see cref="DefaultMemoryPackSettings"/> is used.
        /// </param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method uses MemoryPack by default to deserialize the binary data.
        /// </remarks>
        public static T ReadAsBinary<T>(this DataBuffer buffer, MemoryPackSerializerOptions settings = null)
        {
            settings ??= DefaultMemoryPackSettings;
            int dataSize = Read7BitEncodedInt(buffer);
            Span<byte> data = buffer.Internal_GetSpan(dataSize);
            buffer.Internal_Advance(dataSize);
            return MemoryPackSerializer.Deserialize<T>(data, settings);
        }

        /// <summary>
        /// Reads a string from the specified <see cref="DataBuffer"/> using the given encoding.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the string from.</param>
        /// <param name="encoding">
        /// The <see cref="Encoding"/> to use for decoding the string. 
        /// If not specified, the default encoding (<see cref="DefaultEncoding"/>) is used.
        /// </param>
        /// <returns>The decoded string read from the buffer.</returns>
        public static string ReadString(this DataBuffer buffer, Encoding encoding = null)
        {
            encoding ??= DefaultEncoding;
            int byteCount = Read7BitEncodedInt(buffer);
            Span<byte> data = buffer.Internal_GetSpan(byteCount);
            buffer.Internal_Advance(byteCount);
            return encoding.GetString(data);
        }

        /// <summary>
        /// Reads a primitive array of unmanaged elements from the specified <see cref="DataBuffer"/> and returns a new array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array. Must be an unmanaged type.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the array from.</param>
        /// <returns>A new array of type <typeparamref name="T"/> containing the read data.</returns>
        /// <remarks>
        /// This method reads raw binary data from the buffer and interprets it as an array of type <typeparamref name="T"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T[] ReadArray<T>(this DataBuffer buffer) where T : unmanaged
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
        /// Reads a primitive array of unmanaged elements from the specified <see cref="DataBuffer"/> 
        /// into a preallocated array without allocating a new one.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array. Must be an unmanaged type.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the array from.</param>
        /// <param name="array">The preallocated array to store the read data. Its size must match the expected data.</param>
        /// <returns>The size in bytes of the data read from the buffer.</returns>
        /// <remarks>
        /// This method reads raw binary data from the buffer and directly populates the provided array. 
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ReadArray<T>(this DataBuffer buffer, T[] array) where T : unmanaged
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
        /// Reads a primitive value of an unmanaged type from the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value to read. Must be an unmanaged type.</typeparam>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the value from.</param>
        /// <returns>The value of type <typeparamref name="T"/> read from the buffer.</returns>
        /// <remarks>
        /// This method reads the raw binary representation of the value directly from the buffer.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(this DataBuffer buffer) where T : unmanaged
        {
            return Internal_UnsafeRead<T>(buffer);
        }

        /// <summary>
        /// Reads a network identity's peer ID and identity ID from the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the identity data from.</param>
        /// <param name="peerId">The peer ID associated with the network identity, output as an integer.</param>
        /// <param name="identityId">The identity ID of the network identity, output as an integer.</param>
        /// <remarks>
        /// This method reads two integers from the buffer: the identity ID and the peer ID. 
        /// </remarks>
        public static void ReadIdentity(this DataBuffer buffer, out int peerId, out int identityId)
        {
            peerId = Read<int>(buffer);
            identityId = Read<int>(buffer);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the specified <see cref="DataBuffer"/> in a compressed 7-bit encoded format.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the integer from.</param>
        /// <returns>The 32-bit signed integer decoded from the buffer.</returns>
        /// <remarks>
        /// This method reads integers encoded in a compact 7-bit format, where each byte's most significant bit (MSB)
        /// indicates whether additional bytes follow. It efficiently reduces storage for smaller integers.
        /// <br/><br/>
        /// **Failure Cases:**<br/>
        /// - Reading more than 5 bytes may cause an overflow.
        /// - The fifth byte must fit within the remaining bits of a 32-bit integer and must not have the MSB set.
        /// </remarks>
        /// <exception cref="FormatException">
        /// Thrown if the encoded integer is invalid or causes an overflow.
        /// </exception>
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
                NetworkLogger.PrintHyperlink();
                throw new FormatException("SR.Format_Bad7BitInt");
            }

            result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (int)result;
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the specified <see cref="DataBuffer"/> in a compressed 7-bit encoded format.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> to read the integer from.</param>
        /// <returns>The 64-bit signed integer decoded from the buffer.</returns>
        /// <remarks>
        /// This method reads integers encoded in a compact 7-bit format, where each byte's most significant bit (MSB)
        /// indicates whether additional bytes follow. It efficiently reduces storage for smaller integers.
        /// <br/><br/>
        /// **Failure Cases:**<br/>
        /// - Reading more than 10 bytes may cause an overflow.
        /// - The tenth byte must fit within the remaining bits of a 64-bit integer and must not have the MSB set.
        /// </remarks>
        /// <exception cref="FormatException">
        /// Thrown if the encoded integer is invalid or causes an overflow.
        /// </exception>
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
                NetworkLogger.PrintHyperlink();
                throw new FormatException("SR.Format_Bad7BitInt");
            }

            result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (long)result;
        }

        /// <summary>
        /// Reads and decompresses a <see cref="Vector3"/> from the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> containing the compressed <see cref="Vector3"/>.</param>
        /// <returns>The decompressed <see cref="Vector3"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadPackedVector3(this DataBuffer buffer)
        {
            long packedValue = Read<long>(buffer);
            return VectorCompressor.Decompress(packedValue);
        }

        /// <summary>
        /// Reads and decompresses a <see cref="Quaternion"/> from the specified <see cref="DataBuffer"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> containing the compressed <see cref="Quaternion"/>.</param>
        /// <returns>The decompressed <see cref="Quaternion"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ReadPackedQuaternion(this DataBuffer buffer)
        {
            uint packedValue = Read<uint>(buffer);
            return QuaternionCompressor.Decompress(packedValue);
        }

        /// <summary>
        /// Reads a byte from the buffer and unpacks it to retrieve two boolean values.
        /// </summary>
        /// <param name="buffer">The buffer instance from which the packed data is read.</param>
        /// <param name="v1">The first boolean value, representing the least significant bit.</param>
        /// <param name="v2">The second boolean value, representing the second least significant bit.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadPacked(this DataBuffer buffer, out bool v1, out bool v2)
        {
            byte pByte = buffer.Read<byte>();
            v1 = (pByte & 0b1) == 1;
            v2 = ((pByte >> 1) & 0b1) == 1;
        }

        /// <summary>
        /// Reads three boolean values from a packed byte in the buffer.
        /// </summary>
        /// <param name="buffer">The data buffer from which the packed byte is read.</param>
        /// <param name="v1">The first boolean value, extracted from the least significant bit.</param>
        /// <param name="v2">The second boolean value, extracted from the second least significant bit.</param>
        /// <param name="v3">The third boolean value, extracted from the third least significant bit.</param>
        /// <remarks>
        /// The boolean values are extracted from the three least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadPacked(this DataBuffer buffer, out bool v1, out bool v2, out bool v3)
        {
            byte pByte = buffer.Read<byte>();
            v1 = (pByte & 0b1) == 1;
            v2 = ((pByte >> 1) & 0b1) == 1;
            v3 = ((pByte >> 2) & 0b1) == 1;
        }

        /// <summary>
        /// Reads four boolean values from a packed byte in the stream.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> from which the packed byte is read.</param>
        /// <param name="v1">The first boolean value extracted from the least significant bit.</param>
        /// <param name="v2">The second boolean value extracted from the second least significant bit.</param>
        /// <param name="v3">The third boolean value extracted from the third least significant bit.</param>
        /// <param name="v4">The fourth boolean value extracted from the fourth least significant bit.</param>
        /// <remarks>
        /// The booleans are unpacked from the four least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadPacked(this DataBuffer buffer, out bool v1, out bool v2, out bool v3, out bool v4)
        {
            byte pByte = buffer.Read<byte>();
            v1 = (pByte & 0b1) == 1;
            v2 = ((pByte >> 1) & 0b1) == 1;
            v3 = ((pByte >> 2) & 0b1) == 1;
            v4 = ((pByte >> 3) & 0b1) == 1;
        }

        /// <summary>
        /// Reads five boolean values from a packed byte in the buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> from which the packed byte is read.</param>
        /// <param name="v1">The first boolean value extracted from the least significant bit.</param>
        /// <param name="v2">The second boolean value extracted from the second least significant bit.</param>
        /// <param name="v3">The third boolean value extracted from the third least significant bit.</param>
        /// <param name="v4">The fourth boolean value extracted from the fourth least significant bit.</param>
        /// <param name="v5">The fifth boolean value extracted from the fifth least significant bit.</param>
        /// <remarks>
        /// The booleans are unpacked from the five least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadPacked(this DataBuffer buffer, out bool v1, out bool v2, out bool v3, out bool v4,
            out bool v5)
        {
            byte pByte = buffer.Read<byte>();
            v1 = (pByte & 0b1) == 1;
            v2 = ((pByte >> 1) & 0b1) == 1;
            v3 = ((pByte >> 2) & 0b1) == 1;
            v4 = ((pByte >> 3) & 0b1) == 1;
            v5 = ((pByte >> 4) & 0b1) == 1;
        }

        /// <summary>
        /// Reads six boolean values from a packed byte in the buffer.
        /// </summary>
        /// <param name="buffer">The buffer from which the packed byte is read.</param>
        /// <param name="v1">The first boolean value extracted from the least significant bit.</param>
        /// <param name="v2">The second boolean value extracted from the second least significant bit.</param>
        /// <param name="v3">The third boolean value extracted from the third least significant bit.</param>
        /// <param name="v4">The fourth boolean value extracted from the fourth least significant bit.</param>
        /// <param name="v5">The fifth boolean value extracted from the fifth least significant bit.</param>
        /// <param name="v6">The sixth boolean value extracted from the sixth least significant bit.</param>
        /// <remarks>
        /// The booleans are unpacked from the six least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadPacked(this DataBuffer buffer, out bool v1, out bool v2, out bool v3, out bool v4,
            out bool v5, out bool v6)
        {
            byte pByte = buffer.Read<byte>();
            v1 = (pByte & 0b1) == 1;
            v2 = ((pByte >> 1) & 0b1) == 1;
            v3 = ((pByte >> 2) & 0b1) == 1;
            v4 = ((pByte >> 3) & 0b1) == 1;
            v5 = ((pByte >> 4) & 0b1) == 1;
            v6 = ((pByte >> 5) & 0b1) == 1;
        }

        /// <summary>
        /// Reads seven boolean values from a packed byte in the buffer.
        /// </summary>
        /// <param name="buffer">The buffer instance from which the packed byte will be read.</param>
        /// <param name="v1">The first boolean value extracted from the least significant bit.</param>
        /// <param name="v2">The second boolean value extracted from the second least significant bit.</param>
        /// <param name="v3">The third boolean value extracted from the third least significant bit.</param>
        /// <param name="v4">The fourth boolean value extracted from the fourth least significant bit.</param>
        /// <param name="v5">The fifth boolean value extracted from the fifth least significant bit.</param>
        /// <param name="v6">The sixth boolean value extracted from the sixth least significant bit.</param>
        /// <param name="v7">The seventh boolean value extracted from the seventh least significant bit.</param>
        /// <remarks>
        /// The booleans are unpacked from the seven least significant bits of a single byte.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadPacked(this DataBuffer buffer, out bool v1, out bool v2, out bool v3, out bool v4,
            out bool v5, out bool v6, out bool v7)
        {
            byte pByte = buffer.Read<byte>();
            v1 = (pByte & 0b1) == 1;
            v2 = ((pByte >> 1) & 0b1) == 1;
            v3 = ((pByte >> 2) & 0b1) == 1;
            v4 = ((pByte >> 3) & 0b1) == 1;
            v5 = ((pByte >> 4) & 0b1) == 1;
            v6 = ((pByte >> 5) & 0b1) == 1;
            v7 = ((pByte >> 6) & 0b1) == 1;
        }

        /// <summary>
        /// Reads eight boolean values from a packed byte in the provided buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="DataBuffer"/> from which the packed byte is read.</param>
        /// <param name="v1">Outputs the first boolean value, extracted from the least significant bit of the byte.</param>
        /// <param name="v2">Outputs the second boolean value, extracted from the second least significant bit of the byte.</param>
        /// <param name="v3">Outputs the third boolean value, extracted from the third least significant bit of the byte.</param>
        /// <param name="v4">Outputs the fourth boolean value, extracted from the fourth least significant bit of the byte.</param>
        /// <param name="v5">Outputs the fifth boolean value, extracted from the fifth least significant bit of the byte.</param>
        /// <param name="v6">Outputs the sixth boolean value, extracted from the sixth least significant bit of the byte.</param>
        /// <param name="v7">Outputs the seventh boolean value, extracted from the seventh least significant bit of the byte.</param>
        /// <param name="v8">Outputs the eighth boolean value, extracted from the most significant bit of the byte.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadPacked(this DataBuffer buffer, out bool v1, out bool v2, out bool v3, out bool v4,
            out bool v5, out bool v6, out bool v7, out bool v8)
        {
            byte pByte = buffer.Read<byte>();
            v1 = (pByte & 0b1) == 1;
            v2 = ((pByte >> 1) & 0b1) == 1;
            v3 = ((pByte >> 2) & 0b1) == 1;
            v4 = ((pByte >> 3) & 0b1) == 1;
            v5 = ((pByte >> 4) & 0b1) == 1;
            v6 = ((pByte >> 5) & 0b1) == 1;
            v7 = ((pByte >> 6) & 0b1) == 1;
            v8 = ((pByte >> 7) & 0b1) == 1;
        }
    }

    public static partial class BufferWriterExtensions
    {
        public static ReadOnlyBuffer AsReadOnlyBuffer(this DataBuffer buffer) => new(buffer);
    }
}