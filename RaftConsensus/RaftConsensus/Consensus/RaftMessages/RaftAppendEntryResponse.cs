namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftAppendEntryResponse : RaftBaseMessage
    {
        public bool Success { get; set; }
        public int MatchIndex { get; set; }

        public RaftAppendEntryResponse() { }

        public RaftAppendEntryResponse(string to, string from, string clusterName, int term, bool success, int matchIndex)
            : base(to, from, clusterName, term)
        {
            Success = success;
            MatchIndex = matchIndex;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, Success: {Success}, MatchIndex: {MatchIndex}";
        }
    }
}
