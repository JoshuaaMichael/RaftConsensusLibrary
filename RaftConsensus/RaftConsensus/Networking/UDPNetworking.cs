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
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Exceptions;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Interfaces;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking
{
    public class UDPNetworking : IUDPNetworking
    {
        public event EventHandler<BaseMessage> OnMessageReceived;
        private readonly Queue<Tuple<byte[], IPEndPoint>> _newMessagesReceived;
        private readonly object _newMessagesReceivedLockObject;
        private readonly ManualResetEvent _onMessageReceive;

        public event EventHandler<UdpNetworkingReceiveFailureException> OnMessageReceivedFailure;
        public event EventHandler<UdpNetworkingSendFailureException> OnMessageSendFailure;

        public event EventHandler<string> OnNewConnectedPeer;

        protected readonly NodeIPDictionary _nodeIPs;

        private readonly Queue<BaseMessage> _newMessagesToSend;
        private readonly object _newMessagesToSendLockObject;
        private readonly ManualResetEvent _onMessageToSend;

        private readonly ManualResetEvent _onNetworkingStop;

        private UdpClient _udpClient;
        private string _clientName;

        private EUDPNetworkingStatus _status;
        private readonly object _statusLockObject;

        private readonly Thread _listeningThread;
        private readonly Thread _sendingThread;
        private readonly Thread _processingThread;
        private readonly CountdownEvent _onThreadsStarted;

        private int _clientPort;
        private IPEndPoint _clientIpEndPoint;
        private bool _isRebuilding;
        private readonly ManualResetEvent _isSocketReady;
        private readonly object _isRebuildingLockObject;

        private const int MaxPacketSize = 65507;

        private bool _disposedValue; // To detect redundant calls

        public UDPNetworking()
        {
            _newMessagesReceived = new Queue<Tuple<byte[], IPEndPoint>>();
            _newMessagesReceivedLockObject = new object();
            _onMessageReceive = new ManualResetEvent(false);

            _nodeIPs = new NodeIPDictionary();

            _newMessagesToSend = new Queue<BaseMessage>();
            _newMessagesToSendLockObject = new object();
            _onMessageToSend = new ManualResetEvent(false);

            _onNetworkingStop = new ManualResetEvent(false);

            _clientName = Guid.NewGuid().ToString();

            _status = EUDPNetworkingStatus.Initialized;
            _statusLockObject = new object();

            _listeningThread = new Thread(ListeningThread);
            _sendingThread = new Thread(SendingThread);
            _processingThread = new Thread(ProcessingThread);

            _onThreadsStarted = new CountdownEvent(3);

            _clientPort = -1;
            _clientIpEndPoint = null;
            _isRebuilding = false;
            _isSocketReady = new ManualResetEvent(true);
            _isRebuildingLockObject = new object();
        }

        public void Start(int port)
        {
            StartCommon(port);
        }

        public void Start(IPEndPoint endPoint)
        {
            StartCommon(-1, endPoint);
        }

        private void StartCommon(int port = -1, IPEndPoint ipEndPoint = null)
        {
            lock (_statusLockObject)
            {
                if (_status != EUDPNetworkingStatus.Initialized)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                _status = EUDPNetworkingStatus.Starting;
            }

            if (ipEndPoint != null) //We're initialising using IPEndPoint
            {
                _clientIpEndPoint = ipEndPoint;
                _udpClient = new UdpClient(ipEndPoint);
            }
            else
            {
                _clientPort = port;
                _udpClient = new UdpClient(port);
            }

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
                _status = EUDPNetworkingStatus.Running;
            }

            Task taskCheckingDispose = Task.Run(() =>
            {
                _onNetworkingStop.WaitOne();
            });

            _onThreadsStarted.Signal();
            while (true)
            {
                Task<UdpReceiveResult> result;
                lock (_statusLockObject)
                {
                    if (_status != EUDPNetworkingStatus.Running)
                    {
                        return;
                    }
                }

                try
                {
                    _isSocketReady.WaitOne();
                    result = _udpClient.ReceiveAsync();
                }
                catch (Exception e)
                {
                    Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", RaftLogging.FlattenException(e));
                    RebuildUdpClient();
                    continue;
                }

                var index = Task.WaitAny(taskCheckingDispose, result);

                if (index == 0 || _status != EUDPNetworkingStatus.Running)
                {
                    return;
                }

                try
                {
                    _isSocketReady.WaitOne();
                    byte[] messageBytes = result.Result.Buffer;
                    IPEndPoint endPoint = result.Result.RemoteEndPoint;

                    lock (_newMessagesReceivedLockObject)
                    {
                        _newMessagesReceived.Enqueue(new Tuple<byte[], IPEndPoint>(messageBytes, endPoint));
                        _onMessageReceive.Set();
                    }
                }
                catch (Exception e)
                {
                    Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", RaftLogging.FlattenException(e));
                    RebuildUdpClient();
                }
            }
        }

        private void SendingThread()
        {
            WaitHandle[] resetEvents = new WaitHandle[2];
            resetEvents[0] = _onNetworkingStop;
            resetEvents[1] = _onMessageToSend;

            _onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == 0) //Stopping thread
                {
                    return;
                }

                lock (_statusLockObject)
                {
                    if (_status != EUDPNetworkingStatus.Running)
                    {
                        return;
                    }
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

                if (messageToSend.Length > MaxPacketSize)
                {
                    GenerateSendFailureException("Message is too large to send", message);
                    continue;
                }

                if (message.To != null)
                {
                    message.IPEndPoint = _nodeIPs[message.To];
                }

                if (message.IPEndPoint == null)
                {
                    GenerateSendFailureException("Failed to convert recipient to IPAddress", message);
                    continue;
                }

                try
                {
                    Task<int> sendMessageTask = _udpClient.SendAsync(messageToSend, messageToSend.Length, message.IPEndPoint);
                    sendMessageTask.Wait();

                    if (sendMessageTask.Result > 0) continue;
                    GenerateSendFailureException("Failed to send message", message);
                }
                catch (Exception e)
                {
                    Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", RaftLogging.FlattenException(e));
                    RebuildUdpClient();
                }
            }
        }

        private void ProcessingThread()
        {
            WaitHandle[] resetEvents = new WaitHandle[2];
            resetEvents[0] = _onNetworkingStop;
            resetEvents[1] = _onMessageReceive;

            _onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == 0) //Stopping thread
                {
                    return;
                }

                lock (_statusLockObject)
                {
                    if (_status != EUDPNetworkingStatus.Running)
                    {
                        return;
                    }
                }

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
                    Log(ERaftLogType.Trace, "Received message, pre processing: {0}", message);
                    message = DerivedMessageProcessing(message, newMessageIpEndPoint); //This is for derived classes to do encryption, if it returns null it was consumed
                    Log(ERaftLogType.Trace, "Received message, post processing: {0}", message);
                }
                catch (Exception e)
                {
                    GenerateReceiveFailureException("Failed deserialising byte array", e);
                    continue;
                }

                if (message == null)
                {
                    Log(ERaftLogType.Trace, "Message was consumed during post processing");
                    continue;
                }

                if (_nodeIPs.AddOrUpdateNode(message.From, newMessageIpEndPoint))
                {
                    OnNewConnectedPeer?.Invoke(this, message.From);
                }

                OnMessageReceived?.Invoke(this, message);
            }
        }

        protected void Log(ERaftLogType logType, string format, params object[] args)
        {
            string messagePrepend = string.Format("{0} (Method={1}) - ", _clientName, new StackFrame(1).GetMethod().Name);
            RaftLogging.Instance.Log(logType, messagePrepend + format, args);
        }

        private void RebuildUdpClient()
        {
            lock (_isRebuildingLockObject)
            {
                if (_isRebuilding)
                {
                    return; //It's currently rebuilding
                }
                _isRebuilding = true;
                _isSocketReady.Reset();
            }

            _udpClient.Dispose();
            _udpClient = null;
            _udpClient = _clientPort != -1 ? new UdpClient(_clientPort) : new UdpClient(_clientIpEndPoint);

            DisableIcmpUnreachable();

            lock (_isRebuildingLockObject)
            {
                _isRebuilding = false;
                _isSocketReady.Set();
            }
        }

        private void DisableIcmpUnreachable()
        {
            const uint iocIn = 0x80000000;
            const uint iocVendor = 0x18000000;
            const uint sioUdpConnreset = iocIn | iocVendor | 12;
            _udpClient.Client.IOControl(unchecked((int)sioUdpConnreset), new[] { Convert.ToByte(false) }, null);
        }

        protected void GenerateReceiveFailureException(string message, Exception innerException)
        {
            Log(ERaftLogType.Warn, "Receive failure exception: {0}", message);
            Log(ERaftLogType.Trace, RaftLogging.FlattenException(innerException));
            OnMessageReceivedFailure?.Invoke(this, new UdpNetworkingReceiveFailureException(message, innerException));
        }

        protected void GenerateSendFailureException(string stringMessage, BaseMessage message)
        {
            Log(ERaftLogType.Warn, "Sending failure error message: {0}", stringMessage);
            Log(ERaftLogType.Warn, "Sending failure message contents: {0}", message);
            OnMessageSendFailure?.Invoke(this, new UdpNetworkingSendFailureException(stringMessage, message));
        }

        protected virtual BaseMessage DerivedMessageProcessing(BaseMessage message, IPEndPoint ipEndPoint)
        {
            return message;
        }

        public virtual void SendMessage(BaseMessage message)
        {
            if (_status != EUDPNetworkingStatus.Running)
            {
                if (_status == EUDPNetworkingStatus.Stopped)
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

        public EUDPNetworkingStatus GetStatus()
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
                if (_status == EUDPNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may get peers in"); ;
                }

                return _nodeIPs.GetNodes();
            }
        }

        public int CountPeers()
        {
            lock (_statusLockObject)
            {
                if (_status == EUDPNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may count peers in"); ;
                }

                return _nodeIPs.Count;
            }
        }

        public bool HasPeer(string peerName)
        {
            lock (_statusLockObject)
            {
                if (_status == EUDPNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may check for peers in"); ;
                }

                return _nodeIPs.HasNode(peerName);
            }
        }

        public void ManualAddPeer(string peerName, IPEndPoint endPoint)
        {
            if (_status == EUDPNetworkingStatus.Stopped)
            {
                throw new InvalidOperationException("Library is currently not in a state it may add a peer in"); ;
            }

            _nodeIPs.AddOrUpdateNode(peerName, endPoint);
        }

        public IPEndPoint GetIPFromName(string peerName)
        {
            return _nodeIPs[peerName];
        }

        public void RemovePeer(string peerName)
        {
            lock (_statusLockObject)
            {
                if (_status == EUDPNetworkingStatus.Stopped)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may remove a peer in"); ;
                }

                _nodeIPs.RemoveNode(peerName);
            }
        }

        public string GetClientName()
        {
            return _clientName;
        }

        public void SetClientName(string clientName)
        {
            _clientName = clientName;
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
                    EUDPNetworkingStatus previousStatus;
                    lock (_statusLockObject)
                    {
                        previousStatus = _status;
                        _status = EUDPNetworkingStatus.Stopped;

                        _onNetworkingStop.Set();
                    }
                    if (previousStatus == EUDPNetworkingStatus.Running)
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
