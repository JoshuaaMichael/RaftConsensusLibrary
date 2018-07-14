using System;
using System.Linq;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Messages;
using TeamDecided.RaftConsensus.Networking.Messages.SRP;
using Eneter.SecureRemotePassword;

/*
 * TODO:
 *  - initialBufferedMessage dispose solution
 */

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    internal class SRPSessionManager
    {
        private ISRPStep _stage;
        private readonly string _to;
        private readonly string _from;
        private readonly string _password;
        public string Session { get; private set; }

        private byte[] _clientSecretEphemeralValue; //a
        private byte[] _clientPublicEphemeralValue; //A
        private byte[] _userSalt; //s
        private byte[] _userPasswordVerifier; //v
        private byte[] _serverPublicEphemeralValue; //B
        public byte[] EncryptionKey { get; private set; } //K
        private byte[] _clientProof; //M1
        private byte[] _serverProof; //M2

        private readonly BaseMessage _clientInitialBufferedMessage;
        private SecureMessage _lastSentMessage;
        private DateTime _lastTimeSentMessage;
        private DateTime _lastTimeReicevedMessage;

        private SRPSessionManager(string to, string from, string password)
        {
            _to = to;
            _from = from;
            _password = password;
        }

        public SRPSessionManager(string to, string ownName, string password, BaseMessage clientInitialBufferedMessage)
            : this(to, ownName, password)
        {
            _stage = ISRPStep.Step1;
            _clientInitialBufferedMessage = clientInitialBufferedMessage ?? throw new ArgumentException("Client Initial Buffered Message must not be null");
        }

        public SRPSessionManager(SRPStep1 message, string ownName, string password)
            : this(message.From, ownName, password)
        {
            _stage = ISRPStep.Step2;
            _clientPublicEphemeralValue = message.A;
        }

        public void HandleMessage(SecureMessage message)
        {
            UpdateLastTimeMessageReiceved();
            if (message.GetMessageType() == typeof(SRPStep2))
            {
                CheckAcceptableState(ISRPStep.Step1, ISRPStep.Step3);
                _serverPublicEphemeralValue = ((SRPStep2)message).B;
                _userSalt = ((SRPStep2)message).s;
                _stage = ISRPStep.Step3;
            }
            else if (message.GetMessageType() == typeof(SRPStep3))
            {
                CheckAcceptableState(ISRPStep.Step2, ISRPStep.PendingComplete);
                _clientProof = ((SRPStep3)message).M1;
                _stage = ISRPStep.Step4;
            }
            else if (message.GetMessageType() == typeof(SRPStep4))
            {
                CheckAcceptableState(ISRPStep.Step3, ISRPStep.Step5);
                _serverProof = ((SRPStep4)message).M2;
                _stage = ISRPStep.Step5;
            }
            else if(message.GetMessageType() == typeof(SRPException))
            {
                throw new Exception(((SRPException)message).Message);
            }
            else
            {
                throw new ArgumentException("Unsupported message");
            }
        }

        private void CheckAcceptableState(params ISRPStep[] acceptableState)
        {
            if (acceptableState.Any(step => _stage == step))
            {
                return;
            }

            throw new InvalidOperationException("We are not in the correct state to process this message");
        }

        public BaseMessage GetNextMessage()
        {
            UpdateLastTimeMessageSent();
            switch (_stage)
            {
                case ISRPStep.Step1:
                    return Step1();
                case ISRPStep.Step2:
                    return Step2();
                case ISRPStep.Step3:
                    return Step3();
                case ISRPStep.Step4:
                    return Step4();
                case ISRPStep.Step5:
                    return Step5();
            }

            if (_clientInitialBufferedMessage != null && IsServer() && IsSRPComplete())
            {

            }

            return (IsServer() && IsSRPComplete()) ? _clientInitialBufferedMessage : null;
        }

        private SecureMessage Step1()
        {
            if (_lastSentMessage is SRPStep1) return _lastSentMessage;

            _clientSecretEphemeralValue = SRP.a();                              //Generate new private ephemeral value
            _clientPublicEphemeralValue = SRP.A(_clientSecretEphemeralValue);   //Calculate new public ephemeral value
            _lastSentMessage = new SRPStep1(_to, _from, _clientPublicEphemeralValue);
            return _lastSentMessage;
        }

        private SecureMessage Step2()
        {
            if (_lastSentMessage is SRPStep2) return _lastSentMessage;

            if (!SRP.IsValid_A(_clientPublicEphemeralValue))
            {
                return new SRPException(_to, _from, "Received an invalid A value");
            }

            _userSalt = SRP.s();                                                                            //Generate user salt
            byte[] x = SRP.x(_password, _userSalt);                                                         //Calculates user private key
            _userPasswordVerifier = SRP.v(x);                                                               //Calculates user verifier

            byte[] b = SRP.b();                                                                             //Generate service private ephemeral value
            _serverPublicEphemeralValue = SRP.B(b, _userPasswordVerifier);                                  //Calculate service public ephemeral value
            byte[] u = SRP.u(_clientPublicEphemeralValue, _serverPublicEphemeralValue);                     //Calcualte random scambling value
            EncryptionKey = SRP.K_Service(_clientPublicEphemeralValue, _userPasswordVerifier, u, b);          //Calculate session key

            _lastSentMessage = new SRPStep2(_to, _from, _userSalt, _serverPublicEphemeralValue);
            Session = _lastSentMessage.Session;
            return _lastSentMessage;
        }

        private SecureMessage Step3()
        {
            if (_lastSentMessage is SRPStep2) return _lastSentMessage;

            byte[] u = SRP.u(_clientPublicEphemeralValue, _serverPublicEphemeralValue);
            if(!SRP.IsValid_B_u(_serverPublicEphemeralValue, u))
            {
                return new SRPException(_to, _from, Session, "Received an invalid B or u value");
            }

            byte[] x = SRP.x(_password, _userSalt);                                                         //Calculate user private key
            EncryptionKey = SRP.K_Client(_serverPublicEphemeralValue, x, u, _clientSecretEphemeralValue);     //Calculate session key

            _clientProof = SRP.M1(_clientPublicEphemeralValue, _serverPublicEphemeralValue, EncryptionKey);
            _lastSentMessage = new SRPStep3(_to, _from, Session, _clientProof);
            return _lastSentMessage;
        }

        private SecureMessage Step4()
        {
            if (_lastSentMessage is SRPStep4) return _lastSentMessage;

            if(!_clientProof.SequenceEqual(SRP.M1(_clientPublicEphemeralValue, _serverPublicEphemeralValue, EncryptionKey)))
            {
                return new SRPException(_to, _from, Session, "Failed to confirm M1 value");
            }

            _stage = ISRPStep.PendingComplete; //We've authenticated the client

            _serverProof = SRP.M2(_clientPublicEphemeralValue, _clientProof, EncryptionKey);
            _lastSentMessage = new SRPStep4(_to, _from, Session, _serverProof);
            return _lastSentMessage;
        }

        private SecureMessage Step5()
        {
            if (!_serverProof.SequenceEqual(SRP.M2(_clientPublicEphemeralValue, _clientProof, EncryptionKey)))
            {
                return new SRPException(_to, _from, Session, "Failed to confirm M2 value");
            }

            _stage = ISRPStep.Complete; //We've authenticated the server
            return null;
        }

        private bool IsServer()
        {
            return _clientSecretEphemeralValue == null;
        }

        public bool IsServerAndPendingComplete()
        {
            return IsServer() && _stage == ISRPStep.PendingComplete;
        }

        public void SetServerToComplete()
        {
            if (!IsServerAndPendingComplete())
            {
                throw new InvalidOperationException("This method may only be called when this side of the connection is the \"server\", and they're in a PendingComplete state");
            }

            _stage = ISRPStep.Complete;
        }

        public bool IsSRPComplete()
        {
            return _stage == ISRPStep.PendingComplete || _stage == ISRPStep.Complete;
        }

        private void UpdateLastTimeMessageSent()
        {
            _lastTimeSentMessage = DateTime.UtcNow;
        }

        private void UpdateLastTimeMessageReiceved()
        {
            _lastTimeReicevedMessage = DateTime.UtcNow;
        }

        public bool TimeToRetry(int timeout)
        {
            return !IsSRPComplete() &&
                        ((DateTime.UtcNow - _lastTimeSentMessage).TotalMilliseconds > timeout
                            || (DateTime.UtcNow - _lastTimeReicevedMessage).TotalMilliseconds > timeout);
        }
    }
}
