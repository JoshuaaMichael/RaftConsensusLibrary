using System;
using System.Net;

namespace TeamDecided.RaftConsensus.Consensus
{
    public class RaftNodeNetworkInfo : ICloneable
    {
        public string NodeName { get; private set; }
        public IPEndPoint IPEndPoint { get; private set; }

        public RaftNodeNetworkInfo(string nodeName, IPEndPoint ipEndPoint)
        {
            NodeName = nodeName;
            IPEndPoint = ipEndPoint;
        }

        public object Clone()
        {
            return this;
        }
    }
}
