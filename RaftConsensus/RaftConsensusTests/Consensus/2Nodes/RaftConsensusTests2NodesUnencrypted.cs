namespace TeamDecided.RaftConsensus.Tests.Consensus._2Nodes
{
    internal class RaftConsensusTests2NodesUnencrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests2NodesUnencrypted()
        {
            NumberOfNodesInTest = 3;
            NumberOfActiveNodesInTest = 2;
            UseEncryption = false;
        }
    }
}
