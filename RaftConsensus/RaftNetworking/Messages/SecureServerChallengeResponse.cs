using TeamDecided.RaftNetworking.Enums;

namespace TeamDecided.RaftNetworking.Messages
{
    internal class SecureServerChallengeResponse : SecureMessage
    {
        public ESecureChallengeResult ChallengeResult;
        public byte[] ChallengeResponse;
    }
}
