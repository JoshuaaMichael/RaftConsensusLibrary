using System;
using System.Net;

namespace TeamDecided.RaftConsensus.Consensus
{
    public class RaftNodeNetworkInfo : ICloneable
    {
        public string NodeName { get; private set; }
        public IPEndPoint IpEndPoint { get; private set; }

        public RaftNodeNetworkInfo(string nodeName, IPEndPoint ipEndPoint)
        {
            NodeName = nodeName;
            IpEndPoint = ipEndPoint;
        }

        public object Clone()
        {
            return this;
        }
    }
}
