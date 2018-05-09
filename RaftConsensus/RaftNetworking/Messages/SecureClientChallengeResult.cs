using TeamDecided.RaftNetworking.Enums;

namespace TeamDecided.RaftNetworking.Messages
{
    internal class SecureClientChallengeResult : SecureMessage
    {
        public ESecureChallengeResult ChallengeResult;
    }
}
