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
    /// Provides AES encryption and decryption services.<br/>
    /// Supports 128, 192 and 256 bit keys.
    /// </summary>
    public class AesCryptography
    {
        public static byte[] Encrypt(byte[] data, int offset, int length, byte[] key, out byte[] Iv)
        {
            if (key == null || key.Length < 16)
            {
                throw new ArgumentException(
                    "The encryption key is not provided or is invalid. Please provide a valid key."
                );
            }

            using (Aes aes = Aes.Create())
            {
                try
                {
                    aes.KeySize = 128; // 128 bit key
                    aes.BlockSize = 128; // 128 bit block size
                    aes.Mode = CipherMode.CBC; // Cipher Block Chaining
                    aes.Padding = PaddingMode.PKCS7;

                    aes.Key = key;
                    aes.GenerateIV(); // Generate unique and random IV
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

        public static byte[] Decrypt(byte[] data, int offset, int length, byte[] key, byte[] Iv)
        {
            if (key == null || key.Length < 16)
            {
                throw new ArgumentException(
                    "The encryption key is not provided or is invalid. Please provide a valid key."
                );
            }

            using (Aes aes = Aes.Create())
            {
                try
                {
                    aes.KeySize = 128;
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

        public static byte[] GenerateKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateKey();
                return aes.Key;
            }
        }
    }
}
#pragma warning restore IDE0063