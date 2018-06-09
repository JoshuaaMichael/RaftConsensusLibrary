namespace TeamDecided.RaftConsensus.Networking.Messages
{
    internal class SecureClientHello : SecureMessage
    {
        public byte[] PublicKey;
    }
}
