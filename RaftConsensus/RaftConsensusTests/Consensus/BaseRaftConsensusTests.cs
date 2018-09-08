using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        protected bool UsePersistentStorage = false;
        protected int AttemptsToJoinCluster = 3;
        protected string StorageFilename = TestContext.CurrentContext.TestDirectory + @"\persistentStorage{0}.db";

        protected IConsensus<string, string>[] _nodes;
        protected List<Tuple<string, string>> _commitEntries;
        protected List<int> disposedNodes;

        protected ManualResetEvent _onStartUAS;
        protected ManualResetEvent _onStopUAS;

        protected int NumberOfNodesInTest;
        protected int NumberOfActiveNodesInTest;
        protected int _numberOfCommits;
        protected const int NumberOfDefaultCommits = 3;
        protected const int NumberOfManyCommits = 20;
        protected const int DefaultMillisecondsToKeepAlive = 2000;
        protected const int NetworkDelay = 15;

        [OneTimeSetUp]
        public void Setup()
        {
            RaftLogging.Instance.LogLevel = ERaftLogType.Trace;
            RaftLogging.Instance.WriteToNamedPipe = true;
            RaftLogging.Instance.NamedPipeName = "RaftConsensus0";
            RaftLogging.Instance.NamedPipeRequestNewFile();

            string className = TestContext.CurrentContext.Test.FullName;
            className = className.Substring(0, className.LastIndexOf(".", StringComparison.Ordinal));

            string classNameMessage = $"Testing current classname: {className}";

            RaftLogging.Instance.Log(ERaftLogType.Info, classNameMessage);
            RaftLogging.Instance.NamedPipeWriteToConsole(classNameMessage);
        }

        [SetUp]
        public void BeforeTest()
        {
            string testNameMessage = $"Running test: {TestContext.CurrentContext.Test.MethodName}";
            RaftLogging.Instance.Log(ERaftLogType.Info, testNameMessage);
            RaftLogging.Instance.NamedPipeWriteToConsole(testNameMessage);

            _commitEntries = new List<Tuple<string, string>>();
            _onStartUAS = new ManualResetEvent(false);
            _onStopUAS = new ManualResetEvent(false);

            _numberOfCommits = -1;

            disposedNodes = new List<int>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            RaftLogging.Instance.Dispose();
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

        public void IT_()
        {
            const int targetCommitPerSecond = 60;
            const int numberOfSecondsToTestOver = 30;

            _numberOfCommits = targetCommitPerSecond * numberOfSecondsToTestOver;

            Stopwatch sw = new Stopwatch();
            
            try
            {
                MakeNodes(true);
                NodesJoinCluster();

                sw.Start();
                CommitEntries();
                sw.Stop();

                Assert.Less(sw.Elapsed.TotalSeconds, numberOfSecondsToTestOver);
            }
            finally
            {
                DisposeNodes();
            }
        }

        protected void MakeNodes(bool useSlowNodes = false)
        {
            _nodes = new IConsensus<string, string>[NumberOfNodesInTest];

            for (int i = 0; i < NumberOfNodesInTest; i++)
            {
                MakeSingleNode(i, useSlowNodes);
            }
        }

        protected void MakeSingleNode(int index, bool useSlowNode = false)
        {
            if (useSlowNode)
            {
                _nodes[index] = new RaftConsensusDelayed<string, string>(DefaultNodeName + (index + 1), StartPort + index, NetworkDelay);
            }
            else
            {
                _nodes[index] = new RaftConsensus<string, string>(DefaultNodeName + (index + 1), StartPort + index);
            }

            _nodes[index].OnStartUAS += OnStartUAS;
            _nodes[index].OnStopUAS += OnStopUAS;
            _nodes[index].OnNewCommitedEntry += OnNewCommitedEntry;
            _nodes[index].EnablePersistentStorage(string.Format(StorageFilename, index + 1));

            for (int j = 0; j < _nodes.Length; j++)
            {
                if (index == j)
                {
                    continue;
                }
                _nodes[index].ManualAddPeer(DefaultNodeName + (j + 1), new IPEndPoint(IPAddress.Parse(IPToBind), StartPort + j));
            }
        }

        protected void NodesJoinCluster()
        {
            Task<EJoinClusterResponse>[] joinClusterResponses = new Task<EJoinClusterResponse>[NumberOfActiveNodesInTest];

            for (int i = 0; i < joinClusterResponses.Length; i++)
            {
                if (UseEncryption)
                {
                    joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, ClusterPassword, NumberOfNodesInTest, AttemptsToJoinCluster);
                }
                else
                {
                    joinClusterResponses[i] = _nodes[i].JoinCluster(ClusterName, NumberOfNodesInTest, AttemptsToJoinCluster);
                }
            }

            foreach (Task<EJoinClusterResponse> node in joinClusterResponses)
            {
                node.Wait();
                Assert.AreEqual(EJoinClusterResponse.Accept, node.Result);
            }
        }

        protected void JoinSingleNodeIntoCluster(int index)
        {
            Task<EJoinClusterResponse> joinClusterResponses = UseEncryption ? _nodes[index].JoinCluster(ClusterName, ClusterPassword, NumberOfNodesInTest, AttemptsToJoinCluster) : _nodes[index].JoinCluster(ClusterName, NumberOfNodesInTest, AttemptsToJoinCluster);
            joinClusterResponses.Wait();
        }

        protected void CommitEntries()
        {
            if (_numberOfCommits == -1)
            {
                throw new ArgumentException("Must set the number of commits");
            }

            int simultaneousTasksCount = Math.Min(_numberOfCommits, 5);
            List<Task<ERaftAppendEntryState>> simultanenousTasks = new List<Task<ERaftAppendEntryState>>();

            List<int> taskStoredValue = new List<int>();

            for (int i = 0; i < simultaneousTasksCount; i++)
            {
                simultanenousTasks.Add(FindLeader().AppendEntry("Hello" + i, "World" + i));
                taskStoredValue.Add(i);
            }

            while (simultanenousTasks.Count > 0)
            {
                int index = Task.WaitAny(simultanenousTasks.ToArray());

                if (simultanenousTasks[index].Result == ERaftAppendEntryState.Commited)
                {
                    int max = taskStoredValue.Max();

                    if (max == _numberOfCommits - 1)
                    {
                        simultanenousTasks.RemoveAt(index);
                        taskStoredValue.RemoveAt(index);
                    }
                    else
                    {
                        simultanenousTasks[index] = FindLeader().AppendEntry("Hello" + (max + 1), "World" + (max + 1));
                        taskStoredValue[index] = max + 1;
                    }
                }
                else
                {
                    simultanenousTasks[index] = FindLeader().AppendEntry("Hello" + taskStoredValue[index], "World" + taskStoredValue[index]);
                }
            }
        }

        protected IConsensus<string, string> FindLeader()
        {
            return _nodes[GetLeaderIndex()];
        }

        protected int GetLeaderIndex()
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

        protected void DisposeNodes()
        {
            foreach (IConsensus<string, string> node in _nodes)
            {
                node?.Dispose();
            }
        }

        protected int CalculateMaxPossibleNodesCanLose()
        {
            return (NumberOfNodesInTest > 2) ? (NumberOfNodesInTest / 2) : 0;
        }

        protected int CalculateMinimumMajority()
        {
            return CalculateMaxPossibleNodesCanLose() + 1;
        }

        protected void DisposeLeader()
        {
            int leaderIndex = GetLeaderIndex();
            _nodes[leaderIndex].Dispose();
            disposedNodes.Add(leaderIndex);
        }

        protected void DisposeCountOfNonleaders(int count)
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

        protected void RebuildAndRejoinDisposedNodes()
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

        protected void OnStartUAS(object sender, EventArgs e)
        {
            _onStartUAS.Set();
        }

        protected void OnStopUAS(object sender, EStopUasReason e)
        {
            _onStopUAS.Set();
        }

        protected void OnNewCommitedEntry(object sender, Tuple<string, string> e)
        {
            _commitEntries.Add(e);
        }
    }
}
