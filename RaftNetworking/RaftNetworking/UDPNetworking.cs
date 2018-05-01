using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftNetworking.Enums;
using TeamDecided.RaftNetworking.Exceptions;
using TeamDecided.RaftNetworking.Interfaces;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking
{
    class UDPNetworking : IUDPNetworking
    {
        public event EventHandler<BaseMessage> OnMessageReceived;
        private Queue<BaseMessage> newMessagesReceived;
        private object newMessagesReceivedLockObject;
        private ManualResetEvent onMessageReceive;

        public event EventHandler<UDPNetworkingReceiveFailureException> OnMessageReceivedFailure;
        private Queue<UDPNetworkingReceiveFailureException> newMessageReceiveFailures;
        private object newMessageReceiveFailuresLockObject;
        private ManualResetEvent onMessageReceiveFailure;

        public event EventHandler<UDPNetworkingSendFailureException> OnMessageSendFailure;
        private Queue<UDPNetworkingSendFailureException> newMessageSendFailures;
        private object newMessageSendFailuresLockObject;
        private ManualResetEvent onMessageSendFailure;

        public event EventHandler<string> OnNewConnectedPeer;
        private Queue<string> newConnectedPeers;
        private object newConnectedPeersLockObject;
        private ManualResetEvent onNewConnectedPeer;
        private Dictionary<string, IPEndPoint> peers;
        private object peersLockObject;

        private Queue<BaseMessage> newMessagesToSend;
        private object newMessagesToSendLockObject;
        private ManualResetEvent onMessageToSend;

        private ManualResetEvent onNetworkingStop;

        private UdpClient udpClient;

        private EUDPNetworkingStatus status;
        private object statusLockObject;

        private Task listeningThread;
        private Task sendingThread;
        private Task processingThread;

        public UDPNetworking()
        {
            newMessagesReceived = new Queue<BaseMessage>();
            newMessagesReceivedLockObject = new object();
            onMessageReceive = new ManualResetEvent(false);

            newMessageReceiveFailures = new Queue<UDPNetworkingReceiveFailureException>();
            newMessageReceiveFailuresLockObject = new object();
            onMessageReceiveFailure = new ManualResetEvent(false);

            newMessageSendFailures = new Queue<UDPNetworkingSendFailureException>();
            newMessageSendFailuresLockObject = new object();
            onMessageSendFailure = new ManualResetEvent(false);

            newConnectedPeers = new Queue<string>();
            newConnectedPeersLockObject = new object();
            onNewConnectedPeer = new ManualResetEvent(false);
            peers = new Dictionary<string, IPEndPoint>();
            peersLockObject = new object();

            newMessagesToSend = new Queue<BaseMessage>();
            newMessagesToSendLockObject = new object();
            onMessageToSend = new ManualResetEvent(false);

            onNetworkingStop = new ManualResetEvent(false);

            status = EUDPNetworkingStatus.STOPPED;
            statusLockObject = new object();

            listeningThread = new Task(ListeningThread, TaskCreationOptions.LongRunning);
            sendingThread = new Task(SendingThread, TaskCreationOptions.LongRunning);
            processingThread = new Task(ProcessingThread, TaskCreationOptions.LongRunning);
        }

        public void Start(int port)
        {
            lock (statusLockObject)
            {
                if (status != EUDPNetworkingStatus.STOPPED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                status = EUDPNetworkingStatus.STARTING;
            }
            udpClient = new UdpClient(port);
            StartThreads();
        }

        public void Start(IPEndPoint endPoint)
        {
            lock(statusLockObject)
            {
                if (status != EUDPNetworkingStatus.STOPPED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                status = EUDPNetworkingStatus.STARTING;
            }
            udpClient = new UdpClient(endPoint);
            StartThreads();
        }

        public void Stop()
        {
            //TODO: I don't think we should support stopping, let's add IDisposable. This function's code is incomplete/not thought through.
            lock (statusLockObject)
            {
                if(status != EUDPNetworkingStatus.RUNNING)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may stop from");
                }
                status = EUDPNetworkingStatus.STOPPING;
            }

            StopThreads();

            newMessagesReceived.Clear();
            newMessagesToSend.Clear();
        }

        private void StartThreads()
        {
            listeningThread.Start();
            sendingThread.Start();
            processingThread.Start();
        }

        private void StopThreads()
        {
            onNetworkingStop.Set();
            udpClient.Dispose();
            udpClient = null;

            listeningThread.Wait();
            sendingThread.Wait();
            processingThread.Wait();
        }

        private void ListeningThread()
        {
            //When listening, mark the class as RUNNING
            throw new NotImplementedException();
        }

        private void SendingThread()
        {
            throw new NotImplementedException();
        }

        private void ProcessingThread()
        {
            throw new NotImplementedException();
        }

        public void SendMessage(BaseMessage message)
        {
            lock (newMessagesToSendLockObject)
            {
                newMessagesToSend.Enqueue(message);
                onMessageToSend.Set();
            }
        }

        public EUDPNetworkingStatus GetStatus()
        {
            lock(statusLockObject)
            {
                return status;
            }
        }

        public string[] GetPeers()
        {
            lock (peersLockObject)
            {
                return peers.Keys.ToArray();
            }
        }

        public int CountPeers()
        {
            lock (peersLockObject)
            {
                return peers.Count;
            }
        }

        public bool HasPeer(string peerName)
        {
            lock (peersLockObject)
            {
                return peers.ContainsKey(peerName);
            }
        }

        public void ManualAddPeer(string peerName, IPEndPoint endPoint)
        {
            lock (peersLockObject)
            {
                peers.Add(peerName, endPoint);
            }
        }

        public void RemovePeer(string peerName)
        {
            lock (peersLockObject)
            {
                peers.Remove(peerName);
            }
        }

        protected byte[] SerialiseMessage(BaseMessage message)
        {
            byte[] messageBytes = message.Serialise();
            return CompressMessage(messageBytes);
        }

        protected BaseMessage DeserialiseMessage(byte[] message)
        {
            byte[] messageToDeserialise = DecompressMessage(message);
            return BaseMessage.Deserialise<BaseMessage>(messageToDeserialise);
        }

        protected byte[] CompressMessage(byte[] message)
        {
            //https://www.dotnetperls.com/compress
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(message, 0, message.Length);
                }
                return memory.ToArray();
            }
        }

        protected byte[] DecompressMessage(byte[] message)
        {
            //https://www.dotnetperls.com/decompress
            //Removed do while
            using (GZipStream stream = new GZipStream(new MemoryStream(message), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    while ((count = stream.Read(buffer, 0, size)) > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                    return memory.ToArray();
                }
            }
        }
    }
}
