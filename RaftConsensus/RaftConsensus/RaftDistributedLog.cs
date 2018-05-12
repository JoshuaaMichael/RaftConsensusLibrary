using System;
using System.Collections.Generic;

namespace TeamDecided.RaftConsensus
{
    public class RaftDistributedLog<TKey, TValue>
    {
        Dictionary<TKey, List<RaftLogEntry<TKey, TValue>>> log;
        List<Tuple<TKey, int>> commitIndexLookup;
        int commitIndex;
        int lastApplied;

        public RaftDistributedLog()
        {
        }
    }
}
