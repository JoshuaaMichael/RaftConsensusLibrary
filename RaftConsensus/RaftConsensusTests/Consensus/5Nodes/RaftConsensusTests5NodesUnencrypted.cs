namespace TeamDecided.RaftConsensus.Tests.Consensus._5Nodes
{
    internal class RaftConsensusTests5NodesUnencrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests5NodesUnencrypted()
        {
            NumberOfNodesInTest = 5;
            NumberOfActiveNodesInTest = 5;
            UseEncryption = false;
        }
    }
}
