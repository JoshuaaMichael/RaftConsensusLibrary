using System;
using System.IO;
using System.Security.Cryptography;

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    internal static class CryptoHelper
    {
        private const int SymetricKeyLengthBits = 256;
        private const int SymetricKeyLengthBytes = SymetricKeyLengthBits / 8;

        internal static byte[] Encrypt(byte[] plainText, byte[] symetricKey)
        {
            using (Aes aes = Aes.Create())
            {
                if (plainText == null || plainText.Length == 0)
                {
                    throw new ArgumentNullException(nameof(plainText));
                }

                if (symetricKey == null || symetricKey.Length != SymetricKeyLengthBytes)
                {
                    throw new ArgumentNullException(nameof(plainText));
                }

                if (aes == null)
                {
                    throw new Exception("Failed to create AES object");
                }

                aes.KeySize = SymetricKeyLengthBits;
                aes.Key = symetricKey;

                using (MemoryStream cipherTextStream = new MemoryStream())
                {
                    cipherTextStream.Write(aes.IV, 0, aes.IV.Length);
                    using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(cipherTextStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (BinaryWriter bwEncrypt = new BinaryWriter(csEncrypt))
                            {
                                bwEncrypt.Write(plainText);
                            }
                            return cipherTextStream.ToArray();
                        }
                    }
                }
            }
        }

        internal static byte[] Decrypt(byte[] cipherText, byte[] symetricKey)
        {
            if (cipherText == null || cipherText.Length == 0)
            {
                throw new ArgumentNullException("cipherText is invalid");
            }

            if (symetricKey == null || symetricKey.Length != SymetricKeyLengthBytes)
            {
                throw new ArgumentNullException(nameof(cipherText));
            }

            using (Aes aes = Aes.Create())
            {
                if (aes == null)
                {
                    throw new Exception("Failed to create AES object");
                }

                aes.KeySize = SymetricKeyLengthBits;
                aes.Key = symetricKey;
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    byte[] aesIv = new byte[aes.IV.Length];
                    msDecrypt.Read(aesIv, 0, aes.IV.Length);
                    aes.IV = aesIv;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (BinaryReader brDecrypt = new BinaryReader(csDecrypt))
                            {
                                using (MemoryStream plainTextStream = new MemoryStream())
                                {
                                    byte[] buffer = new byte[256];
                                    int count;

                                    while ((count = brDecrypt.Read(buffer, 0, buffer.Length)) != 0)
                                    {
                                        plainTextStream.Write(buffer, 0, count);
                                    }

                                    return plainTextStream.ToArray();
                                }
                            }
                        }
                    }
                }

            }
        }
    }
}
