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
    public abstract class BaseUdpNetworkingTests
    {
        protected const string IpToBind = "127.0.0.1";
        protected const int SutPort = 5555;
        protected const int RutPort = 5556;

        BaseMessage _sutReceivedMessage;
        protected ManualResetEvent SutOnReceiveMessage;

        BaseMessage _rutReceivedMessage;
        protected ManualResetEvent RutOnReceiveMessage;

        protected IUdpNetworking Sut;
        protected IUdpNetworking Rut;

        public virtual void BeforeEachTest()
        {
            _sutReceivedMessage = null;
            SutOnReceiveMessage = new ManualResetEvent(false);

            _rutReceivedMessage = null;
            RutOnReceiveMessage = new ManualResetEvent(false);
        }

        [TearDown]
        public virtual void AfterEachTest()
        {
            Sut.Dispose();
            Rut.Dispose();
        }

        public void Sut_OnMessageReceived(object sender, BaseMessage e)
        {
            _sutReceivedMessage = e;
            SutOnReceiveMessage.Set();
        }

        public void Rut_OnMessageReceived(object sender, BaseMessage e)
        {
            _rutReceivedMessage = e;
            RutOnReceiveMessage.Set();
        }

        [Test]
        public void IT_StartSendReceiveDispose_SuccessfulSendAndReceive()
        {
            Sut.ManualAddPeer(Rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));
            Rut.OnMessageReceived += Rut_OnMessageReceived;

            Assert.DoesNotThrow(() => { Sut.Start(SutPort); });
            Assert.DoesNotThrow(() => { Rut.Start(RutPort); });

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(Rut.GetClientName(), Sut.GetClientName(), randomStringMessage);

            Assert.DoesNotThrow(() => { Sut.SendMessage(message); });

            RutOnReceiveMessage.WaitOne();

            Sut.Dispose();
            Rut.Dispose();

            Assert.NotNull(_rutReceivedMessage);
            Assert.AreEqual(typeof(StringMessage), _rutReceivedMessage.GetType());

            Assert.AreEqual(_rutReceivedMessage.To, Rut.GetClientName());
            Assert.AreEqual(_rutReceivedMessage.From, Sut.GetClientName());
            Assert.AreEqual(((StringMessage)_rutReceivedMessage).Data, randomStringMessage);
        }

        [Test]
        public void IT_StartSend100ReceiveDispose_SuccessfulSendAndReceive()
        {
            StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive();
        }

        [Test]
        public void IT_StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive()
        {
            const int sleepDelay = 50; //milliseconds
            StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive(sleepDelay);
        }

        private void StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive(int sleepDelay = 0)
        {
            const int numberOfMessages = 100;
            const int overheadWaittime = 1; //Extra time for Secure implementation communication overhead

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

            int waitTime = overheadWaittime + numberOfMessages * 2;
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

        [Test]
        public void IT_SendReceivePeer_SuccessfulAddPeer()
        {
            Rut.OnMessageReceived += Rut_OnMessageReceived;
            Rut.Start(RutPort);

            Sut.Start(SutPort);
            Sut.ManualAddPeer(Rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(Rut.GetClientName(), Sut.GetClientName(), randomStringMessage);

            Assert.DoesNotThrow(() => { Sut.SendMessage(message); });

            RutOnReceiveMessage.WaitOne();

            Assert.IsTrue(Rut.HasPeer(Sut.GetClientName()));
        }

        [Test]
        public void IT_OnNewConnectedPeer_SuccessfulPeer()
        {
            Sut.ManualAddPeer(Rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            Sut.Start(SutPort);

            Assert.IsTrue(Sut.HasPeer(Rut.GetClientName()));
            string[] peers = Sut.GetPeers();
            Assert.IsTrue(Rut.GetClientName() == peers[0]);
        }

        [Test]
        public void UT_NotStartedSendMessage_ThrowsException()
        {
            Sut.ManualAddPeer(Rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(Rut.GetClientName(), Sut.GetClientName(), randomStringMessage);
            Assert.Throws<InvalidOperationException>(() => { Sut.SendMessage(message); });
        }

        [Test]
        public void UT_StartPort_FromInitializedToRunning()
        {
            Assert.True(Sut.GetStatus() == Enums.EudpNetworkingStatus.Initialized);
            Sut.Start(SutPort);
            Assert.True(Sut.GetStatus() == Enums.EudpNetworkingStatus.Running);
        }

        [Test]
        public void UT_StartIPEndPoint_FromInitializedToRunning()
        {
            Assert.True(Sut.GetStatus() == Enums.EudpNetworkingStatus.Initialized);
            Sut.Start(new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            Assert.True(Sut.GetStatus() == Enums.EudpNetworkingStatus.Running);
        }

        [Test]
        public void UT_RemovePeer_PeerNoLongerExists()
        {
            Sut.ManualAddPeer(Rut.GetClientName(), new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));
            Assert.IsTrue(Sut.HasPeer(Rut.GetClientName()));
            Sut.RemovePeer(Rut.GetClientName());
            Assert.IsFalse(Sut.HasPeer(Rut.GetClientName()));
        }

        [Test]
        public void UT_CountPeer_PeerCountIsCorrect()
        {
            string peer1 = Guid.NewGuid().ToString();
            string peer2 = Guid.NewGuid().ToString();
            string peer3 = Guid.NewGuid().ToString();

            Sut.ManualAddPeer(peer1, new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            Sut.ManualAddPeer(peer2, new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            Sut.ManualAddPeer(peer3, new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));

            Assert.IsTrue(Sut.HasPeer(peer1));
            Assert.IsTrue(Sut.HasPeer(peer2));
            Assert.IsTrue(Sut.HasPeer(peer3));
        }
    }
}
