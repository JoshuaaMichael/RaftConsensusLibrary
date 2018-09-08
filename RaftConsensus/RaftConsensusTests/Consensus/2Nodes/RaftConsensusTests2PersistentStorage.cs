namespace TeamDecided.RaftConsensus.Tests.Consensus._2Nodes
{
    internal class RaftConsensusTests2PersistentStorage : BaseRaftConsensusPersistantStorageTests
    {
        public RaftConsensusTests2PersistentStorage()
        {
            NumberOfNodesInTest = 3;
            NumberOfActiveNodesInTest = 2;
            UsePersistentStorage = true;
        }
    }
}
