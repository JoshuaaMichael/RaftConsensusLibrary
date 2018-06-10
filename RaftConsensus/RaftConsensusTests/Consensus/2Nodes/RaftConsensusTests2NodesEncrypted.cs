namespace TeamDecided.RaftConsensus.Tests.Consensus._2Nodes
{
    internal class RaftConsensusTests2NodesEncrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests2NodesEncrypted()
        {
            NumberOfNodesInTest = 3;
            NumberOfActiveNodesInTest = 2;
            UseEncryption = true;
        }
    }
}
