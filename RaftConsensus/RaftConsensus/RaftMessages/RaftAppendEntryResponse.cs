using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftAppendEntryResponse : RaftBaseMessage
    {
        public ELogName LogName { get; set; }
        public int Term { get; set; }
        public bool Success { get; set; }
        public int MatchIndex { get; set; }

        public RaftAppendEntryResponse() { }

        public RaftAppendEntryResponse(string to, string from, string clusterName, ELogName logName, int term, bool success, int matchIndex)
            : base(to, from, clusterName)
        {
            LogName = logName;
            Term = term;
            Success = success;
            MatchIndex = matchIndex;
        }

        public override string ToString()
        {
            return string.Format(base.ToString() + ", LogName:{0}, Term: {1}, Success: {2}, MatchIndex: {3}", LogName, Term, Success, MatchIndex);
        }
    }
}
