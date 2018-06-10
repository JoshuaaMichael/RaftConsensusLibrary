namespace TeamDecided.RaftConsensus.Tests.Consensus._7Nodes
{
    internal class RaftConsensusTests7NodesEncrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests7NodesEncrypted()
        {
            NumberOfNodesInTest = 7;
            NumberOfActiveNodesInTest = 7;
            UseEncryption = true;
        }
    }
}
