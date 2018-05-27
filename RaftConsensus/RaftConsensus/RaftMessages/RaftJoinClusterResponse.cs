using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinClusterResponse : RaftBaseMessage
    {
        public EJoinClusterResponse JoinClusterResponse { get; set; }
        public string LeaderIP { get; set; }
        public int LeaderPort { get; set; }
        public string ClusterName { get; set; }
        public int JoinClusterAttempt { get; set; } //To clear up confusion of late replies from earlier attempts

        public RaftJoinClusterResponse() { }

        public RaftJoinClusterResponse(string to, string from, int joinClusterAttempt, string clusterName, string leaderIP, int leaderPort)
            : base(to, from)
        {
            JoinClusterAttempt = joinClusterAttempt;
            JoinClusterResponse = EJoinClusterResponse.FORWARD;
            ClusterName = clusterName;
            LeaderIP = leaderIP;
            LeaderPort = leaderPort;
        }

        public RaftJoinClusterResponse(string to, string from, int joinClusterAttempt, string clusterName, EJoinClusterResponse joinClusterResponse)
            : base(to, from)
        {
            JoinClusterAttempt = joinClusterAttempt;
            ClusterName = clusterName;
            JoinClusterResponse = joinClusterResponse;
        }

        public override string ToString()
        {
            return string.Format(base.ToString() + ", JoinClusterResponse:{0}, LeaderIP: {1}, LeaderPort: {2}, ClusterName: {3}, JoinClusterAttempt: {4}", JoinClusterResponse, LeaderIP, LeaderPort, ClusterName, JoinClusterAttempt);
        }
    }
}
