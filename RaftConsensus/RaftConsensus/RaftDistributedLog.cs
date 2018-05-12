using System;
using System.Collections.Generic;

namespace TeamDecided.RaftConsensus
{
    public class RaftDistributedLog<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        private Dictionary<TKey, List<RaftLogEntry<TKey, TValue>>> log;
        private List<Tuple<TKey, int>> commitIndexLookup;
        public int CommitIndex { get; private set; }

        public RaftDistributedLog()
        {
            log = new Dictionary<TKey, List<RaftLogEntry<TKey, TValue>>>();
            commitIndexLookup = new List<Tuple<TKey, int>>();
            commitIndexLookup.Add(null); //You cannot use the 0th index of the array
            CommitIndex = 0;
        }

        public bool ContainsKey(TKey key)
        {
            return log.ContainsKey(key);
        }

        public TValue GetValue(TKey key)
        {
            if (log.TryGetValue(key, out List<RaftLogEntry<TKey, TValue>> logEntries))
            {
                TValue newestValue = logEntries[logEntries.Count - 1].Value;
                return (TValue)newestValue.Clone();
            }
            throw new KeyNotFoundException("Value not found from key");
        }

        public TValue[] GetValueHistory(TKey key)
        {
            if (log.TryGetValue(key, out List<RaftLogEntry<TKey, TValue>> logEntries))
            {
                List<TValue> tempCloned = new List<TValue>(logEntries.Count);
                for (int i = 0; i < logEntries.Count; i++)
                {
                    tempCloned.Add((TValue)logEntries[i].Value.Clone());
                }
                return tempCloned.ToArray();
            }
            throw new KeyNotFoundException("Values not found from key");
        }

        public RaftLogEntry<TKey, TValue> GetEntry(TKey key)
        {
            if (log.TryGetValue(key, out List<RaftLogEntry<TKey, TValue>> logEntries))
            {
                RaftLogEntry<TKey, TValue> newestValue = logEntries[logEntries.Count - 1];
                return (RaftLogEntry<TKey, TValue>)newestValue.Clone();
            }
            throw new KeyNotFoundException("Value not found from key");
        }

        public RaftLogEntry<TKey, TValue>[] GetEntryHistory(TKey key)
        {
            if (log.TryGetValue(key, out List<RaftLogEntry<TKey, TValue>> logEntries))
            {
                List<RaftLogEntry<TKey, TValue>> tempCloned = new List<RaftLogEntry<TKey, TValue>>(logEntries.Count);
                for (int i = 0; i < logEntries.Count; i++)
                {
                    tempCloned.Add((RaftLogEntry<TKey, TValue>)logEntries[i].Clone());
                }
                return tempCloned.ToArray();
            }
            throw new KeyNotFoundException("Values not found from key");
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (log.ContainsKey(key))
            {
                value = GetValue(key);
                return true;
            }
            value = default(TValue);
            return false;
        }

        public bool TryGetEntry(TKey key, out RaftLogEntry<TKey, TValue> value)
        {
            if (log.ContainsKey(key))
            {
                value = GetEntry(key);
                return true;
            }
            value = default(RaftLogEntry<TKey, TValue>);
            return false;
        }

        public void AppendEntry(RaftLogEntry<TKey, TValue> entry)
        {
            CommitIndex += 1;
            if (!log.ContainsKey(entry.Key))
            {
                log.Add(entry.Key, new List<RaftLogEntry<TKey, TValue>>());
            }
            log[entry.Key].Add(entry);
            commitIndexLookup.Add(new Tuple<TKey, int>(entry.Key, log[entry.Key].Count - 1));
        }

        public TValue GetValue(int commitIndex)
        {
            if(commitIndex < 1 || commitIndex >= commitIndexLookup.Count)
            {
                return default(TValue);
            }

            Tuple<TKey, int> lookupData = commitIndexLookup[commitIndex];

            return (TValue)log[lookupData.Item1][lookupData.Item2].Value.Clone();
        }
    }
}
