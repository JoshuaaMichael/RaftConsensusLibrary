namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinCluster : RaftBaseMessage
    {
        public string ClusterName { get; set; }
        public string ReferenceName { get; set; } //The temp name that we give us to assosiate against our IP until we knew their real name
        public int JoinClusterAttempt { get; set; } //To clear up confusion of late replies from earlier attempts

        public RaftJoinCluster() { }

        public RaftJoinCluster(string to, string from, string clusterName, string referenceName, int joinClusterAttempt)
            : base(to, from)
        {
            ClusterName = clusterName;
            ReferenceName = referenceName;
            JoinClusterAttempt = joinClusterAttempt;
        }
    }
}
