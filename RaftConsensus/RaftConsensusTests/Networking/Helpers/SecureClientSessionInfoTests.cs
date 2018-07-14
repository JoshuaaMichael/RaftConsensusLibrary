using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;
using TeamDecided.RaftConsensus.Networking.Messages.SRP;

namespace TeamDecided.RaftConsensus.Tests.Networking.Helpers
{
    [TestFixture]
    internal class SecureClientSessionInfoTests
    {
        private const string To = "Server";
        private const string From = "Client";
        private const string Password = "password123";

        [SetUp]
        public void BeforeTest()
        {
        }

        [Test]
        public void SRP_Success()
        {
            SRPSessionManager srpSMClient = new SRPSessionManager(To, From, Password, new StringMessage(To, From, "Hello World"));
            SecureMessage c1 = (SecureMessage) srpSMClient.GetNextMessage();

            SRPSessionManager srpSMServer = new SRPSessionManager((SRPStep1)c1, To, Password);
            SecureMessage s2 = (SecureMessage) srpSMServer.GetNextMessage();

            srpSMClient.HandleMessage(s2);
            SecureMessage c3 = (SecureMessage) srpSMClient.GetNextMessage();

            srpSMServer.HandleMessage(c3);
            SecureMessage s4 = (SecureMessage) srpSMServer.GetNextMessage();

            Assert.IsTrue(srpSMServer.IsServerAndPendingComplete());

            srpSMClient.HandleMessage(s4);
            SecureMessage c5 = (SecureMessage) srpSMClient.GetNextMessage();

            Assert.IsNull(c5);
            Assert.IsTrue(srpSMClient.IsSRPComplete());
            Assert.IsTrue(srpSMServer.IsSRPComplete());
        }
    }
}
