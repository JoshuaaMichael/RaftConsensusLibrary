using System;
using TeamDecided.RaftConsensus.Enums;

namespace TeamDecided.RaftConsensus
{
    public class ConsensusAppendEntryResult<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        public ERaftAppendEntryState State;
        public TKey Key;
        public TValue Value;
    }
}
