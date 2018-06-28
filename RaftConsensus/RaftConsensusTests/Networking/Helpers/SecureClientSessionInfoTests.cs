using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;
using TeamDecided.RaftConsensus.Networking.Messages.SRP;

namespace TeamDecided.RaftConsensus.Tests.Networking.Helpers
{
    [TestFixture]
    internal class SecureClientSessionInfoTests
    {
        [SetUp]
        public void BeforeTest()
        {
        }

        [Test]
        public void SRP_Success()
        {
            SRPSessionManager srpSMClient = new SRPSessionManager("Server", "Client", "password123", null);
            BaseSecureMessage c1 = srpSMClient.GetNextMessage();

            SRPSessionManager srpSMServer = new SRPSessionManager((SRPStep1)c1, "Server", "password123");
            BaseSecureMessage s2 = srpSMServer.GetNextMessage();

            srpSMClient.HandleMessage(s2);
            BaseSecureMessage c3 = srpSMClient.GetNextMessage();

            srpSMServer.HandleMessage(c3);
            BaseSecureMessage s4 = srpSMServer.GetNextMessage();

            srpSMClient.HandleMessage(s4);
            BaseSecureMessage c5 = srpSMClient.GetNextMessage();

            Assert.IsNull(c5);
            //Assert.IsTrue(srpSMClient.IsComplete());
            //Assert.IsTrue(srpSMServer.IsComplete());
        }
    }
}
