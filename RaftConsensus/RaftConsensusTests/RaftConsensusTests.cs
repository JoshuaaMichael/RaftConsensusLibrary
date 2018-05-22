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

        Mock<IUDPNetworking> mockNetwork;

        [SetUp]
        public void BeforeTest()
        {
            mockNetwork = new Mock<IUDPNetworking>(MockBehavior.Strict);
            //mockNetwork.Raise(p => p.OnMessageReceived)
            //mockNetwork.Setup(p => p.OnMessageReceived).Return("expected result");
        }

        [Test]
        public void IT_SingleNodeJoinCluster()
        {
            //This will only test 1 leader node, and 1 peer coming to join the cluster
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes - 1, START_PORT);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower = nodes[1];
            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            //Inform follower of leader IP
            follower.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));

            Task<EJoinClusterResponse> followerJoinTask = follower.JoinCluster(clusterName, clusterPassword);
            followerJoinTask.Wait();

            leader.Dispose();
            follower.Dispose();

            Assert.AreEqual(EJoinClusterResponse.ACCEPT, followerJoinTask.Result);
        }

        [Test]
        public void IT_TwoNodesJoinCluster()
        {
            //This will only test 1 leader node, and 2 peers coming to join the cluster
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower1 = nodes[1];
            IConsensus<string, string> follower2 = nodes[2];
            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            //Inform follower of leader IP
            follower1.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));
            follower2.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword);
            Task<EJoinClusterResponse> follower2JoinTask = follower2.JoinCluster(clusterName, clusterPassword);

            follower1JoinTask.Wait();
            follower2JoinTask.Wait();

            leader.Dispose();
            follower1.Dispose();
            follower2.Dispose();

            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower1JoinTask.Result);
            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower2JoinTask.Result);
        }

        [Test]
        public void IT_TwoNodesJoinClusterOneKnowsLeader()
        {
            //This will only test 1 leader node, and 2 peers coming to join the cluster.
            //Only 1 of the peers will know how to get to the leader
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower1 = nodes[1];
            IConsensus<string, string> follower2 = nodes[2];
            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            //Inform follower of leader IP
            follower1.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));
            follower2.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT + 1)); //Follower2 will connect to follower1

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword);
            follower1JoinTask.Wait();
            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower1JoinTask.Result);

            //Have to wait for follower 1 to join leader first
            Task<EJoinClusterResponse> follower2JoinTask = follower2.JoinCluster(clusterName, clusterPassword);
            follower2JoinTask.Wait();
            Assert.AreEqual(EJoinClusterResponse.ACCEPT, follower2JoinTask.Result);

            follower1.Dispose();
            follower2.Dispose();
        }

        [Test]
        public void IT_ManyNodesJoinCluster()
        {
            //This will only test 1 leader node, and 6 peers coming to join the cluster
            int maxNodes = 7;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);

            IConsensus<string, string> leader = nodes[0];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            //Inform follower of leader IP

            for(int i = 1; i < nodes.Length; i++)
            {
                nodes[i].ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));
            }

            List<Task<EJoinClusterResponse>> joinTasks = new List<Task<EJoinClusterResponse>>();
            joinTasks.Add(null); //Syncs up the indexes
            for (int i = 1; i < nodes.Length; i++)
            {
                joinTasks.Add(nodes[i].JoinCluster(clusterName, clusterPassword));
            }

            for (int i = 1; i < nodes.Length; i++)
            {
                joinTasks[i].Wait();
            }

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
        public void IT_TooManyNodesJoinCluster()
        {
            //This will only test 1 leader node, and 6 peers coming to join the cluster, and 2 extra nodes who will be rejected
            int maxNodes = 7;
            int extraNodes = 2;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes + extraNodes, START_PORT);

            IConsensus<string, string> leader = nodes[0];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            //Inform follower of leader IP

            for (int i = 1; i < nodes.Length; i++)
            {
                nodes[i].ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));
            }

            List<Task<EJoinClusterResponse>> joinTasks = new List<Task<EJoinClusterResponse>>();
            joinTasks.Add(null); //Syncs up the indexes
            for (int i = 1; i < nodes.Length; i++)
            {
                joinTasks.Add(nodes[i].JoinCluster(clusterName, clusterPassword));
            }

            for (int i = 1; i < nodes.Length; i++)
            {
                joinTasks[i].Wait();
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Dispose();
            }

            int accepts = 0;
            int fulls = 0;
            for (int i = 1; i < nodes.Length; i++)
            {
                if(joinTasks[i].Result == EJoinClusterResponse.ACCEPT)
                {
                    accepts += 1;
                }
                if (joinTasks[i].Result == EJoinClusterResponse.REJECT_CLUSTER_FULL)
                {
                    fulls += 1;
                }
            }

            Assert.AreEqual(maxNodes - 1, accepts); //Minus 1 for leader
            Assert.AreEqual(extraNodes, fulls);
        }

        [Test]
        public void IT_MiniClusterCommitEntry()
        {
            //This will only test 1 leader node, and 1 peer
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes - 1, START_PORT);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower = nodes[1];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            //Inform follower of leader IP
            follower.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));

            Task<EJoinClusterResponse> followerJoinTask = follower.JoinCluster(clusterName, clusterPassword);
            followerJoinTask.Wait();

            // request to commit a message
            Task<ERaftAppendEntryState> task = leader.AppendEntry("Hello", "World");
            task.Wait();

            leader.Dispose();
            follower.Dispose();

            // check if message committed
            Assert.AreEqual(ERaftAppendEntryState.COMMITED, task.Result);
        }

        [Test]
        public void IT_TwoNodeClusterCommitEntry()
        {
            //This will only test 1 leader node, and 2 peers
            int maxNodes = 3;

            nodes = RaftConsensus<string, string>.MakeNodesForTest(maxNodes, START_PORT);

            IConsensus<string, string> leader = nodes[0];
            IConsensus<string, string> follower1 = nodes[1];
            IConsensus<string, string> follower2 = nodes[2];

            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, maxNodes);

            //Inform follower of leader IP
            follower1.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));
            follower2.ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT));

            Task<EJoinClusterResponse> follower1JoinTask = follower1.JoinCluster(clusterName, clusterPassword);
            Task<EJoinClusterResponse> follower2JoinTask = follower2.JoinCluster(clusterName, clusterPassword);

            follower1JoinTask.Wait();
            follower2JoinTask.Wait();

            // request to commit a message
            Task<ERaftAppendEntryState> task = leader.AppendEntry("Hello", "World");
            task.Wait();

            leader.Dispose();
            follower1.Dispose();
            follower2.Dispose();

            // check if message committed
            Assert.AreEqual(ERaftAppendEntryState.COMMITED, task.Result);
        }
    }
}
