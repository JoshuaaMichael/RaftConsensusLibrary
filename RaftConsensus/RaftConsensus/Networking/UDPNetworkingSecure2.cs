using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking
{
    public sealed class UdpNetworkingSecure2 : UDPNetworking
    {
        private readonly string _password;
        private readonly Dictionary<string, SRPSessionManager> _secureClients;
        private readonly ManualResetEvent _onDispose;
        private readonly Thread _backgroundThread;
        private const int RetryInterval = 100;

        public UdpNetworkingSecure2(string password)
        {
            _password = password;
            _secureClients = new Dictionary<string, SRPSessionManager>();
            _onDispose = new ManualResetEvent(false);
            _backgroundThread = new Thread(BackgroundThread);
            _backgroundThread.Start();
        }

        private void BackgroundThread()
        {
            while (true)
            {
                if (!_onDispose.WaitOne(RetryInterval))
                {
                    return;
                }

                foreach (KeyValuePair<string, SRPSessionManager> srpManager in _secureClients)
                {
                    if (srpManager.Value.TimeToRetry(RetryInterval))
                    {
                        base.SendMessage(srpManager.Value.GetNextMessage());
                        srpManager.Value.UpdateLastTimeMessageSent();
                    }
                }
            }
        }

        public override void SendMessage(BaseMessage message)
        {
            if (_secureClients.ContainsKey(message.To))
            {
                if (!_secureClients[message.To].IsReadyToSend())
                {
                    Log(ERaftLogType.Debug, "Discarding message, secure communication not setup yet: {0}", message);
                    return;
                }
                //Encrypt message, with compression disabled
                //base.SendMessage(message);
            }
            else
            {
                SRPSessionManager clientSRP = new SRPSessionManager(message.To, ClientName, _password, message);
                base.SendMessage(clientSRP.GetNextMessage());
                clientSRP.UpdateLastTimeMessageSent();
                _secureClients.Add(message.To, clientSRP);
            }
        }

        protected override BaseMessage DerivedMessageProcessing(BaseMessage message, IPEndPoint ipEndPoint)
        {
            //Need to detect when complete so can send the buffered message
            //Detect if we were pending and this finishes us off now

            if (message.GetType() == typeof(SecureMessage))
            {
                //Lookup session
                //Decrypt message
                //Else send them back an exception and log we're discarding and logging
            }
            else if (message.GetType().IsSubclassOf(typeof(SecureMessage)))
            {
                //These are messages which are part of an establishing protocol
                //Process them through the dict
            }
            else
            {
                GenerateReceiveFailureException("Unencrypted message recieved, this is unsupported", null);
            }

            return null;
        }

        public override void Dispose()
        {
            if (_disposedValue) return;
            _onDispose.Set();
            _backgroundThread.Join();
            base.Dispose();
        }
    }
}
