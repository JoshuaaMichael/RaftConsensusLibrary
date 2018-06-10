using TeamDecided.RaftConsensus.Consensus.Enums;

namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftAppendEntryResponse : RaftBaseMessage
    {
        public int Term { get; set; }
        public bool Success { get; set; }
        public int MatchIndex { get; set; }

        public RaftAppendEntryResponse() { }

        public RaftAppendEntryResponse(string to, string from, string clusterName, int term, bool success, int matchIndex)
            : base(to, from, clusterName)
        {
            Term = term;
            Success = success;
            MatchIndex = matchIndex;
        }

        public override string ToString()
        {
            return string.Format(base.ToString() + ", Term: {0}, Success: {1}, MatchIndex: {2}", Term, Success, MatchIndex);
        }
    }
}
