using NUnit.Framework;

namespace TeamDecided.RaftConsensus.Networking.Tests
{
    [TestFixture]
    public class UdpNetworkingTests : BaseUdpNetworkingTests
    {
        [SetUp]
        public override void BeforeEachTest()
        {
            Sut = new UdpNetworking();
            Rut = new UdpNetworking();
            base.BeforeEachTest();
        }
    }
}
