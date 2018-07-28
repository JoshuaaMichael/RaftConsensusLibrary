using NUnit.Framework;
using System;
using System.Net;
using System.Text;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking;
using TeamDecided.RaftConsensus.Networking.Exceptions;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Tests.Networking.Helpers
{
    [TestFixture]
    class RaftUDPClientTests
    {
        protected const string IpToBind = "127.0.0.1";
        protected const int Port = 5555;
        protected RaftUDPClient ruc;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RaftLogging.Instance.LogFilename = TestContext.CurrentContext.TestDirectory + "\\debug.log";
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.LogLevel = ERaftLogType.Debug;
        }

        [SetUp]
        public void SetUp()
        {
            ruc = new RaftUDPClient();
            ruc.Start(new IPEndPoint(IPAddress.Parse(IpToBind), Port));
        }

        [TearDown]
        public void TearDown()
        {
            ruc.Dispose();
        }

        [Test]
        public void UT_StopRaftUdpClient()
        {
            ruc.Stop();

            RaftLogging.Instance.Log(ERaftLogType.Debug, "{0}", "RaftUdpClient Has been stopped");
            Assert.IsFalse(ruc.IsRunning());
        }

        [Test]
        public void UT_DisposedRaftUdpClient()
        {
            ManualResetEvent gotLogEntry = new ManualResetEvent(false);
            bool caughtMessage = false;
            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(string.Format("{0}", "Caught exception. Dumping exception string:")))
                {
                    gotLogEntry.Set();
                    caughtMessage = true;
                }
            };

            StringMessage message = CreateMessage("Stop RaftUDPClient test string");

            ruc.NodeIPs.AddOrUpdateNode(message.To /*sut.ClientName*/, new IPEndPoint(IPAddress.Parse(IpToBind), Port));

            ruc.DisposeSocket();

            ruc.Send(message);
            Assert.IsTrue(caughtMessage);
        }

        [Test]
        public void UT_SendMessageToNullIPFails()
        {
            StringMessage message = CreateMessage("Send to null ip address or unknown RaftUDPClient test string");

            Assert.IsFalse(ruc.Send(message));

            Assert.AreEqual(message.To, ruc.GetSendFailureException().GetMessage().To);

        }

        [Test]
        public void UT_SendOversizeMessageFails()
        {
            StringMessage message = CreateMessage(GetBigString(100000));
            Assert.IsFalse(ruc.Send(message));
            ruc.Dispose();
        }

        [Test]
        public void UT_StartTwice()
        {
            Assert.Throws<InvalidOperationException>(() => ruc.Start(Port));
        }

        [Test]
        public void UT_BlowUpAsync()
        {
            ManualResetEvent gotLogEntry = new ManualResetEvent(false);
            bool caughtMessage = false;
            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(string.Format("{0}", "Cannot access a disposed object.")))
                {
                    gotLogEntry.Set();
                    caughtMessage = true;
                }
            };

            ruc.DisposeSocket();
            ruc.ReceiveAsync();
            Assert.IsTrue(caughtMessage);

        }


    public string GetBigString(int size)
        {
            StringBuilder str = new StringBuilder();
            Random rand = new Random();

            for (int i = 0; i < size; i++)
            {
                str.Append(rand.Next(65, 65 + 26));
            }

            return str.ToString();
        }

        public StringMessage CreateMessage(string message)
        {
            UDPNetworking sut = new UDPNetworking();
            UDPNetworking rut = new UDPNetworking();
            StringMessage _message = new StringMessage(rut.ClientName, sut.ClientName, message);

            return _message;
        }

    }
}
