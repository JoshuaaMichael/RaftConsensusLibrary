using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftNetworking.Interfaces;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking.Tests
{
    [TestFixture]
    public class UDPNetworkingSecureTests
    {

        private BaseMessage recievedMessage;
        private ManualResetEvent onRecieveMessage = new ManualResetEvent(false);

        [Test]
        public void IT_StartSendReceiveDispose_SuccessfulSendAndReceive()
        {
            IUDPNetworking nodeA = new UDPNetworkingSecure("password123");
            IUDPNetworking nodeB = new UDPNetworkingSecure("password123");

            int nodeAListenPort = 5554;
            int nodeBListenPort = 5555;

            nodeA.ManualAddPeer(nodeB.GetClientName(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), nodeBListenPort));
            nodeB.ManualAddPeer(nodeA.GetClientName(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), nodeAListenPort));

            nodeB.OnMessageReceived += NodeB_OnMessageReceived;

            nodeA.Start(nodeAListenPort);
            nodeB.Start(nodeBListenPort);

            StringMessage message = new StringMessage(nodeB.GetClientName(), nodeA.GetClientName(), "Hello World!!!");

            nodeA.SendMessage(message);

            onRecieveMessage.WaitOne();

            Assert.AreEqual(message.Data, ((StringMessage)recievedMessage).Data);

            nodeA.Dispose();
            nodeB.Dispose();
        }

        private void NodeB_OnMessageReceived(object sender, BaseMessage e)
        {
            recievedMessage = e;
            onRecieveMessage.Set();
        }
    }
}
