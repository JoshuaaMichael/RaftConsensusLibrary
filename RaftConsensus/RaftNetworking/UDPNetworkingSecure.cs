using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TeamDecided.RaftNetworking.Enums;
using TeamDecided.RaftNetworking.Helpers;
using TeamDecided.RaftNetworking.Messages;

/* TODO:
 *      Make a thread which times out messages, raises errors where required, will need an extra thread
 *      See about moving away from From/To given in message, and use session lookup
 *      BaseMessage, auto set from
 *      Solve the case of repeatedly trying to connect
 */

namespace TeamDecided.RaftNetworking
{
    public sealed class UDPNetworkingSecure : UDPNetworking
    {
        private static readonly RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

        private Dictionary<string, Queue<BaseMessage>> clientToStoredMessage;
        private object clientToStoredMessageLockObject;
        private Dictionary<string, string> clientToSession;
        private object clientToSessionLockObject;
        private Dictionary<string, byte[]> sessionToSymetricKey;
        private object sessionToSymetricKeyLockObject;
        private Dictionary<string, byte[]> sessionToHMACSecret;
        private object sessionToHMACSecretLockObject;
        private Dictionary<string, byte[]> sessionToChallenge;
        private object sessionToChallengeLockObject;

        private byte[] passwordBytes;
        private RSAParameters rsaPair;
        private byte[] rsaPublicKeyBytes;

        public UDPNetworkingSecure(string password)
        {
            passwordBytes = Encoding.UTF8.GetBytes(password);

            clientToStoredMessage = new Dictionary<string, Queue<BaseMessage>>();
            clientToStoredMessageLockObject = new object();
            clientToSession = new Dictionary<string, string>();
            clientToSessionLockObject = new object();
            sessionToSymetricKey = new Dictionary<string, byte[]>();
            sessionToSymetricKeyLockObject = new object();
            sessionToHMACSecret = new Dictionary<string, byte[]>();
            sessionToHMACSecretLockObject = new object();
            sessionToChallenge = new Dictionary<string, byte[]>();
            sessionToChallengeLockObject = new object();

            RSACryptoServiceProvider rsaPairGen = new RSACryptoServiceProvider(2048);
            rsaPair = rsaPairGen.ExportParameters(true);
            rsaPublicKeyBytes = Encoding.UTF8.GetBytes(rsaPairGen.ToXmlString(false));
        }

        public override void SendMessage(BaseMessage message)
        {
            string session;
            bool clientToSessionContainsKey;
            lock (clientToSessionLockObject)
            {
                clientToSessionContainsKey = clientToSession.TryGetValue(message.To, out session);
            }

            if (!clientToSessionContainsKey)
            {
                SendSecureClientHello(message); //We need to setup a secure session
                return;
            }

            byte[] symetricKey;
            bool sessionToSymetricKeyContainKey;
            lock (sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKeyContainKey = sessionToSymetricKey.TryGetValue(session, out symetricKey);
            }

            byte[] hmacSecret;
            bool sessionToHMACSecretContainKey;
            lock (sessionToHMACSecretLockObject)
            {
                sessionToHMACSecretContainKey = sessionToHMACSecret.TryGetValue(session, out hmacSecret);
            }

            if (!sessionToSymetricKeyContainKey || !sessionToHMACSecretContainKey)
            {
                GenerateSendFailureException("Failed to locate required encryption information to send message", message);
                return;
            }

            byte[] serialisedMessage = SerialiseMessage(message);
            byte[] encryptedMessage = CryptoHelper.Encrypt(serialisedMessage, symetricKey);
            byte[] hmacOfEncryptedMessage = CryptoHelper.GenerateHMAC(encryptedMessage, hmacSecret);

            IPEndPoint ipEndPoint = GetPeerIPEndPoint(message.To);

            SecureMessage secureMessage = new SecureMessage(ipEndPoint, session, encryptedMessage, hmacOfEncryptedMessage);

            base.SendMessage(secureMessage);
        }

        protected override BaseMessage DerivedMessageProcessing(BaseMessage message, IPEndPoint ipEndPoint)
        {
            if (message.GetType().IsSubclassOf(typeof(SecureMessage)) || message.GetType() == typeof(SecureMessage))
            {
                //AES Encrypted message, unknown internal message
                BaseMessage decryptedMessage = null;
                if (message.GetType() == typeof(SecureMessage)) //AES Encrypted
                {
                    decryptedMessage = HandleSecureMessage((SecureMessage)message);
                }
                //Non-AES Encrypted hankshaking messages
                else if (message.GetType() == typeof(SecureClientHello)) //Unencrypted
                {
                    HandleSecureClientHello((SecureClientHello)message, ipEndPoint);
                }
                else if (message.GetType() == typeof(SecureServerHelloResponse)) //RSA Encrypted
                {
                    HandleSecureServerHelloResponse((SecureServerHelloResponse)message, ipEndPoint);
                }

                if(decryptedMessage == null)
                {
                    return null; //The error will have been written to the log inside the decryption method
                }

                //Decrypted the internal message, process the internal message
                if (decryptedMessage.GetType() == typeof(SecureClientChallengeResponse))
                {
                    HandleSecureClientChallengeResponse((SecureClientChallengeResponse)decryptedMessage, ipEndPoint);
                }
                else if (decryptedMessage.GetType() == typeof(SecureServerChallengeResponse))
                {
                    HandleSecureServerChallengeResponse((SecureServerChallengeResponse)decryptedMessage, ipEndPoint);
                }
                else if (decryptedMessage.GetType() == typeof(SecureClientChallengeResult))
                {
                    HandleSecureClientChallengeResult((SecureClientChallengeResult)decryptedMessage, ipEndPoint);
                }
                else if (decryptedMessage.GetType() == typeof(SecureServerGoAhead))
                {
                    HandleSecureServerGoAhead((SecureServerGoAhead)decryptedMessage);
                }
                else
                {
                    return decryptedMessage; //It was something to user sent, not an internal message.
                }
            }
            else
            {
                GenerateReceiveFailureException("Unencrypted message recieved, this is unsupported", null);
            }
            return null;
        }

        private BaseMessage HandleSecureMessage(SecureMessage message)
        {
            byte[] symetricKey;
            bool sessionToSymetricKeyContainKey;
            lock (sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKeyContainKey = sessionToSymetricKey.TryGetValue(message.Session, out symetricKey);
            }

            byte[] hmacSecret;
            bool sessionToHMACSecretContainKey;
            lock (sessionToHMACSecretLockObject)
            {
                sessionToHMACSecretContainKey = sessionToHMACSecret.TryGetValue(message.Session, out hmacSecret);
            }

            if (!sessionToSymetricKeyContainKey || !sessionToHMACSecretContainKey)
            {
                GenerateReceiveFailureException("We did not have sufficient encryption parameters to decrypt the given message. Session: " + message.Session, null);
                return null;
            }

            byte[] hmacOfMessage = CryptoHelper.GenerateHMAC(message.EncryptedData, hmacSecret);
            if (!hmacOfMessage.SequenceEqual(message.HMAC))
            {
                GenerateReceiveFailureException("HMAC of message was not equal to expected", null);
                return null;
            }

            byte[] plainText;
            try
            {
                plainText = CryptoHelper.Decrypt(message.EncryptedData, symetricKey);
            }
            catch
            {
                GenerateReceiveFailureException("Recieved a message that we failed to decrypt with symetric key, discarding", null);
                return null;
            }

            BaseMessage plainTextBaseMessage;
            try
            {
                plainTextBaseMessage = DeserialiseMessage(plainText);
            }
            catch
            {
                GenerateReceiveFailureException("Recieved a message that we failed to deserialise, discarding", null);
                return null;
            }

            return plainTextBaseMessage;
        }

        private void SendSecureClientHello(BaseMessage message)
        {
            lock (clientToStoredMessageLockObject)
            {
                //We're already waiting to hear back from setting up an encrypted channel
                //These will be flushed when we have the channel
                if (clientToStoredMessage.ContainsKey(message.To))
                {
                    clientToStoredMessage[message.To].Enqueue(message);
                    return;
                }
                //We haven't got an encrypted channel yet, store the message for now and we'll send later
                clientToStoredMessage.Add(message.To, new Queue<BaseMessage>());
                clientToStoredMessage[message.To].Enqueue(message);
            }
            SecureClientHello secureClientHello = new SecureClientHello()
            {
                PublicKey = rsaPublicKeyBytes,
                IPEndPoint = GetPeerIPEndPoint(message.To)
            };

            base.SendMessage(secureClientHello);
        }

        private void HandleSecureClientHello(SecureClientHello message, IPEndPoint ipEndPoint)
        {
            if(!CryptoHelper.IsPublicKey(message.PublicKey))
            {
                GenerateReceiveFailureException("Recieved a SecureClientHello with a non-valid public key, discarding", null);
                return;
            }

            if(message.PublicKey.SequenceEqual(rsaPublicKeyBytes))
            {
                GenerateReceiveFailureException("We are talking to ourselves, this is not supported, discarding", null);
                return;
            }

            string session = Guid.NewGuid().ToString();

            byte[] challenge = new byte[16];
            rand.GetBytes(challenge);
            lock (sessionToChallengeLockObject)
            {
                sessionToChallenge.Add(session, challenge);
            }

            byte[] symetricKey = new byte[16];
            rand.GetBytes(symetricKey);
            lock (sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKey.Add(session, symetricKey);
            }

            byte[] hmacSecret = new byte[16];
            rand.GetBytes(hmacSecret);
            lock (sessionToHMACSecretLockObject)
            {
                sessionToHMACSecret.Add(session, hmacSecret);
            }

            //Meed to encrypt each part seperately as RSA keys cannot encrypt much data
            SecureServerHelloResponse secureServerHelloResponse = new SecureServerHelloResponse()
            {
                IPEndPoint = ipEndPoint,
                ServerName = CryptoHelper.RSAEncrypt(Encoding.UTF8.GetBytes(GetClientName()), message.PublicKey),
                SessionInitial = CryptoHelper.RSAEncrypt(Encoding.UTF8.GetBytes(session), message.PublicKey),
                Challenge = CryptoHelper.RSAEncrypt(challenge, message.PublicKey),
                SymetricKey = CryptoHelper.RSAEncrypt(symetricKey, message.PublicKey),
                HMACSecret = CryptoHelper.RSAEncrypt(hmacSecret, message.PublicKey)
            };

            base.SendMessage(secureServerHelloResponse);
        }

        private void HandleSecureServerHelloResponse(SecureServerHelloResponse message, IPEndPoint ipEndPoint)
        {
            string serverName;
            try
            {
                serverName = Encoding.UTF8.GetString(CryptoHelper.RSADecrypt(message.ServerName, rsaPair));
            }
            catch
            {
                GenerateReceiveFailureException("Recieved a SecureServerHelloResponse which we failed to decrypt, discarding", null);
                return; //This was an invalid message or malicious
            }

            lock (clientToStoredMessageLockObject)
            {
                if (!clientToStoredMessage.ContainsKey(serverName))
                {
                    GenerateReceiveFailureException("Recieved a SecureServerHelloResponse which we didn't request, discarding", null);
                    return; //We did not request this connection/response is stale, drop it
                }
            }

            string session;
            byte[] challenge;
            byte[] symetricKey;
            byte[] hmacSecret;
            try
            {
                session = Encoding.UTF8.GetString(CryptoHelper.RSADecrypt(message.SessionInitial, rsaPair));
                challenge = CryptoHelper.RSADecrypt(message.Challenge, rsaPair);
                symetricKey = CryptoHelper.RSADecrypt(message.SymetricKey, rsaPair);
                hmacSecret = CryptoHelper.RSADecrypt(message.HMACSecret, rsaPair);
            }
            catch
            {
                GenerateReceiveFailureException("Recieved a SecureServerHelloResponse which we failed to decrypt, discarding", null);
                return;
            }

            //Store required info. Challenge not required
            lock (clientToSessionLockObject)
            {
                clientToSession.Add(serverName, session);
            }
            lock (sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKey.Add(session, symetricKey);
            }
            lock (sessionToHMACSecretLockObject)
            {
                sessionToHMACSecret.Add(session, hmacSecret);
            }

            byte[] returnChallenge = new byte[16];
            rand.GetBytes(returnChallenge);
            lock (sessionToChallengeLockObject)
            {
                sessionToChallenge.Add(session, returnChallenge);
            }

            SecureClientChallengeResponse secureClientChallengeResponse = new SecureClientChallengeResponse()
            {
                Session = session,
                To = serverName,
                From = GetClientName(),
                ChallengeResponse = CryptoHelper.CompleteChallenge(passwordBytes, challenge),
                Challenge = returnChallenge,
                ClientName = GetClientName()
            };
            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureClientChallengeResponse, session, ipEndPoint, symetricKey, hmacSecret);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureClientChallengeResponse(SecureClientChallengeResponse message, IPEndPoint ipEndPoint)
        {
            byte[] challenge;
            bool sessionToChallengeContainKey;
            lock (sessionToChallengeLockObject)
            {
                sessionToChallengeContainKey = sessionToChallenge.TryGetValue(message.Session, out challenge);
            }

            if (!sessionToChallengeContainKey)
            {
                GenerateReceiveFailureException("Recieved a SecureClientChallengeResponse which we no longer have the challenge for, discarding", null);
                return;
            }

            bool challengeResult = CryptoHelper.VerifyChallenge(passwordBytes, challenge, message.ChallengeResponse);
            ESecureChallengeResult serverChallengeResultEnum = (challengeResult) ? ESecureChallengeResult.ACCEPT : ESecureChallengeResult.REJECT;

            SecureServerChallengeResponse secureServerChallengeResponse = new SecureServerChallengeResponse()
            {
                Session = message.Session,
                To = message.ClientName,
                From = GetClientName(),
                ChallengeResult = serverChallengeResultEnum,
            };

            if (serverChallengeResultEnum == ESecureChallengeResult.ACCEPT)
            {
                secureServerChallengeResponse.ChallengeResponse = CryptoHelper.CompleteChallenge(passwordBytes, message.Challenge);
            }

            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureServerChallengeResponse, message.Session, ipEndPoint);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureServerChallengeResponse(SecureServerChallengeResponse message, IPEndPoint ipEndPoint)
        {
            if(message.ChallengeResult == ESecureChallengeResult.REJECT)
            {
                return; //We failed their test
            }

            if (message.ChallengeResult != ESecureChallengeResult.ACCEPT)
            {
                GenerateReceiveFailureException("Recieved a SecureServerChallengeResponse with an invalid ESecureChallengeResult, discarding", null);
                return;
            }

            byte[] challenge;
            bool sessionToChallengeContainKey;
            lock (sessionToChallengeLockObject)
            {
                sessionToChallengeContainKey = sessionToChallenge.TryGetValue(message.Session, out challenge);
            }

            if (!sessionToChallengeContainKey)
            {
                GenerateReceiveFailureException("Recieved a SecureServerChallengeResponse which we no longer have the challenge for, discarding", null);
                return;
            }

            bool challengeResult = CryptoHelper.VerifyChallenge(passwordBytes, challenge, message.ChallengeResponse);
            ESecureChallengeResult clientChallengeResultEnum = (challengeResult) ? ESecureChallengeResult.ACCEPT : ESecureChallengeResult.REJECT;

            SecureClientChallengeResult secureClientChallengeSucess = new SecureClientChallengeResult()
            {
                Session = message.Session,
                To = message.From,
                From = GetClientName(),
                ChallengeResult = clientChallengeResultEnum
            };

            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureClientChallengeSucess, message.Session, ipEndPoint);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureClientChallengeResult(SecureClientChallengeResult message, IPEndPoint ipEndPoint)
        {
            if (message.ChallengeResult == ESecureChallengeResult.REJECT)
            {
                return; //We failed their test
            }

            SecureServerGoAhead secureServerGoAhead = new SecureServerGoAhead()
            {
                Session = message.Session,
                To = message.From,
                From = GetClientName()
            };

            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureServerGoAhead, message.Session, ipEndPoint);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureServerGoAhead(SecureServerGoAhead message)
        {
            Queue<BaseMessage> storedMessages;
            lock (clientToStoredMessageLockObject)
            {
                storedMessages = clientToStoredMessage[message.From];
                clientToStoredMessage.Remove(message.From);
            }
            for (int i = 0; i < storedMessages.Count; ) //Removing messages moves through the list
            {
                BaseMessage dequeuedMessage = storedMessages.Dequeue();
                SendMessage(dequeuedMessage);
            }
        }

        protected override bool DerivedHandleMessageProcessing(object messageToProcess)
        {
            return false; //Consume the message, it can't be used
        }

        private SecureMessage EncryptExchangeMessageSymetric(SecureMessage message, string session, IPEndPoint ipEndPoint)
        {
            byte[] symetricKey;
            bool sessionToSymetricKeyContainKey;
            lock (sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKeyContainKey = sessionToSymetricKey.TryGetValue(session, out symetricKey);
            }

            byte[] hmacSecret;
            bool sessionToHMACSecretContainKey;
            lock (sessionToHMACSecretLockObject)
            {
                sessionToHMACSecretContainKey = sessionToHMACSecret.TryGetValue(session, out hmacSecret);
            }

            if (!sessionToSymetricKeyContainKey || !sessionToHMACSecretContainKey)
            {
                return null;
            }

            return EncryptExchangeMessageSymetric(message, session, ipEndPoint, symetricKey, hmacSecret);
        }

        private SecureMessage EncryptExchangeMessageSymetric(SecureMessage message, string session, IPEndPoint ipEndPoint, byte[] symetricKey, byte[] hmacSecret)
        {
            byte[] encryptedData = CryptoHelper.Encrypt(SerialiseMessage(message), symetricKey);
            byte[] hmac = CryptoHelper.GenerateHMAC(encryptedData, hmacSecret);

            return new SecureMessage(ipEndPoint, session, encryptedData, hmac);
        }
    }
}
