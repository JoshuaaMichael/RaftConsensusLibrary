using System;

namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftAppendEntry<TKey, TValue> : RaftBaseMessage where TKey : ICloneable where TValue : ICloneable
    {
        public int PrevIndex { get; set; }
        public int PrevTerm { get; set; }
        public int LeaderCommitIndex { get; set; } //The max commit index of the leader
        public RaftLogEntry<TKey, TValue> Entry { get; set; }

        public RaftAppendEntry() { }

        public RaftAppendEntry(string to, string from, string clusterName, int term, int prevIndex, int prevTerm, int leaderCommitIndex, RaftLogEntry<TKey, TValue> entry)
            : base(to, from, clusterName, term)
        {
            PrevIndex = prevIndex;
            PrevTerm = prevTerm;
            LeaderCommitIndex = leaderCommitIndex;
            Entry = entry;
        }

        public RaftAppendEntry(string to, string from, string clusterName, int term, int leaderCommitIndex)
            : base(to, from, clusterName, term)
        {
            Term = term;
            LeaderCommitIndex = leaderCommitIndex;
        }

        public override string ToString()
        {
            return
                $"{base.ToString()}, PrevIndex: {PrevIndex}, PrevTerm: {PrevTerm}, LeaderCommitIndex: {LeaderCommitIndex}, Entry: {Entry}";
        }
    }
}
