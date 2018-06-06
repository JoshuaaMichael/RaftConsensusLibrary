using System;
using System.Net;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Interfaces;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftNetworking.Interfaces;
using System.Collections.Generic;
using System.Threading;
using TeamDecided.RaftCommon.Logging;
using System.IO;

/*  TODO:
 *  
 *  - 
 *  - 
 */

/* Tests TODO:
* Have message reach consensus
* Have stream of message reach consensus
* Test all public members work
* Confirm all state changes are occuring
* Find a way to drop messages and ensure it recovers
* Blow away multiple
* Test all the edge cases of Raft we can think of
* Test with encryption, and without it
* Test trying to enter a cluster, but using the wrong cluster name
* Show cluster built off of different knowledges of network, want to see someone pointed to the leader
* Show cluster rebuild a node after it's left and come back
*/

namespace TeamDecided.RaftConsensus.Tests
{
    [TestFixture]
    public class RaftConsensusTest
    {
        protected const string clusterName = "TestCluster";
        protected const string clusterPassword = "password";
        protected const string IP_TO_BIND = "127.0.0.1";
        protected const int START_PORT = 5555;
        
        IConsensus<string, string>[] nodes;

        List<Tuple<string, string>> entries;

        [SetUp]
        public void BeforeTest()
        {
            RaftLogging.Instance.OverwriteLoggingFile(@"C:\Users\admin\Downloads\debug.log");
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.SetDoInfo(true);
            RaftLogging.Instance.SetDoDebug(true);

            entries = new List<Tuple<string, string>>();
        }

        private void InformOfIPs(params IConsensus<string, string>[] nodes)
        {
            for(int i = 0; i < nodes.Length; i++)
            {
                for (int j = 0; j < nodes.Length; j++)
                {
                    if(i == j)
                    {
                        continue;
                    }
                    nodes[i].ManualAddPeer(nodes[j].GetNodeName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT + j));
                }
            }
        }

        [Test, Repeat(3)]
        public void IT_TwoNodesJoinCluster()
        {
            //This will only test 1 leader node, and 1 peers coming to join the cluster
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is where we don't do that last one
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }
        }
        
        [Test]
        public void IT_ThreeNodesJoinCluster()
        {
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }
        }
        
        [Test]
        public void IT_ThreeNodesMaintainCluster()
        {
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(5000);

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }
        }
        
        [Test]
        public void IT_ManyNodesJoinCluster()
        {
            int maxNodes = 9;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                if(i == 2)
                {
                    Thread.Sleep(1500);
                }
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(3000);

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }
        }

        [Test]
        public void IT_TwoNodesCommitEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is we go down to 2 nodes
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, entries.Count);
        }

        [Test]
        public void IT_ThreeNodeClusterCommitEntry()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, entries.Count);
        }
      
        [Test]
        public void IT_TwoNodesCommitManyEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 20;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is where we go down to 2 nodes
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, entries.Count);
        }

        [Test]
        public void IT_ThreeNodesCommitManyEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 20;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, entries.Count);
        }

        [Test]
        public void IT_StartWithoutLeader()
        {
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < nodes.Length; i++)
            {
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }
        }

        [Test]
        public void IT_ManyNodesCommitManyEntries()
        {
            int maxNodes = 9;
            int entriesToCommit = 5;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                ((RaftConsensus<string, string>)nodes[i]).SetWaitingForJoinClusterTimeout(10000);
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(3000);

            Task[] appendTasks = new Task[entriesToCommit];

            bool foundLeader = false;
            while (!foundLeader)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].IsUASRunning())
                    {
                        for (int j = 0; j < entriesToCommit; j++)
                        {
                            appendTasks[j] = nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                        }
                        foundLeader = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            Assert.AreEqual(entriesToCommit, entries.Count);
        }

        [Test]
        public void IT_TwoNodesJoinClusterCommitEntryCLientEventAppend()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is we go down to 2 nodes
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                nodes[i].OnNewCommitedEntry += OnNewCommitedEntryClient;
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            Thread.Sleep(1000);

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit * (maxNodes - 1), entries.Count);
        }

        [Test]
        public void IT_ThreeNodesJoinClusterRebuildAfterLeaderLoss()
        {
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.ACCEPT);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            bool foundLeader = false;
            while (!foundLeader)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].IsUASRunning())
                    {
                        nodes[i].Dispose();
                        foundLeader = true;
                        break;
                    }
                }
            }

            Thread.Sleep(2000);

            foundLeader = false;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    foundLeader = true;
                    break;
                }
            }

            Assert.IsTrue(foundLeader);

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }
        }

        private void OnNewCommitedEntry(object sender, Tuple<string, string> e)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                if(entries[i].Item1 == e.Item1 && entries[i].Item2 == e.Item2)
                {
                    return;
                }
            }
            entries.Add(e);
        }

        private void OnNewCommitedEntryClient(object sender, Tuple<string, string> e)
        {
            entries.Add(e);
        }

        public void IT_LeaderSendsAppendEntriesMessage_IUDPNetworkingReceivesAppendMessageToSend() { }
        public void IT_FollwerReceivesAppendMessage_FollwerAppendsLogButDoesntCommit() { }
        public void IT_FollowerSendsSuccessMessageToFollower_IUDPNetworkingReceivesMessageToSend() { }
        public void IT_LeaderSendsUpdatedCommitMessageToFollower_IUDPNetworkingReceivesUpdatedAppendMessageToSend() { }
        public void IT_FollowerSendUpdatedAppendMessageWithNewCommit_IUDPNetworkingReceivesUpdatedAppendMessageToSend() { }

        public void IT_FollowerElectionStartsElectionTimeOutRandomizedDuration_FollowerElectionTimeOutDurationIsRandom() { }
        public void IT_FollowerElectionTimeOutEnterCandidateState_FollowerEntersCandidateState() { }
        public void IT_CandidateSendsRequestVoteMessage_IUDPNetworkingReceivesRequestVoteMessage() { }
        public void IT_FollowerRepliesRequestVoteFromNewerTermCandidate_SuccessMessageSent() { }
        public void IT_FollowerRepliesRequestVoteFromOlderTermCandidate_FailMessageSent() { }
        public void IT_FollowerRepliesRequestVoteFromNewerTermCandidateWithOlderCommitIndex_FailMessageSent() { }
        //still reading Raft doc to ensure good test coverage to meet protocol
    }
}
