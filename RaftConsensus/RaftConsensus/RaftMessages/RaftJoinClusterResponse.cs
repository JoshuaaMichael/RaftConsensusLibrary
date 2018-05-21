using Newtonsoft.Json;
using System.Net;
using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinClusterResponse : RaftBaseMessage
    {
        public EJoinClusterResponse JoinClusterResponse { get; set; }
        public string LeaderIP { get; set; }
        public int LeaderPort { get; set; }
        public string ClusterName { get; set; }
        public string ReferenceName { get; set; } //The temp name that the connector gave us to assosiate against our IP until they knew our real name
        public int JoinClusterAttempt { get; set; } //To clear up confusion of late replies from earlier attempts

        public RaftJoinClusterResponse() { }

        public RaftJoinClusterResponse(string to, string from, string referenceName, int joinClusterAttempt, string clusterName, string leaderIP, int leaderPort)
            : base(to, from)
        {
            ReferenceName = referenceName;
            JoinClusterAttempt = joinClusterAttempt;
            JoinClusterResponse = EJoinClusterResponse.FORWARD;
            ClusterName = clusterName;
            LeaderIP = leaderIP;
            LeaderPort = leaderPort;
        }

        public RaftJoinClusterResponse(string to, string from, string referenceName, int joinClusterAttempt, string clusterName, EJoinClusterResponse joinClusterResponse)
            : base(to, from)
        {
            ReferenceName = referenceName;
            JoinClusterAttempt = joinClusterAttempt;
            ClusterName = clusterName;
            JoinClusterResponse = joinClusterResponse;
        }
    }
}
