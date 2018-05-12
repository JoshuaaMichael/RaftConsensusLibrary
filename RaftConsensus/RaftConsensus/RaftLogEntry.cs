using System;
using System.Collections.Generic;


namespace TeamDecided.RaftConsensus
{
    public class RaftLogEntry<TKey, TValue>:IEqualityComparer<TKey>
    {
        public TKey Key { get; private set; }
        public TValue Value { get; private set; }
        public int Term { get; private set; }
        public int CommitIndex { get; private set; }

        public RaftLogEntry(TKey key, TValue value, int term, int commitIndex)
        {
            Key = key;
            Value = value;
            Term = term;
            CommitIndex = commitIndex;
        }

        public bool Equals(TKey x, TKey y)
        {
            throw new NotImplementedException();
        }

        public int GetHashCode(TKey obj)
        {
            throw new NotImplementedException();
        }
    }
}
