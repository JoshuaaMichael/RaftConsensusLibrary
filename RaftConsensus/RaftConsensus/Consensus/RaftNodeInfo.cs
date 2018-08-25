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
            return MsUntilTimeout(timeout) == 0;
            //return _sw.ElapsedMilliseconds > LastSentHeartbeat + timeout //Next time we would naturally send them a heartbeat
            //        && _sw.ElapsedMilliseconds - LastReceived > timeout; //But in case we've heard from them since, is it 150 after that?
        }

        public int MsUntilTimeout(int timeout)
        {
            long lastEvent = Math.Max(LastSentHeartbeat, LastReceived);
            long timeSinceLastEvent = _sw.ElapsedMilliseconds - lastEvent;
            long timeToNextEvent = timeout - timeSinceLastEvent;

            return timeToNextEvent > 0 ? (int) timeToNextEvent : 0;
        }

        public void UpdateLastSentHeartbeat()
        {
            LastSentHeartbeat = _sw.ElapsedMilliseconds;
        }
    }
}
