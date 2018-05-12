using System;
using System.Net;
using NUnit.Framework;
using TeamDecided.RaftNetworking.Exceptions;
using TeamDecided.RaftNetworking.Messages;

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

namespace TeamDecided.RaftNetworking.Tests
{
    [TestFixture]
    public class UDPNetworkingSecureTests : BaseUDPNetworkingTests
    {
        UDPNetworkingReceiveFailureException exceptionMessage;

        [SetUp]
        public override void BeforeEachTest()
        {
            sut = new UDPNetworkingSecure("password123");
            rut = new UDPNetworkingSecure("password123");
            base.BeforeEachTest();
        }

        [Test]
        public void UT_WillNotReceiveUnencryptedMessage_ThrowsException()
        {
            sut = new UDPNetworking();
            sut.ManualAddPeer(rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), RUT_PORT));
            rut.OnMessageReceivedFailure += Rut_OnMessageReceivedFailure;

            Assert.DoesNotThrow(() => { sut.Start(SUT_PORT); });
            Assert.DoesNotThrow(() => { rut.Start(RUT_PORT); });

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(rut.GetClientName(), sut.GetClientName(), randomStringMessage);

            Assert.DoesNotThrow(() => { sut.SendMessage(message); });

            rutOnReceiveMessage.WaitOne();

            sut.Dispose();
            rut.Dispose();

            Assert.NotNull(exceptionMessage);
            Assert.AreEqual(typeof(UDPNetworkingReceiveFailureException), exceptionMessage.GetType());

            Assert.IsTrue(exceptionMessage.Message.Length > 0);
        }

        private void Rut_OnMessageReceivedFailure(object sender, UDPNetworkingReceiveFailureException e)
        {
            exceptionMessage = e;
            rutOnReceiveMessage.Set();
        }
    }
}
