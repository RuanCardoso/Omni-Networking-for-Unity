/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;
using System.Security.Cryptography;

#pragma warning disable IDE0063
namespace Omni.Core.Cryptography
{
    /// <summary>
    /// Provides AES encryption and decryption services for secure data handling.
    /// </summary>
    /// <remarks>
    /// This class implements the Advanced Encryption Standard (AES) algorithm with support for
    /// 128, 192, and 256 bit keys. It uses CBC (Cipher Block Chaining) mode with PKCS7 padding
    /// to ensure secure encryption of data.
    /// 
    /// The default configuration uses:
    /// - 128-bit key size
    /// - 128-bit block size
    /// - CBC mode for operation
    /// - PKCS7 padding
    /// 
    /// This implementation automatically generates initialization vectors (IV) for encryption
    /// operations to enhance security.
    /// </remarks>
    public static class AesEncryptor
    {
#if OMNI_DEBUG
        static int _keySize = 128;
#else
        // for production, the default key size is set to 256 bits
        static int _keySize = 256;
#endif
        /// <summary>
        /// Gets or sets the key size in bits for AES encryption.
        /// </summary>
        /// <value>
        /// The key size in bits. Valid values are 128, 192, and 256.
        /// The default value is 128 bits for debugging and 256 bits for production.
        /// </value>
        /// <exception cref="ArgumentException">Thrown when setting a value other than 128, 192, or 256.</exception>
        public static int KeySize
        {
            get => _keySize;
            set
            {
                if (value != 128 && value != 192 && value != 256)
                {
                    throw new ArgumentException("Key size must be 128, 192, or 256 bits.");
                }

                _keySize = value;
            }
        }

        /// <summary>
        /// Encrypts the entire data array using AES encryption algorithm.
        /// </summary>
        /// <param name="data">The byte array containing the data to encrypt.</param>
        /// <param name="key">The encryption key (minimum 16 bytes/128 bits).</param>
        /// <param name="Iv">When this method returns, contains the initialization vector used for encryption.</param>
        /// <returns>A byte array containing the encrypted data.</returns>
        /// <exception cref="ArgumentException">Thrown when the key is null or less than keysize.</exception>
        /// <exception cref="CryptographicException">Thrown when an encryption error occurs.</exception>
        /// <remarks>
        /// This is a convenience overload that encrypts the entire data array.
        /// This method generates a unique initialization vector (IV) for each encryption operation,
        /// which is required for decryption and returned via the out parameter.
        /// </remarks>
        public static byte[] Encrypt(byte[] data, byte[] key, out byte[] Iv)
        {
            return Encrypt(data, 0, data.Length, key, out Iv);
        }

        /// <summary>
        /// Encrypts data using AES encryption algorithm.
        /// </summary>
        /// <param name="data">The byte array containing the data to encrypt.</param>
        /// <param name="offset">The starting position within the input array.</param>
        /// <param name="length">The number of bytes to encrypt.</param>
        /// <param name="key">The encryption key (minimum 16 bytes/128 bits).</param>
        /// <param name="Iv">When this method returns, contains the initialization vector used for encryption.</param>
        /// <returns>A byte array containing the encrypted data.</returns>
        /// <exception cref="ArgumentException">Thrown when the key is null or less than keysize.</exception>
        /// <exception cref="CryptographicException">Thrown when an encryption error occurs.</exception>
        /// <remarks>
        /// This method generates a unique initialization vector (IV) for each encryption operation,
        /// which is required for decryption and returned via the out parameter.
        /// </remarks>
        public static byte[] Encrypt(byte[] data, int offset, int length, byte[] key, out byte[] Iv)
        {
            try
            {
                if (key == null || key.Length < (_keySize / 8))
                {
                    throw new ArgumentException(
                        "The encryption key is not provided or is invalid. Please provide a valid key."
                    );
                }

                if (offset < 0 || length < 0 || offset + length > data.Length)
                    throw new ArgumentOutOfRangeException("Invalid offset or length");

                using (Aes aes = Aes.Create())
                {
                    try
                    {
                        aes.KeySize = _keySize;
                        aes.BlockSize = 128;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        aes.Key = key;
                        aes.GenerateIV();
                        Iv = aes.IV;

                        ICryptoTransform encryptor = aes.CreateEncryptor();
                        return encryptor.TransformFinalBlock(data, offset, length);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            $"The encryption key is invalid. Please provide a valid key. Error: {ex.Message}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }

        /// <summary>
        /// Decrypts the entire data array using AES decryption algorithm.
        /// </summary>
        /// <param name="data">The byte array containing the encrypted data.</param>
        /// <param name="key">The decryption key (must match the key used for encryption).</param>
        /// <param name="Iv">The initialization vector used during encryption.</param>
        /// <returns>A byte array containing the decrypted data.</returns>
        /// <exception cref="ArgumentException">Thrown when the key is null or less than key size.</exception>
        /// <exception cref="CryptographicException">Thrown when a decryption error occurs.</exception>
        /// <remarks>
        /// This is a convenience overload that decrypts the entire data array.
        /// The same key and initialization vector (IV) used for encryption must be provided
        /// for successful decryption.
        /// </remarks>
        public static byte[] Decrypt(byte[] data, byte[] key, byte[] Iv)
        {
            return Decrypt(data, 0, data.Length, key, Iv);
        }

        /// <summary>
        /// Decrypts data using AES decryption algorithm.
        /// </summary>
        /// <param name="data">The byte array containing the encrypted data.</param>
        /// <param name="offset">The starting position within the input array.</param>
        /// <param name="length">The number of bytes to decrypt.</param>
        /// <param name="key">The decryption key (must match the key used for encryption).</param>
        /// <param name="Iv">The initialization vector used during encryption.</param>
        /// <returns>A byte array containing the decrypted data.</returns>
        /// <exception cref="ArgumentException">Thrown when the key is null or less than key size.</exception>
        /// <exception cref="CryptographicException">Thrown when a decryption error occurs.</exception>
        /// <remarks>
        /// The same key and initialization vector (IV) used for encryption must be provided
        /// for successful decryption.
        /// </remarks>
        public static byte[] Decrypt(byte[] data, int offset, int length, byte[] key, byte[] Iv)
        {
            try
            {
                if (key == null || key.Length < (_keySize / 8))
                {
                    throw new ArgumentException(
                        "The encryption key is not provided or is invalid. Please provide a valid key."
                    );
                }

                if (Iv == null || Iv.Length != 16)
                {
                    throw new ArgumentException(
                        "The initialization vector (IV) is not provided or is invalid."
                    );
                }

                using (Aes aes = Aes.Create())
                {
                    try
                    {
                        aes.KeySize = _keySize;
                        aes.BlockSize = 128;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        aes.IV = Iv;
                        aes.Key = key;

                        ICryptoTransform decryptor = aes.CreateDecryptor();
                        return decryptor.TransformFinalBlock(data, offset, length);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            $"The encryption key is invalid. Please provide a valid key. Error: {ex.Message}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }

        /// <summary>
        /// Generates a secure random key for AES encryption.
        /// </summary>
        /// <returns>A cryptographically secure random key with the currently configured key size.</returns>
        /// <remarks>
        /// This method creates a cryptographically secure random key that can be used
        /// for AES encryption operations. The generated key uses the current KeySize setting
        /// which defaults to 128 bits (16 bytes) or 256 bits (32 bytes) for production.
        /// </remarks>
        public static byte[] GenerateKey()
        {
            byte[] key = new byte[_keySize / 8];
            RandomNumberGenerator.Fill(key);
            return key;
        }
    }
}
#pragma warning restore IDE0063