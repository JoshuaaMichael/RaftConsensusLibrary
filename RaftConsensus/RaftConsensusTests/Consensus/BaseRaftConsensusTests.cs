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
        protected const string ClusterName = "TestCluster";
        protected const string ClusterPassword = "password";
        protected const string IPToBind = "127.0.0.1";
        protected const int StartPort = 5555;
        protected const string DefaultNodeName = "Node";
        protected bool UseEncryption = false;
        protected int AttemptsToJoinCluster = 3;

        private IConsensus<string, string>[] _nodes;
        private List<Tuple<string, string>> _commitEntries;

        private ManualResetEvent _onStartUAS;
        private ManualResetEvent _onStopUAS;

        protected int NumberOfNodesInTest;
        protected int NumberOfActiveNodesInTest;
        private int _numberOfCommits;
        private const int DefaultMillisecondsToKeepAlive = 2000;

        [SetUp]
        public void BeforeTest()
        {
            RaftLogging.Instance.LogFilename = @"C:\Users\admin\Downloads\debug.log";
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.LogLevel = ERaftLogType.Trace;

            _commitEntries = new List<Tuple<string, string>>();
            _onStartUAS = new ManualResetEvent(false);
            _onStopUAS = new ManualResetEvent(false);

            _numberOfCommits = -1;
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
            _numberOfCommits = 3;

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
            _numberOfCommits = 3;

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

        //Commit many entries

        //Rebuild a node after leader loss

        //Maintain when lose max minority

        //Rebuild after complete cluster failure

        private void MakeNodes()
        {
            _nodes = new IConsensus<string, string>[NumberOfNodesInTest];

            for (int i = 0; i < NumberOfNodesInTest; i++)
            {
                _nodes[i] = new RaftConsensus<string, string>(DefaultNodeName + (i + 1), StartPort + i);
                _nodes[i].OnStartUAS += OnStartUAS;
                _nodes[i].OnStopUAS += OnStopUAS;
                _nodes[i].OnNewCommitedEntry += OnNewCommitedEntry;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                for (int j = 0; j < _nodes.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    _nodes[i].ManualAddPeer(_nodes[j].GetNodeName(), new IPEndPoint(IPAddress.Parse(IPToBind), StartPort + j));
                }
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

        private void CommitEntries()
        {
            if (_numberOfCommits == -1)
            {
                throw new ArgumentException("Must set the number of commits");
            }

            Task[] appendTasks = new Task[_numberOfCommits];

            IConsensus<string, string> leader = FindLeader();

            for (int j = 0; j < _numberOfCommits; j++)
            {
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
            while (true)
            {
                foreach (IConsensus<string, string> node in _nodes)
                {
                    if (node.IsUASRunning())
                    {
                        return node;
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
