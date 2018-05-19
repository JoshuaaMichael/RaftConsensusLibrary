namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinCluster : RaftBaseMessage
    {
        public string ClusterName { get; private set; }
        public string ReferenceName { get; private set; } //The temp name that we give us to assosiate against our IP until we knew their real name
        public int JoinClusterAttempt { get; private set; } //To clear up confusion of late replies from earlier attempts

        public RaftJoinCluster(string to, string from, string clusterName, string referenceName, int joinClusterAttempt)
            : base(to, from)
        {
            ClusterName = clusterName;
            ReferenceName = referenceName;
            JoinClusterAttempt = joinClusterAttempt;
        }
    }
}
