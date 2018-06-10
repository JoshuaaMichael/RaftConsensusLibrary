namespace TeamDecided.RaftConsensus.Tests.Consensus._9Nodes
{
    internal class RaftConsensusTests9NodesUnencrypted : BaseRaftConsensusTests
    {
        public RaftConsensusTests9NodesUnencrypted()
        {
            NumberOfNodesInTest = 9;
            NumberOfActiveNodesInTest = 9;
            UseEncryption = false;
        }
    }
}
