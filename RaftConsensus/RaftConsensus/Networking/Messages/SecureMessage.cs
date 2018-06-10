using System.Net;
using Newtonsoft.Json;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    internal class SecureMessage : BaseMessage
    {
        public string Session { get;  set; }
        public byte[] EncryptedData { get; private set; }
        public byte[] Hmac { get; private set; }

        internal SecureMessage() { }

        [JsonConstructor]
        internal SecureMessage(IPEndPoint ipEndPoint, string session, byte[] encryptedData, byte[] hmac)
        {
            IpEndPoint = ipEndPoint;
            Session = session;
            EncryptedData = encryptedData;
            Hmac = hmac;
        }
    }
}
