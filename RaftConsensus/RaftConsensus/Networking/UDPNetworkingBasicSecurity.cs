using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking
{
    public sealed class UDPNetworkingBasicSecurity : UDPNetworking
    {
        private readonly byte[] _password;

        public UDPNetworkingBasicSecurity(string password)
        {
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                _password = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public override void SendMessage(BaseMessage message)
        {
            IPEndPoint to = UDPClient.NodeIPs.GetNodeIPEndPoint(message.To);
            if (to == null)
            {
                GenerateSendFailureException("Discarding message, the given message.To does not link to a known IPAddress", message);
                return;
            }

            byte[] serialisedMessage = message.Serialize();
            byte[] encryptedSerialisedMessage = CryptoHelper.Encrypt(serialisedMessage, _password);

            SecureMessageBasic secureMessageBasic = new SecureMessageBasic(to, encryptedSerialisedMessage);
            base.SendMessage(secureMessageBasic);
        }

        protected override BaseMessage DerivedMessageProcessing(BaseMessage message, IPEndPoint ipEndPoint)
        {
            //Need a way to add new clients into RaftClient and SRPSessionManager

            if (message.GetType() != typeof(SecureMessageBasic))
            {
                GenerateReceiveFailureException("Unsupported message type received from: " + ipEndPoint, null);
                return null;
            }

            SecureMessageBasic secureMessageBasic = (SecureMessageBasic) message;

            try
            {
                byte[] decryptedMessage =
                    CryptoHelper.Decrypt(secureMessageBasic.EncryptedData, _password);
                return BaseMessage.Deserialize(decryptedMessage);
            }
            catch
            {
                GenerateReceiveFailureException("Failed to decrypt/deserialize message from: " + ipEndPoint, null);
                return null;
            }
        }

        public override void Dispose()
        {
            if (DisposedValue) return;
            base.Dispose();
        }
    }
}
