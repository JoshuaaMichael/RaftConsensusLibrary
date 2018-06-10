using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Common;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Consensus.Enums;
using TeamDecided.RaftConsensus.Consensus.Interfaces;
using TeamDecided.RaftConsensus.Consensus.RaftMessages;
using TeamDecided.RaftConsensus.Networking;
using TeamDecided.RaftConsensus.Networking.Interfaces;
using TeamDecided.RaftConsensus.Networking.Messages;

/*
* Order of locks:
*	currentStateLockObject
*	currentTermLockObject
*	nodesInfoLockObject
*	eJoinClusterResponeLockObject
*	lastReceivedMessageLock
*	timeoutValueLockObject
*	votedForLockObject
*	distributedLogLockObject
*	appendEntryTasksLockObject
*/

namespace TeamDecided.RaftConsensus.Consensus
{
    public class RaftConsensus<TKey, TValue> : IConsensus<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        private static Random _rand = new Random();
        private string _clusterName;
        private int _maxNodes;
        private ERaftState _currentState;
        private object _currentStateLockObject;
        private int _currentTerm;
        private string _votedFor;
        private object _votedForLockObject;
        private object _currentTermLockObject;
        private Dictionary<string, NodeInfo> _nodesInfo;
        private object _nodesInfoLockObject;
        private IUdpNetworking _networking;
        private List<Tuple<string, string, int>> _manuallyAddedClients;
        private int _listeningPort;
        private string _nodeName;
        private string _leaderName;

        private List<Tuple<string, IPEndPoint>> _manuallyAddedPeers;

        private RaftDistributedLog<TKey, TValue> _distributedLog;
        private object _distributedLogLockObject;

        #region Timeout values
        private const int NetworkLatency = 50; //ms
        private int _heartbeatInterval = NetworkLatency * 3;
        private int _timeoutValueMin = 10 * NetworkLatency;
        private int _timeoutValueMax = 5 * 10 * NetworkLatency;
        private int _timeoutValue; //The actual timeout value chosen
        private object _timeoutValueLockObject;
        #endregion

        private Thread _backgroundThread;
        private ManualResetEvent _onNotifyBackgroundThread;
        private ManualResetEvent _onReceivedMessage;
        private ManualResetEvent _onShutdown;
        private CountdownEvent _onThreadsStarted;

        private ManualResetEvent _onWaitingToJoinCluster;
        private int _waitingToJoinClusterTimeout = 5000;
        private object _eJoinClusterResponeLockObject;
        private int _joiningClusterAttemptNumber;

        private Dictionary<int, ManualResetEvent> _appendEntryTasks;
        private object _appendEntryTasksLockObject;

        public event EventHandler StartUas;
        public event EventHandler<EStopUasReason> StopUas;
        public event EventHandler<Tuple<TKey, TValue>> OnNewCommitedEntry;

        private bool _disposedValue = false; // To detect redundant calls

        public RaftConsensus(string nodeName, int listeningPort)
        {
            _currentState = ERaftState.Initializing;
            _currentStateLockObject = new object();
            _currentTerm = 0;
            _votedFor = "";
            _votedForLockObject = new object();
            _currentTermLockObject = new object();
            _nodesInfo = new Dictionary<string, NodeInfo>();
            _nodesInfoLockObject = new object();
            this._listeningPort = listeningPort;
            this._nodeName = nodeName;
            _manuallyAddedPeers = new List<Tuple<string, IPEndPoint>>();
            _distributedLog = new RaftDistributedLog<TKey, TValue>();
            _distributedLogLockObject = new object();

            _timeoutValueLockObject = new object();

            _backgroundThread = new Thread(new ThreadStart(BackgroundThread));
            _onNotifyBackgroundThread = new ManualResetEvent(false);
            _onReceivedMessage = new ManualResetEvent(false);
            _onShutdown = new ManualResetEvent(false);
            _onThreadsStarted = new CountdownEvent(1);
            _eJoinClusterResponeLockObject = new object();
            _joiningClusterAttemptNumber = 0;

            _appendEntryTasks = new Dictionary<int, ManualResetEvent>();
            _appendEntryTasksLockObject = new object();

            _manuallyAddedClients = new List<Tuple<string, string, int>>();
        }

        public Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, int maxNodes, bool useEncryption)
        {
            lock (_currentStateLockObject)
            {
                if (_currentState != ERaftState.Initializing)
                {
                    ThrowInvalidOperationException("You may only join, or attempt to join, one cluster at a time");
                }
                if (string.IsNullOrWhiteSpace(clusterName))
                {
                    ThrowArgumentException("clusterName must not be blank");
                }
                if (string.IsNullOrWhiteSpace(clusterPassword))
                {
                    ThrowArgumentException("clusterPassword must not be blank");
                }

                Log(ERaftLogType.Info, "Starting networking stack");

                if(useEncryption)
                {
                    _networking = new UdpNetworkingSecure(clusterPassword);
                }
                else
                {
                    _networking = new UdpNetworking();
                }

                _networking.SetClientName(_nodeName);
                _networking.Start(_listeningPort);
                _networking.OnMessageReceived += OnMessageReceive;
                FlushNetworkPeerBuffer();

                this._maxNodes = maxNodes;

                Log(ERaftLogType.Info, "Trying to join cluster - {0}", clusterName);
                foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
                {
                    IPEndPoint ipEndPoint = _networking.GetIpFromName(node.Key);
                    Log(ERaftLogType.Debug, "I know: nodeName={0}, ipAddress={1}, port={2}", node.Key, ipEndPoint.Address.ToString(), ipEndPoint.Port);
                }

                this._clusterName = clusterName;

                lock (_nodesInfoLockObject)
                {
                    if (_nodesInfo.Count + 1 != maxNodes) //You aren't in the nodesInfo list
                    {
                        ThrowInvalidOperationException("There are not enough nodes known yet");
                    }

                    _onWaitingToJoinCluster = new ManualResetEvent(false);
                }

                _currentState = ERaftState.Follower;
                Log(ERaftLogType.Info, "Set state to follower");
                StartBackgroundThread();
                ChangeStateToFollower();

                Task<EJoinClusterResponse> task = Task.Run(() =>
                {
                    if (_onWaitingToJoinCluster.WaitOne(_waitingToJoinClusterTimeout) == false) //The timeout occured
                    {
                        Log(ERaftLogType.Warn, "Never found cluster in {0} millisecond timeout", _waitingToJoinClusterTimeout);
                        Log(ERaftLogType.Info, "Shutdown background thread");
                        _onShutdown.Dispose();
                        lock (_currentStateLockObject)
                        {
                            Log(ERaftLogType.Info, "Set state to initializing");
                            _currentState = ERaftState.Initializing;
                        }
                        Log(ERaftLogType.Info, "Disposing networking");
                        _networking.Dispose();
                        this._maxNodes = 0;
                        Log(ERaftLogType.Info, "Returning no response message from join attempt");
                        return EJoinClusterResponse.NoResponse;
                    }
                    else
                    {
                        Log(ERaftLogType.Info, "Notifying the user we've succesfully joined the cluster");
                        return EJoinClusterResponse.Accept; //We found the cluster
                    }
                });

                return task;
            }
        }

        private void StartBackgroundThread()
        {
            Log(ERaftLogType.Info, "Starting background thread");
            _backgroundThread.Start();
            _onThreadsStarted.Wait();
            Log(ERaftLogType.Info, "Started background thread");
        }

        public string GetClusterName()
        {
            if (_clusterName == "")
            {
                ThrowInvalidOperationException("Cluster name not set yet, please join a cluster or create a cluster");
            }
            return _clusterName;
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

        public string GetNodeName()
        {
            return _nodeName;
        }
        public TValue ReadEntryValue(TKey key)
        {
            lock (_distributedLogLockObject)
            {
                try
                {
                    return _distributedLog[key].Value;
                }
                catch (KeyNotFoundException e)
                {
                    Log(ERaftLogType.Warn, "Failed to ReadEntryValue for key: {0}", key);
                    throw e;
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
                catch (KeyNotFoundException e)
                {
                    Log(ERaftLogType.Warn, "Failed to ReadEntryValueHistory for key: {0}", key);
                    throw e;
                }
            }
        }
        public void ManualAddPeer(string name, IPEndPoint endPoint)
        {
            Log(ERaftLogType.Debug, "Manually adding peer {0}'s IP details. Address {1}, port {2}", name, endPoint.Address.ToString(), endPoint.Port);
            _manuallyAddedClients.Add(new Tuple<string, string, int>(name, endPoint.Address.ToString(), endPoint.Port));
            lock (_nodesInfoLockObject)
            {
                _nodesInfo.Add(name, new NodeInfo(name));
            }
        }
        private void FlushNetworkPeerBuffer()
        {
            Log(ERaftLogType.Info, "Flushing network peer buffer");
            Log(ERaftLogType.Debug, "Buffer has {0} entries", _manuallyAddedClients.Count);

            int succesfulAdd = 0;
            foreach (Tuple<string, string, int> peer in _manuallyAddedClients)
            {
                if (!_networking.HasPeer(peer.Item1))
                {
                    Log(ERaftLogType.Trace, "Adding node. Node name {0}, node IP {1}, node port {2}", peer.Item1, peer.Item2, peer.Item3);
                    _networking.ManualAddPeer(peer.Item1, new IPEndPoint(IPAddress.Parse(peer.Item2), peer.Item3));
                    succesfulAdd += 1;
                }
            }

            Log(ERaftLogType.Debug, "Added {0}/{1} entries into UDPNetworking", succesfulAdd, _manuallyAddedClients.Count);
            Log(ERaftLogType.Info, "Flushed network peer buffer");
        }
        public Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value)
        {
            lock (_currentStateLockObject)
            {
                if (_currentState != ERaftState.Leader)
                {
                    ThrowInvalidOperationException("You may only append entries when your UAS is active");
                }

                RaftLogEntry<TKey, TValue> entry;
                lock (_currentTermLockObject)
                {
                    entry = new RaftLogEntry<TKey, TValue>(key, value, _currentTerm);
                    Log(ERaftLogType.Info, "Attempting to append new entry to log");
                    Log(ERaftLogType.Debug, entry.ToString());

                    int prevIndex;
                    int prevTerm;
                    int commitIndex;
                    ManualResetEvent waitEvent;
                    int currentLastIndex;

                    lock (_distributedLogLockObject)
                    {
                        prevIndex = _distributedLog.GetLastIndex();
                        Log(ERaftLogType.Trace, "Previous index: {0}", prevIndex);
                        prevTerm = _distributedLog.GetTermOfLastIndex();
                        Log(ERaftLogType.Trace, "Previous term: {0}", prevTerm);
                        commitIndex = _distributedLog.CommitIndex;
                        Log(ERaftLogType.Trace, "Commit index: {0}", commitIndex);
                        _distributedLog.AppendEntry(entry, _distributedLog.GetLastIndex());
                        Log(ERaftLogType.Trace, "Appended entry");
                        waitEvent = new ManualResetEvent(false);
                        currentLastIndex = _distributedLog.GetLastIndex();
                        Log(ERaftLogType.Trace, "Current last index: {0}", currentLastIndex);
                    }

                    lock (_appendEntryTasksLockObject)
                    {
                        Log(ERaftLogType.Trace, "Adding event for notifying of commit");
                        _appendEntryTasks.Add(currentLastIndex, waitEvent);
                    }

                    Task<ERaftAppendEntryState> task = Task.Run(() =>
                    {
                        //TODO: Handle the case where you stop being leader, and it can tech fail
                        waitEvent.WaitOne();
                        Log(ERaftLogType.Info, "Succesfully appended entry to log");
                        Log(ERaftLogType.Debug, "Heard back from commit attempt, success. {0}", entry.ToString());
                        return ERaftAppendEntryState.Commited;
                    });

                    return task;
                }
            }
        }
        public bool IsUasRunning()
        {
            lock (_currentStateLockObject)
            {
                return _currentState == ERaftState.Leader;
            }
        }

        private void BackgroundThread()
        {
            WaitHandle[] waitHandles = new WaitHandle[] { _onShutdown, _onNotifyBackgroundThread };
            _onThreadsStarted.Signal();
            Log(ERaftLogType.Info, "Background thread initialised");
            ERaftState threadState = ERaftState.Initializing;
            while (true)
            {
                int indexOuter = 0;
                try
                {
                    indexOuter = WaitHandle.WaitAny(waitHandles);
                }
                catch(Exception e)
                {
                    Log(ERaftLogType.Fatal, "Threw exception when waiting for events", e.ToString());
                }

                if (indexOuter == 0)
                {
                    Log(ERaftLogType.Info, "Background thread has been told to shutdown");
                    return;
                }
                else if (indexOuter == 1)
                {
                    Log(ERaftLogType.Debug, "Background thread has been notified to update");
                    _onNotifyBackgroundThread.Reset();
                    lock (_currentStateLockObject)
                    {
                        threadState = _currentState;
                    }
                }

                if (threadState == ERaftState.Follower)
                {
                    BackgroundThread_Follower(waitHandles);
                }
                else if (threadState == ERaftState.Candidate)
                {
                    BackgroundThread_Candidate(waitHandles);
                }
                else if (threadState == ERaftState.Leader)
                {
                    BackgroundThread_Leader(waitHandles);
                }
            }
        }
        private void BackgroundThread_Leader(WaitHandle[] waitHandles)
        {
            Log(ERaftLogType.Debug, "Background thread now running as Leader");
            while (true)
            {
                int index = WaitHandle.WaitAny(waitHandles, _heartbeatInterval);
                if (index == WaitHandle.WaitTimeout)
                {
                    Log(ERaftLogType.Debug, "It's time to send out heartbeats.");
                    lock (_currentTermLockObject)
                    {
                        lock (_nodesInfoLockObject)
                        {
                            lock (_distributedLogLockObject)
                            {
                                foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
                                {
                                    RaftAppendEntry<TKey, TValue> heartbeatMessage;
                                    if (_distributedLog.GetLastIndex() >= node.Value.NextIndex)
                                    {
                                        int prevIndex;
                                        int prevTerm;
                                        if (node.Value.NextIndex == 0) //No prev index
                                        {
                                            prevIndex = -1;
                                            prevTerm = -1;
                                        }
                                        else
                                        {
                                            prevIndex = node.Value.NextIndex - 1;
                                            prevTerm = _distributedLog.GetTermOfIndex(prevIndex);
                                        }

                                        Log(ERaftLogType.Trace, "Previous index: {0}", prevIndex);
                                        Log(ERaftLogType.Trace, "Previous term: {0}", prevTerm);

                                        heartbeatMessage =
                                            new RaftAppendEntry<TKey, TValue>(node.Key,
                                                                                    _nodeName,
                                                                                    _clusterName,
                                                                                    ELogName.UasLog,
                                                                                    _currentTerm,
                                                                                    prevIndex,
                                                                                    prevTerm,
                                                                                    _distributedLog.CommitIndex,
                                                                                    _distributedLog[node.Value.NextIndex]);
                                    }
                                    else
                                    {
                                        heartbeatMessage = new RaftAppendEntry<TKey, TValue>(node.Key, _nodeName, _clusterName, ELogName.UasLog, _currentTerm, _distributedLog.CommitIndex);
                                    }
                                    SendMessage(heartbeatMessage);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log(ERaftLogType.Debug, "Leader thread has been signaled, {0}", index);
                    return;
                }
            }
        }
        private void BackgroundThread_Candidate(WaitHandle[] waitHandles)
        {
            Log(ERaftLogType.Debug, "Background thread now running as Candidate");
            int index = WaitHandle.WaitAny(waitHandles, _timeoutValue);
            if (index == WaitHandle.WaitTimeout)
            {
                Log(ERaftLogType.Info, "We didn't get voted in to be leader, time to try again");
                ChangeStateToCandiate();
                return;
            }
            else
            {
                Log(ERaftLogType.Debug, "Candidate thread has been signaled, {0}", index);
                return;
            }
        }
        private void BackgroundThread_Follower(WaitHandle[] waitHandles)
        {
            Log(ERaftLogType.Debug, "Background thread now running as Follower");
            //Add onReceivedMessage to the array of waithandle to wait on, don't edit original array
            WaitHandle[] followerWaitHandles = new WaitHandle[waitHandles.Length + 1];
            Array.Copy(waitHandles, followerWaitHandles, waitHandles.Length);
            int onReceivedMessageIndex = waitHandles.Length;
            followerWaitHandles[onReceivedMessageIndex] = _onReceivedMessage;

            while (true)
            {
                int indexInner = WaitHandle.WaitAny(followerWaitHandles, _timeoutValue);
                if (indexInner == WaitHandle.WaitTimeout)
                {
                    Log(ERaftLogType.Info, "Timing out to be candidate. We haven't heard from the leader in {0} milliseconds", _timeoutValue);
                    ChangeStateToCandiate();
                    return;
                }
                else if (indexInner == onReceivedMessageIndex)
                {
                    _onReceivedMessage.Reset();
                }
                else //We've been signaled. Told to shutdown, onNotifyBackgroundThread doesn't impact us really
                {
                    Log(ERaftLogType.Debug, "Follower thread has been signaled. {0}", indexInner);
                    return;
                }
            }
        }

        private void ChangeStateToFollower()
        {
            Log(ERaftLogType.Info, "Changing state to Follower");
            if (_currentState == ERaftState.Leader)
            {
                Log(ERaftLogType.Info, "Notifying the UAS to stop");
                StopUas?.Invoke(this, EStopUasReason.ClusterLeadershipLost);
            }

            _currentState = ERaftState.Follower;
            RecalculateTimeoutValue();
            _onNotifyBackgroundThread.Set();
        }
        private void ChangeStateToLeader()
        {
            Log(ERaftLogType.Info, "Changing state to Leader");
            _currentState = ERaftState.Leader;

            lock (_distributedLogLockObject)
            {
                foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
                {
                    node.Value.NextIndex = _distributedLog.GetLastIndex() + 1;
                }

                _onNotifyBackgroundThread.Set();
                Log(ERaftLogType.Debug, "Letting everyone know about our new leadership");
                //Blast out to let everyone know about our victory
                foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
                {
                    RaftAppendEntry<TKey, TValue> message = new RaftAppendEntry<TKey, TValue>(node.Key, _nodeName, _clusterName, ELogName.UasLog, _currentTerm, _distributedLog.CommitIndex);
                    SendMessage(message);
                }
            }
            Log(ERaftLogType.Info, "Notifying to start UAS");
            StartUas?.Invoke(this, null);
        }
        private void ChangeStateToCandiate()
        {
            Log(ERaftLogType.Info, "Changing state to Candidate");

            if(_currentState == ERaftState.Stopped)
            {
                Log(ERaftLogType.Trace, "How did you get here?");
            }

            _currentState = ERaftState.Candidate;
            lock (_currentTermLockObject)
            {
                _currentTerm += 1;
            }
            _onNotifyBackgroundThread.Set();
            RecalculateTimeoutValue();

            lock (_nodesInfo)
            {
                Log(ERaftLogType.Info, "Requesting votes for leadership from everyone");
                lock (_distributedLogLockObject)
                {
                    foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
                    {
                        node.Value.VoteGranted = false;
                        RaftRequestVote message = new RaftRequestVote(node.Key, _nodeName, _clusterName, _currentTerm, _distributedLog.GetLastIndex(), _distributedLog.GetTermOfLastIndex());
                        SendMessage(message);
                    }
                }
            }
        }

        private void OnMessageReceive(object sender, BaseMessage message)
        {
            if (!message.GetType().IsSubclassOf(typeof(RaftBaseMessage)) && !(message.GetType() == typeof(RaftBaseMessage)))
            {
                Log(ERaftLogType.Warn, "Dropping message. We don't know what it is. It's not a RaftBaseMessage. It's a {0}", message.GetType());
                Log(ERaftLogType.Trace, "Received message contents: {0}", message.ToString());
                return;
            }

            Log(ERaftLogType.Debug, "Received message. From: {0}, type: {1}", message.From, message.GetType());

            if (((RaftBaseMessage)message).ClusterName != _clusterName)
            {
                Log(ERaftLogType.Warn, "Dropping message. It's for the wrong cluster name");
                Log(ERaftLogType.Debug, "The cluster name they're attempting to join is {0}", ((RaftBaseMessage)message).ClusterName);
                return;
            }

            if (message.MessageType == typeof(RaftAppendEntry<TKey, TValue>))
            {
                HandleAppendEntry((RaftAppendEntry<TKey, TValue>)message);
            }
            else if (message.MessageType == typeof(RaftAppendEntryResponse))
            {
                HandleAppendEntryResponse((RaftAppendEntryResponse)message);
            }
            else if (message.MessageType == typeof(RaftRequestVote))
            {
                HandleCallElection((RaftRequestVote)message);
            }
            else if (message.MessageType == typeof(RaftRequestVoteResponse))
            {
                HandleCallElectionResponse((RaftRequestVoteResponse)message);
            }
            else
            {
                Log(ERaftLogType.Warn, "Dropping message. This is a RaftBaseMessage we don't support. Type: {0}", message.GetType());
            }
        }

        private void HandleAppendEntry(RaftAppendEntry<TKey, TValue> message)
        {
            RaftAppendEntryResponse responseMessage;
            lock (_currentStateLockObject)
            {
                if (_currentState != ERaftState.Leader && _currentState != ERaftState.Candidate && _currentState != ERaftState.Follower)
                {
                    Log(ERaftLogType.Debug, "Recieved AppendEntry from node {0}. We don't recieve these types of requests", message.From);
                    return;
                }

                lock (_currentTermLockObject)
                {
                    if (message.Term > _currentTerm)
                    {
                        Log(ERaftLogType.Info, "Heard from node ({0}) with greater term({1}) than ours({2}). Changing to follower.", message.From, message.Term, _currentTerm);

                        if(_leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                            return;
                        }

                        UpdateTerm(message.Term);
                        _leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }

                    if (message.Term < _currentTerm)
                    {
                        Log(ERaftLogType.Debug, "Recieved AppendEntry from node {0} for a previous term. Sending back a reject.", message.From);
                        responseMessage = new RaftAppendEntryResponse(message.From, _nodeName, _clusterName, message.LogName, _currentTerm, false, -1);
                        SendMessage(responseMessage);
                        return;
                    }

                    if (_currentState == ERaftState.Leader)
                    {
                        Log(ERaftLogType.Debug, "Recieved AppendEntry from node {0}. Discarding as we're the leader.", message.From);
                        return;
                    }
                    else if (_currentState == ERaftState.Candidate)
                    {
                        Log(ERaftLogType.Info, "Recieved AppendEntry from the leader {0} of currentTerm. Going back to being a follower.", message.From);
                        if (_leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                        }
                        _leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }
                    else if (_currentState == ERaftState.Follower)
                    {
                        if (_leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                            return;
                        }
                    }
                }

                RecalculateTimeoutValue();
                _onReceivedMessage.Set();

                lock (_currentTermLockObject)
                {
                    lock (_distributedLogLockObject)
                    {
                        if (message.Entry == null) //Check if this is a heart beat with no data
                        {
                            Log(ERaftLogType.Debug, "This is a heatbeat message from leader {0}", message.From);
                            //Check if we should move up our commit index, and respond to the leader
                            if (message.LeaderCommitIndex > _distributedLog.CommitIndex)
                            {
                                Log(ERaftLogType.Info, "Heartbeat contained a request to update out commit index");
                                FollowerUpdateCommitIndex(message.LeaderCommitIndex);
                            }
                            responseMessage = new RaftAppendEntryResponse(message.From, _nodeName, _clusterName, message.LogName, _currentTerm, true, _distributedLog.GetLastIndex());
                            SendMessage(responseMessage);
                            return;
                        }
                        else
                        {
                            Log(ERaftLogType.Info, "This is a AppendEntry message from {0} has new entries to commit", message.From);

                            if (_distributedLog.GetLastIndex() > message.PrevIndex)
                            {
                                Log(ERaftLogType.Debug, "We received a message but our latestIndex ({0}) was greater than their PrevIndex ({0}). Truncating the rest of the log forward for safety", _distributedLog.GetLastIndex(), message.PrevIndex);
                                _distributedLog.TruncateLog(message.PrevIndex + 1);
                            }

                            if (_distributedLog.GetLastIndex() == message.PrevIndex)
                            {
                                if (_distributedLog.ConfirmPreviousIndex(message.PrevIndex, message.PrevTerm))
                                {
                                    _distributedLog.AppendEntry(message.Entry, message.PrevIndex);
                                    Log(ERaftLogType.Debug, "Confirmed previous index. Appended message");
                                    if (message.LeaderCommitIndex > _distributedLog.CommitIndex)
                                    {
                                        FollowerUpdateCommitIndex(message.LeaderCommitIndex);
                                    }
                                    Log(ERaftLogType.Info, "Responding to leader with the success of our append");
                                    responseMessage = new RaftAppendEntryResponse(message.From, _nodeName, _clusterName, message.LogName, _currentTerm, true, _distributedLog.GetLastIndex());
                                    SendMessage(responseMessage);
                                }
                            }
                            else if (_distributedLog.GetLastIndex() < message.PrevIndex)
                            {
                                Log(ERaftLogType.Debug, "Got entry we weren't ready for, replying with false. Our previous index {0}, their previous index {1}", _distributedLog.GetLastIndex(), message.PrevIndex);
                                responseMessage = new RaftAppendEntryResponse(message.From, _nodeName, _clusterName, message.LogName, _currentTerm, false, _distributedLog.GetLastIndex());
                                SendMessage(responseMessage);
                            }
                        }
                    }
                }
            }
        }
        private void HandleAppendEntryResponse(RaftAppendEntryResponse message)
        {
            lock (_currentStateLockObject)
            {
                if (_currentState != ERaftState.Leader)
                {
                    Log(ERaftLogType.Debug, "Recieved AppendEntry from node {0}. We don't recieve these types of requests", message.From);
                    return;
                }
                lock (_currentTermLockObject)
                {
                    //Are we behind, and we've now got a new leader?
                    if (message.Term > _currentTerm)
                    {
                        Log(ERaftLogType.Info, "Heard from a node ({0}) with greater term({1}) than ours({2}). Changing to follower", message.From, message.Term, _currentTerm);

                        if (_leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                            return;
                        }

                        UpdateTerm(message.Term);
                        _leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }

                    if (message.Term < _currentTerm)
                    {
                        Log(ERaftLogType.Debug, "Recieved AppendEntryResponse from node {0} for a previous term. Discarding.", message.From);
                        return;
                    }

                    if (_currentState != ERaftState.Leader)
                    {
                        return; //We don't recieve these type of requests, drop it
                    }
                    Log(ERaftLogType.Trace, "Recieved AppendEntryResponse from node {0}", message.From);

                    lock (_nodesInfoLockObject)
                    {
                        NodeInfo nodeInfo = _nodesInfo[message.From];
                        nodeInfo.UpdateLastReceived();

                        if (message.MatchIndex == nodeInfo.MatchIndex && message.Success)
                        {
                            if (message.MatchIndex != nodeInfo.NextIndex - 1)
                            {
                                nodeInfo.MatchIndex = message.MatchIndex;
                                nodeInfo.NextIndex = message.MatchIndex + 1;
                                Log(ERaftLogType.Debug, "We've been told to step back their log. Their match is {0}, their new NextIndex is {1}", nodeInfo.MatchIndex, nodeInfo.NextIndex);
                            }
                            else
                            {
                                Log(ERaftLogType.Debug, "It was just a heartbeat");
                            }
                            return;
                        }
                        Log(ERaftLogType.Info, "Setting {0}'s match index to {1} from {2}.", message.From, message.MatchIndex, nodeInfo.MatchIndex);
                        nodeInfo.MatchIndex = message.MatchIndex;
                        nodeInfo.NextIndex = message.MatchIndex + 1;

                        if (message.Success)
                        {
                            Log(ERaftLogType.Info, "The append entry was a success");
                            lock (_distributedLogLockObject)
                            {
                                if (message.MatchIndex > _distributedLog.CommitIndex)
                                {
                                    Log(ERaftLogType.Debug, "Since we've got another commit, we should check if we're at majority now");
                                    //Now we've got another commit, have we reached majority now?
                                    if (CheckForCommitMajority(message.MatchIndex) && _distributedLog.GetTermOfIndex(message.MatchIndex) == _currentTerm)
                                    {
                                        Log(ERaftLogType.Info, "We've reached majority. Time to notify everyone to update.");
                                        //We have! Update our log. Notify everyone to update their logs
                                        lock (_appendEntryTasksLockObject)
                                        {
                                            int oldCommitIndex = _distributedLog.CommitIndex;
                                            _distributedLog.CommitUpToIndex(message.MatchIndex);
                                            _appendEntryTasks[message.MatchIndex].Set();
                                            _appendEntryTasks.Remove(message.MatchIndex);

                                            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
                                            {
                                                RaftAppendEntry<TKey, TValue> updateMessage =
                                                    new RaftAppendEntry<TKey, TValue>(node.Key,
                                                                                            _nodeName,
                                                                                            _clusterName,
                                                                                            ELogName.UasLog,
                                                                                            _currentTerm,
                                                                                            _distributedLog.CommitIndex);
                                                SendMessage(updateMessage);
                                            }
                                            Log(ERaftLogType.Debug, "Running OnNewCommitedEntry. Starting from {0}, going to and including {1}", oldCommitIndex + 1, message.MatchIndex);
                                            for (int i = oldCommitIndex + 1; i <= message.MatchIndex; i++)
                                            {
                                                OnNewCommitedEntry?.Invoke(this, _distributedLog[i].GetTuple());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log(ERaftLogType.Info, "We've haven't reached majority yet");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log(ERaftLogType.Debug, "This follower failed to append entry. Stepping back their next index");
                            if(_nodesInfo[message.From].NextIndex > 0)
                            {
                                _nodesInfo[message.From].NextIndex--;
                            }
                        }
                    }
                }
            }
        }
        private void HandleCallElection(RaftRequestVote message)
        {
            RaftRequestVoteResponse responseMessage;
            lock (_currentStateLockObject)
            {
                if (_currentState != ERaftState.Leader && _currentState != ERaftState.Candidate && _currentState != ERaftState.Follower)
                {
                    Log(ERaftLogType.Debug, "Received message from {0}. We aren't in the correct state to process it. Discarding", message.From);
                    return;
                }

                lock (_currentTermLockObject)
                {
                    if (message.Term > _currentTerm)
                    {
                        Log(ERaftLogType.Info, "Received a RequestVote from {0}. Let's take a look.", message.From);
                        UpdateTerm(message.Term);

                        RecalculateTimeoutValue();
                        _onReceivedMessage.Set();

                        if (_currentState != ERaftState.Follower)
                        {
                            Log(ERaftLogType.Info, "We currently aren't a follower. Changing state to follower.");
                            _leaderName = null;
                            ChangeStateToFollower();
                        }

                        lock (_votedForLockObject)
                        {
                            if (_votedFor == "") //We haven't voted for anyone
                            {
                                lock (_distributedLogLockObject)
                                {
                                    int logLatestIndex = _distributedLog.GetLastIndex();
                                    if (message.LastLogIndex >= logLatestIndex && message.LastTermIndex >= _distributedLog.GetTermOfIndex(logLatestIndex))
                                    {
                                        Log(ERaftLogType.Info, "Their log is at least as up to date as ours, replying accept");
                                        _votedFor = message.From;
                                        responseMessage = new RaftRequestVoteResponse(message.From, _nodeName, _clusterName, _currentTerm, true);
                                    }
                                    else
                                    {
                                        Log(ERaftLogType.Info, "Their log is not at least as up to date as our, replying reject.");
                                        responseMessage = new RaftRequestVoteResponse(message.From, _nodeName, _clusterName, _currentTerm, false);
                                    }
                                }
                            }
                            else if (_votedFor == message.From)
                            {
                                Log(ERaftLogType.Info, "We've already voted for you? Replying accept");
                                responseMessage = new RaftRequestVoteResponse(message.From, _nodeName, _clusterName, _currentTerm, true);
                            }
                            else
                            {
                                Log(ERaftLogType.Info, "We've already voted for someone else in this term ({0}). Akward. Replying rejefct.", _votedFor);
                                responseMessage = new RaftRequestVoteResponse(message.From, _nodeName, _clusterName, _currentTerm, false);
                            }
                        }
                    }
                    else
                    {
                        Log(ERaftLogType.Info, "This message is from the same or an old term, returning false");
                        responseMessage = new RaftRequestVoteResponse(message.From, _nodeName, _clusterName, _currentTerm, false);
                    }
                }
            }
            SendMessage(responseMessage);
        }
        private void HandleCallElectionResponse(RaftRequestVoteResponse message)
        {
            lock (_currentStateLockObject)
            {
                lock (_currentTermLockObject)
                {
                    if (message.Term > _currentTerm)
                    {
                        Log(ERaftLogType.Info, "Heard from a node ({0}) with greater term({1}) than ours({2}). Changing to follower", message.From, message.Term, _currentTerm);
                        UpdateTerm(message.Term);
                        _leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }
                    else if (message.Term < _currentTerm)
                    {
                        Log(ERaftLogType.Debug, "Recieved RaftRequestVoteResponse from node {0} for a previous term. Discarding.", message.From);
                        return;
                    }
                }
                if (_currentState == ERaftState.Candidate && message.Granted)
                {
                    Log(ERaftLogType.Info, "{0} accepted our request, we have their vote", message.From);
                    lock (_currentTermLockObject) //Used by ChangeStateToLeader, maintaining lock ordering
                    {
                        lock (_nodesInfoLockObject)
                        {
                            _nodesInfo[message.From].VoteGranted = true;
                            if (CheckForVoteMajority())
                            {
                                Log(ERaftLogType.Info, "This vote got us to majority. Changing to leader");
                                if (_leaderName == null)
                                {
                                    _onWaitingToJoinCluster.Set();
                                }
                                _leaderName = _nodeName;
                                ChangeStateToLeader(); //This includes sending out the blast
                            }
                            else
                            {
                                Log(ERaftLogType.Info, "This vote didn't get us to majority. Going to continue waiting...");
                            }
                        }
                    }
                }
                else if (_currentState == ERaftState.Candidate && !message.Granted)
                {
                    Log(ERaftLogType.Info, "They rejected our request. Discarding.");
                }
                else
                {
                    Log(ERaftLogType.Debug, "We are not in the correct state to process this message. Discarding.");
                }
            }
        }

        private void FollowerUpdateCommitIndex(int leaderCommitIndex)
        {
            int newCommitIndex = Math.Min(leaderCommitIndex, _distributedLog.GetLastIndex());
            Log(ERaftLogType.Debug, "Updating commit index to {0}, leader's is {1}", newCommitIndex, leaderCommitIndex);
            int oldCommitIndex = _distributedLog.CommitIndex;
            _distributedLog.CommitUpToIndex(newCommitIndex);
            Log(ERaftLogType.Debug, "Running OnNewCommitedEntry. Starting from {0}, going to and including {1}", oldCommitIndex + 1, newCommitIndex);
            for (int i = oldCommitIndex + 1; i <= newCommitIndex; i++)
            {
                OnNewCommitedEntry?.Invoke(this, _distributedLog[i].GetTuple());
            }
        }
        private void FoundCluster(string leader, int term)
        {
            Log(ERaftLogType.Info, "We've found the cluster! It's node {0} on term {1}", leader, term);
            _leaderName = leader;
            UpdateTerm(term);
            _onWaitingToJoinCluster.Set();
            RecalculateTimeoutValue();
            _onNotifyBackgroundThread.Set();
        }

        private void RecalculateTimeoutValue()
        {
            lock (_timeoutValueLockObject)
            {
                Log(ERaftLogType.Trace, "Previous timeout value {0}", _timeoutValue);
                _timeoutValue = _rand.Next(_timeoutValueMin, _timeoutValueMax + 1);
                Log(ERaftLogType.Trace, "New timeout value {0}", _timeoutValue);
            }
        }
        private bool CheckForCommitMajority(int index)
        {
            Log(ERaftLogType.Trace, "Checking to see if we have commit majority for index {0}", index);
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                Log(ERaftLogType.Trace, "Node {0}'s match index is {1}", node.Key, node.Value.MatchIndex);
                if (node.Value.MatchIndex >= index)
                {
                    Log(ERaftLogType.Trace, "Node {0} is at least as up to date as this", node.Key);
                    total += 1;
                }
            }

            int majorityMinimal = (_maxNodes / 2) + 1;
            Log(ERaftLogType.Trace, "Minimal majority required {0}, total as up to date {1}", majorityMinimal, total);
            return (total >= majorityMinimal);
        }
        private bool CheckForVoteMajority()
        {
            Log(ERaftLogType.Trace, "Checking to see if we have vote majority for term {0}", _currentTerm);
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                if (node.Value.VoteGranted)
                {
                    Log(ERaftLogType.Trace, "Node {0} has granted us their vote", node.Key);
                    total += 1;
                }
            }

            int majorityMinimal = (_maxNodes / 2) + 1;
            Log(ERaftLogType.Trace, "Minimal majority required {0}, total voted for us {1}", majorityMinimal, total);
            return (total >= majorityMinimal);
        }
        private void UpdateTerm(int newTerm)
        {
            lock (_nodesInfoLockObject)
            {
                foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
                {
                    node.Value.VoteGranted = false;
                }
            }
            Log(ERaftLogType.Trace, "Setting term to {0} from {1}", newTerm, _currentTerm);
            _currentTerm = newTerm;
            lock (_votedForLockObject)
            {
                _votedFor = "";
            }
        }

        private void SendMessage(RaftBaseMessage message)
        {
            Log(ERaftLogType.Debug, "Sending message. To: {0}, type: {1}", message.To, message.GetType());
            _networking.SendMessage(message);
        }

        private void Log(ERaftLogType logType, string format, params object[] args)
        {
            string messagePrepend = string.Format("{0} (Status={1}) - ", _nodeName, _currentState.ToString());
            RaftLogging.Instance.Log(logType, messagePrepend + format, args);
        }

        #region Get/set timeout/heartbeat values
        public int GetHeartbeatInterval()
        {
            return _heartbeatInterval;
        }

        public void SetHeartbeatInterval(int value)
        {
            lock (_currentStateLockObject)
            {
                if (_currentState == ERaftState.Initializing)
                {
                    _heartbeatInterval = value;
                }
                else
                {
                    ThrowInvalidOperationException("You may not set this value while service is running, only before it starts");
                }
            }
        }

        public int GetWaitingForJoinClusterTimeout()
        {
            return _waitingToJoinClusterTimeout;
        }

        public void SetWaitingForJoinClusterTimeout(int value)
        {
            lock (_currentStateLockObject)
            {
                if (_currentState == ERaftState.Initializing)
                {
                    _waitingToJoinClusterTimeout = value;
                }
                else
                {
                    ThrowInvalidOperationException("You may not set this value while service is running, only before it starts");
                }
            }
        }
        #endregion

        #region Testing/Dependancy injection
        public static RaftConsensus<TKey, TValue>[] MakeNodesForTest(int count, int startPort)
        {
            RaftConsensus<TKey, TValue>[] nodes = new RaftConsensus<TKey, TValue>[count];

            for (int i = 0; i < count; i++)
            {
                nodes[i] = new RaftConsensus<TKey, TValue>("Node" + (i + 1), startPort + i);
            }

            return nodes;
        }
        public static IConsensus<TKey, TValue> RemakeDisposedNode(IConsensus<TKey, TValue>[] nodes, IConsensus<TKey, TValue> node, int startPort)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if(nodes[i].GetNodeName() == node.GetNodeName())
                {
                    node = new RaftConsensus<TKey, TValue>("Node" + (i + 1), startPort + i);
                    return node;
                }
            }
            return null;
        }
        #endregion

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    ERaftState previousStatus;
                    lock (_currentStateLockObject)
                    {
                        previousStatus = _currentState;
                        _currentState = ERaftState.Stopped;
                        Log(ERaftLogType.Trace, "Disposing {0}, previous status: {1}", _nodeName, previousStatus);
                    }
                    if (previousStatus != ERaftState.Initializing)
                    {
                        Log(ERaftLogType.Trace, "We've also got to do some cleanup");
                        if (previousStatus == ERaftState.Leader)
                        {
                            Log(ERaftLogType.Trace, "We were leader, sending message out to stop UAS");
                            StopUas?.Invoke(this, EStopUasReason.ClusterStop);
                        }
                        Log(ERaftLogType.Trace, "Shutting down background thread");
                        _onShutdown.Set();
                        _backgroundThread.Join();
                        Log(ERaftLogType.Trace, "Background thread completed shutdown. Disposing network.");
                        _networking.Dispose();
                        Log(ERaftLogType.Trace, "Network disposed");
                    }
                    else if (previousStatus == ERaftState.Initializing && _joiningClusterAttemptNumber > 0)
                    {
                        Log(ERaftLogType.Trace, "Just had to dispose the network");
                        _networking.Dispose();
                        Log(ERaftLogType.Trace, "Network disposed");
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
