using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using TeamDecided.RaftNetworking.Enums;
using TeamDecided.RaftNetworking.Exceptions;
using TeamDecided.RaftNetworking.Helpers;
using TeamDecided.RaftNetworking.Messages;

/* TODO:
 *      Make thread safe
 *      Make a thread which times out messages, raises errors where required
 *      Base, set from. Remove BaseMessage requirement. Internal set. (well, detect in base if To is set, if it is, set from
 *      Ensure the case of repeatedly trying to connect
 *      How does receive handle the IPAddress map
 *      Need to detect if we're talking to ourselves
 *      Encryption/decryption exceptions
 *      Figure out when IP Addresses are added/session/username
 *      Check session forgery, include in HMAC?
 *      Handle the errors in the Handle methods
 *      Add get IPEndPoint/port number to the IUDPNEtworking interface
 *      Issues with BaseMessage from/to in json serialiser
 * 
 */

namespace TeamDecided.RaftNetworking
{
    public sealed class UDPNetworkingSecure : UDPNetworking
    {
        private static readonly RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

        private Dictionary<string, List<BaseMessage>> clientToStoredMessage;
        private Dictionary<string, string> clientToSession;
        private Dictionary<string, string> sessionToClient;
        private Dictionary<string, byte[]> sessionToSymetricKey;
        private Dictionary<string, byte[]> sessionToHMACSecret;

        private Dictionary<string, byte[]> sessionToChallenge;

        private byte[] passwordBytes;
        private RSAParameters rsaPair;
        private string rsaPublicKey;

        public UDPNetworkingSecure(string password)
        {
            passwordBytes = Encoding.UTF8.GetBytes(password);

            clientToStoredMessage = new Dictionary<string, List<BaseMessage>>();
            clientToSession = new Dictionary<string, string>();
            sessionToClient = new Dictionary<string, string>();
            sessionToSymetricKey = new Dictionary<string, byte[]>();
            sessionToHMACSecret = new Dictionary<string, byte[]>();
            sessionToChallenge = new Dictionary<string, byte[]>();

            RSACryptoServiceProvider rsaPairGen = new RSACryptoServiceProvider(2048);
            rsaPair = rsaPairGen.ExportParameters(true);
            rsaPublicKey = rsaPairGen.ToXmlString(false);
        }

        public override void SendMessage(BaseMessage message)
        {
            if(!clientToSession.ContainsKey(message.To))
            {
                SendSecureClientHello(message); //We need to setup a secure session
                return;
            }

            string session = clientToSession[message.To];

            if(!sessionToSymetricKey.ContainsKey(session) || !sessionToHMACSecret.ContainsKey(session))
            {
                GenerateSendFailureException("Failed to locate required encryption information to send message", message);
                return;
            }

            byte[] symetricKey = sessionToSymetricKey[session];
            byte[] hmacSecret = sessionToHMACSecret[session];

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
                    HandleSecureServerHelloResponse((SecureServerHelloResponse)message);
                }

                if(decryptedMessage == null)
                {
                    return null; //The error will have been written to the log inside the decryption method
                }

                //Decrypted the internal message, process the internal message
                if (decryptedMessage.GetType() == typeof(SecureClientChallengeResponse))
                {
                    HandleSecureClientChallengeResponse((SecureClientChallengeResponse)decryptedMessage);
                }
                else if (decryptedMessage.GetType() == typeof(SecureServerChallengeResponse))
                {
                    HandleSecureServerChallengeResponse((SecureServerChallengeResponse)decryptedMessage);
                }
                else if (decryptedMessage.GetType() == typeof(SecureClientChallengeResult))
                {
                    HandleSecureClientChallengeResult((SecureClientChallengeResult)decryptedMessage);
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
            //This is AES encrypted, the session value will let us lookup values we need to decrypt
            if(!sessionToSymetricKey.ContainsKey(message.Session) || !sessionToHMACSecret.ContainsKey(message.Session))
            {
                GenerateReceiveFailureException("We did not have sufficient encryption parameters to decrypt the given message. Session: " + message.Session, null);
                return null;
            }

            byte[] hmacSecret = sessionToHMACSecret[message.Session];
            byte[] hmacOfMessage = CryptoHelper.GenerateHMAC(message.EncryptedData, hmacSecret);

            if (!hmacOfMessage.SequenceEqual(message.HMAC))
            {
                GenerateReceiveFailureException("HMAC of message was not equal to expected", null);
                return null;
            }

            byte[] symetricKey = sessionToSymetricKey[message.Session];
            byte[] plainText = CryptoHelper.Decrypt(message.EncryptedData, symetricKey);

            return DeserialiseMessage(plainText);
        }

        private void SendSecureClientHello(BaseMessage message)
        {
            //We're already waiting to hear back from setting up an encrypted channel
            //Thse will be flushed when we have the channel
            if(clientToStoredMessage.ContainsKey(message.To))
            {
                clientToStoredMessage[message.To].Add(message);
                return;
            }
            //We haven't got an encrypted channel yet, store the message for now and we'll send later
            clientToStoredMessage.Add(message.To, new List<BaseMessage>());
            clientToStoredMessage[message.To].Add(message);

            SecureClientHello secureClientHello = new SecureClientHello()
            {
                PublicKey = Encoding.UTF8.GetBytes(rsaPublicKey),
                IPEndPoint = GetPeerIPEndPoint(message.To)
            };

            base.SendMessage(secureClientHello);
        }

        private void HandleSecureClientHello(SecureClientHello message, IPEndPoint ipEndPoint)
        {
            string session = Guid.NewGuid().ToString();

            byte[] challenge = new byte[16];
            rand.GetBytes(challenge);
            sessionToChallenge.Add(session, challenge);

            byte[] symetricKey = new byte[16];
            rand.GetBytes(symetricKey);
            sessionToSymetricKey.Add(session, symetricKey);

            byte[] hmacSecret = new byte[16];
            rand.GetBytes(hmacSecret);
            sessionToHMACSecret.Add(session, hmacSecret);

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

        private void HandleSecureServerHelloResponse(SecureServerHelloResponse message)
        {
            string serverName = Encoding.UTF8.GetString(CryptoHelper.RSADecrypt(message.ServerName, rsaPair));

            if(!clientToStoredMessage.ContainsKey(serverName))
            {
                return; //We did not request this connection/response is stale, drop it
            }

            string session = Encoding.UTF8.GetString(CryptoHelper.RSADecrypt(message.SessionInitial, rsaPair));
            byte[] challenge = CryptoHelper.RSADecrypt(message.Challenge, rsaPair);
            byte[] symetricKey = CryptoHelper.RSADecrypt(message.SymetricKey, rsaPair);
            byte[] hmacSecret = CryptoHelper.RSADecrypt(message.HMACSecret, rsaPair);

            //Store required info. Challenge not required
            clientToSession.Add(serverName, session);
            sessionToClient.Add(session, serverName);
            sessionToSymetricKey.Add(session, symetricKey);
            sessionToHMACSecret.Add(session, hmacSecret);

            byte[] returnChallenge = new byte[16];
            rand.GetBytes(returnChallenge);
            sessionToChallenge.Add(session, returnChallenge);

            SecureClientChallengeResponse secureClientChallengeResponse = new SecureClientChallengeResponse()
            {
                Session = session,
                To = serverName,
                From = GetClientName(),
                ChallengeResponse = CryptoHelper.CompleteChallenge(passwordBytes, challenge),
                Challenge = returnChallenge,
                ClientName = GetClientName()
            };
            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureClientChallengeResponse, session, symetricKey, hmacSecret);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureClientChallengeResponse(SecureClientChallengeResponse message)
        {
            if(!sessionToChallenge.ContainsKey(message.Session))
            {
                return; //We no longer have their challenge
            }

            sessionToClient.Add(message.Session, message.ClientName);

            bool challengeResult = CryptoHelper.VerifyChallenge(passwordBytes, sessionToChallenge[message.Session], message.ChallengeResponse);
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

            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureServerChallengeResponse, message.Session);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureServerChallengeResponse(SecureServerChallengeResponse message)
        {
            if(message.ChallengeResult == ESecureChallengeResult.REJECT)
            {
                return; //We failed their test
            }
            if (!sessionToChallenge.ContainsKey(message.Session))
            {
                return; //We no longer have their challenge
            }

            bool challengeResult = CryptoHelper.VerifyChallenge(passwordBytes, sessionToChallenge[message.Session], message.ChallengeResponse);
            ESecureChallengeResult clientChallengeResultEnum = (challengeResult) ? ESecureChallengeResult.ACCEPT : ESecureChallengeResult.REJECT;

            SecureClientChallengeResult secureClientChallengeSucess = new SecureClientChallengeResult()
            {
                Session = message.Session,
                To = message.From,
                ChallengeResult = clientChallengeResultEnum
            };

            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureClientChallengeSucess, message.Session);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureClientChallengeResult(SecureClientChallengeResult message)
        {
            //TODO: Something with this
            throw new NotImplementedException();
        }

        protected override bool DerivedHandleMessageProcessing(object messageToProcess)
        {
            return false; //Consume the message, it can't be used
        }

        private SecureMessage EncryptExchangeMessageSymetric(SecureMessage message, string session)
        {
            if(!sessionToSymetricKey.ContainsKey(session) || !sessionToHMACSecret.ContainsKey(session))
            {
                return null;
            }

            byte[] symetricKey = sessionToSymetricKey[session];
            byte[] hmacSecret = sessionToHMACSecret[session];

            return EncryptExchangeMessageSymetric(message, session, symetricKey, hmacSecret);
        }

        private SecureMessage EncryptExchangeMessageSymetric(SecureMessage message, string session, byte[] symetricKey, byte[] hmacSecret)
        {
            IPEndPoint ipEndPoint = GetPeerIPEndPoint(message.To);
            byte[] encryptedData = CryptoHelper.Encrypt(SerialiseMessage(message), symetricKey);
            byte[] hmac = CryptoHelper.GenerateHMAC(encryptedData, hmacSecret);

            return new SecureMessage(ipEndPoint, session, encryptedData, hmac);
        }
    }
}
