namespace TeamDecided.RaftConsensus.Enums
{
    public enum EJoinClusterResponse
    {
        ACCEPT = 0,
        FORWARD = 1,
        REJECT_WRONG_CLUSTER_NAME = 2,
        REJECT_NAME_TAKEN = 3,
        REJECT_CLUSTER_FULL = 4,
        REJECT_UNKNOWN_ERROR = 5,
        NO_RESPONSE = 6,
        NOT_YET_SET
    }
}
