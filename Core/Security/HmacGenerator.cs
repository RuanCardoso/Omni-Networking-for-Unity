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
using System.Linq;
using System.Security.Cryptography;

#pragma warning disable IDE0063
namespace Omni.Core.Cryptography
{
    /// <summary>
    /// Provides utility methods for generating and validating HMAC-SHA256 signatures.
    /// </summary>
    public static class HmacGenerator
    {
        private const int _keySize = 256;
        /// <summary>
        /// Computes the HMAC-SHA256 signature for the specified data using the provided key.
        /// </summary>
        /// <param name="data">The data to compute the HMAC for.</param>
        /// <param name="key">The key to use for the HMAC computation.</param>
        /// <returns>The computed HMAC signature as a byte array.</returns>
        public static byte[] Compute(byte[] data, byte[] key)
        {
            return Compute(data, 0, data.Length, key);
        }

        /// <summary>
        /// Computes the HMAC-SHA256 signature for a portion of the specified data using the provided key.
        /// </summary>
        /// <param name="data">The data to compute the HMAC for.</param>
        /// <param name="key">The key to use for the HMAC computation.</param>
        /// <param name="offset">The zero-based offset in the data at which to begin computing the HMAC.</param>
        /// <param name="length">The number of bytes from data to include in the HMAC computation.</param>
        /// <returns>The computed HMAC signature as a byte array.</returns>
        public static byte[] Compute(byte[] data, int offset, int length, byte[] key)
        {
            using (HMAC hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data, offset, length);
            }
        }

        /// <summary>
        /// Validates if the provided HMAC signature matches the computed HMAC for the specified data and key.
        /// </summary>
        /// <param name="data">The data to validate the HMAC against.</param>
        /// <param name="key">The key to use for HMAC computation.</param>
        /// <param name="hmac">The HMAC signature to validate.</param>
        /// <returns>True if the HMAC signature is valid; otherwise, false.</returns>
        public static bool Validate(byte[] data, byte[] key, byte[] hmac)
        {
            return Validate(data, 0, data.Length, key, hmac);
        }

        /// <summary>
        /// Validates if the provided HMAC signature matches the computed HMAC for a portion of the specified data and key.
        /// </summary>
        /// <param name="data">The data to validate the HMAC against.</param>
        /// <param name="key">The key to use for HMAC computation.</param>
        /// <param name="offset">The zero-based offset in the data at which to begin computing the HMAC.</param>
        /// <param name="length">The number of bytes from data to include in the HMAC computation.</param>
        /// <param name="hmac">The HMAC signature to validate.</param>
        /// <returns>True if the HMAC signature is valid; otherwise, false.</returns>
        public static bool Validate(byte[] data, int offset, int length, byte[] key, ReadOnlySpan<byte> hmac)
        {
            ReadOnlySpan<byte> hash = Compute(data, offset, length, key);
            if (hash.Length != hmac.Length)
                return false;

            return hash.SequenceEqual(hmac);
        }

        /// <summary>
        /// Generates a cryptographically secure random key for HMAC-SHA256 operations.
        /// </summary>
        /// <returns>A 32-byte (256-bit) random key as a byte array.</returns>
        public static byte[] GenerateKey()
        {
            byte[] key = new byte[_keySize / 8];
            RandomNumberGenerator.Fill(key);
            return key;
        }
    }
}
#pragma warning restore IDE0063