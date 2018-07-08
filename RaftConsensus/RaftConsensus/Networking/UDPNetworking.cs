using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
        public event EventHandler<UdpNetworkingReceiveFailureException> OnMessageReceivedFailure;
        public event EventHandler<UdpNetworkingSendFailureException> OnMessageSendFailure;
        public event EventHandler<string> OnNewConnectedPeer;

        private readonly RaftPCQueue<Tuple<byte[], IPEndPoint>> _newMessagesReceived;
        private readonly RaftPCQueue<BaseMessage> _newMessagesToSend;

        private readonly ManualResetEvent _onNetworkingStop;

        protected readonly RaftUDPClient UDPClient;
        public string ClientName { get; set; }

        private EUDPNetworkingStatus _status;
        private readonly object _statusLockObject;

        private readonly Thread _listeningThread;
        private readonly Thread _sendingThread;
        private readonly Thread _processingThread;
        private readonly CountdownEvent _onThreadsStarted;

        protected bool DisposedValue; // To detect redundant calls

        public UDPNetworking()
        {
            _newMessagesReceived = new RaftPCQueue<Tuple<byte[], IPEndPoint>>();
            _newMessagesToSend = new RaftPCQueue<BaseMessage>();

            _onNetworkingStop = new ManualResetEvent(false);

            UDPClient = new RaftUDPClient();
            ClientName = Guid.NewGuid().ToString();

            _status = EUDPNetworkingStatus.Initialized;
            _statusLockObject = new object();

            _listeningThread = new Thread(ListeningThread);
            _sendingThread = new Thread(SendingThread);
            _processingThread = new Thread(ProcessingThread);
            _onThreadsStarted = new CountdownEvent(3);
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

            if (ipEndPoint == null)
            {
                UDPClient.Start(port);
            }
            else
            {
                UDPClient.Start(ipEndPoint);
            }

            _listeningThread.Start();
            _sendingThread.Start();
            _processingThread.Start();

            _onThreadsStarted.Wait();

            lock (_statusLockObject)
            {
                _status = EUDPNetworkingStatus.Running;
            }
        }

        public virtual void SendMessage(BaseMessage message)
        {
            if (_status != EUDPNetworkingStatus.Running)
            {
                if (_status == EUDPNetworkingStatus.Stopped)
                {
                    return;
                }
                throw new InvalidOperationException("Library is currently not in a state it may send in");
            }

            Log(ERaftLogType.Trace, "Enqueuing message to be send, contents: {0}", message);
            _newMessagesToSend.Enqueue(message);
        }

        private void ListeningThread()
        {
            Task taskCheckingDispose = Task.Run(() =>
            {
                _onNetworkingStop.WaitOne();
            });

            _onThreadsStarted.Signal();
            while (true)
            {
                try
                {
                    Task<UdpReceiveResult> result = UDPClient.ReceiveAsync();

                    if (Task.WaitAny(taskCheckingDispose, result) == 0)
                    {
                        return;
                    }

                    _newMessagesReceived.Enqueue(new Tuple<byte[], IPEndPoint>(result.Result.Buffer, result.Result.RemoteEndPoint));
                }
                catch (Exception e)
                {
                    Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", RaftLogging.FlattenException(e));
                }
            }
        }

        private void SendingThread()
        {
            WaitHandle[] resetEvents = new WaitHandle[2];
            resetEvents[0] = _onNetworkingStop;
            resetEvents[1] = _newMessagesToSend.Flag;

            _onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == 0)
                {
                    return;
                }

                BaseMessage message = _newMessagesToSend.Dequeue();
                Log(ERaftLogType.Trace, "Sending message: {0}", message);

                if (!UDPClient.Send(message))
                {
                    GenerateSendFailureException(UDPClient.GetSendFailureException());
                }
            }
        }

        private void ProcessingThread()
        {
            WaitHandle[] resetEvents = new WaitHandle[2];
            resetEvents[0] = _onNetworkingStop;
            resetEvents[1] = _newMessagesReceived.Flag;

            _onThreadsStarted.Signal();
            int index;
            while ((index = WaitHandle.WaitAny(resetEvents)) != -1)
            {
                if (index == 0) //Stopping thread
                {
                    return;
                }

                try
                {
                    Tuple<byte[], IPEndPoint> messageToProcess = _newMessagesReceived.Dequeue();
                    BaseMessage message = BaseMessage.Deserialize(messageToProcess.Item1);
                    Log(ERaftLogType.Trace, "Received message, pre processing: {0}", message);
                    message = DerivedMessageProcessing(message, messageToProcess.Item2); //This is for derived classes to do encryption, if it returns null it was consumed

                    if (message == null)
                    {
                        Log(ERaftLogType.Trace, "Message was consumed during post processing");
                        continue;
                    }

                    if (UDPClient.NodeIPs.AddOrUpdateNode(message.From, messageToProcess.Item2))
                    {
                        OnNewConnectedPeer?.Invoke(this, message.From);
                    }

                    OnMessageReceived?.Invoke(this, message);
                }
                catch (Exception e)
                {
                    GenerateReceiveFailureException("Failed deserialising byte array", e);
                }
            }
        }

        protected virtual BaseMessage DerivedMessageProcessing(BaseMessage message, IPEndPoint ipEndPoint)
        {
            return message;
        }

        protected void Log(ERaftLogType logType, string format, params object[] args)
        {
            string messagePrepend = string.Format("(Method={0}) - ", new StackFrame(1).GetMethod().Name);
            RaftLogging.Instance.Log(logType, messagePrepend + format, args);
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

        protected void GenerateSendFailureException(UdpNetworkingSendFailureException exception)
        {
            Log(ERaftLogType.Warn, "Sending failure error message: {0}", exception.Message);
            Log(ERaftLogType.Warn, "Sending failure message contents: {0}", exception.GetMessage());
            OnMessageSendFailure?.Invoke(this, exception);
        }

        public EUDPNetworkingStatus GetStatus()
        {
            lock (_statusLockObject)
            {
                return _status;
            }
        }

        protected IPEndPoint GetPeerIPEndPoint(string peerName)
        {
            return UDPClient.NodeIPs[peerName];
        }

        public string[] GetPeers()
        {
            return UDPClient.NodeIPs.GetNodes();
        }

        public int CountPeers()
        {
            return UDPClient.NodeIPs.Count;
        }

        public bool HasPeer(string peerName)
        {
            return UDPClient.NodeIPs.HasNode(peerName);
        }

        public void ManualAddPeer(string peerName, IPEndPoint endPoint)
        {
            UDPClient.NodeIPs.AddOrUpdateNode(peerName, endPoint);
        }

        public IPEndPoint GetIPFromName(string peerName)
        {
            return UDPClient.NodeIPs[peerName];
        }

        public void RemovePeer(string peerName)
        {
            UDPClient.NodeIPs.RemoveNode(peerName);
        }

        public virtual void Dispose()
        {
            if (DisposedValue) return;

            lock (_statusLockObject)
            {
                _onNetworkingStop.Set();

                if (_status == EUDPNetworkingStatus.Running)
                {
                    _listeningThread.Join();
                    _sendingThread.Join();
                    _processingThread.Join();
                }

                _status = EUDPNetworkingStatus.Stopped;
            }

            UDPClient?.Dispose();

            DisposedValue = true;
        }
    }
}
