namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftRequestVoteResponse : RaftBaseMessage
    {
        public bool Granted { get; set; }

        public RaftRequestVoteResponse(string to, string from, string clusterName, int term, bool granted)
            : base(to, from, clusterName, term)
        {
            Granted = granted;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, Granted: {Granted}";
        }
    }
}
