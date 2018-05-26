namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinCluster : RaftBaseMessage
    {
        public string ClusterName { get; set; }
        public int JoinClusterAttempt { get; set; } //To clear up confusion of late replies from earlier attempts

        public RaftJoinCluster() { }

        public RaftJoinCluster(string to, string from, string clusterName, int joinClusterAttempt)
            : base(to, from)
        {
            ClusterName = clusterName;
            JoinClusterAttempt = joinClusterAttempt;
        }
    }
}
