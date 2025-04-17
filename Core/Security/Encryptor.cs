using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable

// https://stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp

namespace Omni.Core.Cryptography
{
    /// <summary>
    /// Provides encryption and decryption functionality using AES algorithm.
    /// </summary>
    /// <remarks>
    /// This class implements AES encryption with CBC mode and PKCS7 padding.
    /// It uses PBKDF2 for key derivation with a random salt.
    /// The encrypted output format is: [salt (16 bytes)][IV (16 bytes)][encrypted data]
    /// </remarks>
    public static class Encryptor
    {
        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int KeySize = 128;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        /// <summary>
        /// Encrypts a plaintext string using a passphrase.
        /// </summary>
        /// <param name="plainText">The readable text to be encrypted.</param>
        /// <param name="passPhrase">The passphrase used to encrypt the text.</param>
        /// <returns>A byte array containing the encrypted data.</returns>
        public static byte[] Encrypt(string plainText, string passPhrase)
        {
            return Encrypt(plainText, Encoding.UTF8.GetBytes(passPhrase));
        }

        /// <summary>
        /// Encrypts a plaintext string using a byte array passphrase.
        /// </summary>
        /// <param name="plainText">The readable text to be encrypted.</param>
        /// <param name="passPhrase">The byte array used as encryption key.</param>
        /// <returns>A byte array containing the encrypted data.</returns>
        public static byte[] Encrypt(string plainText, byte[] passPhrase)
        {
            return Encrypt(Encoding.UTF8.GetBytes(plainText), passPhrase);
        }

        /// <summary>
        /// Encrypts a byte array using a string passphrase.
        /// </summary>
        /// <param name="data">The byte array to be encrypted.</param>
        /// <param name="passPhrase">The passphrase used to encrypt the data.</param>
        /// <returns>A byte array containing the encrypted data.</returns>
        public static byte[] Encrypt(byte[] data, string passPhrase)
        {
            return Encrypt(data, Encoding.UTF8.GetBytes(passPhrase));
        }

        /// <summary>
        /// Encrypts a byte array using a byte array passphrase.
        /// </summary>
        /// <param name="data">The byte array to be encrypted.</param>
        /// <param name="passPhrase">The byte array used as encryption key.</param>
        /// <returns>A byte array containing the salt, IV, and encrypted data.</returns>
        /// <remarks>
        /// This method performs the actual encryption process:
        /// 1. Generates random salt and initialization vector (IV)
        /// 2. Derives a key from the passphrase using PBKDF2
        /// 3. Creates an AES encryption algorithm with CBC mode and PKCS7 padding
        /// 4. Encrypts the data using the derived key and IV
        /// 5. Concatenates the salt, IV, and encrypted data
        /// 6. Returns the result as a byte array
        /// 
        /// The salt and IV are prepended to the encrypted data so they can be
        /// extracted during decryption to recreate the same encryption parameters.
        /// </remarks>
        /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
        public static byte[] Encrypt(byte[] data, byte[] passPhrase)
        {
            try
            {
                var saltStringBytes = Generate128BitsOfRandomEntropy();
                var ivStringBytes = Generate128BitsOfRandomEntropy();
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(KeySize / 8);
                    using (var symmetricKey = Aes.Create())
                    {
                        symmetricKey.KeySize = KeySize;
                        symmetricKey.BlockSize = 128;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(data, 0, data.Length);
                                    cryptoStream.FlushFinalBlock();
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    return cipherTextBytes;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }

        /// <summary>
        /// Decrypts data that was encrypted using the Encrypt method.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="passPhrase">The password used for decryption as a string.</param>
        /// <returns>The decrypted data as a byte array.</returns>
        public static byte[] Decrypt(byte[] encryptedData, string passPhrase)
        {
            return Decrypt(encryptedData, Encoding.UTF8.GetBytes(passPhrase));
        }

        /// <summary>
        /// Decrypts data that was encrypted using the Encrypt method.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="passPhrase">The byte array containing the password used for decryption.</param>
        /// <returns>The decrypted data as a byte array.</returns>
        /// <remarks>
        /// This method decrypts data that was encrypted using the Encrypt method.
        /// The encryptedData is expected to be in the format of [salt] + [IV] + [encrypted data].
        /// The first 16 bytes represent the salt, the next 16 bytes represent the IV,
        /// and the remaining bytes represent the actual encrypted data.
        /// </remarks>
        /// <exception cref="CryptographicException">Thrown when decryption fails.</exception>
        public static byte[] Decrypt(byte[] encryptedData, byte[] passPhrase)
        {
            try
            {
                var saltStringBytes = encryptedData.Take(KeySize / 8).ToArray();
                var ivStringBytes = encryptedData.Skip(KeySize / 8).Take(KeySize / 8).ToArray();
                var cipherTextBytes = encryptedData.Skip((KeySize / 8) * 2)
                    .Take(encryptedData.Length - ((KeySize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(KeySize / 8);
                    using (var symmetricKey = Aes.Create())
                    {
                        symmetricKey.KeySize = KeySize;
                        symmetricKey.BlockSize = 128;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    using (var resultStream = new MemoryStream())
                                    {
                                        cryptoStream.CopyTo(resultStream);
                                        return resultStream.ToArray();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException(ex.Message);
            }
        }

        /// <summary>
        /// Decrypts data that was encrypted using the Encrypt method and converts it to a string.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="passPhrase">The password used for decryption as a string.</param>
        /// <returns>The decrypted string.</returns>
        public static string DecryptToString(byte[] encryptedData, string passPhrase)
        {
            return DecryptToString(encryptedData, Encoding.UTF8.GetBytes(passPhrase));
        }

        /// <summary>
        /// Decrypts data that was encrypted using the Encrypt method and converts it to a string.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="passPhrase">The byte array containing the password used for decryption.</param>
        /// <returns>The decrypted string.</returns>
        public static string DecryptToString(byte[] encryptedData, byte[] passPhrase)
        {
            var decryptedBytes = Decrypt(encryptedData, passPhrase);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Generates 16 bytes (128 bits) of cryptographically secure random data.
        /// </summary>
        /// <returns>A byte array containing 16 random bytes.</returns>
        private static byte[] Generate128BitsOfRandomEntropy()
        {
            byte[] randomBytes = new byte[16];
            using var rngCsp = RandomNumberGenerator.Create();
            rngCsp.GetBytes(randomBytes);
            return randomBytes;
        }
    }
}