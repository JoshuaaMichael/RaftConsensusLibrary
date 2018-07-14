using System.Net;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    internal class SecureMessage : BaseMessage
    {
        public string Session { get; }
        public byte[] EncryptedData { get; }

        //Used by derived types who don't have a session yet
        protected SecureMessage(string to, string from)
            : base(to, from)
        {
            Compressable = false;
        }

        //Used to send SecureMessages which have a session
        protected SecureMessage(string to, string from, string session)
            : this(to, from)
        {
            Session = session;
        }

        //Used to send/receive encrypted data
        public SecureMessage(IPEndPoint to, string session, byte[] encryptedData)
        {
            IPEndPoint = to;
            Session = session;
            EncryptedData = encryptedData;
            Compressable = false;
        }
    }
}
