namespace TeamDecided.RaftConsensus.Consensus.Enums
{
    public enum ERaftState
    {
        INITIALIZING,
        ATTEMPTING_TO_START_CLUSTER,
        ATTEMPTING_TO_JOIN_CLUSTER,
        FOLLOWER,
        CANDIDATE,
        LEADER,
        STOPPED
    }
}
