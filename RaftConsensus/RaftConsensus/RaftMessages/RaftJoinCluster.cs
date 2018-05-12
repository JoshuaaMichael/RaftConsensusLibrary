namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftJoinCluster : RaftBaseMessage
    {
        public string ClusterName { get; private set; }

        public RaftJoinCluster(string to, string from, string clusterName)
            : base(to, from) { ClusterName = clusterName; }
    }
}
