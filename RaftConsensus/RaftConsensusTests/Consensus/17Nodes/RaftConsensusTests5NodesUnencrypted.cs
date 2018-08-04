
namespace TeamDecided.RaftConsensus.Tests.Consensus._17Nodes
{
    internal class RaftConsensusTests17NodesUnencrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests17NodesUnencrypted()
        {
            NumberOfNodesInTest = 17;
            NumberOfActiveNodesInTest = 17;
            UseEncryption = false;
        }
    }
}
