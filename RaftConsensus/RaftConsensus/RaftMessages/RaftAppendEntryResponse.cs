using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftAppendEntryResponse : RaftBaseMessage
    {
        public ELogName LogName { get; private set; }
        public int Term { get; private set; }
        public bool Success { get; private set; }
        public int MatchIndex { get; private set; }

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
