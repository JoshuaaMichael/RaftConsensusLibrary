using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    internal static class CryptoHelper
    {
        private const int SymetricKeyLengthBits = 128;
        private const int SymetricKeyLengthBytes = SymetricKeyLengthBits / 8;

        internal static byte[] Encrypt(byte[] plainText, byte[] symetricKey)
        {
            using (Aes aes = Aes.Create())
            {
                if (plainText == null || plainText.Length == 0)
                {
                    throw new ArgumentNullException("plainText is invalid");
                }

                if (symetricKey == null || symetricKey.Length != SymetricKeyLengthBytes)
                {
                    throw new ArgumentNullException("symetricKey is invalid");
                }

                if (aes == null)
                {
                    throw new Exception("Failed to create AES object");
                }

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
                throw new ArgumentNullException("symetricKey is invalid");
            }

            using (Aes aes = Aes.Create())
            {
                if (aes == null)
                {
                    throw new Exception("Failed to create AES object");
                }

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

        internal static byte[] GenerateHmac(byte[] data, byte[] hashKey)
        {
            using (HMACSHA256 hmac = new HMACSHA256(hashKey))
            {
                return hmac.ComputeHash(data);
            }
        }

        internal static bool VerifyHmac(byte[] data, byte[] hashKey, byte[] hmac)
        {
            byte[] calcualtedHmac = GenerateHmac(data, hashKey);
            return calcualtedHmac.SequenceEqual(hmac);
        }

        internal static byte[] CompleteChallenge(byte[] password, byte[] challenge)
        {
            byte[] temp = new byte[password.Length + challenge.Length];
            password.CopyTo(temp, 0);
            challenge.CopyTo(temp, password.Length);
            return new SHA256Managed().ComputeHash(temp);
        }

        internal static bool VerifyChallenge(byte[] password, byte[] challenge, byte[] challengeAttempt)
        {
            byte[] completedChallenge = CompleteChallenge(password, challenge);
            return challengeAttempt.SequenceEqual(completedChallenge);
        }

        internal static byte[] RsaEncrypt(byte[] plainText, byte[] publicKey)
        {
            byte[] encryptedData;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                string publicKeyStr = Encoding.UTF8.GetString(publicKey);
                rsa.FromXmlString(publicKeyStr);
                encryptedData = rsa.Encrypt(plainText, true);
            }
            return encryptedData;
        }

        internal static byte[] RsaDecrypt(byte[] dataToDecrypt, RSAParameters rsaKeyInfo)
        {
            byte[] decryptedData;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaKeyInfo); //Needs private info
                decryptedData = rsa.Decrypt(dataToDecrypt, true);
            }
            return decryptedData;
        }

        internal static bool IsPublicKey(byte[] publicKey)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
                {
                    rsa.FromXmlString(Encoding.UTF8.GetString(publicKey));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
