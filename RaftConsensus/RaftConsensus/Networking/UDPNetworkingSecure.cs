using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;
using TeamDecided.RaftConsensus.Networking.Messages.SRP;

/*  TODO:
 *      - Seperate completed _secureClients from those being created, this minimises wasted effort for background thread
 *      - Check for dead locking or race conditions between background thread
 *      - Write a faster way to look up clients by their session
 *      - Ensure proper exception handling for resiliency to exceptions
 *      - Add ability to change RetryInterval
 *      - Add ability to handle protocol out of sequence style exceptions from in flight delayed messages
 *      - Validate SRPStep1 data, not be blank, not already exist
 *      - Store dict by session, make sure you can't resend an SRPStep1 to drop existing connection
 *      - Instead of sending in DerivedMessageProcessing, flag the background thread. Minimise exception handling.
 *      - Handle timeouts of other party not responding
 *      - Handle issue of receiving SRPStep1's out of order
 *      - Retry counter
 */

namespace TeamDecided.RaftConsensus.Networking
{
    public sealed class UdpNetworkingSecure : UDPNetworking
    {
        private readonly string _password;
        private readonly Dictionary<string, SRPSessionManager> _secureClientsByName;
        private readonly ManualResetEvent _onDispose;
        private readonly Thread _backgroundThread;
        private const int RetryInterval = 100;

        public UdpNetworkingSecure(string password)
        {
            _password = password;
            _secureClientsByName = new Dictionary<string, SRPSessionManager>();
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

                foreach (KeyValuePair<string, SRPSessionManager> srpManager in _secureClientsByName)
                {
                    if (!srpManager.Value.TimeToRetry(RetryInterval)) continue;
                    base.SendMessage(srpManager.Value.GetNextMessage());
                }
            }
        }

        public override void SendMessage(BaseMessage message)
        {
            if (!_secureClientsByName.ContainsKey(message.To))
            {
                SRPSessionManager clientSRP = new SRPSessionManager(message.To, ClientName, _password, message);
                base.SendMessage(clientSRP.GetNextMessage());
                _secureClientsByName.Add(message.To, clientSRP);
                return;
            }

            if (!_secureClientsByName[message.To].IsSRPComplete())
            {
                Log(ERaftLogType.Debug, "Discarding message, secure communication not setup yet: {0}", message);
                return;
            }

            IPEndPoint to = UDPClient.NodeIPs.GetNodeIPEndPoint(message.To);
            if (to == null)
            {
                GenerateSendFailureException("Discarding message, the given message.To does not link to a known IPAddress", message);
                return;
            }

            byte[] serialisedMessage = message.Serialize();
            byte[] key = _secureClientsByName[message.To].EncryptionKey;
            byte[] encryptedSerialisedMessage = CryptoHelper.Encrypt(serialisedMessage, key);

            string session = _secureClientsByName[message.To].Session;
            SecureMessage secureMessage = new SecureMessage(to, session, encryptedSerialisedMessage);
            base.SendMessage(secureMessage);
        }

        protected override BaseMessage DerivedMessageProcessing(BaseMessage message, IPEndPoint ipEndPoint)
        {
            //Need a way to add new clients into RaftClient and SRPSessionManager

            if (message.GetType() != typeof(SecureMessage) && !message.GetType().IsSubclassOf(typeof(SecureMessage)))
            {
                HandleReceiveFailure("Unsupported message type received", message);
                return null;
            }

            if (message.GetType() == typeof(SRPStep1))
            {
                UDPClient.NodeIPs.AddOrUpdateNode(message.From, ipEndPoint);
                _secureClientsByName.Remove(message.From); //TODO: Re-think. I think it'll break later protocol
                SRPSessionManager srpSessionManager =
                    new SRPSessionManager((SRPStep1)message, ClientName, _password);
                base.SendMessage(srpSessionManager.GetNextMessage());
                _secureClientsByName.Add(message.From, srpSessionManager);
                return null;
            }

            //TODO, lookup via session

            SecureMessage secureMessage = (SecureMessage) message;
            SRPSessionManager mgr = _secureClientsByName.FirstOrDefault(c => c.Value.Session.Equals(secureMessage.Session)).Value;

            if (mgr == null)
            {
                HandleReceiveFailure("Failed to find SRPSessionManager from session: " + secureMessage.Session, message);
                return null;
            }

            if (message.GetType() == typeof(SecureMessage))
            {
                if (!mgr.IsSRPComplete())
                {
                    HandleReceiveFailure("Currently not in a state to decrypt the message, session: " + secureMessage.Session, message);
                    return null;
                }

                if (mgr.IsServerAndPendingComplete())
                {
                    mgr.SetServerToComplete();
                }

                try
                {
                    byte[] decryptedMessage =
                        CryptoHelper.Decrypt(secureMessage.EncryptedData, mgr.EncryptionKey);
                    return BaseMessage.Deserialize(decryptedMessage);
                }
                catch
                {
                    HandleReceiveFailure("Failed to decrypt/deserialize message: " + secureMessage.Session, message);
                    return null;
                }
            }

            if (message.GetType() == typeof(SRPException))
            {
                HandleReceiveFailure("Received error: " + ((SRPException) message).Message + " from " +
                                     secureMessage.Session, message);
                return null;
            }

            try
            {
                mgr.HandleMessage(secureMessage);
                if (mgr.GetNextMessage() != null)
                {
                    base.SendMessage(mgr.GetNextMessage());
                }
            }
            catch (Exception e)
            {
                HandleReceiveFailure("Caught error when processing message through SRPManager: " + e.Message, message);
            }

            return null;
        }

        private void HandleReceiveFailure(string errorString, BaseMessage failedMessage)
        {
            GenerateReceiveFailureException(errorString, null);
            base.SendMessage(new SRPException(failedMessage.From, ClientName, errorString));
        }

        public override void Dispose()
        {
            if (DisposedValue) return;
            _onDispose.Set();
            _backgroundThread.Join();
            base.Dispose();
        }
    }
}
