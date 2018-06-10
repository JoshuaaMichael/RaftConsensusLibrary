using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using TeamDecided.RaftConsensus.Networking;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Tests.Networking
{
    [TestFixture]
    public class UdpNetworkingTests : BaseUdpNetworkingTests
    {
        [SetUp]
        public override void BeforeEachTest()
        {
            Sut = new UDPNetworking();
            Rut = new UDPNetworking();
            base.BeforeEachTest();
        }

        [Test]
        public void IT_StartSend25ReceiveDispose_SuccessfulSendAndReceive()
        {
            StartSend25DelayedReceiveDispose_SuccessfulSendAndReceive();
        }

        [Test]
        public void IT_StartSend25DelayedReceiveDispose_SuccessfulSendAndReceive()
        {
            const int sleepDelay = 50; //milliseconds
            StartSend25DelayedReceiveDispose_SuccessfulSendAndReceive(sleepDelay);
        }

        private void StartSend25DelayedReceiveDispose_SuccessfulSendAndReceive(int sleepDelay = 0)
        {
            const int numberOfMessages = 25;
            const int overheadWaittime = 5; //Extra time for Secure implementation communication overhead

            Dictionary<string, BaseMessage> messageBuffer = new Dictionary<string, BaseMessage>();
            CountdownEvent cde = new CountdownEvent(numberOfMessages);

            Sut.ManualAddPeer(Rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));
            Rut.OnMessageReceived += delegate (Object o, BaseMessage e)
            {
                StringMessage message = (StringMessage)e;
                messageBuffer.Add(message.Data, message);
                cde.Signal();
            };

            Assert.DoesNotThrow(() => { Sut.Start(SutPort); });
            Assert.DoesNotThrow(() => { Rut.Start(RutPort); });

            string stringMessage = Guid.NewGuid().ToString();
            for (int i = 0; i < numberOfMessages; i++)
            {
                string data = string.Format("{0}_{1}", stringMessage, i);
                StringMessage sendingMessage = new StringMessage(Rut.GetClientName(), Sut.GetClientName(), data);
                Assert.DoesNotThrow(() => { Sut.SendMessage(sendingMessage); });
                Thread.Sleep(sleepDelay);
            }

            int waitTime = overheadWaittime + numberOfMessages * 15;
            if (!cde.Wait(waitTime))
            {
                Assert.Fail(string.Format("Failed to receive back the messages({0}) before timeout({1}ms) occured", numberOfMessages, waitTime));
            }

            Assert.IsTrue(messageBuffer.Count == numberOfMessages);

            for (int i = 0; i < numberOfMessages; i++)
            {
                string data = string.Format("{0}_{1}", stringMessage, i);

                Assert.IsTrue(messageBuffer.ContainsKey(data));

                Assert.IsTrue(typeof(StringMessage) == messageBuffer[data].GetType());

                StringMessage message = (StringMessage)messageBuffer[data];
                Assert.AreEqual(message.To, Rut.GetClientName());
                Assert.AreEqual(message.From, Sut.GetClientName());
            }
        }
    }
}
