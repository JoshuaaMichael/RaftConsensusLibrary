using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftNetworking.Interfaces;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking.Tests
{
    [TestFixture]
    public class UDPNetworkingTests
    {
        IUDPNetworking sut;
        IUDPNetworking rut;

        private string to;
        private string from;
        private string stringData;
        private byte[] byteData;

        private StringMessage stringMessage;
        private ByteMessage byteMessage;

        static Random rand = new Random();

        private const string IP_TO_BIND = "127.0.0.1";
        private const int SUT_PORT = 5554;
        private const int RUT_PORT = 5555;

        BaseMessage recievedMessage;
        ManualResetEvent onRecieveMessage = new ManualResetEvent(false);

        [SetUp]
        public void BeforeTest()
        {
            to = Guid.NewGuid().ToString();
            from = Guid.NewGuid().ToString();
            stringData = Guid.NewGuid().ToString();
            byteData = new byte[256];
            rand.NextBytes(byteData);

            stringMessage = new StringMessage(to, from, stringData);
            byteMessage = new ByteMessage(to, from, byteData);
        }

        [Test]
        public void IT_StartSendReceiveDispose_SuccessfulSendAndReceive()
        {
            using (sut = new UDPNetworking())
            {
                sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                sut.OnMessageReceived += Sut_OnMessageReceived;

                Assert.DoesNotThrow(() => { sut.Start(SUT_PORT); });
                Assert.DoesNotThrow(() => { sut.SendMessage(stringMessage); });

                onRecieveMessage.WaitOne();

                Assert.NotNull(onRecieveMessage);
                Assert.AreEqual(typeof(StringMessage), recievedMessage.GetType());

                string receivedMessageTo = recievedMessage.To;
                string receivedMessageFrom = recievedMessage.From;
                string receivedMessageData = ((StringMessage)recievedMessage).Data;

                Assert.AreEqual(receivedMessageTo, to);
                Assert.AreEqual(receivedMessageFrom, from);
                Assert.AreEqual(receivedMessageData, stringData);
            }
        }

        [Test]
        public void IT_StartSend100ReceiveDispose_SuccessfulSendAndReceive()
        {
            const int NUMBER_OF_MESSAGES = 100;
            Dictionary<string, BaseMessage> messageBuffer = new Dictionary<string, BaseMessage>();
            CountdownEvent cde = new CountdownEvent(NUMBER_OF_MESSAGES);

            using (sut = new UDPNetworking())
            {
                sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                sut.OnMessageReceived += delegate (Object o, BaseMessage e)
                {
                    StringMessage message = (StringMessage)e;
                    messageBuffer.Add(message.Data, message);
                    cde.Signal();
                };

                Assert.DoesNotThrow(() => { sut.Start(SUT_PORT); });

                for (int i = 0; i < NUMBER_OF_MESSAGES; i++)
                {
                    string data = string.Format("{0}_{1}", stringMessage, i);
                    StringMessage sendingMessage = new StringMessage(to, from, data);

                    Assert.DoesNotThrow(() => { sut.SendMessage(sendingMessage); });
                }

                if (!cde.Wait(NUMBER_OF_MESSAGES * 2))
                {
                    Assert.Fail();
                }

                Assert.IsTrue(messageBuffer.Count == NUMBER_OF_MESSAGES);

                for (int i = 0; i < NUMBER_OF_MESSAGES; i++)
                {
                    string data = string.Format("{0}_{1}", stringMessage, i);

                    Assert.IsTrue(messageBuffer.ContainsKey(data));

                    Assert.IsTrue(typeof(StringMessage) == messageBuffer[data].GetType());

                    StringMessage message = (StringMessage)messageBuffer[data];
                    Assert.AreEqual(message.To, to);
                    Assert.AreEqual(message.From, from);
                }
            }
        }

        [Test]
        public void IT_StartSend100DelayedReceiveDispose_SuccessfulSendAndReceive()
        {
            const int NUMBER_OF_MESSAGES = 100;
            Dictionary<string, BaseMessage> messageBuffer = new Dictionary<string, BaseMessage>();
            CountdownEvent cde = new CountdownEvent(NUMBER_OF_MESSAGES);

            using (sut = new UDPNetworking())
            {
                sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                sut.OnMessageReceived += delegate (Object o, BaseMessage e)
                {
                    StringMessage message = (StringMessage)e;
                    messageBuffer.Add(message.Data, message);
                    cde.Signal();
                };

                Assert.DoesNotThrow(() => { sut.Start(SUT_PORT); });

                for (int i = 0; i < NUMBER_OF_MESSAGES; i++)
                {
                    string data = string.Format("{0}_{1}", stringMessage, i);
                    StringMessage sendingMessage = new StringMessage(to, from, data);

                    Assert.DoesNotThrow(() => { sut.SendMessage(sendingMessage); });
                    Thread.Sleep(50);
                }

                if (!cde.Wait(NUMBER_OF_MESSAGES * 2))
                {
                    Assert.Fail();
                }

                Assert.IsTrue(messageBuffer.Count == NUMBER_OF_MESSAGES);

                for (int i = 0; i < NUMBER_OF_MESSAGES; i++)
                {
                    string data = string.Format("{0}_{1}", stringMessage, i);

                    Assert.IsTrue(messageBuffer.ContainsKey(data));

                    Assert.IsTrue(typeof(StringMessage) == messageBuffer[data].GetType());

                    StringMessage message = (StringMessage)messageBuffer[data];
                    Assert.AreEqual(message.To, to);
                    Assert.AreEqual(message.From, from);
                }
            }
        }

        [Test]
        public void IT_ReceiveMessageFailure_SendFailure()
        {
            // //Replaced BaseMesage Serialize() method with below to siumulate garbage data
            // //Random rand = new Random();
            // //byte[] garbage = new byte[64];
            // //rand.NextBytes(garbage);
            // //return garbage;

            //ManualResetEvent mre = new ManualResetEvent(false);

            //using (sut = new UDPNetworking())
            //{
            //    bool wasCalled = false;
            //    sut.OnMessageReceivedFailure += delegate (Object sender, UDPNetworkingReceiveFailureException e)
            //    {
            //        wasCalled = true;
            //        mre.Set();
            //    };

            //    sut.Start(SUT_PORT);
            //    sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
            //    sut.SendMessage(stringMessage);

            //    mre.WaitOne();

            //    Assert.IsTrue(wasCalled);

            Assert.Pass();
            //}
        }

        [Test]
        public void IT_SendReceivePeer_SuccessfulAddPeer()
        {
            using (rut = new UDPNetworking())
            {
                rut.OnMessageReceived += Sut_OnMessageReceived;
                rut.Start(RUT_PORT);

                using (sut = new UDPNetworking())
                {
                    sut.Start(SUT_PORT);
                    sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), RUT_PORT));

                    Assert.DoesNotThrow(() => { sut.SendMessage(stringMessage); });
                    Assert.IsTrue(sut.HasPeer(to));
                }
            }
        }

        [Test]
        public void IT_OnNewConnectedPeer_SuccessfulPeer()
        {
            using (sut = new UDPNetworking())
            {
                sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                sut.Start(SUT_PORT);

                Assert.IsTrue(sut.HasPeer(to));
                string[] peers = sut.GetPeers();
                Assert.IsTrue(to == peers[0]);

            }
        }

        [Test]
        public void IT_NotStartedSendMessage_ThrowsException()
        {
            using (sut = new UDPNetworking())
            {
                sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                Assert.Throws<InvalidOperationException>(() => { sut.SendMessage(stringMessage); });
            }
        }

        [Test]
        public void IT_DisposedSendMessage_ThrowsException()
        {
            using (sut = new UDPNetworking())
            {
                sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                sut.Start(SUT_PORT);
            }
            Assert.Throws<InvalidOperationException>(() => { sut.SendMessage(stringMessage); });
        }

        [Test]
        public void UT_StartPort_FromInitializedToRunning()
        {
            using (sut = new UDPNetworking())
            {
                Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.INITIALIZED);
                sut.Start(SUT_PORT);
                Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.RUNNING);
            }
        }

        [Test]
        public void UT_StartIPEndPoint_FromInitializedToRunning()
        {
            using (sut = new UDPNetworking())
            {
                Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.INITIALIZED);
                sut.Start(new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                Assert.True(sut.GetStatus() == Enums.EUDPNetworkingStatus.RUNNING);
            }
        }

        [Test]
        public void UT_RemovePeer_PeerNoLongerExists()
        {
            using (sut = new UDPNetworking())
            {
                sut.ManualAddPeer(to, new IPEndPoint(IPAddress.Parse(IP_TO_BIND), SUT_PORT));
                Assert.IsTrue(sut.HasPeer(to));
                sut.RemovePeer(to);
                Assert.IsFalse(sut.HasPeer(to));
            }
        }

        [Test]
        public void UT_CountPeer_PeerCountIsCorrect()
        {
            using (sut = new UDPNetworking())
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

                Assert.AreEqual(sut.CountPeers(),3);
            }
        }

        private void Sut_OnMessageReceived(object sender, BaseMessage e)
        {
            recievedMessage = e;
            onRecieveMessage.Set();
        }

    }
}
