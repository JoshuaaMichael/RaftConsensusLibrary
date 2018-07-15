using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Consensus.DistributedLog;
using TeamDecided.RaftConsensus.Consensus.Enums;
using TeamDecided.RaftConsensus.Consensus.Interfaces;
using TeamDecided.RaftConsensus.Consensus.RaftMessages;
using TeamDecided.RaftConsensus.Networking;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Interfaces;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Consensus
{
    public class RaftConsensus<TKey, TValue> : IConsensus<TKey, TValue>
        where TKey : ICloneable where TValue : ICloneable
    {
        public event EventHandler<Tuple<TKey, TValue>> OnNewCommitedEntry;
        public event EventHandler OnStartUAS;
        public event EventHandler<EStopUasReason> OnStopUAS;

        private static readonly Random Rand = new Random();
        public string ClusterName { get; }
        private int _maxNodes;
        private ERaftState _currentState;
        private object _currentStateLockObject;
        private int _currentTerm;
        private string _votedFor;
        public string NodeName { get; }
        private string _leaderName;

        private readonly IUDPNetworking _networking;
        private readonly int _listeningPort;

        private readonly Dictionary<string, NodeInfo> _nodesInfo;
        private readonly RaftDistributedLog<TKey, TValue> _distributedLog;
        private readonly object _distributedLogLockObject;
        private readonly Dictionary<int, ManualResetEvent> _appendEntryTasks;

        private readonly RaftPCQueue<RaftBaseMessage> _raftMessageQueue;
        private readonly MessageProcesser<RaftBaseMessage> _messageProcesser;

        #region Timeout values
        private const int NetworkLatency = 50; //ms
        private int _heartbeatInterval = NetworkLatency * 3;
        private int _timeoutValueMin = 10 * NetworkLatency;
        private int _timeoutValueMax = 5 * 10 * NetworkLatency;
        private int _timeoutValue; //The actual timeout value chosen
        #endregion

        private ManualResetEvent _onWaitingToJoinCluster;
        private int _waitingToJoinClusterTimeout = 10000;
        private int _joiningClusterAttemptNumber;

        private readonly Thread _backgroundThread;
        private ManualResetEvent _onNotifyBackgroundThread;
        private ManualResetEvent _onShutdown;
        private ManualResetEvent _onThreadStarted;

        public RaftConsensus(string nodeName, int listeningPort)
        {
            NodeName = nodeName;
            _networking = new UDPNetworking();
            _networking.OnMessageReceived += OnMessageReceived;
            _listeningPort = listeningPort;

            _currentState = ERaftState.Initializing;
            _currentStateLockObject = new object();
            _currentTerm = 0;
            _votedFor = "";

            _nodesInfo = new Dictionary<string, NodeInfo>();
            _distributedLog = new RaftDistributedLog<TKey, TValue>();
            _distributedLogLockObject = new object();
            _appendEntryTasks = new Dictionary<int, ManualResetEvent>();

            _raftMessageQueue = new RaftPCQueue<RaftBaseMessage>();
            _messageProcesser = new MessageProcesser<RaftBaseMessage>();
            _messageProcesser.Register(typeof(RaftAppendEntry<TKey, TValue>), HandleRaftAppendEntry);
            _messageProcesser.Register(typeof(RaftAppendEntryResponse), HandleRaftAppendEntryResponse);
            _messageProcesser.Register(typeof(RaftRequestVote), HandleRaftRequestVote);
            _messageProcesser.Register(typeof(RaftRequestVoteResponse), HandleRaftRequestVoteResponse);

            _backgroundThread = new Thread(BackgroundThread);
            _onNotifyBackgroundThread = new ManualResetEvent(false);
            _onShutdown = new ManualResetEvent(false);
            _onThreadStarted = new ManualResetEvent(false);
            _joiningClusterAttemptNumber = 0;
        }

        public Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, int maxNodes, int attempts, bool useEncryption)
        {
            throw new NotImplementedException();
        }

        public Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value)
        {
            throw new NotImplementedException();
        }

        private void StartBackgroundThread()
        {
            Log(ERaftLogType.Info, "Starting background thread");
            _backgroundThread.Start();
            _onThreadStarted.WaitOne();
            Log(ERaftLogType.Info, "Started background thread");
        }

        private void BackgroundThread()
        {
            _onThreadStarted.Set();

            WaitLoop waitLoop = new WaitLoop();

            waitLoop.RegisterExceptionFunc(e =>
            {
                Log(ERaftLogType.Fatal, "Threw exception when waiting for events", e.ToString());
                return false;
            });

            waitLoop.RegisterAction(manualResetEvent =>
            {
                Log(ERaftLogType.Info, "Background thread has been told to shutdown");
                return true;
            }, _onShutdown);

            waitLoop.RegisterAction(manualResetEvent =>
            {
                Log(ERaftLogType.Debug, "Background thread has been notified to update");
                manualResetEvent.Reset();

                waitLoop.UpdateTimeoutFuncTimeout(_currentState == ERaftState.Leader ? _heartbeatInterval : _timeoutValue);

                return false;
            }, _onNotifyBackgroundThread);

            waitLoop.RegisterAction(manualResetEvent =>
            {
                ProcessMessage();

                //Recalc what is left of timeout

                return false;
            }, _raftMessageQueue.Flag);

            waitLoop.RegisterTimeoutFunc(() =>
            {
                if (_currentState == ERaftState.Follower)
                {
                    ProcessTimeout_Follower();
                }
                else if (_currentState == ERaftState.Candidate)
                {
                    ProcessTimeout_Candidate();
                }
                else if (_currentState == ERaftState.Leader)
                {
                    ProcessTimeout_Leader();
                }
                else
                {
                    return false;
                }

                return true;
            }, _timeoutValue);

            waitLoop.Run();
        }

        private void ProcessMessage()
        {

        }

        private void ProcessTimeout_Follower()
        {
            //Haven't heard from the leader, candidate time!
        }

        private void ProcessTimeout_Candidate()
        {
            //No one voted for me, let's become candidate again
        }

        private void ProcessTimeout_Leader()
        {
            //Send heartbeats
        }

        private void HandleRaftAppendEntry(RaftBaseMessage message)
        {

        }

        private void HandleRaftAppendEntryResponse(RaftBaseMessage message)
        {

        }

        private void HandleRaftRequestVote(RaftBaseMessage message)
        {

        }

        private void HandleRaftRequestVoteResponse(RaftBaseMessage message)
        {

        }

        private void OnMessageReceived(object sender, BaseMessage message)
        {
            Log(ERaftLogType.Debug, "Received message. From: {0}, type: {1}", message.From, message.GetType());
            Log(ERaftLogType.Trace, "Received message contents: {0}", message.ToString());

            if (!message.GetType().IsSubclassOf(typeof(RaftBaseMessage)) && !(message.GetType() == typeof(RaftBaseMessage)))
            {
                Log(ERaftLogType.Warn, "Dropping message. It's not a RaftBaseMessage");
                return;
            }

            RaftBaseMessage raftBaseMessage = (RaftBaseMessage)message;

            if (raftBaseMessage.ClusterName != ClusterName)
            {
                Log(ERaftLogType.Warn, "Dropping message. It's for the wrong cluster name - {0}", raftBaseMessage.ClusterName);
                return;
            }

            _raftMessageQueue.Enqueue(raftBaseMessage);
        }

        private void SendMessage(RaftBaseMessage message)
        {
            Log(ERaftLogType.Debug, "Sending message. To: {0}, type: {1}", message.To, message.GetType());
            Log(ERaftLogType.Trace, "Sending message contents: {0}", message.ToString());
            _networking.SendMessage(message);
        }

        private void Log(ERaftLogType logType, string format, params object[] args)
        {
            string messagePrepend = string.Format("{0} (Status={1}) - ", NodeName, _currentState.ToString());
            RaftLogging.Instance.Log(logType, messagePrepend + format, args);
        }

        private void ThrowInvalidOperationException(string exceptionMessage)
        {
            Log(ERaftLogType.Warn, exceptionMessage);
            throw new InvalidOperationException(exceptionMessage);
        }

        private void ThrowArgumentException(string exceptionMessage)
        {
            Log(ERaftLogType.Warn, exceptionMessage);
            throw new ArgumentException(exceptionMessage);
        }

        public void ManualAddPeer(string name, IPEndPoint endPoint)
        {
            lock (_currentStateLockObject)
            {
                if (_currentState != ERaftState.Initializing)
                {
                    ThrowInvalidOperationException("Can only manually add a peer before joining cluster");
                }

                Log(ERaftLogType.Debug, "Manually adding peer {0}'s IP details. Address {1}, port {2}", name,
                    endPoint.Address.ToString(), endPoint.Port);
                _nodesInfo.Add(name, new NodeInfo(name));
                _networking.ManualAddPeer(name, endPoint);
            }
        }

        public bool HasJoinedCluster()
        {
            lock (_currentStateLockObject)
            {
                return _currentState == ERaftState.Follower || 
                       _currentState == ERaftState.Candidate ||
                       _currentState == ERaftState.Leader;
            }
        }

        public bool IsUASRunning()
        {
            lock (_currentStateLockObject)
            {
                return _currentState == ERaftState.Leader;
            }
        }

        public int NumberOfCommits()
        {
            lock (_distributedLogLockObject)
            {
                return _distributedLog.CommitIndex + 1;
            }
        }

        public TValue ReadEntryValue(TKey key)
        {
            lock (_distributedLogLockObject)
            {
                try
                {
                    return _distributedLog.GetValue(key);
                }
                catch (KeyNotFoundException)
                {
                    Log(ERaftLogType.Warn, "Failed to GetValue for key: {0}", key);
                    throw;
                }
            }
        }

        public TValue[] ReadEntryValueHistory(TKey key)
        {
            lock (_distributedLogLockObject)
            {
                try
                {
                    return _distributedLog.GetValueHistory(key);
                }
                catch (KeyNotFoundException)
                {
                    Log(ERaftLogType.Warn, "Failed to GetValueHistory for key: {0}", key);
                    throw;
                }
            }
        }

        public void Dispose()
        {
            lock (_currentStateLockObject)
            {
                Log(ERaftLogType.Trace, "Disposing {0}, previous status: {1}", NodeName, _currentState);

                if (_currentState == ERaftState.Leader)
                {
                    Log(ERaftLogType.Trace, "We were leader, sending message out to stop UAS");
                    OnStopUAS?.Invoke(this, EStopUasReason.ClusterStop);
                }

                Log(ERaftLogType.Trace, "Shutting down background thread, if running");
                _onShutdown.Set();
                _backgroundThread.Join();
                Log(ERaftLogType.Trace, "Background thread completed shutdown, if running");

                Log(ERaftLogType.Trace, "Disposing network, if required");
                _networking?.Dispose();
                Log(ERaftLogType.Trace, "Network disposed, if required");

                _currentState = ERaftState.Stopped;
            }
        }
    }
}
