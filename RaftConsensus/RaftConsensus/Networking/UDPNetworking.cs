using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Common;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Exceptions;
using TeamDecided.RaftConsensus.Networking.Interfaces;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking
{
    public class UdpNetworking : IUdpNetworking
    {
        public event EventHandler<BaseMessage> OnMessageReceived;
        private Queue<Tuple<byte[], IPEndPoint>> _newMessagesReceived;
        private object _newMessagesReceivedLockObject;
        private ManualResetEvent _onMessageReceive;

        public event EventHandler<UdpNetworkingReceiveFailureException> OnMessageReceivedFailure;
        private Queue<UdpNetworkingReceiveFailureException> _newMessageReceiveFailures;
        private object _newMessageReceiveFailuresLockObject;
        private ManualResetEvent _onMessageReceiveFailure;

        public event EventHandler<UdpNetworkingSendFailureException> OnMessageSendFailure;
        private Queue<UdpNetworkingSendFailureException> _newMessageSendFailures;
        private object _newMessageSendFailuresLockObject;
        private ManualResetEvent _onMessageSendFailure;

        public event EventHandler<string> OnNewConnectedPeer;
        private Queue<string> _newConnectedPeers;
        private object _newConnectedPeersLockObject;
        private ManualResetEvent _onNewConnectedPeer;
        private Dictionary<string, IPEndPoint> _peers;
        private object _peersLockObject;

        private Queue<BaseMessage> _newMessagesToSend;
        private object _newMessagesToSendLockObject;
        private ManualResetEvent _onMessageToSend;

        private ManualResetEvent _onNetworkingStop;

        private UdpClient _udpClient;
        private string _clientName;

        private EudpNetworkingStatus _status;
        private object _statusLockObject;

        private Thread _listeningThread;
        private Thread _sendingThread;
        private Thread _processingThread;
        private CountdownEvent _onThreadsStarted;

        private int _clientPort;
        private IPEndPoint _clientIpEndPoint;
        private bool _isRebuilding;
        private ManualResetEvent _isSocketReady;
        private object _isRebuildingLockObject;

        private bool _disposedValue = false; // To detect redundant calls

        public UdpNetworking()
        {
            _newMessagesReceived = new Queue<Tuple<byte[], IPEndPoint>>();
            _newMessagesReceivedLockObject = new object();
            _onMessageReceive = new ManualResetEvent(false);

            _newMessageReceiveFailures = new Queue<UdpNetworkingReceiveFailureException>();
            _newMessageReceiveFailuresLockObject = new object();
            _onMessageReceiveFailure = new ManualResetEvent(false);

            _newMessageSendFailures = new Queue<UdpNetworkingSendFailureException>();
            _newMessageSendFailuresLockObject = new object();
            _onMessageSendFailure = new ManualResetEvent(false);

            _newConnectedPeers = new Queue<string>();
            _newConnectedPeersLockObject = new object();
            _onNewConnectedPeer = new ManualResetEvent(false);
            _peers = new Dictionary<string, IPEndPoint>();
            _peersLockObject = new object();

            _newMessagesToSend = new Queue<BaseMessage>();
            _newMessagesToSendLockObject = new object();
            _onMessageToSend = new ManualResetEvent(false);

            _onNetworkingStop = new ManualResetEvent(false);

            _clientName = Guid.NewGuid().ToString();

            _status = EudpNetworkingStatus.Initialized;
            _statusLockObject = new object();

            _listeningThread = new Thread(new ThreadStart(ListeningThread));
            _sendingThread = new Thread(new ThreadStart(SendingThread));
            _processingThread = new Thread(new ThreadStart(ProcessingThread));

            _onThreadsStarted = new CountdownEvent(3);

            _clientPort = -1;
            _clientIpEndPoint = null;
            _isRebuilding = false;
            _isSocketReady = new ManualResetEvent(true);
            _isRebuildingLockObject = new object();
        }

        public void Start(int port)
        {
            lock (_statusLockObject)
            {
                if (_status != EudpNetworkingStatus.Initialized)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                _status = EudpNetworkingStatus.Starting;
            }
            _clientPort = port;
            _udpClient = new UdpClient(port);
            DisableIcmpUnreachable();
            StartThreads();
        }

        public void Start(IPEndPoint endPoint)
        {
            lock (_statusLockObject)
            {
                if (_status != EudpNetworkingStatus.Initialized)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                _status = EudpNetworkingStatus.Starting;
            }
            _clientIpEndPoint = endPoint;
            _udpClient = new UdpClient(endPoint);
            DisableIcmpUnreachable();
            StartThreads();
        }

        private void StartThreads()
        {
            _listeningThread.Start();
            _sendingThread.Start();
            _processingThread.Start();

            _onThreadsStarted.Wait();
        }

        private void ListeningThread()
        {
            lock (_statusLockObject)
            {
                _status = EudpNetworkingStatus.Running;
            }

            Task taskCheckingDispose = Task.Run(() =>
            {
                _onNetworkingStop.WaitOne();
            });

            _onThreadsStarted.Signal();
            while (true)
            {
                byte[] messageBytes = null;
                IPEndPoint endPoint = null;

                Task<UdpReceiveResult> result = null;
                lock (_statusLockObject)
                {
                    if (_status != EudpNetworkingStatus.Running)
                    {
                        return;
                    }
                    try
                    {
                        _isSocketReady.WaitOne();
                        result = _udpClient.ReceiveAsync();
                    }
                    catch(Exception e)
                    {
                        Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", FlattenException(e));
                        RebuildUdpClient();
                        continue;
                    }
                }

                int index;
                index = Task.WaitAny(taskCheckingDispose, result);

                if (index == 0)
                {
                    return;
                }


                if (_status != EudpNetworkingStatus.Running)
                {
                    return;
                }

                try
                {
                    _isSocketReady.WaitOne();
                    messageBytes = result.Result.Buffer;

                    endPoint = result.Result.RemoteEndPoint;

                    lock (_newMessagesReceivedLockObject)
                    {
                        _newMessagesReceived.Enqueue(new Tuple<byte[], IPEndPoint>(messageBytes, endPoint));
                        _onMessageReceive.Set();
                    }
                }
                catch (Exception e)
                {
                    Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", FlattenException(e));
                    RebuildUdpClient();
                    continue;
                }                
            }
        }

        private void SendingThread()
        {
            ManualResetEvent[] resetEvents = new ManualResetEvent[2];
            resetEvents[0] = _onNetworkingStop;
            resetEvents[1] = _onMessageToSend;

            _onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == 0) //Stopping thread
                {
                    break;
                }

                lock (_statusLockObject)
                {
                    if (_status != EudpNetworkingStatus.Running)
                    {
                        return; //The object is being disposed
                    }

                    BaseMessage message;
                    lock (_newMessagesToSendLockObject)
                    {
                        message = _newMessagesToSend.Dequeue();
                        Log(ERaftLogType.Trace, "Sending message: {0}", message);
                        if (_newMessagesToSend.Count == 0)
                        {
                            _onMessageToSend.Reset();
                        }
                    }
                    byte[] messageToSend = SerialiseMessage(message);

                    if (messageToSend.Length > 65507) //Max size of a packet supported
                    {
                        GenerateSendFailureException("Message is too large to send", message);
                        Log(ERaftLogType.Warn, "Message is too large to send", message);
                        continue;
                    }

                    IPEndPoint recipient;
                    if(message.To == null)
                    {
                        if (message.IpEndPoint == null)
                        {
                            GenerateSendFailureException("Failed to convert recipient to IPAddress", message);
                            Log(ERaftLogType.Warn, "Failed to convert recipient to IPAddress", message);
                            continue;
                        }
                        recipient = message.IpEndPoint;
                    }
                    else
                    {
                        recipient = GetPeerIpEndPoint(message.To);
                    }

                    try
                    {
                        Task<int> sendMessageTask = _udpClient.SendAsync(messageToSend, messageToSend.Length, recipient);

                        sendMessageTask.Wait();

                        if (sendMessageTask.Result <= 0)
                        {
                            GenerateSendFailureException("Failed to send message", message);
                            Log(ERaftLogType.Warn, "Failed to send message", message);
                            continue;
                        }
                        //else { sent succesfully }
                    }
                    catch (Exception e)
                    {
                        Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", FlattenException(e));
                        RebuildUdpClient();
                        continue;
                    }                     
                }
            }
        }

        private void ProcessingThread()
        {
            ManualResetEvent[] resetEvents = new ManualResetEvent[5];
            resetEvents[(int)EProcessingThreadArrayIndex.OnNetworkingStop] = _onNetworkingStop;
            resetEvents[(int)EProcessingThreadArrayIndex.OnMessageReceive] = _onMessageReceive;
            resetEvents[(int)EProcessingThreadArrayIndex.OnMessageReceiveFailure] = _onMessageReceiveFailure;
            resetEvents[(int)EProcessingThreadArrayIndex.OnMessageSendFailure] = _onMessageSendFailure;
            resetEvents[(int)EProcessingThreadArrayIndex.OnNewConnectedPeer] = _onNewConnectedPeer;

            _onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == (int)EProcessingThreadArrayIndex.OnNetworkingStop) //Stopping thread
                {
                    break;
                }

                lock (_statusLockObject)
                {
                    if (_status != EudpNetworkingStatus.Running)
                    {
                        return;
                    }

                    if (index == (int)EProcessingThreadArrayIndex.OnMessageReceive)
                    {
                        Tuple<byte[], IPEndPoint> messageToProcess;
                        lock (_newMessagesReceivedLockObject)
                        {
                            messageToProcess = _newMessagesReceived.Dequeue();

                            if (_newMessagesReceived.Count == 0)
                            {
                                _onMessageReceive.Reset();
                            }
                        }

                        byte[] newMessageByteArray = messageToProcess.Item1;
                        IPEndPoint newMessageIpEndPoint = messageToProcess.Item2;

                        BaseMessage message;
                        try
                        {
                            message = DeserialiseMessage(newMessageByteArray);
                            message = DerivedMessageProcessing(message, newMessageIpEndPoint); //This is for derived classes to do encryption, if it returns null it was consumed
                        }
                        catch (Exception e)
                        {
                            GenerateReceiveFailureException("Failed deserialising byte array", e);
                            continue;
                        }

                        Log(ERaftLogType.Trace, "Received message: {0}", message);

                        if (message == null)
                        {
                            continue;
                        }

                        lock (_peersLockObject)
                        {
                            if (!_peers.ContainsKey(message.From))
                            {
                                _peers.Add(message.From, newMessageIpEndPoint);

                                lock (_newConnectedPeersLockObject)
                                {
                                    _newConnectedPeers.Enqueue(message.From);
                                    _onNewConnectedPeer.Set();
                                }
                            }
                        }

                        OnMessageReceived?.Invoke(this, message);
                    }
                    else if (index == (int)EProcessingThreadArrayIndex.OnMessageReceiveFailure)
                    {
                        HandleMessageProcessing(_newMessageReceiveFailures, _newMessageReceiveFailuresLockObject, _onMessageReceiveFailure, OnMessageReceivedFailure);
                    }
                    else if (index == (int)EProcessingThreadArrayIndex.OnMessageSendFailure)
                    {
                        HandleMessageProcessing(_newMessageSendFailures, _newMessageSendFailuresLockObject, _onMessageSendFailure, OnMessageSendFailure);
                    }
                    else if (index == (int)EProcessingThreadArrayIndex.OnNewConnectedPeer)
                    {
                        HandleMessageProcessing(_newConnectedPeers, _newConnectedPeersLockObject, _onNewConnectedPeer, OnNewConnectedPeer);
                    }
                }
            }
        }

        protected void Log(ERaftLogType logType, string format, params object[] args)
        {
            string messagePrepend = string.Format("{0} (Method={1}) - ", _clientName, new StackFrame(1).GetMethod().Name);
            RaftLogging.Instance.Log(logType, messagePrepend + format, args);
        }

        public static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }

        private void RebuildUdpClient()
        {
            lock (_isRebuildingLockObject)
            {
                if (_isRebuilding)
                {
                    return; //It's currently rebuilding
                }
                else
                {
                    _isRebuilding = true;
                    _isSocketReady.Reset();
                }
            }

            _udpClient.Dispose();
            _udpClient = null;
            if (_clientPort != -1) //We initied this initially with a port number
            {
                _udpClient = new UdpClient(_clientPort);
            }
            else
            {
                _udpClient = new UdpClient(_clientIpEndPoint);
            }

            DisableIcmpUnreachable();

            lock (_isRebuildingLockObject)
            {
                _isRebuilding = false;
                _isSocketReady.Set();
            }
        }

        private void DisableIcmpUnreachable()
        {
            uint iocIn = 0x80000000;
            uint iocVendor = 0x18000000;
            uint sioUdpConnreset = iocIn | iocVendor | 12;
            _udpClient.Client.IOControl((int)sioUdpConnreset, new byte[] { Convert.ToByte(false) }, null);
        }

        protected IPEndPoint GetPeerIpEndPoint(string name)
        {
            lock (_peersLockObject)
            {
                return _peers[name];
            }
        }

        protected void GenerateReceiveFailureException(string message, Exception innerException)
        {
            lock (_newMessageReceiveFailuresLockObject)
            {
                Log(ERaftLogType.Warn, "Receive failure exception: {0}", message);
                Log(ERaftLogType.Trace, FlattenException(innerException));
                _newMessageReceiveFailures.Enqueue(new UdpNetworkingReceiveFailureException(message, innerException));
                _onMessageReceiveFailure.Set();
            }
        }

        protected void GenerateSendFailureException(string stringMessage, BaseMessage message)
        {
            lock (_newMessageSendFailuresLockObject)
            {
                Log(ERaftLogType.Warn, "Sending failure exception: {0}", message);
                _newMessageSendFailures.Enqueue(new UdpNetworkingSendFailureException(stringMessage, message));
                _onMessageSendFailure.Set();
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
            if (_status != EudpNetworkingStatus.Running)
            {
                if(_status == EudpNetworkingStatus.Stopped)
                {
                    return;
                }
                throw new InvalidOperationException("Library is currently not in a state it may send in"); ;
            }

            lock (_newMessagesToSendLockObject)
            {
                Log(ERaftLogType.Trace, "Enqueuing message to be send, contents: {0}", message);
                _newMessagesToSend.Enqueue(message);
                _onMessageToSend.Set();
            }
        }

        public EudpNetworkingStatus GetStatus()
        {
            lock (_statusLockObject)
            {
                return _status;
            }
        }

        public string[] GetPeers()
        {
            lock (_statusLockObject)
            {
                if (_status == EudpNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (_peersLockObject)
                {
                    return _peers.Keys.ToArray();
                }
            }
        }

        public int CountPeers()
        {
            lock (_statusLockObject)
            {
                if (_status == EudpNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (_peersLockObject)
                {
                    return _peers.Count;
                }
            }
        }

        public bool HasPeer(string peerName)
        {
            lock (_statusLockObject)
            {
                if (_status == EudpNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (_peersLockObject)
                {
                    return _peers.ContainsKey(peerName);
                }
            }
        }

        public void ManualAddPeer(string peerName, IPEndPoint endPoint)
        {
            if (_status == EudpNetworkingStatus.Stopped)
            {
                throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
            }
            lock (_peersLockObject)
            {
                _peers.Add(peerName, endPoint);
            }
        }

        public IPEndPoint GetIpFromName(string peerName)
        {
            IPEndPoint toReturn;
            lock(_peersLockObject)
            {
                _peers.TryGetValue(peerName, out toReturn);
            }

            return toReturn;
        }

        public void RemovePeer(string peerName)
        {
            lock (_statusLockObject)
            {
                if (_status == EudpNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in"); ;
                }
                lock (_peersLockObject)
                {
                    _peers.Remove(peerName);
                }
            }
        }

        public string GetClientName()
        {
            return _clientName;
        }

        public void SetClientName(string clientName)
        {
            this._clientName = clientName;
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
            if (!_disposedValue)
            {
                if (disposing)
                {
                    EudpNetworkingStatus previousStatus;
                    lock (_statusLockObject)
                    {
                        previousStatus = _status;
                        _status = EudpNetworkingStatus.Stopped;

                        _onNetworkingStop.Set();
                    }
                    if(previousStatus == EudpNetworkingStatus.Running)
                    {
                        _listeningThread.Join();
                        _sendingThread.Join();
                        _processingThread.Join();
                    }
                    if (_udpClient != null)
                    {
                        _udpClient.Dispose();
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
