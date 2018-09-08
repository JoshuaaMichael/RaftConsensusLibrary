namespace TeamDecided.RaftConsensus.Tests.Consensus._3Nodes
{
    internal class RaftConsensusTests3PersistentStorage : BaseRaftConsensusPersistantStorageTests
    {
        public RaftConsensusTests3PersistentStorage()
        {
            NumberOfNodesInTest = 3;
            NumberOfActiveNodesInTest = 3;
            UsePersistentStorage = true;
        }
    }
}
