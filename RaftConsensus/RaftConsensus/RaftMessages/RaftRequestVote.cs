namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftRequestVote : RaftBaseMessage
    {
        public int Term { get; private set; }
        public int LastLogIndex { get; private set; }
        public int LastTermIndex { get; private set; }

        public RaftRequestVote(string to, string from, int term, int lastLogIndex, int lastTermIndex)
            : base(to, from)
        {
            Term = term;
            LastLogIndex = lastLogIndex;
            LastTermIndex = lastTermIndex;
        }

        public override string ToString()
        {
            return string.Format(base.ToString() + ", Term:{0}, LastLogIndex: {1}, LastTermIndex: {2}", Term, LastLogIndex, LastTermIndex);
        }
    }
}
