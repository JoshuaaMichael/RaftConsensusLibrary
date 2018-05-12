namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftRequestVoteResponse : RaftBaseMessage
    {
        public int Term { get; private set; }
        public bool Granted { get; private set; }

        public RaftRequestVoteResponse(string to, string from, int term, bool granted)
            : base(to, from)
        {
            Term = term;
            Granted = granted;
        }
    }
}
