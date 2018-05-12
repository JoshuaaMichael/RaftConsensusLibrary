using System.Net;
using Newtonsoft.Json;

namespace TeamDecided.RaftNetworking.Messages
{
    internal class SecureMessage : BaseMessage
    {
        public string Session { get;  set; }
        public byte[] EncryptedData { get; private set; }
        public byte[] HMAC { get; private set; }

        internal SecureMessage() { }

        [JsonConstructor]
        internal SecureMessage(IPEndPoint ipEndPoint, string session, byte[] encryptedData, byte[] hmac)
        {
            IPEndPoint = ipEndPoint;
            Session = session;
            EncryptedData = encryptedData;
            HMAC = hmac;
        }
    }
}
