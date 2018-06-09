using System.Collections.Generic;
using TeamDecided.RaftCommon;

namespace RaftPrototype
{
    public class RaftBootstrapConfig
    {
        public string clusterName;
        public string clusterPassword;
        public int maxNodes;
        public List<string> nodeNames = new List<string>();
        public List<string> nodeIPAddresses = new List<string>();
        public List<int> nodePorts = new List<int>();
        public ERaftLogType logLevel;
    }
}
