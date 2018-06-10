namespace TeamDecided.RaftConsensus.Consensus.Enums
{
    public enum ERaftState
    {
        Initializing,
        AttemptingToStartCluster,
        AttemptingToJoinCluster,
        Follower,
        Candidate,
        Leader,
        Stopped
    }
}
