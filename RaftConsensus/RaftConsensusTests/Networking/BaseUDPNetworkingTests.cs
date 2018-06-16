using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Interfaces;
using TeamDecided.RaftConsensus.Networking.Messages;

/*
    TODO:
        Test for corrupted data
*/

namespace TeamDecided.RaftConsensus.Tests.Networking
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

        protected IUDPNetworking Sut;
        protected IUDPNetworking Rut;

        public virtual void BeforeEachTest()
        {
            RaftLogging.Instance.LogFilename = @"C:\Users\admin\Downloads\debug.log";
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.LogLevel = ERaftLogType.Debug;

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
            Sut.ManualAddPeer(Rut.ClientName, new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));
            Rut.OnMessageReceived += Rut_OnMessageReceived;

            Assert.DoesNotThrow(() => { Sut.Start(SutPort); });
            Assert.DoesNotThrow(() => { Rut.Start(RutPort); });

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(Rut.ClientName, Sut.ClientName, randomStringMessage);

            Assert.DoesNotThrow(() => { Sut.SendMessage(message); });

            RutOnReceiveMessage.WaitOne();

            Sut.Dispose();
            Rut.Dispose();

            Assert.NotNull(_rutReceivedMessage);
            Assert.AreEqual(typeof(StringMessage), _rutReceivedMessage.GetType());

            Assert.AreEqual(_rutReceivedMessage.To, Rut.ClientName);
            Assert.AreEqual(_rutReceivedMessage.From, Sut.ClientName);
            Assert.AreEqual(((StringMessage)_rutReceivedMessage).Data, randomStringMessage);
        }

        [Test]
        public void IT_SendReceivePeer_SuccessfulAddPeer()
        {
            Rut.OnMessageReceived += Rut_OnMessageReceived;
            Rut.Start(RutPort);

            Sut.Start(SutPort);
            Sut.ManualAddPeer(Rut.ClientName, new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));

            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(Rut.ClientName, Sut.ClientName, randomStringMessage);

            Assert.DoesNotThrow(() => { Sut.SendMessage(message); });

            RutOnReceiveMessage.WaitOne();

            Assert.IsTrue(Rut.HasPeer(Sut.ClientName));
        }

        [Test]
        public void IT_OnNewConnectedPeer_SuccessfulPeer()
        {
            Sut.ManualAddPeer(Rut.ClientName, new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            Sut.Start(SutPort);

            Assert.IsTrue(Sut.HasPeer(Rut.ClientName));
            string[] peers = Sut.GetPeers();
            Assert.IsTrue(Rut.ClientName == peers[0]);
        }

        [Test]
        public void UT_NotStartedSendMessage_ThrowsException()
        {
            Sut.ManualAddPeer(Rut.ClientName, new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            string randomStringMessage = Guid.NewGuid().ToString();
            StringMessage message = new StringMessage(Rut.ClientName, Sut.ClientName, randomStringMessage);
            Assert.Throws<InvalidOperationException>(() => { Sut.SendMessage(message); });
        }

        [Test]
        public void UT_StartPort_FromInitializedToRunning()
        {
            Assert.True(Sut.GetStatus() == EUDPNetworkingStatus.Initialized);
            Sut.Start(SutPort);
            Assert.True(Sut.GetStatus() == EUDPNetworkingStatus.Running);
        }

        [Test]
        public void UT_StartIPEndPoint_FromInitializedToRunning()
        {
            Assert.True(Sut.GetStatus() == EUDPNetworkingStatus.Initialized);
            Sut.Start(new IPEndPoint(IPAddress.Parse(IpToBind), SutPort));
            Assert.True(Sut.GetStatus() == EUDPNetworkingStatus.Running);
        }

        [Test]
        public void UT_RemovePeer_PeerNoLongerExists()
        {
            Sut.ManualAddPeer(Rut.ClientName, new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));
            Assert.IsTrue(Sut.HasPeer(Rut.ClientName));
            Sut.RemovePeer(Rut.ClientName);
            Assert.IsFalse(Sut.HasPeer(Rut.ClientName));
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
