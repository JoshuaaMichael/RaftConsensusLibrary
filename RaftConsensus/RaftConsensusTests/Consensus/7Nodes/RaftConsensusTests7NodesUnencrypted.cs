namespace TeamDecided.RaftConsensus.Tests.Consensus._7Nodes
{
    internal class RaftConsensusTests7NodesUnencrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests7NodesUnencrypted()
        {
            NumberOfNodesInTest = 7;
            NumberOfActiveNodesInTest = 7;
            UseEncryption = false;
        }
    }
}
