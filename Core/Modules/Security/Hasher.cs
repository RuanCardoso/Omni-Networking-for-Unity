using System;
using System.Security.Cryptography;
using System.Text;

namespace Omni.Core.Cryptography
{
    /// <summary>
    /// Defines the available hash algorithms that can be used for cryptographic operations.
    /// </summary>
    public enum HashAlgorithm
    {
        MD5,
        SHA1,
        SHA256,
        SHA384,
        SHA512,
        BCrypt
    }

    /// <summary>
    /// Provides methods for computing and validating cryptographic hashes using various algorithms.
    /// </summary>
    public class Hasher
    {
        /// <summary>
        /// Validates if the provided input matches the specified hash when processed with the selected algorithm.
        /// </summary>
        /// <param name="input">The input string to validate.</param>
        /// <param name="hash">The hash to compare against.</param>
        /// <param name="algorithm">The hash algorithm to use. Defaults to SHA256.</param>
        /// <returns>True if the input matches the hash; otherwise, false.</returns>
        public static bool Validate(string input, string hash, HashAlgorithm algorithm = HashAlgorithm.SHA256)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }

            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentException("Hash cannot be null or empty", nameof(hash));
            }

            if (algorithm == HashAlgorithm.BCrypt)
            {
                return BCrypt.Net.BCrypt.Verify(input, hash);
            }

            string hashOfInput = Compute(input, algorithm);
            return hashOfInput == hash;
        }

        /// <summary>
        /// Computes a hash value for the specified input string using the selected algorithm.
        /// </summary>
        /// <param name="input">The input string to hash.</param>
        /// <param name="algorithm">The hash algorithm to use. Defaults to SHA256.</param>
        /// <returns>A string representation of the computed hash in lowercase hexadecimal format.</returns>
        /// <exception cref="ArgumentException">Thrown when input is null or empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when an unsupported operation is attempted.</exception>
        /// <exception cref="NotImplementedException">Thrown when an unsupported algorithm is specified.</exception>
        public static string Compute(string input, HashAlgorithm algorithm = HashAlgorithm.SHA256)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }

            if (algorithm == HashAlgorithm.BCrypt)
            {
                return BCrypt.Net.BCrypt.HashPassword(input);
            }

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = Compute(inputBytes, algorithm);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Computes a hash value for the specified byte array using the selected algorithm.
        /// </summary>
        /// <param name="input">The byte array to hash.</param>
        /// <param name="algorithm">The hash algorithm to use. Defaults to SHA256.</param>
        /// <returns>A byte array containing the computed hash.</returns>
        /// <exception cref="ArgumentException">Thrown when input is null or empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when BCrypt is specified (not supported for byte arrays).</exception>
        /// <exception cref="NotImplementedException">Thrown when an unsupported algorithm is specified.</exception>
        public static byte[] Compute(byte[] input, HashAlgorithm algorithm = HashAlgorithm.SHA256)
        {
            if (input == null || input.Length == 0)
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }

            switch (algorithm)
            {
                case HashAlgorithm.MD5:
                    {
                        using MD5 md5 = MD5.Create();
                        return md5.ComputeHash(input);
                    }
                case HashAlgorithm.SHA1:
                    {
                        using SHA1 sha1 = SHA1.Create();
                        return sha1.ComputeHash(input);
                    }
                case HashAlgorithm.SHA256:
                    {
                        using SHA256 sha256 = SHA256.Create();
                        return sha256.ComputeHash(input);
                    }
                case HashAlgorithm.SHA384:
                    {
                        using SHA384 sha384 = SHA384.Create();
                        return sha384.ComputeHash(input);
                    }
                case HashAlgorithm.SHA512:
                    {
                        using SHA512 sha512 = SHA512.Create();
                        return sha512.ComputeHash(input);
                    }
                case HashAlgorithm.BCrypt:
                    {
                        throw new NotSupportedException("BCrypt hashing is not supported for byte arrays. Use string input instead.");
                    }
                default:
                    throw new NotImplementedException("Hash algorithm not implemented!");
            }
        }
    }
}