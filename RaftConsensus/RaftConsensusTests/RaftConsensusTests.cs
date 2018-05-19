using System;
using System.Net;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Interfaces;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftNetworking.Interfaces;

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
        protected const string IP_TO_BIND = "127.0.0.1";
        protected const int START_PORT = 5555;
        
        IConsensus<string, string>[] nodes;

        Mock<IUDPNetworking> mockNetwork;

        [SetUp]
        public void BeforeTest()
        {
            mockNetwork = new Mock<IUDPNetworking>(MockBehavior.Strict);
//            mockNetwork.Raise(p=> p.OnMessageReceived)
//            mockNetwork.Setup(p=> p.OnMessageReceived ).Return("expected result"); 
        }

        [Test]
        public void IT_HaveMessageReachConsensus()
        {
            string clusterName = "TestCluster";
            string clusterPassword = "password";

            nodes = RaftConsensus<string, string>.MakeNodesForTest(3, START_PORT);

            IConsensus<string, string> leader = nodes[0];
            // create a cluster
            leader.CreateCluster(clusterName, clusterPassword, nodes.Length);

            // inform nodes of each other
            for(int i = 0; i < nodes.Length; i++)
            {
                for (int j = 0; j < nodes.Length; j++)
                {
                    if(i == j)
                    {
                        continue;
                    }
                    nodes[i].ManualAddPeer(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT + j));
                }
            }

            // joining cluster
            for (int i = 1; i < nodes.Length; i++)
            {
                nodes[i].JoinCluster(clusterName, clusterPassword);
            }

            // request to commit a message
            Task<ERaftAppendEntryState> task = nodes[0].AppendEntry("Hello", "World");
            task.Wait();

            // check if message committed
            Assert.Equals(task.Result, ERaftAppendEntryState.COMMITED);
        }
    }
}
