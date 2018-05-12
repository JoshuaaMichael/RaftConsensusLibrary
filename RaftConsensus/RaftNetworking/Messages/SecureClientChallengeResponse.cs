namespace TeamDecided.RaftNetworking.Messages
{
    internal class SecureClientChallengeResponse : SecureMessage
    {
        public byte[] ChallengeResponse;
        public byte[] Challenge;
        public string ClientName;
    }
}
