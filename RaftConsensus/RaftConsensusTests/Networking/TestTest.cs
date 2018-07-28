using NUnit.Framework;
using System;
using System.Net;
using System.Text;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking;
using TeamDecided.RaftConsensus.Networking.Messages;


namespace TeamDecided.RaftConsensus.Tests.Networking
{

    //[TestFixture]
    class TestTest
    {
        static Random rand = new Random();

        internal class UDPNetworkTesting : UDPNetworking
        {
            public void CreateGenerateReceiveFailureException(string messageGUID)
            {
                this.GenerateReceiveFailureException(string.Format("Test: {0}", messageGUID), new Exception());
            }

            public void CreateGenerateSendFailureException(BaseMessage message)
            {
                this.GenerateSendFailureException(((StringMessage)message).Data, message);//(string.Format("Test: {0}", guid));
            }
        }
        
        
        public void TestGenerateSend()
        {
            RaftLogging.Instance.LogFilename = @"C:\Users\Tori\Downloads\debug.log"; // BaseUDPNetworkingTest[SetUp]
            RaftLogging.Instance.DeleteExistingLogFile(); // BaseUDPNetworkingTest[SetUp]
            RaftLogging.Instance.LogLevel = ERaftLogType.Debug; // BaseUDPNetworkingTest[SetUp]


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



            // set local ip
            //string ipToBind = "127.0.0.1";
            // create a pair of objects
            UDPNetworking sut = new UDPNetworking();// UDPNetworkingTest[SetUp]
            UDPNetworking rut = new UDPNetworking();// UDPNetworkingTest[SetUp]
            // configure ports for objects
            //int sutPort = 5555;
            //int rutPort = 5556;
            //ManualResetEvent sutOnReceiveMessage = new ManualResetEvent(false); // BaseUDPNetworkingTest[SetUp]
            //ManualResetEvent rutOnReceiveMessage = new ManualResetEvent(false); // BaseUDPNetworkingTest[SetUp]
            //sut.Start(sutPort);

            //StringMessage message = new StringMessage(rut.ClientName, sut.ClientName, messageData);
            StringMessage message = new StringMessage(rut.ClientName, sut.ClientName, Guid.NewGuid().ToString());

            testable.CreateGenerateSendFailureException(message);

            //sut.ManualAddPeer(rut.ClientName, new IPEndPoint(IPAddress.Parse(ipToBind), rutPort));

            //sut.SendMessage(message); // happen on another thread ninnny

            gotLogEntry.WaitOne(5000);

            Assert.IsTrue(caughtMessage);
        }


        public void TestGenerateReceive()
        {
            RaftLogging.Instance.LogFilename = @"C:\Users\Tori\Downloads\debug.log"; // BaseUDPNetworkingTest[SetUp]
            RaftLogging.Instance.DeleteExistingLogFile(); // BaseUDPNetworkingTest[SetUp]
            RaftLogging.Instance.LogLevel = ERaftLogType.Debug; // BaseUDPNetworkingTest[SetUp]

            string messageGUID = Guid.NewGuid().ToString();

            ManualResetEvent gotLogEntry = new ManualResetEvent(false);
            bool caughtMessage = false;
            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(string.Format("{0}", messageGUID)))
                {
                    gotLogEntry.Set();
                    caughtMessage = true;
                }
            };

            UDPNetworkTesting testable = new UDPNetworkTesting();
            testable.CreateGenerateReceiveFailureException(messageGUID);

            Assert.IsTrue(caughtMessage);
        }

        [Test]
        public void test1()
        {
            RaftLogging.Instance.LogFilename = @"C:\Users\Tori\Downloads\debug.log"; // BaseUDPNetworkingTest[SetUp]
            RaftLogging.Instance.DeleteExistingLogFile(); // BaseUDPNetworkingTest[SetUp]
            RaftLogging.Instance.LogLevel = ERaftLogType.Debug; // BaseUDPNetworkingTest[SetUp]

            // set local ip
            string ipToBind = "127.0.0.1";

            // create a pair of objects
            UDPNetworking sut = new UDPNetworking();// UDPNetworkingTest[SetUp]
            UDPNetworking rut = new UDPNetworking();// UDPNetworkingTest[SetUp]

            // configure ports for objects
            int sutPort = 5555;
            int rutPort = 5556;


            ManualResetEvent sutOnReceiveMessage = new ManualResetEvent(false); // BaseUDPNetworkingTest[SetUp]
            ManualResetEvent rutOnReceiveMessage = new ManualResetEvent(false); // BaseUDPNetworkingTest[SetUp]

            sut.Start(sutPort);

            StringMessage message = new StringMessage(rut.ClientName, sut.ClientName, GetBigString(1000000));
            sut.ManualAddPeer(rut.ClientName, new IPEndPoint(IPAddress.Parse(ipToBind), rutPort));

            Assert.AreEqual(sut.CountPeers(),1);

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
    }
}
