using TeamDecided.RaftConsensus.Networking.Enums;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    internal class SecureServerChallengeResponse : SecureMessage
    {
        public ESecureChallengeResult ChallengeResult;
        public byte[] ChallengeResponse;
    }
}
