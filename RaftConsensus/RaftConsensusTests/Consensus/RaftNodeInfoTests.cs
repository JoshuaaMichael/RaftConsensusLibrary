using System;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Consensus;

namespace TeamDecided.RaftConsensus.Tests.Consensus
{
    [TestFixture]
    public class RaftNodeInfoTests
    {
        NodeInfo _node1;
        string _nodeName;

        [SetUp]
        public void BeforeTest()
        {
            _nodeName = Guid.NewGuid().ToString();
            _node1 = new NodeInfo(_nodeName);
        }

        [Test]
        public void UT_GetNodeName_ReturnsNodeName()
        {
            Assert.AreEqual(_nodeName, _node1.NodeName);
        }

        //TODO
        //[Test]
        public void UT_SetLastReceived_ReturnLastReceivedMatch()
        {
            //DateTime current = _node1.LastReceived;
            //Thread.Sleep(1); //Wow, computers are fast. Needed this.
            //_node1.UpdateLastReceived();
            //Assert.Greater(_node1.LastReceived, current);
        }
    }
}
