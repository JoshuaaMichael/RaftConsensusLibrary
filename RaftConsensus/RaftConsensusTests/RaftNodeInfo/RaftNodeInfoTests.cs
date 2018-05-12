using System;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftConsensus;

namespace TeamDecided.RaftConsensus.Tests.RaftNodeInfo
{
    [TestFixture]
    public class RaftNodeInfoTests
    {
        NodeInfo node1;

        string nodeName;
        int nextIndex;
        int matchIndex;
        bool voteGranted;
        //DateTime lastReceived;
        //DateTime rpcDue;

        [SetUp]
        public void BeforeTest()
        {
            nodeName = Guid.NewGuid().ToString();
            node1 = new NodeInfo(nodeName);
        }

        [Test]
        public void UT_GetNodeName_ReturnsNodeName()
        {
            Assert.AreEqual(nodeName, node1.NodeName);
        }

        [Test]
        public void UT_GetNodeIndex_ReturnIndex()
        {
            Assert.AreEqual(nextIndex, node1.NextIndex);
        }

        [Test]
        public void UT_SetNodeIndex_ReturnIndexMatch()
        {
            node1.NextIndex = ++nextIndex;
            Assert.AreEqual(nextIndex, node1.NextIndex);
        }

        [Test]
        public void UT_GetMatchIndex_ReturnMatchIndex()
        {
            Assert.AreEqual(matchIndex, node1.MatchIndex);
        }

        [Test]
        public void UT_SetMatchIndex_ReturnMatchIndexMatch()
        {
            int match = 2;

            node1.MatchIndex = match;
            Assert.AreEqual(matchIndex, node1.MatchIndex);
        }

        [Test]
        public void UT_GetVoteGranted_ReturnVote()
        {
            Assert.AreEqual(voteGranted, node1.VoteGranted);
        }

        [Test]
        public void UT_SetVoteGranted_ReturnVoteMatch()
        {
            bool vote = true;
            node1.VoteGranted = vote;
            Assert.IsTrue(node1.VoteGranted);
        }

        [Test]
        public void UT_GetLastReceived_ReturnLastReceived()
        {
            Assert.Fail();
            //Assert.AreEqual(voteGranted, node1.GetVoteGranted());
        }

        [Test]
        public void UT_SetLastReceived_ReturnLastReceivedMatch()
        {
            Thread.Sleep(100);
            DateTime current = node1.LastReceived;
            node1.UpdateLastReceived();
            Assert.Greater(node1.LastReceived, current);
        }

        [Test]
        public void UT_GetRPCDue_ReturnsRpcDue()
        {
            Assert.Fail();
        }

        [Test]
        public void UT_SetRPCDue_ReturnsRpcDueMatch()
        {
            Assert.Fail();
        }

    }
}
