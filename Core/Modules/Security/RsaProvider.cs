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
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

#pragma warning disable IDE0063
namespace Omni.Core.Cryptography
{
    /// <summary>
    /// Provides extension methods for RSA key import/export in PEM format for legacy .NET versions.
    /// </summary>
    public static class RsaPemLegacyExtensions
    {
        /// <summary>
        /// Converts an RSA key to a PEM formatted string.
        /// </summary>
        /// <param name="rsa">The RSA key to convert.</param>
        /// <param name="isPrivate">Whether the key is a private key or a public key.</param>
        /// <returns>A PEM formatted string representing the RSA key.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rsa"/> is null.</exception>
        public static string ToPemString(this RSA rsa, bool isPrivate)
        {
            if (rsa == null)
                throw new ArgumentNullException(nameof(rsa));

            object keyObject;
            if (isPrivate)
            {
                var ackp = DotNetUtilities.GetRsaKeyPair(rsa);
                keyObject = ackp;
            }
            else
            {
                var keyParameters = DotNetUtilities.GetRsaPublicKey(rsa);
                keyObject = keyParameters;
            }

            using var writer = new StringWriter();
            using var pemWriter = new PemWriter(writer);
            pemWriter.WriteObject(keyObject);
            return writer.ToString();
        }

        /// <summary>
        /// Converts a PEM formatted string to an RSA key.
        /// </summary>
        /// <param name="rsa">The RSA key to convert.</param>
        /// <param name="pem">A PEM formatted string representing the RSA key.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rsa"/> or <paramref name="pem"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="pem"/> is empty or whitespace.</exception>
        public static void FromPemString(this RSA rsa, string pem)
        {
            if (rsa == null)
                throw new ArgumentNullException(nameof(rsa));

            if (string.IsNullOrWhiteSpace(pem))
                throw new ArgumentException("PEM string cannot be null or empty.", nameof(pem));

            using var reader = new StringReader(pem);
            using var pemReader = new PemReader(reader);
            object pemObject = pemReader.ReadObject();
            switch (pemObject)
            {
                case AsymmetricCipherKeyPair keyPair:
                    {
                        var privKey = (RsaPrivateCrtKeyParameters)keyPair.Private;
                        RSAParameters parameters = DotNetUtilities.ToRSAParameters(privKey);
                        rsa.ImportParameters(parameters);
                        break;
                    }

                case RsaPrivateCrtKeyParameters privKey:
                    {
                        RSAParameters parameters = DotNetUtilities.ToRSAParameters(privKey);
                        rsa.ImportParameters(parameters);
                        break;
                    }

                case RsaKeyParameters pubKey:
                    {
                        RSAParameters parameters = DotNetUtilities.ToRSAParameters(pubKey);
                        rsa.ImportParameters(parameters);
                        break;
                    }

                default:
                    throw new CryptographicException("Unsupported PEM format or key type.");
            }
        }
    }

    public enum RsaKeyFormat
    {
        /// <summary>
        /// Specifies the PEM format for RSA keys.
        /// </summary>
        Pem,

        /// <summary>
        /// Specifies the XML format for RSA keys.
        /// </summary>
        Xml
    }

    public enum RsaHashAlgorithm
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
    /// Provides RSA encryption, decryption, signing and validation functionality.
    /// Uses different key sizes based on build configuration for performance/security balance.
    /// All methods are thread-safe.
    /// </summary>
    public static class RsaProvider
    {
        // The code implements RSA cryptography in C#. It differentiates key sizes (512 bits for UNITY_EDITOR and 2048 bits for other environments) likely due to a consideration of security and performance.
        // Larger keys offer more security but require more computational resources. Thus, a smaller key may be preferred during development (UNITY_EDITOR) to improve performance, while a larger key is used in production environments to ensure adequate security.
        // This differentiation enables a balance between security and performance, adapting to the execution environment.
#if OMNI_DEBUG
        private const int keySize = 512;
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
        /// <param name="privateKey">The generated private key in XML/PEM format.</param>
        /// <param name="publicKey">The generated public key in XML/PEM format.</param>
        public static void GetKeys(out string privateKey, out string publicKey, RsaKeyFormat format = RsaKeyFormat.Pem)
        {
            using (RSA rsa = RSA.Create(keySize))
            {
                if (format == RsaKeyFormat.Pem)
                {
                    privateKey = rsa.ToPemString(isPrivate: true);
                    publicKey = rsa.ToPemString(isPrivate: false);
                }
                else
                {
                    privateKey = rsa.ToXmlString(includePrivateParameters: true);
                    publicKey = rsa.ToXmlString(includePrivateParameters: false);
                }
            }
        }

        /// <summary>
        /// Encrypts data using RSA with the provided public key.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="publicKey">The public key in XML/PEM format.</param>
        /// <returns>The encrypted data.</returns>
        /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
        public static byte[] Encrypt(byte[] data, string publicKey, RsaKeyFormat format = RsaKeyFormat.Pem)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("The data to encrypt cannot be null or empty.", nameof(data));

            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentException("The public key cannot be null or empty.", nameof(publicKey));

            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    if (format == RsaKeyFormat.Pem)
                    {
                        rsa.FromPemString(publicKey);
                    }
                    else
                    {
                        rsa.FromXmlString(publicKey);
                    }

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
        /// <param name="privateKey">The private key in XML/PEM format.</param>
        /// <returns>The decrypted data.</returns>
        /// <exception cref="CryptographicException">Thrown when decryption fails.</exception>
        public static byte[] Decrypt(byte[] data, string privateKey, RsaKeyFormat format = RsaKeyFormat.Pem)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("The data to encrypt cannot be null or empty.", nameof(data));

            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentException("The private key cannot be null or empty.", nameof(privateKey));

            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    if (format == RsaKeyFormat.Pem)
                    {
                        rsa.FromPemString(privateKey);
                    }
                    else
                    {
                        rsa.FromXmlString(privateKey);
                    }

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
        /// <param name="publicKey">The public key in XML/PEM format.</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        /// <exception cref="CryptographicException">Thrown when validation fails.</exception>
        public static bool Validate(byte[] data, byte[] signature, string publicKey, RsaKeyFormat format = RsaKeyFormat.Pem, RsaHashAlgorithm hashAlgorithm = RsaHashAlgorithm.SHA256)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("The data to encrypt cannot be null or empty.", nameof(data));

            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentException("The public key cannot be null or empty.", nameof(publicKey));

            if (signature == null || signature.Length == 0)
                throw new ArgumentException("The signature cannot be null or empty.", nameof(signature));

            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    if (format == RsaKeyFormat.Pem)
                    {
                        rsa.FromPemString(publicKey);
                    }
                    else
                    {
                        rsa.FromXmlString(publicKey);
                    }

                    var hashAlgorithmName = hashAlgorithm switch
                    {
                        RsaHashAlgorithm.SHA256 => HashAlgorithmName.SHA256,
                        RsaHashAlgorithm.SHA384 => HashAlgorithmName.SHA384,
                        RsaHashAlgorithm.SHA512 => HashAlgorithmName.SHA512,
                        _ => throw new ArgumentException("Invalid hash algorithm specified."),
                    };

                    return rsa.VerifyData(data, signature, hashAlgorithmName, RSASignaturePadding.Pkcs1);
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
        /// <param name="privateKey">The private key in XML/PEM format.</param>
        /// <returns>The signature for the data.</returns>
        /// <exception cref="CryptographicException">Thrown when signing fails.</exception>
        public static byte[] Compute(byte[] data, string privateKey, RsaKeyFormat format = RsaKeyFormat.Pem, RsaHashAlgorithm hashAlgorithm = RsaHashAlgorithm.SHA256)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("The data to encrypt cannot be null or empty.", nameof(data));

            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentException("The private key cannot be null or empty.", nameof(privateKey));

            try
            {
                using (RSA rsa = RSA.Create(keySize))
                {
                    if (format == RsaKeyFormat.Pem)
                    {
                        rsa.FromPemString(privateKey);
                    }
                    else
                    {
                        rsa.FromXmlString(privateKey);
                    }

                    var hashAlgorithmName = hashAlgorithm switch
                    {
                        RsaHashAlgorithm.SHA256 => HashAlgorithmName.SHA256,
                        RsaHashAlgorithm.SHA384 => HashAlgorithmName.SHA384,
                        RsaHashAlgorithm.SHA512 => HashAlgorithmName.SHA512,
                        _ => throw new ArgumentException("Invalid hash algorithm specified."),
                    };

                    return rsa.SignData(data, hashAlgorithmName, RSASignaturePadding.Pkcs1);
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