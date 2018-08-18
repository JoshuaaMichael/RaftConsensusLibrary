using System;
using System.Diagnostics;

namespace TeamDecided.RaftConsensus.Consensus
{
    public class NodeInfo
    {
        public string NodeName { get; private set; }
        public int NextIndex { get; set; }
        public int MatchIndex { get; set; }
        public bool VoteGranted { get; set; }
        public long LastReceived { get; private set; }
        public long LastSentHeartbeat { get; private set; }
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public NodeInfo(string nodeName)
        {
            NodeName = nodeName;
            LastReceived = _sw.ElapsedMilliseconds;
            LastSentHeartbeat = _sw.ElapsedMilliseconds;
            NextIndex = 0;
            MatchIndex = -1;
        }

        public void UpdateLastReceived()
        {
            LastReceived = _sw.ElapsedMilliseconds;
        }

        public bool ReadyForHeartbeat(int timeout)
        {
            return _sw.ElapsedMilliseconds > LastSentHeartbeat + timeout
                    && _sw.ElapsedMilliseconds - LastReceived > timeout;
        }

        public int MsUntilTimeout(int timeout)
        {
            //Last time sent heartbeat
            //  
            //

            return Math.Max(0, (int) (LastReceived + timeout - _sw.ElapsedMilliseconds));
        }

        //TODO: Figure out why this is giving us negative values and breaking the waiting loop

        public void UpdateLastSentHeartbeat() //make MsTimeout 150
        {
            LastSentHeartbeat = _sw.ElapsedMilliseconds;
        }
    }
}
