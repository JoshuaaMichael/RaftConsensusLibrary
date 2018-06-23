using System;
using System.Linq;
using System.Security.Cryptography;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Messages;
using TeamDecided.RaftConsensus.Networking.Messages.SRP;
using Eneter.SecureRemotePassword;

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    public class SRPSessionManager
    {
        private static readonly RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();

        private ISRPStep _stage;

        private string _password;

        private string _session;

        private readonly string _to;
        private readonly string _from;
        private byte[] a;
        private byte[] A;

        private byte[] s;
        private byte[] v;
        private byte[] B;
        private byte[] K;

        private byte[] M1;

        BaseSecureMessage _lastSentMessage;
        private DateTime _lastTimeSentMessage;
        private DateTime _lastTimeReicevedMessage;

        private SRPSessionManager(ISRPStep stage)
        {
            _stage = stage;
        }

        public SRPSessionManager(string to, string from, string password)
            : this(ISRPStep.NOT_CONTACTED)
        {
            _to = to;
            _from = from;
            _password = password;
        }

        public SRPSessionManager(SRPStep1 message, string ownName, string password)
            : this(ISRPStep.STEP_2)
        {
            A = message.A;
            _from = ownName;
            _password = password;
        }

        public void HandleMessage(BaseSecureMessage message)
        {
            if (_stage == ISRPStep.NOT_CONTACTED)
            {
                throw new ArgumentException("Cannot process a message yet, we haven't even sent a message yet");
            }

            if (_stage == ISRPStep.COMPLETE)
            {
                throw new ArgumentException("Cannot process any more message. Exchange already complete.");
            }

            if (message.GetMessageType() == typeof(SRPStep1))
            {
                //We must have had some packet loss, GetNextMessage() will handle that
                return;
            }
            else if(message.GetMessageType() == typeof(SRPStep2))
            {
                if (_stage == ISRPStep.STEP_1)
                {
                    B = ((SRPStep2)message).B;
                    s = ((SRPStep2)message).s;
                    _stage = ISRPStep.STEP_3;
                }
                else if (_stage == ISRPStep.STEP_3)
                {
                    //We must have had some packet loss, GetNextMessage() will handle that
                }
                else
                {
                    throw new ArgumentException("We are not in the correct state to process this message");
                }
            }
            else if (message.GetMessageType() == typeof(SRPStep3))
            {
                if (_stage == ISRPStep.STEP_2)
                {
                    M1 = ((SRPStep3)message).M1;
                    _stage = ISRPStep.STEP_4;
                }
                else if (_stage == ISRPStep.STEP_4)
                {
                    //We must have had some packet loss, GetNextMessage() will handle that
                }
                else
                {
                    throw new ArgumentException("We are not in the correct state to process this message");
                }
            }
            else if (message.GetMessageType() == typeof(SRPStep4))
            {
                if (_stage == ISRPStep.STEP_3)
                {
                    _stage = ISRPStep.COMPLETE;
                }
                else if (_stage == ISRPStep.COMPLETE)
                {
                    //We must have had some packet loss, GetNextMessage() will handle that
                }
                else
                {
                    throw new ArgumentException("We are not in the correct state to process this message");
                }
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

        public BaseSecureMessage GetNextMessage()
        {
            if (_stage == ISRPStep.NOT_CONTACTED)
            {
                _stage = ISRPStep.STEP_1;
                return Step1();
            }
            else if(_stage == ISRPStep.STEP_1)
            {
                return Step1();
            }
            else if (_stage == ISRPStep.STEP_2)
            {
                return Step2();
            }
            else if (_stage == ISRPStep.STEP_3)
            {
                return Step3();
            }
            else if (_stage == ISRPStep.STEP_4)
            {
                return Step4();
            }
            else //_stage == ISRPStage.COMPLETE
            {
                return null;
            }
        }

        private BaseSecureMessage Step1()
        {
            if (!(_lastSentMessage is SRPStep1))
            {
                a = SRP.a();    //Generate new private ephemeral value
                A = SRP.A(a);   //Calculate new public ephemeral value
                _lastSentMessage = new SRPStep1(_to, _from, A);
            }

            return _lastSentMessage;
        }

        private BaseSecureMessage Step2()
        {
            if (!(_lastSentMessage is SRPStep2))
            {
                if (!SRP.IsValid_A(A))
                {
                    return new SRPException(_to, _from, "Received an invalid A value");
                }

                s = SRP.s();                            //Generate user salt
                byte[] x = SRP.x(_password, s);         //Calculates user private key
                v = SRP.v(x);                           //Calculates user verifier

                byte[] b = SRP.b();                     //Generate service private ephemeral value
                B = SRP.B(b, v);                        //Calculate service public ephemeral value
                byte[] u = SRP.u(A, B);                 //Calcualte random scambling value
                K = SRP.K_Service(A, v, u, b);          //Calculate session key

                _lastSentMessage = new SRPStep2(_to, _from, s, B);
                _session = _lastSentMessage.Session;
            }

            return _lastSentMessage;
        }

        private BaseSecureMessage Step3()
        {
            if (!(_lastSentMessage is SRPStep2))
            {
                byte[] u = SRP.u(A, B);
                if(!SRP.IsValid_B_u(B, u))
                {
                    return new SRPException(_to, _from, _session, "Received an invalid B or u value");
                }

                byte[] x = SRP.x(_password, s);     //Calculate user private key
                K = SRP.K_Client(B, x, u, a);       //Calculate session key

                M1 = SRP.M1(A, B, K);

                _lastSentMessage = new SRPStep3(_to, _from, _session, M1);
            }

            return _lastSentMessage;
        }

        private BaseSecureMessage Step4()
        {
            if (!(_lastSentMessage is SRPStep4))
            {
                if(!M1.SequenceEqual(SRP.M1(A, B, K)))
                {
                    return new SRPException(_to, _from, _session, "Failed to confirm M1 value");
                }

                _stage = ISRPStep.COMPLETE;

                byte[] M2 = SRP.M2(A, M1, K);
                _lastSentMessage = new SRPStep4(_to, _from, _session, M2);
            }
            return _lastSentMessage;
        }

        private BaseSecureMessage Step5()
        {
            //Confirm M2
        }

        //Step5
        //Confirm M2, send all clear

        //Step6
        //

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
