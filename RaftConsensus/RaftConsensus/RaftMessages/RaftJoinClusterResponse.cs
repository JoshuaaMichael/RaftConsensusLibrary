using System.Net;
using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinClusterResponse : RaftBaseMessage
    {
        public EJoinClusterResponse JoinClusterResponse { get; private set; }
        public string LeaderName { get; private set; }
        public IPEndPoint LeaderIP { get; private set; }

        public RaftJoinClusterResponse(string to, string from, EJoinClusterResponse joinClusterResponse, string leaderName, IPEndPoint leaderIP)
            : base(to, from)
        {
            JoinClusterResponse = joinClusterResponse;
            LeaderName = leaderName;
            LeaderIP = leaderIP;
        }

        public RaftJoinClusterResponse(string to, string from, EJoinClusterResponse joinClusterResponse)
            : base(to, from)
        {
            JoinClusterResponse = joinClusterResponse;
        }
    }
}
