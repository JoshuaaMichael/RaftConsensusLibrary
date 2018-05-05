using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using TeamDecided.RaftNetworking.Exceptions;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking
{
    public class UDPNetworkingSecure : UDPNetworking
    {
        //private readonly SecureRandom random = new SecureRandom();

        //Dictionary<string, BaseMessage> sessionToStoredMessage;
        Dictionary<string, string> clientToSession;
        Dictionary<string, byte[]> sessionToSymetricKey;
        Dictionary<string, byte[]> sessionToHMACSecret;

        //Need to override SendMessage
        //Need to hook into RecieveMessage
        //Need to hook into the user, don't show it until they're authenticated

        //private byte[] passwordHashed;
        //private byte[] passwordSalt;

        private const int SYMETRIC_KEY_LENGTH_BITS = 128;
        private const int SYMETRIC_KEY_LENGTH_BYTES = SYMETRIC_KEY_LENGTH_BITS / 8;
        private const int HMAC_SECRET_LENGTH_BITS = 128;
        private const int HMAC_SECRET_LENGTH_BYTES = HMAC_SECRET_LENGTH_BITS / 8;
        private const int PASSWORD_HASH_LENGTH_BITS = 256;
        private const int PASSWORD_HASH_LENGTH_BYTES = PASSWORD_HASH_LENGTH_BITS / 8;
        //private const int DIFFIE_HELLMAN_CERTAINTY = 25; //Recommended value from BouncyCastle spec

        public UDPNetworkingSecure(string password)
        {
            clientToSession = new Dictionary<string, string>();
            sessionToSymetricKey = new Dictionary<string, byte[]>();
            sessionToHMACSecret = new Dictionary<string, byte[]>();
        }

        public override void SendMessage(BaseMessage message)
        {
            if(!clientToSession.ContainsKey(message.To))
            {
                //We need to setup a secure session
                ClientAuthStep1(message);
                return;
            }

            string session = clientToSession[message.To];
            byte[] symetricKey = sessionToSymetricKey[session];
            byte[] hmacSecret = sessionToHMACSecret[session];

            byte[] serialisedMessage = SerialiseMessage(message);
            byte[] encryptedMessage = Encrypt(serialisedMessage, symetricKey);

            string str1 = Encoding.UTF8.GetString(serialisedMessage);
            string str2 = Encoding.UTF8.GetString(encryptedMessage);

            byte[] hmacOfEncryptedMessage = GenerateHMAC(encryptedMessage, hmacSecret);

            IPEndPoint ipEndPoint = GetPeerIPEndPoint(message.To);

            SecureMessage secureMessage = new SecureMessage(ipEndPoint, session, encryptedMessage, hmacOfEncryptedMessage);

            base.SendMessage(secureMessage);
        }

        protected override BaseMessage DerivedMessageProcessing(BaseMessage message)
        {
            if (message.GetType() == typeof(SecureMessage))
            {
                SecureMessage secureMessage = (SecureMessage)message;

                string session = secureMessage.Session;
                byte[] hmacSecret = sessionToHMACSecret[session];
                byte[] hmacOfMessage = GenerateHMAC(secureMessage.EncryptedData, hmacSecret);

                if (!hmacOfMessage.SequenceEqual(secureMessage.HMAC))
                {
                    GenerateReceiveFailureException("HMAC of message was not equal to expected", null);
                    return null;
                }

                byte[] symetricKey = sessionToSymetricKey[session];
                byte[] plainText = Decrypt(secureMessage.EncryptedData, symetricKey);

                string str1 = Encoding.UTF8.GetString(secureMessage.EncryptedData);
                string str2 = Encoding.UTF8.GetString(plainText);

                BaseMessage returnMessage = DeserialiseMessage(plainText);

                return returnMessage;
            }
            else if (message.GetType() == typeof(SecureHello))
            {
                ServerAuthStep1((SecureHello)message);
                return null; //Consume the message, it's internal to UDPNetworkingSecure
            }
            else if (message.GetType() == typeof(SecureHelloResponse))
            {
                ClientAuthStep2((SecureHelloResponse)message);
                return null; //Consume the message, it's internal to UDPNetworkingSecure
            }
            else
            {
                GenerateReceiveFailureException("Unencrypted message recieved, this is unsupported", null);
                return null;
            }
        }

        protected override bool DerivedHandleMessageProcessing(object messageToProcess)
        {
            return false; //Consume the message, it can't be used
        }

        public void ManualAdd(string client, string session, byte[] symetricKey, byte[] hmacSecret)
        {
            clientToSession.Add(client, session);
            sessionToSymetricKey.Add(session, symetricKey);
            sessionToHMACSecret.Add(session, hmacSecret);
        }

        private void ClientAuthStep1(BaseMessage message)
        {

        }

        private void ServerAuthStep1(SecureHello message)
        {

        }

        private void ClientAuthStep2(SecureHelloResponse message)
        {

        }

        private static byte[] Encrypt(byte[] plainText, byte[] symetricKey)
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

        private static byte[] Decrypt(byte[] cipherText, byte[] symetricKey)
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

        private static byte[] GenerateHMAC(byte[] data, byte[] hashKey)
        {
            using (HMACSHA256 hmac = new HMACSHA256(hashKey))
            {
                return hmac.ComputeHash(data);
            }
        }

        private static bool VerifyHMAC(byte[] data, byte[] hashKey, byte[] hmac)
        {
            byte[] calcualtedHMAC = GenerateHMAC(data, hashKey);
            return calcualtedHMAC.SequenceEqual(hmac);
        }


        //private void ServerAuthStep1()
        //{
        //    DHParametersGenerator paramGen = new DHParametersGenerator();
        //    paramGen.Init(SYMETRIC_KEY_LENGTH_BITS + HMAC_SECRET_LENGTH_BITS, DIFFIE_HELLMAN_CERTAINTY, random);
        //    DHParameters parameters = paramGen.GenerateParameters();

        //    //testMutualVerification(new Srp6GroupParameters(parameters.P, parameters.G));

        //    Srp6VerifierGenerator gen = new Srp6VerifierGenerator();
        //    //gen.Init(group, new Sha256Digest());
        //    //BigInteger v = gen.GenerateVerifier(s, I, P);
        //}

    }
}
