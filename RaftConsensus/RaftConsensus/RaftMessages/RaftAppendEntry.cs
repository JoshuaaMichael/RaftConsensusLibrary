using System;
using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftAppendEntry<TKey, TValue> : RaftBaseMessage where TKey : ICloneable where TValue : ICloneable
    {
        public ELogName LogName { get; set; }
        public int Term { get; set; }
        public int PrevIndex { get; set; }
        public int PrevTerm { get; set; }
        public int LeaderCommitIndex { get; set; } //The max commit index of the leader
        public RaftLogEntry<TKey, TValue> Entry { get; set; }

        public RaftAppendEntry() { }

        public RaftAppendEntry(string to, string from, ELogName logName, int term, int prevIndex, int prevTerm, int leaderCommitIndex, RaftLogEntry<TKey, TValue> entry)
            : base(to, from)
        {
            LogName = logName;
            Term = term;
            PrevIndex = prevIndex;
            PrevTerm = prevTerm;
            LeaderCommitIndex = leaderCommitIndex;
            Entry = entry;
        }

        public RaftAppendEntry(string to, string from, ELogName logName, int term, int leaderCommitIndex)
            : base(to, from)
        {
            LogName = logName;
            Term = term;
            LeaderCommitIndex = leaderCommitIndex;
        }

        //This is the index of this entry
        public int GetLogIndex()
        {
            return PrevIndex + 1;
        }

        public override string ToString()
        {
            return string.Format(base.ToString() + ", LogName:{0}, Term: {1}, PrevIndex: {2}, PrevTerm: {3}, LeaderCommitIndex: {4}, Entry: {5}", LogName, Term, PrevIndex, PrevTerm, LeaderCommitIndex, Entry);
        }
    }
}
