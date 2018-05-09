using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TeamDecided.RaftNetworking.Helpers
{
    internal static class CryptoHelper
    {
        private const int SYMETRIC_KEY_LENGTH_BITS = 128;
        private const int SYMETRIC_KEY_LENGTH_BYTES = SYMETRIC_KEY_LENGTH_BITS / 8;
        private const int HMAC_SECRET_LENGTH_BITS = 128;
        private const int HMAC_SECRET_LENGTH_BYTES = HMAC_SECRET_LENGTH_BITS / 8;

        internal static byte[] Encrypt(byte[] plainText, byte[] symetricKey)
        {
            using (Aes aes = Aes.Create())
            {
                if (plainText == null || plainText.Length == 0)
                    throw new ArgumentNullException("plainText is invalid");
                if (symetricKey == null || symetricKey.Length != SYMETRIC_KEY_LENGTH_BYTES)
                    throw new ArgumentNullException("symetricKey is invalid");

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
                            byte[] cipherText = cipherTextStream.ToArray();
                            return cipherText;
                        }
                    }
                }
            }
        }

        internal static byte[] Decrypt(byte[] cipherText, byte[] symetricKey)
        {
            if (cipherText == null || cipherText.Length == 0)
                throw new ArgumentNullException("cipherText is invalid");
            if (symetricKey == null || symetricKey.Length != SYMETRIC_KEY_LENGTH_BYTES)
                throw new ArgumentNullException("symetricKey is invalid");

            using (Aes aes = Aes.Create())
            {
                aes.Key = symetricKey;
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    byte[] aesIV = new byte[aes.IV.Length];
                    msDecrypt.Read(aesIV, 0, aes.IV.Length);
                    aes.IV = aesIV;
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
                                        plainTextStream.Write(buffer, 0, count);
                                    return plainTextStream.ToArray();
                                }
                            }
                        }
                    }
                }

            }
        }

        internal static byte[] GenerateHMAC(byte[] data, byte[] hashKey)
        {
            using (HMACSHA256 hmac = new HMACSHA256(hashKey))
            {
                return hmac.ComputeHash(data);
            }
        }

        internal static bool VerifyHMAC(byte[] data, byte[] hashKey, byte[] hmac)
        {
            byte[] calcualtedHMAC = GenerateHMAC(data, hashKey);
            return calcualtedHMAC.SequenceEqual(hmac);
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

        internal static byte[] RSAEncrypt(byte[] plainText, byte[] publicKey)
        {
            byte[] encryptedData;
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048))
            {
                RSA.FromXmlString(Encoding.UTF8.GetString(publicKey));
                encryptedData = RSA.Encrypt(plainText, true);
            }
            return encryptedData;
        }

        internal static byte[] RSADecrypt(byte[] DataToDecrypt, RSAParameters RSAKeyInfo)
        {
            byte[] decryptedData;
            using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
            {
                RSA.ImportParameters(RSAKeyInfo); //Needs private info
                decryptedData = RSA.Decrypt(DataToDecrypt, true);
            }
            return decryptedData;
        }
    }
}
