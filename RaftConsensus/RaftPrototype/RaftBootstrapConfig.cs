using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftPrototype
{
    public class RaftBootstrapConfig
    {
        public string clusterName;
        public string clusterPassword;
        public string leaderIP;
        public int maxNodes;
        public List<string> nodeNames = new List<string>();
        public List<string> nodeIPAddresses = new List<string>();
        public List<int> nodePorts = new List<int>();
    }
}
