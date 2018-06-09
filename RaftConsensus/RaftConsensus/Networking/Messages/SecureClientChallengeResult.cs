using TeamDecided.RaftConsensus.Networking.Enums;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    internal class SecureClientChallengeResult : SecureMessage
    {
        public ESecureChallengeResult ChallengeResult;
    }
}
