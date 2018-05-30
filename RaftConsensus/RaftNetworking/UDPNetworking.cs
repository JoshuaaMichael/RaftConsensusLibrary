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
        private Queue<Tuple<byte[], IPEndPoint>> newMessagesReceived;
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
        private string clientName;

        private EUDPNetworkingStatus status;
        private object statusLockObject;

        private Task listeningThread;
        private Task sendingThread;
        private Task processingThread;
        private CountdownEvent onThreadsStarted;

        private int clientPort;
        private IPEndPoint clientIPEndPoint;
        private bool isRebuilding;
        private ManualResetEvent isSocketReady;
        private object isRebuildingLockObject;

        private bool disposedValue = false; // To detect redundant calls

        public UDPNetworking()
        {
            newMessagesReceived = new Queue<Tuple<byte[], IPEndPoint>>();
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

            clientName = Guid.NewGuid().ToString();

            status = EUDPNetworkingStatus.INITIALIZED;
            statusLockObject = new object();

            listeningThread = new Task(ListeningThread, TaskCreationOptions.LongRunning);
            sendingThread = new Task(SendingThread, TaskCreationOptions.LongRunning);
            processingThread = new Task(ProcessingThread, TaskCreationOptions.LongRunning);

            onThreadsStarted = new CountdownEvent(3);

            clientPort = -1;
            clientIPEndPoint = null;
            isRebuilding = false;
            isSocketReady = new ManualResetEvent(true);
            isRebuildingLockObject = new object();
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
            clientPort = port;
            udpClient = new UdpClient(port);
            StartThreads();
        }

        public void Start(IPEndPoint endPoint)
        {
            lock (statusLockObject)
            {
                if (status != EUDPNetworkingStatus.INITIALIZED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                status = EUDPNetworkingStatus.STARTING;
            }
            clientIPEndPoint = endPoint;
            udpClient = new UdpClient(endPoint);
            StartThreads();
        }

        private void StartThreads()
        {
            listeningThread.Start();
            sendingThread.Start();
            processingThread.Start();

            onThreadsStarted.Wait();
        }

        private void ListeningThread()
        {
            lock (statusLockObject)
            {
                status = EUDPNetworkingStatus.RUNNING;
            }

            Task taskCheckingDispose = Task.Run(() =>
            {
                onNetworkingStop.WaitOne();
            });

            onThreadsStarted.Signal();
            while (true)
            {
                byte[] messageBytes = null;
                IPEndPoint endPoint = null;

                Task<UdpReceiveResult> result = null;
                lock (statusLockObject)
                {
                    if (status != EUDPNetworkingStatus.RUNNING)
                    {
                        return;
                    }
                    try
                    {
                        isSocketReady.WaitOne();
                        result = udpClient.ReceiveAsync();
                    }
                    catch
                    {
                        RebuildUDPClient();
                        continue;
                    }
                }

                int index;
                index = Task.WaitAny(taskCheckingDispose, result);

                if (index == 0)
                {
                    return;
                }


                if (status != EUDPNetworkingStatus.RUNNING)
                {
                    return;
                }

                try
                {
                    isSocketReady.WaitOne();
                    messageBytes = result.Result.Buffer;
                }
                catch
                {
                    RebuildUDPClient();
                    continue;
                }
                endPoint = result.Result.RemoteEndPoint;

                lock (newMessagesReceivedLockObject)
                {
                    newMessagesReceived.Enqueue(new Tuple<byte[], IPEndPoint>(messageBytes, endPoint));
                    onMessageReceive.Set();
                }
            }
        }

        private void RebuildUDPClient()
        {
            lock(isRebuildingLockObject)
            {
                if(isRebuilding)
                {
                    return; //It's currently rebuilding
                }
                else
                {
                    isRebuilding = true;
                    isSocketReady.Reset();
                }
            }

            udpClient.Dispose();
            udpClient = null;
            if(clientPort != -1) //We initied this initially with a port number
            {
                udpClient = new UdpClient(clientPort);
            }
            else
            {
                udpClient = new UdpClient(clientIPEndPoint);
            }

            lock(isRebuildingLockObject)
            {
                isRebuilding = false;
                isSocketReady.Set();
            }
        }

        private void SendingThread()
        {
            ManualResetEvent[] resetEvents = new ManualResetEvent[2];
            resetEvents[0] = onNetworkingStop;
            resetEvents[1] = onMessageToSend;

            onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == 0) //Stopping thread
                {
                    break;
                }

                lock (statusLockObject)
                {
                    if (status != EUDPNetworkingStatus.RUNNING)
                    {
                        return; //The object is being disposed
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
                        lock (newMessageSendFailuresLockObject)
                        {
                            newMessageSendFailures.Enqueue(new UDPNetworkingSendFailureException("Message is too large to send", message));
                            onMessageSendFailure.Set();
                        }
                        continue;
                    }

                    IPEndPoint recipient;
                    if(message.To == null)
                    {
                        if (message.IPEndPoint == null)
                        {
                            GenerateSendFailureException("Failed to convert recipient to IPAddress", message);
                            continue;
                        }
                        recipient = message.IPEndPoint;
                    }
                    else
                    {
                        recipient = GetPeerIPEndPoint(message.To);
                    }

                    try
                    {
                        Task<int> sendMessageTask = udpClient.SendAsync(messageToSend, messageToSend.Length, recipient);

                        sendMessageTask.Wait();

                        if (sendMessageTask.Result <= 0)
                        {
                            GenerateSendFailureException("Failed to send message", message);
                            continue;
                        }
                        //else { sent succesfully }
                    }
                    catch
                    {
                        RebuildUDPClient();
                        continue;
                    }                     
                }
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

            onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == (int)EProcessingThreadArrayIndex.ON_NETWORKING_STOP) //Stopping thread
                {
                    break;
                }

                lock (statusLockObject)
                {
                    if (status != EUDPNetworkingStatus.RUNNING)
                    {
                        return;
                    }

                    if (index == (int)EProcessingThreadArrayIndex.ON_MESSAGE_RECEIVE)
                    {
                        Tuple<byte[], IPEndPoint> messageToProcess;
                        lock (newMessagesReceivedLockObject)
                        {
                            messageToProcess = newMessagesReceived.Dequeue();

                            if (newMessagesReceived.Count == 0)
                            {
                                onMessageReceive.Reset();
                            }
                        }

                        byte[] newMessageByteArray = messageToProcess.Item1;
                        IPEndPoint newMessageIPEndPoint = messageToProcess.Item2;

                        BaseMessage message;
                        try
                        {
                            message = DeserialiseMessage(newMessageByteArray);
                            message = DerivedMessageProcessing(message, newMessageIPEndPoint); //This is for derived classes to do encryption, if it returns null it was consumed
                        }
                        catch (Exception e)
                        {
                            GenerateReceiveFailureException("Failed deserialising byte array", e);
                            continue;
                        }

                        if (message == null)
                        {
                            continue;
                        }

                        lock (peersLockObject)
                        {
                            if (!peers.ContainsKey(message.From))
                            {
                                peers.Add(message.From, newMessageIPEndPoint);

                                lock (newConnectedPeersLockObject)
                                {
                                    newConnectedPeers.Enqueue(message.From);
                                    onNewConnectedPeer.Set();
                                }
                            }
                        }

                        OnMessageReceived?.Invoke(this, message);
                    }
                    else if (index == (int)EProcessingThreadArrayIndex.ON_MESSAGE_RECEIVE_FAILURE)
                    {
                        HandleMessageProcessing(newMessageReceiveFailures, newMessageReceiveFailuresLockObject, onMessageReceiveFailure, OnMessageReceivedFailure);
                    }
                    else if (index == (int)EProcessingThreadArrayIndex.ON_MESSAGE_SEND_FAILURE)
                    {
                        HandleMessageProcessing(newMessageSendFailures, newMessageSendFailuresLockObject, onMessageSendFailure, OnMessageSendFailure);
                    }
                    else if (index == (int)EProcessingThreadArrayIndex.ON_NEW_CONNECTED_PEER)
                    {
                        HandleMessageProcessing(newConnectedPeers, newConnectedPeersLockObject, onNewConnectedPeer, OnNewConnectedPeer);
                    }
                }
            }
        }

        protected IPEndPoint GetPeerIPEndPoint(string name)
        {
            lock (peersLockObject)
            {
                return peers[name];
            }
        }

        protected void GenerateReceiveFailureException(string message, Exception innerException)
        {
            lock (newMessageReceiveFailuresLockObject)
            {
                newMessageReceiveFailures.Enqueue(new UDPNetworkingReceiveFailureException(message, innerException));
                onMessageReceiveFailure.Set();
            }
        }

        protected void GenerateSendFailureException(string stringMessage, BaseMessage message)
        {
            lock (newMessageSendFailuresLockObject)
            {
                newMessageSendFailures.Enqueue(new UDPNetworkingSendFailureException(stringMessage, message));
                onMessageSendFailure.Set();
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

            if(DerivedHandleMessageProcessing(messageToProcess)) //If message was consumed by derived class
            {
                return;
            }
            eventHandler?.Invoke(this, messageToProcess);
        }

        protected virtual bool DerivedHandleMessageProcessing(object messageToProcess)
        {
            return false;
        }

        protected virtual BaseMessage DerivedMessageProcessing(BaseMessage message, IPEndPoint ipEndPoint)
        {
            return message;
        }

        public virtual void SendMessage(BaseMessage message)
        {
            if (status != EUDPNetworkingStatus.RUNNING)
            {
                if(status == EUDPNetworkingStatus.STOPPED)
                {
                    return;
                }
                throw new InvalidOperationException("Library is currently not in a state it may send in"); ;
            }

            lock (newMessagesToSendLockObject)
            {
                newMessagesToSend.Enqueue(message);
                onMessageToSend.Set();
            }
        }

        public EUDPNetworkingStatus GetStatus()
        {
            lock (statusLockObject)
            {
                return status;
            }
        }

        public string[] GetPeers()
        {
            lock (statusLockObject)
            {
                if (status == EUDPNetworkingStatus.STOPPED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (peersLockObject)
                {
                    return peers.Keys.ToArray();
                }
            }
        }

        public int CountPeers()
        {
            lock (statusLockObject)
            {
                if (status == EUDPNetworkingStatus.STOPPED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (peersLockObject)
                {
                    return peers.Count;
                }
            }
        }

        public bool HasPeer(string peerName)
        {
            lock (statusLockObject)
            {
                if (status == EUDPNetworkingStatus.STOPPED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (peersLockObject)
                {
                    return peers.ContainsKey(peerName);
                }
            }
        }

        public void ManualAddPeer(string peerName, IPEndPoint endPoint)
        {
            if (status == EUDPNetworkingStatus.STOPPED)
            {
                throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
            }
            lock (peersLockObject)
            {
                peers.Add(peerName, endPoint);
            }
        }

        public IPEndPoint GetIPFromName(string peerName)
        {
            IPEndPoint toReturn;
            lock(peersLockObject)
            {
                peers.TryGetValue(peerName, out toReturn);
            }

            return toReturn;
        }

        public void RemovePeer(string peerName)
        {
            lock (statusLockObject)
            {
                if (status == EUDPNetworkingStatus.STOPPED)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (peersLockObject)
                {
                    peers.Remove(peerName);
                }
            }
        }

        public string GetClientName()
        {
            return clientName;
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
                    EUDPNetworkingStatus previousStatus;
                    lock (statusLockObject)
                    {
                        previousStatus = status;
                        status = EUDPNetworkingStatus.STOPPED;

                        onNetworkingStop.Set();
                    }
                    if(previousStatus == EUDPNetworkingStatus.RUNNING)
                    {
                        listeningThread.Wait();
                        sendingThread.Wait();
                        processingThread.Wait();
                    }
                    if (udpClient != null)
                    {
                        udpClient.Dispose();
                    }
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
