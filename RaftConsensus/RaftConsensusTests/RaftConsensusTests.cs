using System;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Consensus.Interfaces;
using TeamDecided.RaftConsensus.Consensus.Enums;
using System.Collections.Generic;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Common;

namespace TeamDecided.RaftConsensus.Consensus.Tests
{
    [TestFixture]
    public class RaftConsensusTest
    {
        protected const string ClusterName = "TestCluster";
        protected const string ClusterPassword = "password";
        protected const string IpToBind = "127.0.0.1";
        protected const int StartPort = 5555;
        
        IConsensus<string, string>[] _nodes;

        List<Tuple<string, string>> _entries;

        ManualResetEvent _onStartUas;

        [SetUp]
        public void BeforeTest()
        {
            RaftLogging.Instance.LogFilename = @"C:\Users\admin\Downloads\debug.log";
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.LogLevel = ERaftLogType.Trace;

            _entries = new List<Tuple<string, string>>();
            _onStartUas = new ManualResetEvent(false);
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
                    nodes[i].ManualAddPeer(nodes[j].GetNodeName(), new IPEndPoint(IPAddress.Parse(IpToBind), StartPort + j));
                }
            }
        }

        private void InformOfIPs(IConsensus<string, string> node)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if(_nodes[i].GetNodeName() == node.GetNodeName())
                {
                    continue;
                }
                node.ManualAddPeer(_nodes[i].GetNodeName(), new IPEndPoint(IPAddress.Parse(IpToBind), StartPort + i));
            }
        }

        [Test, Repeat(3)]
        public void IT_TwoNodesJoinCluster()
        {
            //This will only test 1 leader node, and 1 peers coming to join the cluster
            int maxNodes = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is where we don't do that last one
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }

        [Test]
        public void IT_TwoNodesJoinClusterSecure()
        {
            //This will only test 1 leader node, and 1 peers coming to join the cluster
            int maxNodes = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is where we don't do that last one
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }

        [Test]
        public void IT_ThreeNodesJoinCluster()
        {
            int maxNodes = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }
        
        [Test]
        public void IT_ThreeNodesMaintainCluster()
        {
            int maxNodes = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(5000);

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }
        
        [Test]
        public void IT_ManyNodesJoinCluster()
        {
            int maxNodes = 9;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            //There is a timing issue in this test
            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                if(i <= 2)
                {
                    Thread.Sleep(1500);
                }
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(3000);

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }

        [Test]
        public void IT_TwoNodesCommitEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is we go down to 2 nodes
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].IsUasRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = _nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, _entries.Count);
        }

        [Test]
        public void IT_ThreeNodeClusterCommitEntry()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].IsUasRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = _nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, _entries.Count);
        }
      
        [Test]
        public void IT_TwoNodesCommitManyEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 20;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is where we go down to 2 nodes
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].IsUasRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = _nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, _entries.Count);
        }

        [Test]
        public void IT_ThreeNodesCommitManyEntries()
        {
            int maxNodes = 3;
            int entriesToCommit = 20;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].IsUasRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = _nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit, _entries.Count);
        }

        [Test]
        public void IT_StartWithoutLeader()
        {
            int maxNodes = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < _nodes.Length; i++)
            {
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }

        //[Test]
        public void IT_ManyNodesCommitManyEntries()
        {
            int maxNodes = 9;
            int entriesToCommit = 5;

            //There is a timing issue in this test
            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
                ((RaftConsensus<string, string>)_nodes[i]).SetWaitingForJoinClusterTimeout(10000);
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(3000);

            Task[] appendTasks = new Task[entriesToCommit];

            bool foundLeader = false;
            while (!foundLeader)
            {
                for (int i = 0; i < _nodes.Length; i++)
                {
                    if (_nodes[i].IsUasRunning())
                    {
                        for (int j = 0; j < entriesToCommit; j++)
                        {
                            appendTasks[j] = _nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
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

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }

            Assert.AreEqual(entriesToCommit, _entries.Count);
        }

        [Test]
        public void IT_TwoNodesJoinClusterCommitEntryClientEventAppend()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes - 1]; //This is we go down to 2 nodes
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntryClient;
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].IsUasRunning())
                {
                    for (int j = 0; j < entriesToCommit; j++)
                    {
                        appendTasks[j] = _nodes[i].AppendEntry("Hello" + (j + 1), "World" + (j + 1));
                    }
                    break;
                }
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            Thread.Sleep(1000);

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual(entriesToCommit * (maxNodes - 1), _entries.Count);
        }

        [Test]
        public void IT_ThreeNodesJoinClusterRebuildAfterLeaderLoss()
        {
            int maxNodes = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            bool foundLeader = false;
            while (!foundLeader)
            {
                for (int i = 0; i < _nodes.Length; i++)
                {
                    if (_nodes[i].IsUasRunning())
                    {
                        _nodes[i].Dispose();
                        foundLeader = true;
                        break;
                    }
                }
            }

            Thread.Sleep(2000);

            foundLeader = false;
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].IsUasRunning())
                {
                    foundLeader = true;
                    break;
                }
            }

            Assert.IsTrue(foundLeader);

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }

        [Test]
        public void IT_ThreeNodeClusterCommitEntryDontRecommitAfterRecover()
        {
            int maxNodes = 3;
            int entriesToCommit = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntryClient;
                _nodes[i].StartUas += RaftConsensusTest_StartUAS;
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            Task[] appendTasks = new Task[entriesToCommit];

            IConsensus<string, string> leader = FindLeader(_nodes);
            for (int j = 0; j < entriesToCommit; j++)
            {
                appendTasks[j] = leader.AppendEntry("Hello" + (j + 1), "World" + (j + 1));
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            Thread.Sleep(1000);

            FindLeader(_nodes).Dispose();

            leader = FindLeader(_nodes);
            for (int j = 0; j < entriesToCommit; j++)
            {
                appendTasks[j] = leader.AppendEntry("Hello" + (j + 1 + entriesToCommit), "World" + (j + 1 + entriesToCommit));
            }

            for (int i = 0; i < appendTasks.Length; i++)
            {
                appendTasks[i].Wait();
            }

            Thread.Sleep(1000);

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }

            // check if message committed
            Assert.AreEqual((entriesToCommit * 3) + (entriesToCommit * 2), _entries.Count);
        }

        [Test]
        public void IT_ThreeNodeClusterRejoinAfterNodeFailure()
        {
            int maxNodes = 3;

            _nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, StartPort);
            InformOfIPs(_nodes);

            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[maxNodes];
            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                _nodes[i].StartUas += RaftConsensusTest_StartUAS;
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, maxNodes, true);
            }

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                joinClusterResponses[i].Wait();
                Assert.True(joinClusterResponses[i].Result == EJoinClusterResponse.Accept);
            }

            Thread.Sleep(1000); //Let's see if we can keep this thing alive for a bit

            IConsensus<string, string> disposedNode = FindLeader(_nodes);
            disposedNode.Dispose();

            FindLeader(_nodes);

            disposedNode = RaftConsensus<string, string>.RemakeDisposedNode(_nodes, disposedNode, StartPort);
            InformOfIPs(disposedNode);
            Task<EJoinClusterResponse> rejoin = disposedNode.JoinCluster(ClusterName, ClusterPassword, maxNodes, true);

            rejoin.Wait();

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].Dispose();
            }
        }

        private IConsensus<string, string> FindLeader(IConsensus<string, string>[] nodes)
        {
            while (true)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].IsUasRunning())
                    {
                        return nodes[i];
                    }
                }
            }
        }

        private void RaftConsensusTest_StartUAS(object sender, EventArgs e)
        {
            _onStartUas.Set();
        }

        private void OnNewCommitedEntry(object sender, Tuple<string, string> e)
        {
            for(int i = 0; i < _entries.Count; i++)
            {
                if(_entries[i].Item1 == e.Item1 && _entries[i].Item2 == e.Item2)
                {
                    return;
                }
            }
            _entries.Add(e);
        }

        private void OnNewCommitedEntryClient(object sender, Tuple<string, string> e)
        {
            _entries.Add(e);
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
