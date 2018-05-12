using System;

namespace TeamDecided.RaftConsensus
{
    public class NodeInfo
    {
        public string NodeName { get; private set; }
        public int NextIndex { get; set; }
        public int MatchIndex { get; set; }
        public bool VoteGranted { get; set; }
        public DateTime LastReceived { get; private set; }
        public DateTime RPCDue { get; set; }

        public NodeInfo(string nodeName)
        {
            this.NodeName = nodeName;
            LastReceived = DateTime.Now;
        }

        public void UpdateLastReceived()
        {
            LastReceived = DateTime.Now;
        }
    }
}
