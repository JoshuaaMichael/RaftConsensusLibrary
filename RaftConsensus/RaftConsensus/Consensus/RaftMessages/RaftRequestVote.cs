namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftRequestVote : RaftBaseMessage
    {
        public int LastLogIndex { get; set; }
        public int LastTermIndex { get; set; }

        public RaftRequestVote(string to, string from, string clusterName, int term, int lastLogIndex, int lastTermIndex)
            : base(to, from, clusterName, term)
        {
            LastLogIndex = lastLogIndex;
            LastTermIndex = lastTermIndex;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, LastLogIndex: {LastLogIndex}, LastTermIndex: {LastTermIndex}";
        }
    }
}
