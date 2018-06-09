namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftRequestVoteResponse : RaftBaseMessage
    {
        public int Term { get; private set; }
        public bool Granted { get; private set; }

        public RaftRequestVoteResponse(string to, string from, string clusterName, int term, bool granted)
            : base(to, from, clusterName)
        {
            Term = term;
            Granted = granted;
        }

        public override string ToString()
        {
            return string.Format(base.ToString() + ", Term:{0}, Granted: {1}", Term, Granted);
        }
    }
}
