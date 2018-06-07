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
            CommitIndex = -1;
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

        public void AppendEntry(RaftLogEntry<TKey, TValue> entry, int lastIndex)
        {
            //if last index (or more) already in log already exists, drop them from the log
            int index = lastIndex + 1;
            if(commitIndexLookup.Count - 1 >= index)
            {
                //TODO: Confirm if we should be more specifically be checking for the conflict of "same index but different terms"
                TruncateLog(index);
            }

            if (!log.ContainsKey(entry.Key))
            {
                log.Add(entry.Key, new List<RaftLogEntry<TKey, TValue>>());
            }
            log[entry.Key].Add(entry);
            commitIndexLookup.Add(new Tuple<TKey, int>(entry.Key, log[entry.Key].Count - 1));
        }

        public void AppendEntry(RaftLogEntry<TKey, TValue> entry)
        {
            //This commits in the next available index, it should only be used by testing and doesn't follow Raft rules
            if (!log.ContainsKey(entry.Key))
            {
                log.Add(entry.Key, new List<RaftLogEntry<TKey, TValue>>());
            }
            log[entry.Key].Add(entry);
            commitIndexLookup.Add(new Tuple<TKey, int>(entry.Key, log[entry.Key].Count - 1));
        }

        public void TruncateLog(int index)
        {
            //Drops forward and inclusive of the index given
            int lastLogEntry = commitIndexLookup.Count - 1;

            for(int i = lastLogEntry; i >= index; i--)
            {
                Tuple<TKey, int> commitIndexLookupInfo = commitIndexLookup[i];
                commitIndexLookup.RemoveAt(i);

                //We're going backwards through, so this is always safe
                log[commitIndexLookupInfo.Item1].RemoveAt(commitIndexLookupInfo.Item2);
                if(log[commitIndexLookupInfo.Item1].Count == 0)
                {
                    log.Remove(commitIndexLookupInfo.Item1);
                }
            }
        }

        public int GetTermOfIndex(int index)
        {
            if (index < 0 || index >= commitIndexLookup.Count)
            {
                return -1;
            }
            return GetEntry(index).Term;
        }

        public bool ConfirmPreviousIndex(int prevIndex, int prevTerm)
        {
            if(prevIndex == -1) { return true; } //No preexisting entries yet

            int lastIndex = commitIndexLookup.Count - 1;

            if(lastIndex != prevIndex)
            {
                return false;
            }

            RaftLogEntry<TKey, TValue> lastEntry = GetEntry(lastIndex);

            return (lastEntry.Term == prevTerm);
        }

        public int GetTermOfLastCommit()
        {
            if(CommitIndex == -1) { return -1; } //No previous entries
            Tuple<TKey, int> lookupData = commitIndexLookup[CommitIndex];
            return log[lookupData.Item1][lookupData.Item2].Term;
        }

        public int GetTermOfLastIndex()
        {
            if (commitIndexLookup.Count == 0) { return -1; } //No previous entries
            Tuple<TKey, int> lookupData = commitIndexLookup[commitIndexLookup.Count - 1];
            return log[lookupData.Item1][lookupData.Item2].Term;
        }

        public int GetLastIndex()
        {
            return commitIndexLookup.Count - 1;
        }

        public void CommitUpToIndex(int index)
        {
            CommitIndex = index;
        }

        public TValue GetValue(int commitIndex)
        {
            if(commitIndex < 0 || commitIndex >= commitIndexLookup.Count)
            {
                throw new InvalidOperationException("Failed to get value from log at index " + commitIndex);
            }

            Tuple<TKey, int> lookupData = commitIndexLookup[commitIndex];

            return (TValue)log[lookupData.Item1][lookupData.Item2].Value.Clone();
        }

        private RaftLogEntry<TKey, TValue> GetEntry(int commitIndex)
        {
            if (commitIndex < 0 || commitIndex >= commitIndexLookup.Count)
            {
                throw new InvalidOperationException("Failed to get value from log at index " + commitIndex);
            }

            Tuple<TKey, int> lookupData = commitIndexLookup[commitIndex];

            return log[lookupData.Item1][lookupData.Item2];
        }

        public RaftLogEntry<TKey, TValue> this[int index]
        {
            get
            {
                if (index < 0 || index >= commitIndexLookup.Count)
                {
                    return null;
                }
                return GetEntry(index);
            }
        }
    }
}
