using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamDecided.RaftConsensus.Consensus.DistributedLog
{
    public class RaftDistributedLog<TKey, TValue> : IRaftDistributedLog<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        private readonly Dictionary<TKey, List<RaftLogEntry<TKey, TValue>>> _log;
        private readonly List<Tuple<TKey, int>> _logIndexLookup;

        public int CommitIndex { get; private set; }
        public int LatestIndex => _logIndexLookup.Count - 1;
        public int LatestIndexTerm => ((LatestIndex == -1) ? -1 : GetEntry(LatestIndex).Term);

        public RaftDistributedLog()
        {
            _log = new Dictionary<TKey, List<RaftLogEntry<TKey, TValue>>>();
            _logIndexLookup = new List<Tuple<TKey, int>>();
            CommitIndex = -1;
        }

        public void AppendEntry(RaftLogEntry<TKey, TValue> entry)
        {
            if (!_log.ContainsKey(entry.Key))
            {
                _log.Add(entry.Key, new List<RaftLogEntry<TKey, TValue>>());
            }
            _log[entry.Key].Add(entry);
            _logIndexLookup.Add(new Tuple<TKey, int>(entry.Key, _log[entry.Key].Count - 1));
        }

        public bool AppendEntry(RaftLogEntry<TKey, TValue> entry, int prevIndex, int prevTerm)
        {
            if (prevIndex < LatestIndex)
            {
                Truncate(prevIndex);
            }

            if (!ConfirmPreviousIndex(prevIndex, prevTerm)) return false;

            AppendEntry(entry);
            return true;
        }

        /// <summary>
        /// Truncates everything from the log forward, and exclusive, of the given index
        /// </summary>
        /// <param name="index">Exclusive index to truncate forwards from</param>
        private void Truncate(int index)
        {
            int lastLogEntry = LatestIndex;

            for (int i = lastLogEntry; i > index; i--)
            {
                Tuple<TKey, int> dictIndex = _logIndexLookup[i];
                _logIndexLookup.RemoveAt(i);

                _log[dictIndex.Item1].RemoveAt(dictIndex.Item2);
                if (_log[dictIndex.Item1].Count == 0)
                {
                    _log.Remove(dictIndex.Item1);
                }
            }
        }

        private bool ConfirmPreviousIndex(int prevIndex, int prevTerm)
        {
            if (prevIndex == -1 && LatestIndex == -1) { return true; } //No preexisting entries yet

            if(prevIndex != LatestIndex) { return false; }

            Tuple<TKey, int> dictIndex = _logIndexLookup[prevIndex];
            return _log[dictIndex.Item1][dictIndex.Item2].Term == prevTerm;
        }

        public bool ContainsKey(TKey key)
        {
            return _log.ContainsKey(key);
        }

        public RaftLogEntry<TKey, TValue> GetEntry(int index)
        {
            Tuple<TKey, int> dictIndex = _logIndexLookup[index];
            return (RaftLogEntry<TKey, TValue>)_log[dictIndex.Item1][dictIndex.Item2].Clone();
        }

        public RaftLogEntry<TKey, TValue> GetEntry(TKey key)
        {
            return (RaftLogEntry<TKey, TValue>)_log[key].Last().Clone();
        }

        public RaftLogEntry<TKey, TValue>[] GetEntryHistory(TKey key)
        {
            List<RaftLogEntry<TKey, TValue>> entries = _log[key];
            RaftLogEntry<TKey, TValue>[] entryHistory = new RaftLogEntry<TKey, TValue>[entries.Count];

            for(int i = 0; i < entries.Count; i++)
            {
                entryHistory[i] = (RaftLogEntry<TKey, TValue>)entries[i].Clone();
            }

            return entryHistory;
        }

        public TValue GetValue(int index)
        {
            Tuple<TKey, int> dictIndex = _logIndexLookup[index];
            return (TValue)_log[dictIndex.Item1][dictIndex.Item2].Value.Clone();
        }

        public TValue GetValue(TKey key)
        {
            return (TValue)_log[key].Last().Value.Clone();
        }

        public TValue[] GetValueHistory(TKey key)
        {
            List<RaftLogEntry<TKey, TValue>> entries = _log[key];
            TValue[] entryHistory = new TValue[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                entryHistory[i] = (TValue)entries[i].Value.Clone();
            }

            return entryHistory;
        }

        public int GetTerm(int index)
        {
            if (index == -1) return -1; //No entries yet
            Tuple<TKey, int> dictIndex = _logIndexLookup[index];
            return _log[dictIndex.Item1][dictIndex.Item2].Term;
        }

        public void CommitUpToIndex(int index)
        {
            if(index > LatestIndex)
            {
                throw new ArgumentException("Cannot commit at an index larger than stored in log");
            }
            CommitIndex = index;
        }
    }
}
