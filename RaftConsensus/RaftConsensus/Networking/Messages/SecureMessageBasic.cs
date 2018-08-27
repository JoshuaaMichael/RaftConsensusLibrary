using System.Net;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    internal class SecureMessageBasic : BaseMessage
    {
        public byte[] EncryptedData { get; }

        protected SecureMessageBasic(string to, string from)
            : base(to, from)
        {
            Compressable = false;
        }

        public SecureMessageBasic(IPEndPoint to, byte[] encryptedData)
        {
            IPEndPoint = to;
            EncryptedData = encryptedData;
            Compressable = false;
        }
    }
}
