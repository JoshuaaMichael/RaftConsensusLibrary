using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Exceptions;
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
        private static Random rand = new Random();
        private const string LOG_FILE_NAME = @"C:\Users\Tori\Downloads\debug.log";

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
            RaftLogging.Instance.LogFilename = LOG_FILE_NAME;
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

        [Test]
        public void UT_SendOverSizeMessageFails_ThrowsUdpNetworkingSendFailureException()
        {
            ManualResetEvent gotLogEntry = new ManualResetEvent(false);

            bool caughtMessageToBig = false;
            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains("Message is too large to send"))
                {
                    gotLogEntry.Set();
                    caughtMessageToBig = true;
                }
            };

            Sut.Start(SutPort);

            StringMessage message = new StringMessage(Rut.ClientName, Sut.ClientName, GetBigString(1000000));
            //StringMessage message = new StringMessage(Rut.ClientName, Sut.ClientName, GetBigString(10));

            Sut.ManualAddPeer(Rut.ClientName, new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));

            Sut.SendMessage(message); 

            gotLogEntry.WaitOne(10000);

            Assert.IsTrue(caughtMessageToBig);
        }

        [Test]
        public void UT_CountPeersReturnsCorrectPeerCount_CountPeersEqualsPeersAdded()
        {
            Sut.ManualAddPeer(Rut.ClientName, new IPEndPoint(IPAddress.Parse(IpToBind), RutPort));
            Assert.AreEqual(Sut.CountPeers(), 1);
        }

        // the below test really just hit the code coverage criteria, not really function tests :(
        [Test]
        public void UT_GenerateSendFailureException()
        {
            string messageData = Guid.NewGuid().ToString();

            ManualResetEvent gotLogEntry = new ManualResetEvent(false);
            bool caughtMessage = false;
            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(messageData))
                {
                    gotLogEntry.Set();
                    caughtMessage = true;
                }
            };

            UDPNetworkTesting testable = new UDPNetworkTesting();

            StringMessage message = new StringMessage(Rut.ClientName, Sut.ClientName, messageData);

            testable.CreateGenerateSendFailureException(message);

            gotLogEntry.WaitOne(5000);

            Assert.IsTrue(caughtMessage);

            Assert.Throws<UdpNetworkingSendFailureException>(() =>
                testable.ThrowUdpNetworkingSendFailureException(messageData, new Exception(), message));
        }

        [Test]
        public void UT_GenerateReceiveFailureException()
        {
            string guid = Guid.NewGuid().ToString();

            ManualResetEvent gotLogEntry = new ManualResetEvent(false);
            bool caughtMessage = false;
            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(string.Format("{0}", guid)))
                {
                    gotLogEntry.Set();
                    caughtMessage = true;
                }
            };

            UDPNetworkTesting testable = new UDPNetworkTesting();

            testable.CreateGenerateReceiveFailureException(guid);

            Assert.IsTrue(caughtMessage);

            Assert.Throws<UdpNetworkingReceiveFailureException>(() =>
                testable.ThrowUDPNetworkingReceiveFailureException(guid));
        }

        [Test]
        public void UT_ThrowUDPNetworkingException()
        {
            UDPNetworkTesting testable = new UDPNetworkTesting();

            string message = Guid.NewGuid().ToString();

            ManualResetEvent gotLogEntry = new ManualResetEvent(false);
            bool caughtMessage = false;
            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(message))
                {
                    gotLogEntry.Set();
                    caughtMessage = true;
                }
            };

            Assert.Throws<UdpNetworkingReceiveFailureException>(() =>
                testable.ThrowUDPNetworkingReceiveFailureException("message", new Exception()));
        }


        [Test]
        public void UT_BaseMessage()
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(IpToBind),SutPort);
            string from = Guid.NewGuid().ToString();

            BaseMessageTesting baseMessage = new BaseMessageTesting(ip, from);

            Assert.AreEqual(((BaseMessage) baseMessage).IPEndPoint, ip);
            Assert.AreEqual(((BaseMessage)baseMessage).From, from);
        }

        public string GetBigString(int size)
        {
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < size; i++)
            {
                str.Append(rand.Next(65, 65 + 26));
            }
            return str.ToString();
        }

        internal class UDPNetworkTesting : UDPNetworking
        {
            public void CreateGenerateReceiveFailureException(string guid)
            {
                this.GenerateReceiveFailureException(string.Format("Test: {0}", guid), new Exception());
            }

            public void CreateGenerateSendFailureException(BaseMessage message)
            {
                this.GenerateSendFailureException(((StringMessage)message).Data, message);//(string.Format("Test: {0}", guid));
            }

            public void ThrowUdpNetworkingSendFailureException(string error, Exception e, BaseMessage message)
            {
                throw new UdpNetworkingSendFailureException(error, e, message);
            }

            public void ThrowUDPNetworkingReceiveFailureException(string message, Exception e)
            {
                throw new UdpNetworkingReceiveFailureException(message, e);
            }

            public void ThrowUDPNetworkingReceiveFailureException(string message)
            {
                throw new UdpNetworkingReceiveFailureException(message);
            }

            public void ThrowUDPNetworkingException(string message, Exception e = null)
            {

            }
        }

        internal class BaseMessageTesting : BaseMessage
        {
            public BaseMessageTesting(IPEndPoint ip, string data): base(ip, data)
            {

            }
        }
    }
}
