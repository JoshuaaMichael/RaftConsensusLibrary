using System;

namespace TeamDecided.RaftConsensus.Consensus.DistributedLog
{
    public interface IRaftDistributedLog<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        bool ContainsKey(TKey key);

        TValue GetValue(int index);
        TValue GetValue(TKey key);
        TValue[] GetValueHistory(TKey key);

        RaftLogEntry<TKey, TValue> GetEntry(int index);
        RaftLogEntry<TKey, TValue> GetEntry(TKey key);
        RaftLogEntry<TKey, TValue>[] GetEntryHistory(TKey key);

        void AppendEntry(RaftLogEntry<TKey, TValue> entry);
        bool AppendEntry(RaftLogEntry<TKey, TValue> entry, int prevIndex, int prevTerm);

        int GetTerm(int index);

        void CommitUpToIndex(int index);

        int CommitIndex { get; }
        int LatestIndex { get; }
        int LatestIndexTerm { get; }
    }
}
