namespace TeamDecided.RaftConsensus.Tests.Consensus._3Nodes
{
    internal class RaftConsensusTests3NodesEncrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests3NodesEncrypted()
        {
            NumberOfNodesInTest = 3;
            NumberOfActiveNodesInTest = 3;
            UseEncryption = true;
        }
    }
}
