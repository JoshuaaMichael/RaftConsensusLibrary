namespace TeamDecided.RaftConsensus.Consensus.Enums
{
    public enum ERaftState
    {
        Initializing,
        Follower,
        Candidate,
        Leader,
        Stopped
    }
}
