using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Consensus;
using TeamDecided.RaftConsensus.Consensus.Enums;
using TeamDecided.RaftConsensus.Consensus.Interfaces;

namespace TeamDecided.RaftConsensus.Tests.Consensus
{
    [TestFixture]
    internal abstract class BaseRaftConsensusTests
    {
        private const string LOG_FILE_NAME = @"C:\Users\Tori\Downloads\debug.log";
        protected const string ClusterName = "TestCluster";
        protected const string ClusterPassword = "password";
        protected const string IPToBind = "127.0.0.1";
        protected const int StartPort = 5555;
        protected const string DefaultNodeName = "Node";
        protected bool UseEncryption = false;
        protected int AttemptsToJoinCluster = 3;

        private IConsensus<string, string>[] _nodes;
        private List<Tuple<string, string>> _commitEntries;
        private List<int> disposedNodes;

        private ManualResetEvent _onStartUAS;
        private ManualResetEvent _onStopUAS;

        protected int NumberOfNodesInTest;
        protected int NumberOfActiveNodesInTest;
        private int _numberOfCommits;
        private const int NumberOfDefaultCommits = 3;
        private const int NumberOfManyCommits = 20;
        private const int DefaultMillisecondsToKeepAlive = 2000;

        [SetUp]
        public void BeforeTest()
        {
            RaftLogging.Instance.LogFilename = LOG_FILE_NAME;
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.LogLevel = ERaftLogType.Trace;

            _commitEntries = new List<Tuple<string, string>>();
            _onStartUAS = new ManualResetEvent(false);
            _onStopUAS = new ManualResetEvent(false);

            _numberOfCommits = -1;

            disposedNodes = new List<int>();
        }

        [Test]
        public void IT_JoinCluster()
        {
            try
            {
                MakeNodes();
                NodesJoinCluster();
            }
            finally
            {
                DisposeNodes();
            }
        }

        [Test]
        public void IT_JoinClusterAndKeepAlive()
        {
            try
            {
                MakeNodes();
                NodesJoinCluster();
                Thread.Sleep(DefaultMillisecondsToKeepAlive);
            }
            finally
            {
                DisposeNodes();
            }
        }

        [Test]
        public void IT_JoinClusterCommitEntries()
        {
            _numberOfCommits = NumberOfDefaultCommits;

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
        }

        [Test]
        public void IT_JoinClusterCommitEntriesAndKeepAlive()
        {
            _numberOfCommits = NumberOfDefaultCommits;

            try
            {
                MakeNodes();
                NodesJoinCluster();
                CommitEntries();
                Thread.Sleep(DefaultMillisecondsToKeepAlive);
            }
            finally
            {
                DisposeNodes();
            }
        }

        [Test]
        public void IT_JoinClusterCommitManyEntries()
        {
            _numberOfCommits = NumberOfManyCommits;

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
        }

        [Test]
        public void IT_JoinClusterCommitManyEntriesAndKeepAlive()
        {
            _numberOfCommits = NumberOfManyCommits;

            try
            {
                MakeNodes();
                NodesJoinCluster();
                CommitEntries();
                Thread.Sleep(DefaultMillisecondsToKeepAlive);
            }
            finally
            {
                DisposeNodes();
            }
        }

        [Test]
        public void IT_MaintainClusterAfterLeaderLoss()
        {
            if (NumberOfActiveNodesInTest < 3) //2 node clusters can't rebuild after losing 1
            {
                return;
            }
            _numberOfCommits = NumberOfDefaultCommits;

            try
            {
                MakeNodes();
                NodesJoinCluster();
                CommitEntries();
                DisposeLeader();
                FindLeader();
            }
            finally
            {
                DisposeNodes();
            }
        }

        [Test]
        public void IT_MaintainClusterAfterMaxMinorityLoss()
        {
            if (NumberOfActiveNodesInTest < 3) //2 node clusters can't rebuild after losing 1
            {
                return;
            }
            _numberOfCommits = NumberOfDefaultCommits;

            try
            {
                MakeNodes();
                NodesJoinCluster();
                CommitEntries();

                int maxPossibleLostNodes = CalculateMaxPossibleNodesCanLose();
                DisposeLeader();
                DisposeCountOfNonleaders(maxPossibleLostNodes - 1);
                FindLeader();
            }
            finally
            {
                DisposeNodes();
            }
        }

        [Test]
        public void IT_MaintainClusterAfterMajorityLoss()
        {
            _numberOfCommits = NumberOfDefaultCommits;

            try
            {
                MakeNodes();
                NodesJoinCluster();
                CommitEntries();

                int minimumMajority = CalculateMinimumMajority();
                DisposeLeader();
                DisposeCountOfNonleaders(minimumMajority - 1);

                RebuildAndRejoinDisposedNodes();

                FindLeader();
            }
            finally
            {
                DisposeNodes();
            }
        }

        private void MakeNodes()
        {
            _nodes = new IConsensus<string, string>[NumberOfNodesInTest];

            for (int i = 0; i < NumberOfNodesInTest; i++)
            {
                MakeSingleNode(i);
            }
        }

        private void MakeSingleNode(int index)
        {
            _nodes[index] = new RaftConsensus<string, string>(DefaultNodeName + (index + 1), StartPort + index);
            _nodes[index].OnStartUAS += OnStartUAS;
            _nodes[index].OnStopUAS += OnStopUAS;
            _nodes[index].OnNewCommitedEntry += OnNewCommitedEntry;

            for (int j = 0; j < _nodes.Length; j++)
            {
                if (index == j)
                {
                    continue;
                }
                _nodes[index].ManualAddPeer(DefaultNodeName + (j + 1), new IPEndPoint(IPAddress.Parse(IPToBind), StartPort + j));
            }
        }

        private void NodesJoinCluster()
        {
            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[NumberOfActiveNodesInTest];

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                if (NumberOfNodesInTest > 3 && i < 3)
                {
                    Thread.Sleep(1500);
                }
                joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, NumberOfNodesInTest, AttemptsToJoinCluster, UseEncryption);
            }

            foreach (Task<EJoinClusterResponse> node in joinClusterResponses)
            {
                node.Wait();
                Assert.AreEqual(EJoinClusterResponse.Accept, node.Result);
            }
        }

        private void JoinSingleNodeIntoCluster(int index)
        {
            Task<EJoinClusterResponse> joinClusterResponses = _nodes[index].JoinCluster(ClusterName, ClusterPassword,
                NumberOfNodesInTest, AttemptsToJoinCluster, UseEncryption);
            joinClusterResponses.Wait();
        }

        private void CommitEntries()
        {
            if (_numberOfCommits == -1)
            {
                throw new ArgumentException("Must set the number of commits");
            }

            Task[] appendTasks = new Task[_numberOfCommits];

            IConsensus<string, string> leader;

            for (int j = 0; j < _numberOfCommits; j++)
            {
                leader = FindLeader();
                appendTasks[j] = leader.AppendEntry("Hello" + (j + 1), "World" + (j + 1));
            }

            foreach (Task task in appendTasks)
            {
                task.Wait();
            }

            leader = FindLeader();

            Assert.AreEqual(_numberOfCommits, leader.NumberOfCommits());
        }

        private IConsensus<string, string> FindLeader()
        {
            return _nodes[GetLeaderIndex()];
        }

        private int GetLeaderIndex()
        {
            while (true)
            {
                for(int i = 0; i < _nodes.Length; i++)
                {
                    if (_nodes[i].IsUASRunning())
                    {
                        return i;
                    }
                }
            }
        }

        private void DisposeNodes()
        {
            foreach (IConsensus<string, string> node in _nodes)
            {
                node.Dispose();
            }
        }

        private int CalculateMaxPossibleNodesCanLose()
        {
            return (NumberOfNodesInTest > 2) ? (NumberOfNodesInTest / 2) : 0;
        }

        private int CalculateMinimumMajority()
        {
            return CalculateMaxPossibleNodesCanLose() + 1;
        }

        private void DisposeLeader()
        {
            int leaderIndex = GetLeaderIndex();
            _nodes[leaderIndex].Dispose();
            disposedNodes.Add(leaderIndex);
        }

        private void DisposeCountOfNonleaders(int count)
        {
            int disposedCount = 0;
            for (int i = 0; i < count || disposedCount > count; i++)
            {
                if (!_nodes[i].IsUASRunning()) continue;
                _nodes[i].Dispose();
                disposedCount += 1;
                disposedNodes.Add(i);
            }
        }

        private void RebuildAndRejoinDisposedNodes()
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (disposedNodes.Contains(i))
                {
                    MakeSingleNode(i);
                    JoinSingleNodeIntoCluster(i);
                }
            }
        }

        private void OnStartUAS(object sender, EventArgs e)
        {
            _onStartUAS.Set();
        }

        private void OnStopUAS(object sender, EStopUasReason e)
        {
            _onStopUAS.Set();
        }

        private void OnNewCommitedEntry(object sender, Tuple<string, string> e)
        {
            _commitEntries.Add(e);
        }
    }
}
