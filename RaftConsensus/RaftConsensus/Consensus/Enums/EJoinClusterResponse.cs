namespace TeamDecided.RaftConsensus.Consensus.Enums
{
    public enum EJoinClusterResponse
    {
        Accept,
        Forward,
        RejectWrongClusterName,
        RejectLeaderUnknown,
        RejectNameTaken,
        RejectClusterFull ,
        RejectUnknownError ,
        NoResponse,
        NotYetSet
    }
}
