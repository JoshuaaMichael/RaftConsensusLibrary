using System;
using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftAppendEntry<TKey, TValue> : RaftBaseMessage where TKey : ICloneable where TValue : ICloneable
    {
        public ELogName LogName { get; private set; }
        public int Term { get; private set; }
        public int PrevIndex { get; private set; }
        public int PrevTerm { get; private set; }
        public int CommitIndex { get; private set; }
        public RaftLogEntry<TKey, TValue> Entry { get; private set; }

        public RaftAppendEntry(string to, string from, ELogName logName, int term, int prevIndex, int prevTerm, int commitIndex, RaftLogEntry<TKey, TValue> entry)
            : base(to, from)
        {
            LogName = logName;
            Term = term;
            PrevIndex = prevIndex;
            PrevTerm = prevTerm;
            CommitIndex = commitIndex;
            Entry = entry;
        }

        public RaftAppendEntry(string to, string from, ELogName logName, int term, int prevIndex, int prevTerm, int commitIndex)
            : base(to, from)
        {
            LogName = logName;
            Term = term;
            PrevIndex = prevIndex;
            PrevTerm = prevTerm;
            CommitIndex = commitIndex;
        }
    }
}
