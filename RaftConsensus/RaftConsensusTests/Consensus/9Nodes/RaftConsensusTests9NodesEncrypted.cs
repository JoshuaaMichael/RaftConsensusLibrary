namespace TeamDecided.RaftConsensus.Tests.Consensus._9Nodes
{
    internal class RaftConsensusTests9NodesEncrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests9NodesEncrypted()
        {
            NumberOfNodesInTest = 9;
            NumberOfActiveNodesInTest = 9;
            UseEncryption = true;
        }
    }
}
