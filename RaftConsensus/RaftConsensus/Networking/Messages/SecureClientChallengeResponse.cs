namespace TeamDecided.RaftConsensus.Networking.Messages
{
    internal class SecureClientChallengeResponse : SecureMessage
    {
        public byte[] ChallengeResponse;
        public byte[] Challenge;
        public string ClientName;
    }
}
