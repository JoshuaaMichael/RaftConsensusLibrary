using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Consensus.Interfaces;
using TeamDecided.RaftConsensus.Tests.Consensus;

namespace TeamDecided.RaftConsensus.Tests.Consensus
{
    internal class BaseRaftConsensusPersistantStorageTests : BaseRaftConsensusTests
    {
        [Test]
        public void IT_ContinueFromPersistentStorage()
        {
            _numberOfCommits = NumberOfDefaultCommits;

            DeleteFiles();

            //Add some commits
            try
            {
                MakeNodes();
                NodesJoinCluster();
                CommitEntries();
            }
            finally
            {
                DisposeNodes();
            }

            try
            {
                MakeNodes();
                NodesJoinCluster();
                Assert.IsTrue(ConfirmCommitedEntries(_numberOfCommits));
                CommitEntries();
                Assert.IsTrue(ConfirmCommitedEntries(_numberOfCommits * 2));
            }
            finally
            {
                DisposeNodes();
            }
        }

        private void DeleteFiles()
        {
            for (int i = 1; i <= NumberOfActiveNodesInTest; i++)
            {
                if (File.Exists(string.Format(StorageFilename, i)))
                {
                    File.Delete(string.Format(StorageFilename, i));
                }
            }
        }

        public bool ConfirmCommitedEntries(int upToNumber)
        {
            bool latest = false;
            foreach (IConsensus<string, string> node in _nodes)
            {
                if (node.DoesEntryValueExist("Hello" + upToNumber))
                {
                    latest = true;
                }
            }

            return latest;
        }

        //Won't continue from wrong cluster name
    }
}
