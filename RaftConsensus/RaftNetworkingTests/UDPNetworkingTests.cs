using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftNetworking.Interfaces;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking.Tests
{
    [TestFixture]
    public class UDPNetworkingTests
    {
        
        private string to;
        private string from;
        private string stringData;
        private byte[] byteData;

        private StringMessage stringMessage;
        private ByteMessage byteMessage;

        static Random rand = new Random();

        private const string IP_TO_BIND = "127.0.0.1";
        private const int SUT_PORT = 5554;

        BaseMessage recievedMessage;
        ManualResetEvent onRecieveMessage = new ManualResetEvent(false);

        [SetUp]
        public void BeforeTest()
        {
            to = Guid.NewGuid().ToString();
            from = Guid.NewGuid().ToString();
            stringData = Guid.NewGuid().ToString();
            byteData = new byte[256];

            stringMessage = new StringMessage(to, from, stringData);
            byteMessage = new ByteMessage(to, from, byteData);
        }

        [Test]
        public void IT_StartSendReceiveDispose_()
        {
            IUDPNetworking sut = new UDPNetworking();
            sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
            sut.OnMessageReceived += Sut_OnMessageReceived;

            Assert.DoesNotThrow(() => { sut.Start(SUT_PORT); });
            Assert.DoesNotThrow(() => { sut.SendMessage(stringMessage); });

            //onRecieveMessage.WaitOne();
            //You've got a message, deal with it and check it's the same

            sut.Dispose();
            Assert.True(sut.IsDisposed());
        }

        private void Sut_OnMessageReceived(object sender, BaseMessage e)
        {
            recievedMessage = e;
            onRecieveMessage.Set();
        }
    }
}
