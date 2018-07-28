using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using TeamDecided.RaftConsensus.Networking.Helpers;

namespace TeamDecided.RaftConsensus.Tests.Networking.Helpers
{
    [TestFixture]
    class NodeIPDictionaryTests
    {
        private NodeIPDictionary nid;
        private const string IpToBind = "127.0.0.1";
        private const int Port = 5555;
        private const string _nodeName = "TestNode";
        private IPEndPoint _ipEndPoint;

        [SetUp]
        public void SetUp()
        {
            _ipEndPoint = new IPEndPoint(IPAddress.Parse(IpToBind), Port);
            nid = new NodeIPDictionary();
            nid.AddOrUpdateNode(_nodeName, _ipEndPoint);
        }

        [Test]
        public void UT_AddOrUpdateNode_NodeExists()
        {
            _ipEndPoint = new IPEndPoint(IPAddress.Parse(IpToBind), Port + 1);
            nid.AddOrUpdateNode(_nodeName, _ipEndPoint);
            Assert.IsTrue(_ipEndPoint.Equals(nid.GetNodeIPEndPoint(_nodeName)));
        }

        [Test]
        public void UT_HasNodesAfterAdding_NodeExists()
        {
            Assert.IsTrue(nid.HasNode(_nodeName));
            Assert.NotZero(nid.Count);
        }

        [Test]
        public void UT_RemoveNodes_NodeDoesNotExists()
        {
            Assert.IsTrue(nid.RemoveNode(_nodeName));

            Assert.IsFalse(nid.HasNode(_nodeName));
        }

        [Test]
        public void UT_GetNodesHasNode_NodeExists()
        {
            string[] nodes = nid.GetNodes();

            Assert.IsTrue(nodes.Contains(_nodeName));
        }
    }
}
