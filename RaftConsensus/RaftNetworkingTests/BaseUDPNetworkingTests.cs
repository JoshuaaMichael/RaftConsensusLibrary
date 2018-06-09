using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking.Interfaces;
using TeamDecided.RaftConsensus.Networking.Messages;

/*
    TODO:
        Test for corrupted data
*/

namespace TeamDecided.RaftConsensus.Networking.Tests
{
    public abstract class BaseUDPNetworkingTests
    {
        protected const string IP_TO_BIND = "127.0.0.1";
        protected const int SUT_PORT = 5555;
        protected const int RUT_PORT = 5556;

        BaseMessage sutReceivedMessage;
        protected ManualResetEvent sutOnReceiveMessage;

        BaseMessage rutReceivedMessage;
        protected ManualResetEvent rutOnReceiveMessage;

        protected IUDPNetworking sut;
        protected IUDPNetworking rut;

        public virtual void BeforeEachTest()
        {
            sutReceivedMessage = null;
            sutOnReceiveMessage = new ManualResetEvent(false);

            rutReceivedMessage = null;
            rutOnReceiveMessage = new ManualResetEvent(false);
        }

        [TearDown]
        public virtual void AfterEachTest()
        {
            sut.Dispose();
            rut.Dispose();
        }

        public void Sut_OnMessageReceived(object sender, BaseMessage e)
        {
            sutReceivedMessage = e;
            sutOnReceiveMessage.Set();
        }

        public void Rut_OnMessageReceived(object sender, BaseMessage e)
        {
            rutReceivedMessage = e;
            rutOnReceiveMessage.Set();
        }

        [Test]
        public void IT_StartSendReceiveDispose_SuccessfulSendAndReceive()
        {
            sut.ManualAddPeer(rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), RUT_PORT));
            rut.OnMessageReceived += Rut_OnMessageReceived;

            Assert.DoesNotThrow(() => { sut.Start(SUT_PORT); });
            Assert.DoesNotThrow(() => { rut.Start(RUT_PORT); });

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(rut.GetClientName(), sut.GetClientName(), randomStringMessage);

            Assert.DoesNotThrow(() => { sut.SendMessage(message); });

            rutOnReceiveMessage.WaitOne();

            sut.Dispose();
            rut.Dispose();

            Assert.NotNull(rutReceivedMessage);
            Assert.AreEqual(typeof(StringMessage), rutReceivedMessage.GetType());

            Assert.AreEqual(rutReceivedMessage.To, rut.GetClientName());
            Assert.AreEqual(rutReceivedMessage.From, sut.GetClientName());
            Assert.AreEqual(((StringMessage)rutReceivedMessage).Data, randomStringMessage);
        }

        [Test]
        public void IT_StartSend100ReceiveDispose_SuccessfulSendAndReceive()
        {
            StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive();
        }

        [Test]
        public void IT_StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive()
        {
            const int SLEEP_DELAY = 50; //milliseconds
            StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive(SLEEP_DELAY);
        }

        private void StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive(int sleepDelay = 0)
        {
            const int NUMBER_OF_MESSAGES = 100;
            const int OVERHEAD_WAITTIME = 1; //Extra time for Secure implementation communication overhead

            Dictionary<string, BaseMessage> messageBuffer = new Dictionary<string, BaseMessage>();
            CountdownEvent cde = new CountdownEvent(NUMBER_OF_MESSAGES);

            sut.ManualAddPeer(rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), RUT_PORT));
            rut.OnMessageReceived += delegate (Object o, BaseMessage e)
            {
                StringMessage message = (StringMessage)e;
                messageBuffer.Add(message.Data, message);
                cde.Signal();
            };

            Assert.DoesNotThrow(() => { sut.Start(SUT_PORT); });
            Assert.DoesNotThrow(() => { rut.Start(RUT_PORT); });

            string stringMessage = Guid.NewGuid().ToString();
            for (int i = 0; i < NUMBER_OF_MESSAGES; i++)
            {
                string data = string.Format("{0}_{1}", stringMessage, i);
                StringMessage sendingMessage = new StringMessage(rut.GetClientName(), sut.GetClientName(), data);
                Assert.DoesNotThrow(() => { sut.SendMessage(sendingMessage); });
                Thread.Sleep(sleepDelay);
            }

            int waitTime = OVERHEAD_WAITTIME + NUMBER_OF_MESSAGES * 2;
            if (!cde.Wait(waitTime))
            {
                Assert.Fail(string.Format("Failed to receive back the messages({0}) before timeout({1}ms) occured", NUMBER_OF_MESSAGES, waitTime));
            }

            Assert.IsTrue(messageBuffer.Count == NUMBER_OF_MESSAGES);

            for (int i = 0; i < NUMBER_OF_MESSAGES; i++)
            {
                string data = string.Format("{0}_{1}", stringMessage, i);

                Assert.IsTrue(messageBuffer.ContainsKey(data));

                Assert.IsTrue(typeof(StringMessage) == messageBuffer[data].GetType());

                StringMessage message = (StringMessage)messageBuffer[data];
                Assert.AreEqual(message.To, rut.GetClientName());
                Assert.AreEqual(message.From, sut.GetClientName());
            }
        }

        [Test]
        public void IT_SendReceivePeer_SuccessfulAddPeer()
        {
            rut.OnMessageReceived += Rut_OnMessageReceived;
            rut.Start(RUT_PORT);

            sut.Start(SUT_PORT);
            sut.ManualAddPeer(rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), RUT_PORT));

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(rut.GetClientName(), sut.GetClientName(), randomStringMessage);

            Assert.DoesNotThrow(() => { sut.SendMessage(message); });

            rutOnReceiveMessage.WaitOne();

            Assert.IsTrue(rut.HasPeer(sut.GetClientName()));
        }

        [Test]
        public void IT_OnNewConnectedPeer_SuccessfulPeer()
        {
            sut.ManualAddPeer(rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
            sut.Start(SUT_PORT);

            Assert.IsTrue(sut.HasPeer(rut.GetClientName()));
            string[] peers = sut.GetPeers();
            Assert.IsTrue(rut.GetClientName() == peers[0]);
        }

        [Test]
        public void UT_NotStartedSendMessage_ThrowsException()
        {
            sut.ManualAddPeer(rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(rut.GetClientName(), sut.GetClientName(), randomStringMessage);
            Assert.Throws<InvalidOperationException>(() => { sut.SendMessage(message); });
        }

        [Test]
        public void UT_StartPort_FromInitializedToRunning()
        {
            Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.INITIALIZED);
            sut.Start(SUT_PORT);
            Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.RUNNING);
        }

        [Test]
        public void UT_StartIPEndPoint_FromInitializedToRunning()
        {
            Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.INITIALIZED);
            sut.Start(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
            Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.RUNNING);
        }

        [Test]
        public void UT_RemovePeer_PeerNoLongerExists()
        {
            sut.ManualAddPeer(rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), RUT_PORT));
            Assert.IsTrue(sut.HasPeer(rut.GetClientName()));
            sut.RemovePeer(rut.GetClientName());
            Assert.IsFalse(sut.HasPeer(rut.GetClientName()));
        }

        [Test]
        public void UT_CountPeer_PeerCountIsCorrect()
        {
            string peer1 = Guid.NewGuid().ToString();
            string peer2 = Guid.NewGuid().ToString();
            string peer3 = Guid.NewGuid().ToString();

            sut.ManualAddPeer(peer1, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
            sut.ManualAddPeer(peer2, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
            sut.ManualAddPeer(peer3, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));

            Assert.IsTrue(sut.HasPeer(peer1));
            Assert.IsTrue(sut.HasPeer(peer2));
            Assert.IsTrue(sut.HasPeer(peer3));
        }
    }
}
