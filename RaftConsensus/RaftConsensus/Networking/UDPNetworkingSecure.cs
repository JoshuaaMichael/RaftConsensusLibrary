using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TeamDecided.RaftConsensus.Common;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;

/* TODO:
 *      Make a thread which times out messages, raises errors where required, will need an extra thread
 *      See about moving away from From/To given in message, and use session lookup
 *      BaseMessage, auto set from
 *      Solve the case of repeatedly trying to connect
 */

namespace TeamDecided.RaftConsensus.Networking
{
    public sealed class UdpNetworkingSecure : UDPNetworking
    {
        private static readonly RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();

        private Dictionary<string, Queue<BaseMessage>> _clientToStoredMessage;
        private object _clientToStoredMessageLockObject;
        private Dictionary<string, string> _clientToSession;
        private object _clientToSessionLockObject;
        private Dictionary<string, byte[]> _sessionToSymetricKey;
        private object _sessionToSymetricKeyLockObject;
        private Dictionary<string, byte[]> _sessionToHmacSecret;
        private object _sessionToHmacSecretLockObject;
        private Dictionary<string, byte[]> _sessionToChallenge;
        private object _sessionToChallengeLockObject;

        private byte[] _passwordBytes;
        private RSAParameters _rsaPair;
        private byte[] _rsaPublicKeyBytes;

        public UdpNetworkingSecure(string password)
        {
            _passwordBytes = Encoding.UTF8.GetBytes(password);

            _clientToStoredMessage = new Dictionary<string, Queue<BaseMessage>>();
            _clientToStoredMessageLockObject = new object();
            _clientToSession = new Dictionary<string, string>();
            _clientToSessionLockObject = new object();
            _sessionToSymetricKey = new Dictionary<string, byte[]>();
            _sessionToSymetricKeyLockObject = new object();
            _sessionToHmacSecret = new Dictionary<string, byte[]>();
            _sessionToHmacSecretLockObject = new object();
            _sessionToChallenge = new Dictionary<string, byte[]>();
            _sessionToChallengeLockObject = new object();

            RSACryptoServiceProvider rsaPairGen = new RSACryptoServiceProvider(2048);
            _rsaPair = rsaPairGen.ExportParameters(true);
            _rsaPublicKeyBytes = Encoding.UTF8.GetBytes(rsaPairGen.ToXmlString(false));
        }

        public override void SendMessage(BaseMessage message)
        {
            string session;
            bool clientToSessionContainsKey;
            lock (_clientToSessionLockObject)
            {
                clientToSessionContainsKey = _clientToSession.TryGetValue(message.To, out session);
            }

            if (!clientToSessionContainsKey)
            {
                Log(ERaftLogType.Debug, "Trying to send message to someone we haven't talked to yet ({0}). Need to establish secure session.", message.To);
                SendSecureClientHello(message); //We need to setup a secure session
                return;
            }

            Log(ERaftLogType.Trace, "Sending message to someone we've got a secure session with, {0}", message.To);

            byte[] symetricKey;
            bool sessionToSymetricKeyContainKey;
            lock (_sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKeyContainKey = _sessionToSymetricKey.TryGetValue(session, out symetricKey);
            }

            byte[] hmacSecret;
            bool sessionToHmacSecretContainKey;
            lock (_sessionToHmacSecretLockObject)
            {
                sessionToHmacSecretContainKey = _sessionToHmacSecret.TryGetValue(session, out hmacSecret);
            }

            if (!sessionToSymetricKeyContainKey || !sessionToHmacSecretContainKey)
            {
                Log(ERaftLogType.Warn, "Failed to locate required encryption information to send message: {0}", message);
                GenerateSendFailureException("Failed to locate required encryption information to send message", message);
                return;
            }

            byte[] serialisedMessage = SerialiseMessage(message);
            byte[] encryptedMessage = CryptoHelper.Encrypt(serialisedMessage, symetricKey);
            byte[] hmacOfEncryptedMessage = CryptoHelper.GenerateHmac(encryptedMessage, hmacSecret);

            IPEndPoint ipEndPoint = GetPeerIPEndPoint(message.To);

            SecureMessage secureMessage = new SecureMessage(ipEndPoint, session, encryptedMessage, hmacOfEncryptedMessage);

            Log(ERaftLogType.Trace, "Sending encrypted message");

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
                    Log(ERaftLogType.Trace, "Got an encrypted message. Decrypting");
                    decryptedMessage = HandleSecureMessage((SecureMessage)message, ipEndPoint);
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
                else if (message.GetType() == typeof(SecureClientDecryptFailed)) //Unencrypted
                {
                    Log(ERaftLogType.Trace, "Received a message from a client who is failing to decrypt our message");
                    HandleClientDecryptFailed((SecureClientDecryptFailed)message);
                }

                if (decryptedMessage == null)
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
                    Log(ERaftLogType.Trace, "This message was not one of the protocol messages, forward to user");
                    return decryptedMessage;
                }
            }
            else
            {
                GenerateReceiveFailureException("Unencrypted message recieved, this is unsupported", null);
            }
            return null;
        }

        private BaseMessage HandleSecureMessage(SecureMessage message, IPEndPoint ipEndPoint)
        {
            byte[] symetricKey;
            bool sessionToSymetricKeyContainKey;
            lock (_sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKeyContainKey = _sessionToSymetricKey.TryGetValue(message.Session, out symetricKey);
            }

            byte[] hmacSecret;
            bool sessionToHmacSecretContainKey;
            lock (_sessionToHmacSecretLockObject)
            {
                sessionToHmacSecretContainKey = _sessionToHmacSecret.TryGetValue(message.Session, out hmacSecret);
            }

            if (!sessionToSymetricKeyContainKey || !sessionToHmacSecretContainKey)
            {
                GenerateReceiveFailureException("We did not have sufficient encryption parameters to decrypt the given message. Session: " + message.Session, null);

                Log(ERaftLogType.Debug, "Received message we can't decrypt, notifying");
                SecureClientDecryptFailed secureClientHello = new SecureClientDecryptFailed()
                {
                    IPEndPoint = ipEndPoint,
                    Session = message.Session
                };
                base.SendMessage(secureClientHello);
                return null;
            }

            byte[] hmacOfMessage = CryptoHelper.GenerateHmac(message.EncryptedData, hmacSecret);
            if (!hmacOfMessage.SequenceEqual(message.Hmac))
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
            Log(ERaftLogType.Debug, "Sending secure client hello to {0}", message.To);
            lock (_clientToStoredMessageLockObject)
            {
                if (_clientToStoredMessage.ContainsKey(message.To))
                {
                    Log(ERaftLogType.Debug, "We're waiting for a secure channel, dropping stored message and replacing with this one");
                    _clientToStoredMessage[message.To].Clear();
                    _clientToStoredMessage[message.To].Enqueue(message);
                    return;
                }
                Log(ERaftLogType.Debug, "We haven't got a secure channel yet, we'll store this message and send it later");
                _clientToStoredMessage.Add(message.To, new Queue<BaseMessage>());
                _clientToStoredMessage[message.To].Enqueue(message);
            }
            SecureClientHello secureClientHello = new SecureClientHello()
            {
                PublicKey = _rsaPublicKeyBytes,
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

            if(message.PublicKey.SequenceEqual(_rsaPublicKeyBytes))
            {
                GenerateReceiveFailureException("We are talking to ourselves, this is not supported, discarding", null);
                return;
            }

            string session = Guid.NewGuid().ToString();

            byte[] challenge = new byte[16];
            Rand.GetBytes(challenge);
            lock (_sessionToChallengeLockObject)
            {
                _sessionToChallenge.Add(session, challenge);
            }

            byte[] symetricKey = new byte[16];
            Rand.GetBytes(symetricKey);
            lock (_sessionToSymetricKeyLockObject)
            {
                _sessionToSymetricKey.Add(session, symetricKey);
            }

            byte[] hmacSecret = new byte[16];
            Rand.GetBytes(hmacSecret);
            lock (_sessionToHmacSecretLockObject)
            {
                _sessionToHmacSecret.Add(session, hmacSecret);
            }

            //Meed to encrypt each part seperately as RSA keys cannot encrypt much data
            SecureServerHelloResponse secureServerHelloResponse = new SecureServerHelloResponse()
            {
                IPEndPoint = ipEndPoint,
                ServerName = CryptoHelper.RsaEncrypt(Encoding.UTF8.GetBytes(GetClientName()), message.PublicKey),
                SessionInitial = CryptoHelper.RsaEncrypt(Encoding.UTF8.GetBytes(session), message.PublicKey),
                Challenge = CryptoHelper.RsaEncrypt(challenge, message.PublicKey),
                SymetricKey = CryptoHelper.RsaEncrypt(symetricKey, message.PublicKey),
                HmacSecret = CryptoHelper.RsaEncrypt(hmacSecret, message.PublicKey)
            };

            base.SendMessage(secureServerHelloResponse);
        }

        private void HandleSecureServerHelloResponse(SecureServerHelloResponse message, IPEndPoint ipEndPoint)
        {
            string serverName;
            try
            {
                serverName = Encoding.UTF8.GetString(CryptoHelper.RsaDecrypt(message.ServerName, _rsaPair));
            }
            catch
            {
                GenerateReceiveFailureException("Recieved a SecureServerHelloResponse which we failed to decrypt, discarding", null);
                return; //This was an invalid message or malicious
            }

            lock (_clientToStoredMessageLockObject)
            {
                if (!_clientToStoredMessage.ContainsKey(serverName))
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
                session = Encoding.UTF8.GetString(CryptoHelper.RsaDecrypt(message.SessionInitial, _rsaPair));
                challenge = CryptoHelper.RsaDecrypt(message.Challenge, _rsaPair);
                symetricKey = CryptoHelper.RsaDecrypt(message.SymetricKey, _rsaPair);
                hmacSecret = CryptoHelper.RsaDecrypt(message.HmacSecret, _rsaPair);
            }
            catch
            {
                GenerateReceiveFailureException("Recieved a SecureServerHelloResponse which we failed to decrypt, discarding", null);
                return;
            }

            //Store required info. Challenge not required
            lock (_clientToSessionLockObject)
            {
                _clientToSession.Add(serverName, session);
            }
            lock (_sessionToSymetricKeyLockObject)
            {
                _sessionToSymetricKey.Add(session, symetricKey);
            }
            lock (_sessionToHmacSecretLockObject)
            {
                _sessionToHmacSecret.Add(session, hmacSecret);
            }

            byte[] returnChallenge = new byte[16];
            Rand.GetBytes(returnChallenge);
            lock (_sessionToChallengeLockObject)
            {
                _sessionToChallenge.Add(session, returnChallenge);
            }

            SecureClientChallengeResponse secureClientChallengeResponse = new SecureClientChallengeResponse()
            {
                Session = session,
                To = serverName,
                From = GetClientName(),
                ChallengeResponse = CryptoHelper.CompleteChallenge(_passwordBytes, challenge),
                Challenge = returnChallenge,
                ClientName = GetClientName()
            };
            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureClientChallengeResponse, session, ipEndPoint, symetricKey, hmacSecret);
            base.SendMessage(messageToSend);
        }

        private void HandleClientDecryptFailed(SecureClientDecryptFailed message)
        {
            //TODO: Rewrite this in a way which doesn't allow cleartext DOS attack

            //Remove all session information for this user
            lock (_clientToSessionLockObject)
            {
                string clientName = "";
                foreach(KeyValuePair<string, string> pair in _clientToSession)
                {
                    if(pair.Value == message.Session)
                    {
                        clientName = pair.Key;
                    }
                }
                if(clientName != "")
                {
                    _clientToSession.Remove(clientName);
                }
            }

            lock(_sessionToSymetricKeyLockObject)
            {
                if(_sessionToSymetricKey.ContainsKey(message.Session))
                {
                    _sessionToSymetricKey.Remove(message.Session);
                }
            }

            lock (_sessionToHmacSecretLockObject)
            {
                if (_sessionToHmacSecret.ContainsKey(message.Session))
                {
                    _sessionToHmacSecret.Remove(message.Session);
                }
            }

            lock (_sessionToChallengeLockObject)
            {
                if (_sessionToChallenge.ContainsKey(message.Session))
                {
                    _sessionToChallenge.Remove(message.Session);
                }
            }
        }

        private void HandleSecureClientChallengeResponse(SecureClientChallengeResponse message, IPEndPoint ipEndPoint)
        {
            byte[] challenge;
            bool sessionToChallengeContainKey;
            lock (_sessionToChallengeLockObject)
            {
                sessionToChallengeContainKey = _sessionToChallenge.TryGetValue(message.Session, out challenge);
            }

            if (!sessionToChallengeContainKey)
            {
                GenerateReceiveFailureException("Recieved a SecureClientChallengeResponse which we no longer have the challenge for, discarding", null);
                return;
            }

            bool challengeResult = CryptoHelper.VerifyChallenge(_passwordBytes, challenge, message.ChallengeResponse);
            ESecureChallengeResult serverChallengeResultEnum = (challengeResult) ? ESecureChallengeResult.Accept : ESecureChallengeResult.Reject;

            SecureServerChallengeResponse secureServerChallengeResponse = new SecureServerChallengeResponse()
            {
                Session = message.Session,
                To = message.ClientName,
                From = GetClientName(),
                ChallengeResult = serverChallengeResultEnum,
            };

            if (serverChallengeResultEnum == ESecureChallengeResult.Accept)
            {
                secureServerChallengeResponse.ChallengeResponse = CryptoHelper.CompleteChallenge(_passwordBytes, message.Challenge);
            }

            SecureMessage messageToSend = EncryptExchangeMessageSymetric(secureServerChallengeResponse, message.Session, ipEndPoint);
            base.SendMessage(messageToSend);
        }

        private void HandleSecureServerChallengeResponse(SecureServerChallengeResponse message, IPEndPoint ipEndPoint)
        {
            if(message.ChallengeResult == ESecureChallengeResult.Reject)
            {
                Log(ERaftLogType.Debug, "We failed node {0}'s challenge", message.From);
                return; //We failed their test
            }

            if (message.ChallengeResult != ESecureChallengeResult.Accept)
            {
                GenerateReceiveFailureException("Recieved a SecureServerChallengeResponse with an invalid ESecureChallengeResult, discarding", null);
                return;
            }

            byte[] challenge;
            bool sessionToChallengeContainKey;
            lock (_sessionToChallengeLockObject)
            {
                sessionToChallengeContainKey = _sessionToChallenge.TryGetValue(message.Session, out challenge);
            }

            if (!sessionToChallengeContainKey)
            {
                GenerateReceiveFailureException("Recieved a SecureServerChallengeResponse which we no longer have the challenge for, discarding", null);
                return;
            }

            bool challengeResult = CryptoHelper.VerifyChallenge(_passwordBytes, challenge, message.ChallengeResponse);
            ESecureChallengeResult clientChallengeResultEnum = (challengeResult) ? ESecureChallengeResult.Accept : ESecureChallengeResult.Reject;

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
            if (message.ChallengeResult == ESecureChallengeResult.Reject)
            {
                Log(ERaftLogType.Debug, "We failed node {0}'s challenge", message.From);
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
            lock (_clientToStoredMessageLockObject)
            {
                storedMessages = _clientToStoredMessage[message.From];
                _clientToStoredMessage.Remove(message.From);
            }
            for (int i = 0; i < storedMessages.Count; ) //Removing messages moves through the list
            {
                BaseMessage dequeuedMessage = storedMessages.Dequeue();
                SendMessage(dequeuedMessage);
            }
        }

        private SecureMessage EncryptExchangeMessageSymetric(SecureMessage message, string session, IPEndPoint ipEndPoint)
        {
            byte[] symetricKey;
            bool sessionToSymetricKeyContainKey;
            lock (_sessionToSymetricKeyLockObject)
            {
                sessionToSymetricKeyContainKey = _sessionToSymetricKey.TryGetValue(session, out symetricKey);
            }

            byte[] hmacSecret;
            bool sessionToHmacSecretContainKey;
            lock (_sessionToHmacSecretLockObject)
            {
                sessionToHmacSecretContainKey = _sessionToHmacSecret.TryGetValue(session, out hmacSecret);
            }

            if (!sessionToSymetricKeyContainKey || !sessionToHmacSecretContainKey)
            {
                return null;
            }

            return EncryptExchangeMessageSymetric(message, session, ipEndPoint, symetricKey, hmacSecret);
        }

        private SecureMessage EncryptExchangeMessageSymetric(SecureMessage message, string session, IPEndPoint ipEndPoint, byte[] symetricKey, byte[] hmacSecret)
        {
            byte[] encryptedData = CryptoHelper.Encrypt(SerialiseMessage(message), symetricKey);
            byte[] hmac = CryptoHelper.GenerateHmac(encryptedData, hmacSecret);

            return new SecureMessage(ipEndPoint, session, encryptedData, hmac);
        }
    }
}
