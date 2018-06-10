using System;
using System.Net;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking.Exceptions;
using TeamDecided.RaftConsensus.Networking.Messages;

/*
    TODO:
        Test all the new messages
        Flag won't recieve an unencrypted message
        Send message with wrong session id
        Send message encypted with wrong symetric key
        Send message encypted with wrong HMAC secret
        Failing to deserialise message, send corrupted
        Received a non-valid public key
        Fail to decrypt the SecureClientHello message from public keys
        Server recevieving a challenge response I no longer have challenge to
        Client failed challenge
        Client recevieving a challenge response I no longer have challenge to
        Server failed challenge
 */

namespace TeamDecided.RaftConsensus.Networking.Tests
{
    [TestFixture]
    public class UdpNetworkingSecureTests : BaseUdpNetworkingTests
    {
        UdpNetworkingReceiveFailureException _exceptionMessage;

        [SetUp]
        public override void BeforeEachTest()
        {
            Sut = new UdpNetworkingSecure("password123");
            Rut = new UdpNetworkingSecure("password123");
            base.BeforeEachTest();
        }

        [Test]
        public void UT_WillNotReceiveUnencryptedMessage_ThrowsException()
        {
            Sut = new UdpNetworking();
            Sut.ManualAddPeer(Rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));
            Rut.OnMessageReceivedFailure += Rut_OnMessageReceivedFailure;

            Assert.DoesNotThrow(() => { Sut.Start(SutPort); });
            Assert.DoesNotThrow(() => { Rut.Start(RutPort); });

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(Rut.GetClientName(), Sut.GetClientName(), randomStringMessage);

            Assert.DoesNotThrow(() => { Sut.SendMessage(message); });

            RutOnReceiveMessage.WaitOne();

            Sut.Dispose();
            Rut.Dispose();

            Assert.NotNull(_exceptionMessage);
            Assert.AreEqual(typeof(UdpNetworkingReceiveFailureException), _exceptionMessage.GetType());

            Assert.IsTrue(_exceptionMessage.Message.Length > 0);
        }

        private void Rut_OnMessageReceivedFailure(object sender, UdpNetworkingReceiveFailureException e)
        {
            _exceptionMessage = e;
            RutOnReceiveMessage.Set();
        }
    }
}
