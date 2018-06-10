namespace TeamDecided.RaftConsensus.Tests.Consensus._5Nodes
{
    internal class RaftConsensusTests5NodesEncrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests5NodesEncrypted()
        {
            NumberOfNodesInTest = 5;
            NumberOfActiveNodesInTest = 5;
            UseEncryption = true;
        }
    }
}
