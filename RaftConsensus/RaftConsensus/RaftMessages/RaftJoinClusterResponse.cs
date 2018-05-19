using System.Net;
using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinClusterResponse : RaftBaseMessage
    {
        public EJoinClusterResponse JoinClusterResponse { get; private set; }
        public IPEndPoint LeaderIP { get; private set; }
        public string ReferenceName { get; private set; } //The temp name that the connector gave us to assosiate against our IP until they knew our real name
        public int JoinClusterAttempt { get; private set; } //To clear up confusion of late replies from earlier attempts

        public RaftJoinClusterResponse(string to, string from, string referenceName, int joinClusterAttempt, IPEndPoint leaderIP)
            : base(to, from)
        {
            ReferenceName = referenceName;
            JoinClusterAttempt = joinClusterAttempt;
            JoinClusterResponse = EJoinClusterResponse.FORWARD;
            LeaderIP = leaderIP;
        }

        public RaftJoinClusterResponse(string to, string from, string referenceName, int joinClusterAttempt, EJoinClusterResponse joinClusterResponse)
            : base(to, from)
        {
            ReferenceName = referenceName;
            JoinClusterAttempt = joinClusterAttempt;
            JoinClusterResponse = joinClusterResponse;
        }
    }
}
