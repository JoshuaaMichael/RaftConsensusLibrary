namespace TeamDecided.RaftConsensus.Tests.Consensus._5Nodes
{
    internal class RaftConsensusTests5PersistentStorage : BaseRaftConsensusPersistantStorageTests
    {
        public RaftConsensusTests5PersistentStorage()
        {
            NumberOfNodesInTest = 5;
            NumberOfActiveNodesInTest = 5;
            UsePersistentStorage = true;
        }
    }
}
