namespace RaftConsensusLibrary.Messages.Enums
{
    internal enum ERaftMessageType
    {
        AppendEntryRequest,
        AppendEntryResponse,
        RequestVoteRequest,
        RequestVoteResponse
    }
}
