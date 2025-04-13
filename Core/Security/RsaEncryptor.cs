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
    /// Provides RSA encryption, decryption, signing and validation functionality.
    /// Uses different key sizes based on build configuration for performance/security balance.
    /// </summary>
    public static class RsaEncryptor
    {
        // The code implements RSA cryptography in C#. It differentiates key sizes (1024 bits for UNITY_EDITOR and 2048 bits for other environments) likely due to a consideration of security and performance.
        // Larger keys offer more security but require more computational resources. Thus, a smaller key may be preferred during development (UNITY_EDITOR) to improve performance, while a larger key is used in production environments to ensure adequate security.
        // This differentiation enables a balance between security and performance, adapting to the execution environment.
#if OMNI_DEBUG
        private const int keySize = 1024;
#else
        // 2048 bits is the minimum recommended key size for production environments.
        private const int keySize = 2048;
#endif

        /// <summary>
        /// Gets the maximum data size that can be encrypted with the current key size.
        /// /// </summary>
        public static int GetMaxDataSize()
        {
            return (keySize / 8) - 11;
        }

        /// <summary>
        /// Generates a new RSA key pair.
        /// </summary>
        /// <param name="privateKey">The generated private key in XML format.</param>
        /// <param name="publicKey">The generated public key in XML format.</param>
        public static void GetKeys(out string privateKey, out string publicKey)
        {
            using (RSA rsa = RSA.Create(keySize))
            {
                privateKey = rsa.ToXmlString(true);
                publicKey = rsa.ToXmlString(false);
            }
        }

        /// <summary>
        /// Encrypts data using RSA with the provided public key.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="publicKey">The public key in XML format.</param>
        /// <returns>The encrypted data.</returns>
        /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
        public static byte[] Encrypt(byte[] data, string publicKey)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("The data to encrypt cannot be null or empty.", nameof(data));

            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentException("The public key cannot be null or empty.", nameof(publicKey));

            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    rsa.FromXmlString(publicKey);
                    return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }

        /// <summary>
        /// Decrypts data using RSA with the provided private key.
        /// </summary>
        /// <param name="data">The encrypted data to decrypt.</param>
        /// <param name="privateKey">The private key in XML format.</param>
        /// <returns>The decrypted data.</returns>
        /// <exception cref="CryptographicException">Thrown when decryption fails.</exception>
        public static byte[] Decrypt(byte[] data, string privateKey)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("The data to encrypt cannot be null or empty.", nameof(data));

            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentException("The private key cannot be null or empty.", nameof(privateKey));

            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    rsa.FromXmlString(privateKey);
                    return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }

        /// <summary>
        /// Validates if the signature for the provided data is valid using the specified public key.
        /// </summary>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to validate.</param>
        /// <param name="publicKey">The public key in XML format.</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        /// <exception cref="CryptographicException">Thrown when validation fails.</exception>
        public static bool Validate(byte[] data, byte[] signature, string publicKey)
        {
            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    rsa.FromXmlString(publicKey);
                    return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }

        /// <summary>
        /// Signs data using RSA with the provided private key.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="privateKey">The private key in XML format.</param>
        /// <returns>The signature for the data.</returns>
        /// <exception cref="CryptographicException">Thrown when signing fails.</exception>
        public static byte[] Sign(byte[] data, string privateKey)
        {
            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    rsa.FromXmlString(privateKey);
                    return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }
    }
}
#pragma warning restore IDE0063