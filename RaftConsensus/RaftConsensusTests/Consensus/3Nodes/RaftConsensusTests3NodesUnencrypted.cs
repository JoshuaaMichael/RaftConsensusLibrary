namespace TeamDecided.RaftConsensus.Tests.Consensus._3Nodes
{
    internal class RaftConsensusTests3NodesUnencrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests3NodesUnencrypted()
        {
            NumberOfNodesInTest = 3;
            NumberOfActiveNodesInTest = 3;
            UseEncryption = false;
        }
    }
}
