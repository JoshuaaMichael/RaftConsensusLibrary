using System;
using System.Threading;
using NUnit.Framework;

namespace TeamDecided.RaftConsensus.Consensus.Tests
{
    [TestFixture]
    public class RaftNodeInfoTests
    {
        NodeInfo node1;
        string nodeName;

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
        public void UT_SetLastReceived_ReturnLastReceivedMatch()
        {
            DateTime current = node1.LastReceived;
            Thread.Sleep(1); //Wow, computers are fast. Needed this.
            node1.UpdateLastReceived();
            Assert.Greater(node1.LastReceived, current);
        }
    }
}
