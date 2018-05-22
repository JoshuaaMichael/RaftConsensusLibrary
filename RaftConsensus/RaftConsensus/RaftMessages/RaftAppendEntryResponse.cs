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

        public RaftAppendEntryResponse(string to, string from, ELogName logName, int term, bool success, int matchIndex)
            : base(to, from)
        {
            LogName = logName;
            Term = term;
            Success = success;
            MatchIndex = matchIndex;
        }
    }
}
