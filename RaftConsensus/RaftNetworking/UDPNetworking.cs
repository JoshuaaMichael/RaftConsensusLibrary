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
    public class UDPNetworking : IUDPNetworking
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

        private bool disposedValue = false; // To detect redundant calls

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

            status = EUDPNetworkingStatus.INITIALIZED;
            statusLockObject = new object();

            listeningThread = new Task(ListeningThread, TaskCreationOptions.LongRunning);
            sendingThread = new Task(SendingThread, TaskCreationOptions.LongRunning);
            processingThread = new Task(ProcessingThread, TaskCreationOptions.LongRunning);
        }

        public void Start(int port)
        {
            lock (statusLockObject)
            {
                if (status != EUDPNetworkingStatus.INITIALIZED)
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
                if (status != EUDPNetworkingStatus.INITIALIZED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                status = EUDPNetworkingStatus.STARTING;
            }
            udpClient = new UdpClient(endPoint);
            StartThreads();
        }

        private void StartThreads()
        {
            listeningThread.Start();
            sendingThread.Start();
            processingThread.Start();
        }

        private void ListeningThread()
        {
            lock(statusLockObject)
            {
                status = EUDPNetworkingStatus.RUNNING;
            }

            while (true)
            {
                BaseMessage message = null;
                byte[] messageBytes;
                IPEndPoint endPoint = null;

                try
                {
                    Task<UdpReceiveResult> result = udpClient.ReceiveAsync();
                    result.Wait();
                    messageBytes = result.Result.Buffer;
                    endPoint = result.Result.RemoteEndPoint;
                }
                catch (ObjectDisposedException e)
                {
                    //There isn't a way to abort the thread, so it's done by disposing of the socket
                    lock (statusLockObject)
                    {
                        if(status != EUDPNetworkingStatus.STOPPING)
                        {
                            throw e;
                        }
                    }
                    return;
                }
                
                try
                {
                    message = DeserialiseMessage(messageBytes);
                }
                catch (Exception e)
                {
                    lock(newMessageReceiveFailuresLockObject)
                    {
                        newMessageReceiveFailures.Enqueue(new UDPNetworkingReceiveFailureException("Failed deserialising byte array", e));
                        onMessageReceiveFailure.Set();
                    }
                    continue;
                }

                lock (peersLockObject)
                {
                    if (!peers.ContainsKey(message.From))
                    {
                        peers.Add(message.From, endPoint);

                        lock(newConnectedPeersLockObject)
                        {
                            newConnectedPeers.Enqueue(message.From);
                            onNewConnectedPeer.Set();
                        }
                    }
                }
                lock (newMessagesReceivedLockObject)
                {
                    newMessagesReceived.Enqueue(message);
                    onMessageReceive.Set();
                }
            }
        }

        private void SendingThread()
        {
            ManualResetEvent[] resetEvents = new ManualResetEvent[2];
            resetEvents[0] = onNetworkingStop;
            resetEvents[1] = onMessageToSend;

            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == 0) //Stopping thread
                {
                    break;
                }

                BaseMessage message;
                lock (newMessagesToSendLockObject)
                {
                    message = newMessagesToSend.Dequeue();
                    if (newMessagesToSend.Count == 0)
                    {
                        onMessageToSend.Reset();
                    }
                }
                byte[] messageToSend = SerialiseMessage(message);

                if (messageToSend.Length > 65507) //Max size of a packet supported
                {
                    lock(newMessageSendFailuresLockObject)
                    {
                        newMessageSendFailures.Enqueue(new UDPNetworkingSendFailureException("Message is too large to send", message));
                        onMessageSendFailure.Set();
                    }
                    continue;
                }

                IPEndPoint recipient;
                lock (peersLockObject)
                {
                    recipient = peers[message.To];
                }

                Task<int> sendMessageTask = udpClient.SendAsync(messageToSend, messageToSend.Length, recipient);
                sendMessageTask.Wait();

                if (sendMessageTask.Result <= 0)
                {
                    lock (newMessageSendFailuresLockObject)
                    {
                        newMessageSendFailures.Enqueue(new UDPNetworkingSendFailureException("Failed to send message", message));
                        onMessageSendFailure.Set();
                    }
                    continue;
                } //else { sent succesfully }
            }
        }

        private void ProcessingThread()
        {
            ManualResetEvent[] resetEvents = new ManualResetEvent[5];
            resetEvents[(int)EProcessingThreadArrayIndex.ON_NETWORKING_STOP] = onNetworkingStop;
            resetEvents[(int)EProcessingThreadArrayIndex.ON_MESSAGE_RECEIVE] = onMessageReceive;
            resetEvents[(int)EProcessingThreadArrayIndex.ON_MESSAGE_RECEIVE_FAILURE] = onMessageReceiveFailure;
            resetEvents[(int)EProcessingThreadArrayIndex.ON_MESSAGE_SEND_FAILURE] = onMessageSendFailure;
            resetEvents[(int)EProcessingThreadArrayIndex.ON_NEW_CONNECTED_PEER] = onNewConnectedPeer;

            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == (int)EProcessingThreadArrayIndex.ON_NETWORKING_STOP) //Stopping thread
                {
                    break;
                }
                else if(index == (int)EProcessingThreadArrayIndex.ON_MESSAGE_RECEIVE)
                {
                    HandleMessageProcessing(newMessagesReceived, newMessagesReceivedLockObject, onMessageReceive, OnMessageReceived);
                }
                else if(index == (int)EProcessingThreadArrayIndex.ON_MESSAGE_RECEIVE_FAILURE)
                {
                    HandleMessageProcessing(newMessageReceiveFailures, newMessageReceiveFailuresLockObject, onMessageReceiveFailure, OnMessageReceivedFailure);
                }
                else if (index == (int)EProcessingThreadArrayIndex.ON_MESSAGE_SEND_FAILURE)
                {
                    HandleMessageProcessing(newMessageSendFailures, newMessageSendFailuresLockObject, onMessageSendFailure, OnMessageSendFailure);
                }
                else if(index == (int)EProcessingThreadArrayIndex.ON_NEW_CONNECTED_PEER)
                {
                    HandleMessageProcessing(newConnectedPeers, newConnectedPeersLockObject, onNewConnectedPeer, OnNewConnectedPeer);
                }
            }
        }

        private void HandleMessageProcessing<T>(Queue<T> queue, object lockObject, ManualResetEvent manualResetEvent, EventHandler<T> eventHandler)
        {
            T messageToProcess;
            lock (lockObject)
            {
                messageToProcess = queue.Dequeue();
                if (queue.Count == 0)
                {
                    manualResetEvent.Reset();
                }
            }
            eventHandler(this, messageToProcess);
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
            byte[] messageBytes = message.Serialize();
            return CompressMessage(messageBytes);
        }

        protected BaseMessage DeserialiseMessage(byte[] message)
        {
            byte[] messageToDeserialise = DecompressMessage(message);
            return BaseMessage.Deserialize<BaseMessage>(messageToDeserialise);
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

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    onNetworkingStop.Set();
                    udpClient.Dispose();
                    udpClient = null;

                    listeningThread.Wait();
                    sendingThread.Wait();
                    processingThread.Wait();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
