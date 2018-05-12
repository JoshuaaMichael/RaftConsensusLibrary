namespace TeamDecided.RaftNetworking.Messages
{
    internal class SecureClientHello : SecureMessage
    {
        public byte[] PublicKey;
    }
}
