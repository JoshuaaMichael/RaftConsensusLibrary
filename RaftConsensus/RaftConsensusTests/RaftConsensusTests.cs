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

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower1 = nodes[1];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword, maxNodes);

            follower1JoinTask.Wait();

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for(int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower1JoinTask.Result);
        }

        [Test]
        public void IT_ThreeNodesJoinCluster()
        {
            //This will only test 1 leader node, and 2 peers coming to join the cluster
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower1 = nodes[1];
            IConsensus<string, string> follower2 = nodes[2];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword, maxNodes);
            Task<EJoinClusterResponse> follower2JoinTask = follower2.JoinCluster(clusterName, clusterPassword, maxNodes);

            follower1JoinTask.Wait();
            follower2JoinTask.Wait();

            Thread.Sleep(500);

            leader.Dispose();
            follower1.Dispose();
            follower2.Dispose();

            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower1JoinTask.Result);
            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower2JoinTask.Result);
        }

        [Test]
        public void IT_ThreeNodesMaintainCluster()
        {
            //This will only test 1 leader node, and 2 peers coming to join the cluster
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower1 = nodes[1];
            IConsensus<string, string> follower2 = nodes[2];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword, maxNodes);
            Task<EJoinClusterResponse> follower2JoinTask = follower2.JoinCluster(clusterName, clusterPassword, maxNodes);

            follower1JoinTask.Wait();
            follower2JoinTask.Wait();

            Thread.Sleep(1000);

            leader.Dispose();
            follower1.Dispose();
            follower2.Dispose();

            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower1JoinTask.Result);
            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower2JoinTask.Result);
        }

        [Test]
        public void IT_ManyNodesJoinCluster()
        {
            //This will only test 1 leader node, and 6 peers coming to join the cluster
            int maxNodes = 7;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            IConsensus<string, string> leader = nodes[0];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            List<Task<EJoinClusterResponse>> joinTasks = new List<Task<EJoinClusterResponse>>
            {
                null //Syncs up the indexes
            };
            for (int i = 1; i < nodes.Length; i++)
            {
                joinTasks.Add(nodes[i].JoinCluster(clusterName, clusterPassword, maxNodes));
            }

            for (int i = 1; i < nodes.Length; i++)
            {
                joinTasks[i].Wait();
            }

            Thread.Sleep(1000);

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            for (int i = 1; i < nodes.Length; i++)
            {
                Assert.AreEqual(EJoinClusterResponse.ACCEPT, joinTasks[i].Result);
            }
        }

        [Test]
        public void IT_ThreeNodeClusterCommitEntry()
        {
            //This will only test 1 leader node, and 2 peers
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower1 = nodes[1];
            IConsensus<string, string> follower2 = nodes[2];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword, maxNodes);
            Task<EJoinClusterResponse> follower2JoinTask = follower2.JoinCluster(clusterName, clusterPassword, maxNodes);

            follower1JoinTask.Wait();
            follower2JoinTask.Wait();

            // request to commit a message
            Task<ERaftAppendEntryState> task1 = leader.AppendEntry("Hello1", "World1");
            task1.Wait();

            // request to commit a message
            Task<ERaftAppendEntryState> task2 = leader.AppendEntry("Hello2", "World2");
            task2.Wait();

            leader.Dispose();
            follower1.Dispose();
            follower2.Dispose();

            // check if message committed
            Assert.AreEqual(ERaftAppendEntryState.COMMITED, task1.Result);
            Assert.AreEqual(ERaftAppendEntryState.COMMITED, task2.Result);
        }

        [Test]
        public void IT_TwoNodesCommitEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            RaftConsensus<string, string> leader = (RaftConsensus<string, string>)nodes[0];
            RaftConsensus<string, string> follower1 = (RaftConsensus<string, string>)nodes[1];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            leader.OnNewCommitedEntry += OnNewCommitedEntry;
            follower1.OnNewCommitedEntry += OnNewCommitedEntry;

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword, maxNodes);
            follower1JoinTask.Wait();

            Task[] appendTasks = new Task[entriesToCommit];

            for(int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = leader.AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                }
            }

            for(int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            leader.Dispose();
            follower1.Dispose();

            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower1JoinTask.Result);

            Assert.AreEqual(entriesToCommit, entries.Count);
        }

        public void IT_TwoNodesCommitManyEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 20;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);
            InformOfIPs(nodes);

            RaftConsensus<string, string> leader = (RaftConsensus<string, string>)nodes[0];
            RaftConsensus<string, string> follower1 = (RaftConsensus<string, string>)nodes[1];

            leader.SetHeartbeatInterval(50);
            follower1.SetHeartbeatInterval(50);

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            leader.OnNewCommitedEntry += OnNewCommitedEntry;
            follower1.OnNewCommitedEntry += OnNewCommitedEntry;

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword, maxNodes);
            follower1JoinTask.Wait();

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsUASRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = leader.AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            leader.Dispose();
            follower1.Dispose();

            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower1JoinTask.Result);

            Assert.AreEqual(entriesToCommit, entries.Count);
        }

        private void OnNewCommitedEntry(object sender, Tuple<string, string> e)
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
