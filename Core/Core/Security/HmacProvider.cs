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

namespace Omni.Core.Cryptography
{
    /// <summary>
    /// Specifies the available hash algorithms that can be used for computing 
    /// the HMAC (Hash-based Message Authentication Code).
    /// </summary>
    public enum HmacAlgorithm
    {
        /// <summary>
        /// Specifies the SHA256 hash algorithm.
        /// </summary>
        SHA256,

        /// <summary>
        /// Specifies the SHA384 hash algorithm.
        /// </summary>
        SHA384,

        /// <summary>
        /// Specifies the SHA512 hash algorithm.
        /// </summary>
        SHA512
    }

    /// <summary>
    /// Provides methods for computing and validating cryptographic 
    /// hashes using various HMAC (Hash-based Message Authentication Code) algorithms.
    /// </summary>
    public static class HmacProvider
    {
        /// <summary>
        /// Computes the HMAC (Hash-based Message Authentication Code) for the specified data using the specified key and algorithm.
        /// </summary>
        /// <param name="data">The data to compute the HMAC for.</param>
        /// <param name="key">The key to use for computing the HMAC.</param>
        /// <param name="algorithm">The hash algorithm to use. Defaults to SHA256.</param>
        /// <returns>A byte array containing the computed HMAC.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data or key is null.</exception>
        public static byte[] Compute(byte[] data, byte[] key, HmacAlgorithm algorithm = HmacAlgorithm.SHA256)
        {
            return Compute(data, 0, data.Length, key, algorithm);
        }

        /// <summary>
        /// Computes the HMAC (Hash-based Message Authentication Code) for the specified data using the specified key and algorithm.
        /// </summary>
        /// <param name="data">The data to compute the HMAC for.</param>
        /// <param name="offset">The offset into the data array at which the data begins.</param>
        /// <param name="length">The number of bytes to use from the data array.</param>
        /// <param name="key">The key to use for computing the HMAC.</param>
        /// <param name="algorithm">The hash algorithm to use. Defaults to SHA256.</param>
        /// <returns>A byte array containing the computed HMAC.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data or key is null.</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid algorithm is specified.</exception>
        public static byte[] Compute(byte[] data, int offset, int length, byte[] key, HmacAlgorithm algorithm = HmacAlgorithm.SHA256)
        {
            if (data == null || key == null)
                throw new ArgumentNullException("Data or Key cannot be null.");

            using HMAC alg = algorithm switch
            {
                HmacAlgorithm.SHA256 => new HMACSHA256(key),
                HmacAlgorithm.SHA384 => new HMACSHA384(key),
                HmacAlgorithm.SHA512 => new HMACSHA512(key),
                _ => throw new ArgumentException("Invalid algorithm specified.")
            };

            return alg.ComputeHash(data, offset, length);
        }

        /// <summary>
        /// Validates an HMAC (Hash-based Message Authentication Code) against the computed HMAC for the specified data using the specified key and algorithm.
        /// </summary>
        /// <param name="data">The data used to compute the HMAC.</param>
        /// <param name="key">The key used to compute the HMAC.</param>
        /// <param name="hmac">The HMAC to validate against.</param>
        /// <param name="algorithm">The hash algorithm to use. Defaults to SHA256.</param>
        /// <returns>True if the computed HMAC matches the provided HMAC; otherwise, false.</returns>
        public static bool Validate(byte[] data, byte[] key, byte[] hmac, HmacAlgorithm algorithm = HmacAlgorithm.SHA256)
        {
            return Validate(data, 0, data.Length, key, hmac, algorithm);
        }

        /// <summary>
        /// Validates an HMAC (Hash-based Message Authentication Code) against the computed HMAC for the specified data using the specified key and algorithm.
        /// </summary>
        /// <param name="data">The data used to compute the HMAC.</param>
        /// <param name="offset">The offset into the data array at which the data begins.</param>
        /// <param name="length">The number of bytes to use from the data array.</param>
        /// <param name="key">The key used to compute the HMAC.</param>
        /// <param name="hmac">The HMAC to validate against.</param>
        /// <param name="algorithm">The hash algorithm to use. Defaults to SHA256.</param>
        /// <returns>True if the computed HMAC matches the provided HMAC; otherwise, false.</returns>
        public static bool Validate(byte[] data, int offset, int length, byte[] key, ReadOnlySpan<byte> hmac, HmacAlgorithm algorithm = HmacAlgorithm.SHA256)
        {
            ReadOnlySpan<byte> hash = Compute(data, offset, length, key, algorithm);
            if (hash.Length != hmac.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(hash, hmac);
        }

        /// <summary>
        /// Generates a random key for the specified HMAC algorithm.
        /// </summary>
        /// <param name="algorithm">The HMAC algorithm to use. Defaults to SHA256.</param>
        /// <returns>A byte array containing the generated key.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported algorithm is specified.</exception>
        public static byte[] GenerateKey(HmacAlgorithm algorithm = HmacAlgorithm.SHA256)
        {
            int keySize = algorithm switch
            {
                HmacAlgorithm.SHA256 => 256,
                HmacAlgorithm.SHA384 => 384,
                HmacAlgorithm.SHA512 => 512,
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), "Unsupported HMAC algorithm.")
            };

            byte[] key = new byte[keySize / 8];
            RandomNumberGenerator.Fill(key);
            return key;
        }
    }
}