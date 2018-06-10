using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamDecided.RaftConsensus.Consensus
{
    public class RaftLogEntry<TKey, TValue> : IEqualityComparer<TKey>, ICloneable where TKey : ICloneable where TValue : ICloneable
    {
        public TKey Key { get; }
        public TValue Value { get; }
        public int Term { get; }

        [JsonConstructor]
        public RaftLogEntry(TKey key, TValue value, int term)
        {
            Key = key;
            Value = value;
            Term = term;
        }

        public bool Equals(TKey x, TKey y)
        {
            return x != null && x.Equals(y);
        }

        public int GetHashCode(TKey obj)
        {
            return obj.GetHashCode();
        }

        public object Clone()
        {
            RaftLogEntry<TKey, TValue> clone = new RaftLogEntry<TKey, TValue>((TKey)Key.Clone(), (TValue)Value.Clone(), Term);
            return clone;
        }

        public override string ToString()
        {
            return string.Format("Key:{0}, Value: {1}, Term: {2}", Key, Value, Term);
        }

        public Tuple<TKey, TValue> GetTuple()
        {
            return new Tuple<TKey, TValue>(Key, Value);
        }
    }
}
