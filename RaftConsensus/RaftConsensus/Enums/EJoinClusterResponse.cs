namespace TeamDecided.RaftConsensus.Enums
{
    public enum EJoinClusterResponse
    {
        ACCEPT,
        FORWARD,
        REJECT_WRONG_CLUSTER_NAME,
        REJECT_LEADER_UNKNOWN,
        REJECT_NAME_TAKEN,
        REJECT_CLUSTER_FULL ,
        REJECT_UNKNOWN_ERROR ,
        NO_RESPONSE,
        NOT_YET_SET
    }
}
