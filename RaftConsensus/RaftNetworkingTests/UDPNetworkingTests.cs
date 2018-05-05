using System;
using NUnit.Framework;
//using NUnit.Mocks;
using TeamDecided.RaftNetworking;
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

        [SetUp]
        public void BeforeTest()
        {
            to = new Guid().ToString();
            from = new Guid().ToString();
            stringData = new Guid().ToString();
            byteData = new byte[256];

            stringMessage = new StringMessage(to, from, stringData);
            byteMessage = new ByteMessage(to, from, byteData);

        }

        [Test]
        public void IT_StartSendDispose_()
        {
            IUDPNetworking sut = new UDPNetworking();
            Assert.DoesNotThrow(() => { sut.Start(5554); });
            Assert.DoesNotThrow(() => { sut.SendMessage(stringMessage);  });
            Assert.IsNotNull(sut);
            sut.Dispose();
            Assert.IsNull(sut);
        }
    }
}
