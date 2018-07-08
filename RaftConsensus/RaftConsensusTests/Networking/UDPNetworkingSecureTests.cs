using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking;

namespace TeamDecided.RaftConsensus.Tests.Networking
{
    internal class UdpNetworkingSecureTests : BaseUdpNetworkingTests
    {
        [SetUp]
        public override void BeforeEachTest()
        {
            Sut = new UdpNetworkingSecure("password123");
            Rut = new UdpNetworkingSecure("password123");
            base.BeforeEachTest();
        }
    }
}
