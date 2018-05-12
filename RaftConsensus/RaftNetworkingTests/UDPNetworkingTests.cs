using NUnit.Framework;

namespace TeamDecided.RaftNetworking.Tests
{
    [TestFixture]
    public class UDPNetworkingTests : BaseUDPNetworkingTests
    {
        [SetUp]
        public override void BeforeEachTest()
        {
            sut = new UDPNetworking();
            rut = new UDPNetworking();
            base.BeforeEachTest();
        }
    }
}
