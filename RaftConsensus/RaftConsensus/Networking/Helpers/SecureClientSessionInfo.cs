using System;
using System.Linq;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Messages;
using TeamDecided.RaftConsensus.Networking.Messages.SRP;
using Eneter.SecureRemotePassword;

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    public class SRPSessionManager
    {
        private ISRPStep _stage;
        private readonly string _to;
        private readonly string _from;
        private readonly string _password;
        private string _session;

        private byte[] _clientSecretEphemeralValue; //a
        private byte[] _clientPublicEphemeralValue; //A
        private byte[] _userSalt; //s
        private byte[] _userPasswordVerifier; //v
        private byte[] _serverPublicEphemeralValue; //B
        private byte[] _sessionKey; //K
        private byte[] _clientProof; //M1
        private byte[] _serverProof; //M2

        private BaseSecureMessage _lastSentMessage;
        private DateTime _lastTimeSentMessage;
        private DateTime _lastTimeReicevedMessage;

        public SRPSessionManager(string to, string from, string password)
        {
            _stage = ISRPStep.Step1;
            _to = to;
            _from = from;
            _password = password;
        }

        public SRPSessionManager(SRPStep1 message, string ownName, string password)
        {
            _stage = ISRPStep.Step2;
            _clientPublicEphemeralValue = message.A;
            _to = message.From;
            _from = ownName;
            _password = password;
        }

        public void HandleMessage(BaseSecureMessage message)
        {
            if(message.GetMessageType() == typeof(SRPStep2))
            {
                if (_stage != ISRPStep.Step1 && _stage != ISRPStep.Step3)
                {
                    throw new ArgumentException("We are not in the correct state to process this message");
                }
                _serverPublicEphemeralValue = ((SRPStep2)message).B;
                _userSalt = ((SRPStep2)message).s;
                _stage = ISRPStep.Step3;
            }
            else if (message.GetMessageType() == typeof(SRPStep3))
            {
                if (_stage != ISRPStep.Step2 && _stage != ISRPStep.PendingComplete)
                {
                    throw new ArgumentException("We are not in the correct state to process this message");
                }
                _clientProof = ((SRPStep3)message).M1;
                _stage = ISRPStep.Step4;
            }
            else if (message.GetMessageType() == typeof(SRPStep4))
            {
                if (_stage != ISRPStep.Step3 && _stage != ISRPStep.Step5)
                {
                    throw new ArgumentException("We are not in the correct state to process this message");
                }
                _serverProof = ((SRPStep4)message).M2;
                _stage = ISRPStep.Step5;
            }
            else if(message.GetMessageType() == typeof(SRPException))
            {
                throw new Exception(((SRPException)message).Message);
            }
            else
            {
                throw new ArgumentException("Unsupported message: " + message);
            }
        }

        public BaseSecureMessage GetNextMessage()
        {
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
                default:
                    return null;
            }
        }

        private BaseSecureMessage Step1()
        {
            if (_lastSentMessage is SRPStep1) return _lastSentMessage;

            _clientSecretEphemeralValue = SRP.a();                              //Generate new private ephemeral value
            _clientPublicEphemeralValue = SRP.A(_clientSecretEphemeralValue);   //Calculate new public ephemeral value
            _lastSentMessage = new SRPStep1(_to, _from, _clientPublicEphemeralValue);
            return _lastSentMessage;
        }

        private BaseSecureMessage Step2()
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
            _sessionKey = SRP.K_Service(_clientPublicEphemeralValue, _userPasswordVerifier, u, b);          //Calculate session key

            _lastSentMessage = new SRPStep2(_to, _from, _userSalt, _serverPublicEphemeralValue);
            _session = _lastSentMessage.Session;
            return _lastSentMessage;
        }

        private BaseSecureMessage Step3()
        {
            if (_lastSentMessage is SRPStep2) return _lastSentMessage;

            byte[] u = SRP.u(_clientPublicEphemeralValue, _serverPublicEphemeralValue);
            if(!SRP.IsValid_B_u(_serverPublicEphemeralValue, u))
            {
                return new SRPException(_to, _from, _session, "Received an invalid B or u value");
            }

            byte[] x = SRP.x(_password, _userSalt);                                                         //Calculate user private key
            _sessionKey = SRP.K_Client(_serverPublicEphemeralValue, x, u, _clientSecretEphemeralValue);     //Calculate session key

            _clientProof = SRP.M1(_clientPublicEphemeralValue, _serverPublicEphemeralValue, _sessionKey);
            _lastSentMessage = new SRPStep3(_to, _from, _session, _clientProof);
            return _lastSentMessage;
        }

        private BaseSecureMessage Step4()
        {
            if (_lastSentMessage is SRPStep4) return _lastSentMessage;

            if(!_clientProof.SequenceEqual(SRP.M1(_clientPublicEphemeralValue, _serverPublicEphemeralValue, _sessionKey)))
            {
                return new SRPException(_to, _from, _session, "Failed to confirm M1 value");
            }

            _stage = ISRPStep.PendingComplete; //We've authenticated the client

            _serverProof = SRP.M2(_clientPublicEphemeralValue, _clientProof, _sessionKey);
            _lastSentMessage = new SRPStep4(_to, _from, _session, _serverProof);
            return _lastSentMessage;
        }

        private BaseSecureMessage Step5()
        {
            if (!_serverProof.SequenceEqual(SRP.M2(_clientPublicEphemeralValue, _clientProof, _sessionKey)))
            {
                return new SRPException(_to, _from, _session, "Failed to confirm M2 value");
            }

            _stage = ISRPStep.Complete; //We've authenticated the server
            return null;
        }

        public bool IsPendingComplete()
        {
            if (_clientSecretEphemeralValue == null) //I'm the server
            {
                return _stage == ISRPStep.PendingComplete;
            }

            throw new InvalidOperationException("This method may only be called when this side of the connection is the \"server\"");
        }

        public void SetServerToComplete()
        {
            if (!IsPendingComplete())
            {
                throw new InvalidOperationException("This method may only be called when this side of the connection is the \"server\", and they're in a PendingComplete state");
            }

            _stage = ISRPStep.Complete;
        }

        public bool IsReadyToSend()
        {
            return _stage == ISRPStep.PendingComplete || _stage == ISRPStep.Complete;
        }

        public void UpdateLastTimeMessageSent()
        {
            _lastTimeSentMessage = DateTime.UtcNow;
        }

        public void UpdateLastTimeMessageReiceved()
        {
            _lastTimeReicevedMessage = DateTime.UtcNow;
        }
    }
}
