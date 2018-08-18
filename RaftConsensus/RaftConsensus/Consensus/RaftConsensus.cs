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

//TODO: Make joining threadsafe

namespace TeamDecided.RaftConsensus.Consensus
{
    public class RaftConsensus<TKey, TValue> : IConsensus<TKey, TValue>
        where TKey : ICloneable where TValue : ICloneable
    {
        public event EventHandler<Tuple<TKey, TValue>> OnNewCommitedEntry;
        public event EventHandler OnStartUAS;
        public event EventHandler<EStopUasReason> OnStopUAS;

        private static readonly Random Rand = new Random();
        public string ClusterName { get; private set; }
        private int _maxNodes;
        private ERaftState _currentState;
        private int _currentTerm;
        private string _votedFor;
        public string NodeName { get; }
        private string _leaderName;

        private readonly IUDPNetworking _networking;
        private readonly int _listeningPort;

        private readonly Dictionary<string, NodeInfo> _nodesInfo;
        private readonly RaftDistributedLogPersistent<TKey, TValue> _distributedLog;
        private readonly Dictionary<int, ManualResetEvent> _appendEntryTasks;

        private readonly RaftPCQueue<RaftBaseMessage> _raftMessageQueue;
        private readonly MessageProcesser<RaftBaseMessage> _messageProcesser;
        private readonly StateMessageWhitelistFilter<ERaftState> _stateMessageFilter;

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
        private ManualResetEvent _onLeadershipLost;
        private CountdownEvent _countdownAppendEntryFailures;

        private bool isDisposed;

        public RaftConsensus(string nodeName, int listeningPort)
        {
            NodeName = nodeName;
            _networking = new UDPNetworking();
            _networking.OnMessageReceived += OnMessageReceived;
            _listeningPort = listeningPort;

            _currentState = ERaftState.Initializing;
            _currentTerm = 0;
            _votedFor = "";
            _leaderName = "";

            _nodesInfo = new Dictionary<string, NodeInfo>();
            _distributedLog = new RaftDistributedLogPersistent<TKey, TValue>();
            _appendEntryTasks = new Dictionary<int, ManualResetEvent>();

            _raftMessageQueue = new RaftPCQueue<RaftBaseMessage>();
            _messageProcesser = new MessageProcesser<RaftBaseMessage>();
            _messageProcesser.Register(typeof(RaftAppendEntry<TKey, TValue>), HandleRaftAppendEntry);
            _messageProcesser.Register(typeof(RaftAppendEntryResponse), HandleRaftAppendEntryResponse);
            _messageProcesser.Register(typeof(RaftRequestVote), HandleRaftRequestVote);
            _messageProcesser.Register(typeof(RaftRequestVoteResponse), HandleRaftRequestVoteResponse);

            _stateMessageFilter = new StateMessageWhitelistFilter<ERaftState>();

            _stateMessageFilter.Add(ERaftState.Follower, typeof(RaftAppendEntry<TKey, TValue>));
            _stateMessageFilter.Add(ERaftState.Follower, typeof(RaftRequestVote));

            _stateMessageFilter.Add(ERaftState.Candidate, typeof(RaftAppendEntry<TKey, TValue>));
            _stateMessageFilter.Add(ERaftState.Candidate, typeof(RaftRequestVote));
            _stateMessageFilter.Add(ERaftState.Candidate, typeof(RaftRequestVoteResponse));

            _stateMessageFilter.Add(ERaftState.Leader, typeof(RaftAppendEntry<TKey, TValue>));
            _stateMessageFilter.Add(ERaftState.Leader, typeof(RaftRequestVote));
            _stateMessageFilter.Add(ERaftState.Leader, typeof(RaftAppendEntryResponse));

            _backgroundThread = new Thread(BackgroundThread);
            _onNotifyBackgroundThread = new ManualResetEvent(false);
            _onShutdown = new ManualResetEvent(false);
            _onThreadStarted = new ManualResetEvent(false);
            _onLeadershipLost = new ManualResetEvent(false);
            _joiningClusterAttemptNumber = 0;
        }

        public Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, int maxNodes, int attempts, bool useEncryption)
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
            _networking.ClientName = NodeName;
            _networking.Start(_listeningPort);

            _maxNodes = maxNodes;

            Log(ERaftLogType.Info, "Trying to join cluster - {0}", clusterName);
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                IPEndPoint ipEndPoint = _networking.GetIPFromName(node.Key);
                Log(ERaftLogType.Debug, "I know: nodeName={0}, ipAddress={1}, port={2}", node.Key, ipEndPoint.Address.ToString(), ipEndPoint.Port);
            }

            ClusterName = clusterName;

            if (_nodesInfo.Count + 1 != maxNodes) //"+ 1" since you aren't in the nodesInfo list
            {
                ThrowInvalidOperationException("There are not enough nodes known yet");
            }

            _onWaitingToJoinCluster = new ManualResetEvent(false);

            _currentState = ERaftState.Follower;
            Log(ERaftLogType.Info, "Set state to follower");
            ChangeStateToFollower();
            StartBackgroundThread();

            return Task.Run(() => JoinClusterTask(attempts));
        }
        private EJoinClusterResponse JoinClusterTask(int attempts)
        {
            int attemptNumber = 1;
            while (true)
            {
                if (attemptNumber > attempts)
                {
                    Log(ERaftLogType.Warn, "Never found cluster after {0} attempt(s), each with {1} millisecond timeout", attemptNumber - 1, _waitingToJoinClusterTimeout);
                    Log(ERaftLogType.Info, "Shuting down background thread");
                    _onShutdown.Set();
                    _backgroundThread.Join();

                    Log(ERaftLogType.Trace, "Restoring initial state of class");
                    _currentState = ERaftState.Initializing;
                    _maxNodes = 0;
                    ClusterName = "";
                    _leaderName = "";
                    _networking.ClientName = "";
                    _networking.Stop();

                    Log(ERaftLogType.Info, "Returning no response message from join attempt");
                    return EJoinClusterResponse.NoResponse;
                }

                Log(ERaftLogType.Debug, "Waiting to join cluster...");

                if (_onWaitingToJoinCluster.WaitOne(_waitingToJoinClusterTimeout) == false)
                {
                    Log(ERaftLogType.Info, "Didn't find cluster. This was attempt {0} of {1}", attemptNumber, attempts);
                    attemptNumber += 1;
                    continue;
                }

                Log(ERaftLogType.Info, "Notifying the user we've succesfully joined the cluster");
                return EJoinClusterResponse.Accept;
            }
        }

        public Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value)
        {
            if (_currentState != ERaftState.Leader)
            {
                ThrowInvalidOperationException("You may only append entries when your UAS is active");
            }

            RaftLogEntry<TKey, TValue> entry = new RaftLogEntry<TKey, TValue>(key, value, _currentTerm);
            Log(ERaftLogType.Info, "Attempting to append new entry to log");
            Log(ERaftLogType.Debug, entry.ToString());

            Log(ERaftLogType.Trace, "Previous index: {0}", _distributedLog.LatestIndex);
            Log(ERaftLogType.Trace, "Previous term: {0}", _distributedLog.LatestIndexTerm);
            Log(ERaftLogType.Trace, "Commit index: {0}", _distributedLog.CommitIndex);
            _distributedLog.AppendEntry(entry);
            Log(ERaftLogType.Trace, "Appended entry");
            Log(ERaftLogType.Trace, "New last index: {0}", _distributedLog.LatestIndex);
            ManualResetEvent waitEvent = new ManualResetEvent(false);

            Log(ERaftLogType.Trace, "Adding event for notifying of commit");
            lock (_appendEntryTasks)
            {
                _appendEntryTasks.Add(_distributedLog.LatestIndex, waitEvent);
            }

            int temp = _distributedLog.LatestIndex;
            //If node is not up to date, Send this message

            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                if (_distributedLog.LatestIndex >= node.Value.NextIndex)
                {
                    SendMessage(MakeNextEntryForNode(node.Key, node.Value.NextIndex));
                }
            }

            return Task.Run(() => AppendEntryTask(temp, waitEvent, entry));
        }

        private ERaftAppendEntryState AppendEntryTask(int latestIndex, WaitHandle waitEvent, RaftLogEntry<TKey, TValue> entry)
        {
            int waitHandleIndex = WaitHandle.WaitAny(new[] { _onShutdown, _onLeadershipLost, waitEvent });

            lock (_appendEntryTasks)
            {
                _appendEntryTasks.Remove(latestIndex);
            }

            _countdownAppendEntryFailures?.Signal();

            switch (waitHandleIndex)
            {
                case 0:
                    Log(ERaftLogType.Info, "Failed to appended entry to log, shutting down");
                    return ERaftAppendEntryState.Failed;
                case 1:
                    Log(ERaftLogType.Info, "Failed to appended entry to log, lost leadership");
                    return ERaftAppendEntryState.Failed;
                case 2:
                    Log(ERaftLogType.Info, "Succesfully appended entry to log - {0}", entry.ToString());
                    return ERaftAppendEntryState.Commited;
                default:
                    return ERaftAppendEntryState.Failed;
            }
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

            waitLoop.RegisterAction((manualResetEvent, elapsedWait) =>
            {
                Log(ERaftLogType.Info, "Background thread has been told to shutdown");
                return true;
            }, _onShutdown);

            waitLoop.RegisterAction((manualResetEvent, elapsedWait) =>
            {
                Log(ERaftLogType.Debug, "Background thread has been notified to update");
                manualResetEvent.Reset();

                waitLoop.TimeoutMs = _currentState == ERaftState.Leader ? _heartbeatInterval : _timeoutValue;

                return false;
            }, _onNotifyBackgroundThread);

            waitLoop.RegisterAction((manualResetEvent, elapsedWait) =>
            {
                Log(ERaftLogType.Trace, "WaitLoop processing _raftMessageQueue Flag");

                ERaftState prevState = _currentState;

                ProcessMessage();

                if (prevState != _currentState)
                {
                    waitLoop.TimeoutMs = _currentState == ERaftState.Leader ? _heartbeatInterval : _timeoutValue;
                    return false;
                }

                if (_currentState == ERaftState.Follower)
                {
                    waitLoop.TimeoutMs = _timeoutValue;
                    return false;
                }
                
                waitLoop.TimeoutMs = Math.Max(0, waitLoop.TimeoutMs - elapsedWait);

                return false;
            }, _raftMessageQueue.Flag);

            waitLoop.RegisterTimeoutFunc(() =>
            {
                switch (_currentState)
                {
                    case ERaftState.Follower:
                        ProcessTimeout_Follower();
                        return false;
                    case ERaftState.Candidate:
                        ProcessTimeout_Candidate();
                        waitLoop.TimeoutMs = _timeoutValue;
                        return false;
                    case ERaftState.Leader:
                        waitLoop.TimeoutMs = ProcessTimeout_Leader();
                        return false;
                    default:
                        return true;
                }
            }, _timeoutValue);

            waitLoop.Run();
        }

        private void ProcessMessage()
        {
            while (_raftMessageQueue.Count() > 0) //TODO: Decide if keeping this
            {
                RaftBaseMessage message = _raftMessageQueue.Dequeue();
                Log(ERaftLogType.Trace, "Dequeuing message of Type {0} for processing. GUID: {1}", message.GetMessageType(), message.MessageGuid);

                if (_leaderName != "" && message.Term > _currentTerm)
                {
                    Log(ERaftLogType.Info, "The message's term is greater than ours, handling new term");
                    HandleNewTerm(message);
                }

                if (!_stateMessageFilter.Check(_currentState, message.GetMessageType()))
                {
                    Log(ERaftLogType.Trace, "Discarding message, we are currently not in the correct state to process this message");
                    return;
                }

                _messageProcesser.Process(message);
            }
        }
        private int ProcessTimeout_Leader()
        {
            int lowestNextTimeout = _heartbeatInterval;
            Log(ERaftLogType.Debug, "It's time to send out heartbeats");
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                if (node.Value.ReadyForHeartbeat(_heartbeatInterval))
                {
                    Log(ERaftLogType.Trace, "Actually sending heartbeat");
                    node.Value.UpdateLastSentHeartbeat();
                    SendMessage(MakeNextEntryForNode(node.Key, node.Value.NextIndex));
                    continue;
                }

                //node1 - 110   20
                //node2 - 90    0   150
                //node3 - 150   60

                int temp = node.Value.MsUntilTimeout(_heartbeatInterval);
                //Last time we sent them a heartbeat
                //
                if (temp < lowestNextTimeout)
                {
                    lowestNextTimeout = temp;
                }
            }

            return lowestNextTimeout;
        }
        private void ProcessTimeout_Follower()
        {
            Log(ERaftLogType.Info, "Timing out to be candidate. We haven't heard from the leader in {0}ms", _timeoutValue);
            ChangeStateToCandiate();
        }
        private void ProcessTimeout_Candidate()
        {
            Log(ERaftLogType.Info, "We didn't get voted in to be leader, time to try again");
            ChangeStateToCandiate();
        }

        private RaftAppendEntry<TKey, TValue> MakeNextEntryForNode(string nodeName)
        {
            int nodeNextIndex = _nodesInfo[nodeName].NextIndex;
            return MakeNextEntryForNode(nodeName, nodeNextIndex);
        }
        private RaftAppendEntry<TKey, TValue> MakeNextEntryForNode(string nodeName, int nodeNextIndex)
        {
            if (_distributedLog.LatestIndex < nodeNextIndex)
            {
                return new RaftAppendEntry<TKey, TValue>(nodeName, NodeName, ClusterName,
                    _currentTerm, _distributedLog.CommitIndex);
            }

            return new RaftAppendEntry<TKey, TValue>(nodeName,
                NodeName,
                ClusterName,
                _currentTerm,
                nodeNextIndex - 1,
                _distributedLog.GetTerm(nodeNextIndex - 1),
                _distributedLog.CommitIndex,
                _distributedLog.GetEntry(nodeNextIndex));
        }

        private void ChangeStateToFollower()
        {
            Log(ERaftLogType.Info, "Changing state to Follower");
            if (_currentState == ERaftState.Leader)
            {
                Log(ERaftLogType.Info, "Notifying the UAS to stop");
                OnStopUAS?.Invoke(this, EStopUasReason.ClusterLeadershipLost);
                Log(ERaftLogType.Info, "Setting all the outstanding append entry tasks to failed");
                FailAppendEntryTasks(true, false);
            }

            _currentState = ERaftState.Follower;
            RecalculateTimeoutValue();
            _onNotifyBackgroundThread.Set();
        }
        private void ChangeStateToLeader()
        {
            Log(ERaftLogType.Info, "Changing state to Leader");
            _currentState = ERaftState.Leader;
            _onNotifyBackgroundThread.Set();
            Log(ERaftLogType.Info, "Notifying UAS to start");
            OnStartUAS?.Invoke(this, null);

            Log(ERaftLogType.Debug, "Letting everyone know about our new leadership");
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                node.Value.NextIndex = _distributedLog.LatestIndex + 1;
                SendMessage(new RaftAppendEntry<TKey, TValue>(node.Key, NodeName, ClusterName, _currentTerm, _distributedLog.CommitIndex));
            }
        }
        private void ChangeStateToCandiate()
        {
            Log(ERaftLogType.Info, "Changing state to Candidate");
            _currentState = ERaftState.Candidate;
            _currentTerm += 1;
            RecalculateTimeoutValue();
            _onNotifyBackgroundThread.Set();

            Log(ERaftLogType.Info, "Requesting votes for leadership from everyone");
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                node.Value.VoteGranted = false;
                SendMessage(new RaftRequestVote(node.Key, NodeName, ClusterName, _currentTerm, _distributedLog.LatestIndex, _distributedLog.LatestIndexTerm));
            }
        }

        private void HandleRaftAppendEntry(RaftBaseMessage message)
        {
            if (message.Term < _currentTerm)
            {
                Log(ERaftLogType.Debug, "Replying false, the message's term is less than ours");
                SendMessage(new RaftAppendEntryResponse(message.From, NodeName, ClusterName, _currentTerm, false, -1));
                return;
            }

            if (_leaderName == "")
            {
                Log(ERaftLogType.Info, "We've just found the cluster");
                FoundCluster(message.From, message.Term);
            }

            if (_currentState == ERaftState.Candidate)
            {
                Log(ERaftLogType.Info, "Recieved AppendEntry from a valid leader {0}. Going back to being a follower.", message.From);
                _leaderName = message.From;
                ChangeStateToFollower();
            }

            RaftAppendEntry<TKey, TValue> appendEntryMessage = (RaftAppendEntry<TKey, TValue>) message;

            if (appendEntryMessage.Entry == null) //Is this a heart beat with no data?
            {
                Log(ERaftLogType.Debug, "This is a heatbeat message from leader {0}", message.From);
                if (appendEntryMessage.LeaderCommitIndex > _distributedLog.CommitIndex) //Check if we should move up our commit index, and respond to the leader
                {
                    Log(ERaftLogType.Info, "Heartbeat contained a request to update out commit index");
                    FollowerUpdateCommitIndex(appendEntryMessage.LeaderCommitIndex);
                }

                SendMessage(new RaftAppendEntryResponse(appendEntryMessage.From, NodeName, ClusterName, _currentTerm,
                    true,
                    _distributedLog.LatestIndex));
            }
            else
            {
                Log(ERaftLogType.Info, "This is an AppendEntry message from {0} has entries to commit", appendEntryMessage.From);

                if (_distributedLog.AppendEntry(appendEntryMessage.Entry, appendEntryMessage.PrevIndex, appendEntryMessage.PrevTerm))
                {
                    Log(ERaftLogType.Debug, "Confirmed previous index. Appended message");
                    if (appendEntryMessage.LeaderCommitIndex > _distributedLog.CommitIndex)
                    {
                        FollowerUpdateCommitIndex(appendEntryMessage.LeaderCommitIndex);
                    }
                    Log(ERaftLogType.Info, "Responding to leader with the success of our append");
                    SendMessage(new RaftAppendEntryResponse(appendEntryMessage.From, NodeName, ClusterName, _currentTerm, true,
                        _distributedLog.LatestIndex));
                }
                else
                {
                    Log(ERaftLogType.Debug, "Failed to confirm previous index, replying with false. Our previous index {0}, their previous index {1}", _distributedLog.LatestIndex, appendEntryMessage.PrevIndex);
                    SendMessage(new RaftAppendEntryResponse(appendEntryMessage.From, NodeName, ClusterName, _currentTerm, false,
                        _distributedLog.LatestIndex));
                }
            }
        }
        private void HandleRaftAppendEntryResponse(RaftBaseMessage message)
        {
            if (message.Term < _currentTerm)
            {
                Log(ERaftLogType.Debug, "Discarding message, the message's term is less than ours");
                return;
            }

            RaftAppendEntryResponse appendEntryResponse = (RaftAppendEntryResponse) message;

            NodeInfo nodeInfo = _nodesInfo[appendEntryResponse.From]; //TODO: Handle case where we've never heard from them
            nodeInfo.UpdateLastReceived();

            if (appendEntryResponse.MatchIndex == nodeInfo.MatchIndex && appendEntryResponse.Success)
            {
                if (appendEntryResponse.MatchIndex == nodeInfo.NextIndex - 1)
                {
                    Log(ERaftLogType.Debug, "It was just a heartbeat");
                }
                else
                {
                    nodeInfo.MatchIndex = appendEntryResponse.MatchIndex;
                    nodeInfo.NextIndex = appendEntryResponse.MatchIndex + 1;
                    Log(ERaftLogType.Debug,
                        "We've been told to step back their log. Their match is {0}, their new NextIndex is {1}",
                        nodeInfo.MatchIndex, nodeInfo.NextIndex);
                }

                return;
            }

            Log(ERaftLogType.Trace, "Setting {0}'s match index to {1} from {2}.", message.From, appendEntryResponse.MatchIndex, nodeInfo.MatchIndex);
            nodeInfo.MatchIndex = appendEntryResponse.MatchIndex;
            nodeInfo.NextIndex = appendEntryResponse.MatchIndex + 1;

            if (!appendEntryResponse.Success)
            {
                Log(ERaftLogType.Debug, "This follower failed to append entry. Stepping back their next index");
                if (_nodesInfo[message.From].NextIndex > 0)
                {
                    _nodesInfo[message.From].NextIndex--;
                }
                return;
            }

            Log(ERaftLogType.Info, "The append entry was a success");

            RaftAppendEntry<TKey, TValue> returnMessage = null;
            if (appendEntryResponse.MatchIndex <= _distributedLog.CommitIndex)
            {
                Log(ERaftLogType.Trace, "This node is not up to date, preparing message with next entry");
                returnMessage = MakeNextEntryForNode(message.From); //Send this message on any returns below
            }

            Log(ERaftLogType.Debug, "Since we've got another commit, we should check if we're at majority now");

            if (!CheckForCommitMajority(appendEntryResponse.MatchIndex) ||
                _distributedLog.GetTerm(appendEntryResponse.MatchIndex) != _currentTerm)
            {
                Log(ERaftLogType.Info, "We've haven't reached majority yet");
                if (returnMessage == null) return;

                Log(ERaftLogType.Trace, "Responding with just the next entry, we did not advance commit index");
                SendMessage(returnMessage);
                return;
            }

            Log(ERaftLogType.Info, "We've reached majority. Time to notify everyone to update.");

            int oldCommitIndex = _distributedLog.CommitIndex;
            _distributedLog.CommitUpToIndex(appendEntryResponse.MatchIndex);

            lock (_appendEntryTasks)
            {
                if (_appendEntryTasks.ContainsKey(appendEntryResponse.MatchIndex))
                {
                    _appendEntryTasks[appendEntryResponse.MatchIndex].Set();
                }
            }

            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                if (returnMessage != null && node.Key == message.From)
                {
                    returnMessage.LeaderCommitIndex = _distributedLog.CommitIndex;
                    SendMessage(returnMessage);
                    continue;
                }

                SendMessage(new RaftAppendEntry<TKey, TValue>(node.Key,
                    NodeName,
                    ClusterName,
                    _currentTerm,
                    _distributedLog.CommitIndex));
            }

            Log(ERaftLogType.Debug, "Running OnNewCommitedEntry. Starting from {0}, going to and including {1}",
                oldCommitIndex + 1, appendEntryResponse.MatchIndex);
            for (int i = oldCommitIndex + 1; i <= appendEntryResponse.MatchIndex; i++)
            {
                OnNewCommitedEntry?.Invoke(this, _distributedLog.GetEntry(i).ToTuple());
            }
        }
        private void HandleRaftRequestVote(RaftBaseMessage message)
        {
            RaftRequestVote requestVoteMessage = (RaftRequestVote)message;
            Log(ERaftLogType.Info, "Received a RequestVote from {0}. Let's take a look.", message.From);

            if (message.Term < _currentTerm)
            {
                Log(ERaftLogType.Debug, "Replying reject, the message's term is less than or equal to ours");
                SendMessage(new RaftRequestVoteResponse(message.From, NodeName, ClusterName, _currentTerm, false));
                return;
            }

            if (message.Term == _currentTerm && message.From == _leaderName)
            {
                Log(ERaftLogType.Debug, "Replying accept, they are the leader of our current term");
                SendMessage(new RaftRequestVoteResponse(message.From, NodeName, ClusterName, _currentTerm, true));
                return;
            }

            if (message.Term == _currentTerm)
            {
                Log(ERaftLogType.Debug, "Replying reject, the message's term is equal to ours");
                SendMessage(new RaftRequestVoteResponse(message.From, NodeName, ClusterName, _currentTerm, false));
                return;
            }

            if (_leaderName == "")
            {
                Log(ERaftLogType.Info, "We've just found the cluster");
                FoundCluster(message.From, message.Term);
            }

            if (_currentState != ERaftState.Follower)
            {
                Log(ERaftLogType.Info, "We currently aren't a follower. Changing state to follower.");
                _leaderName = null;
                ChangeStateToFollower();
            }

            if (_votedFor == "") //We haven't voted for anyone
            {
                int logLatestIndex = _distributedLog.LatestIndex;
                if (requestVoteMessage.LastLogIndex >= logLatestIndex && requestVoteMessage.LastTermIndex >= _distributedLog.GetTerm(logLatestIndex))
                {
                    Log(ERaftLogType.Info, "Their log is at least as up to date as ours, replying accept");
                    _votedFor = message.From;
                    SendMessage(new RaftRequestVoteResponse(message.From, NodeName, ClusterName, _currentTerm, true));
                }
                else
                {
                    Log(ERaftLogType.Info, "Their log is not at least as up to date as our, replying reject.");
                    SendMessage(new RaftRequestVoteResponse(message.From, NodeName, ClusterName, _currentTerm, false));
                }
            }
            else if (_votedFor == message.From)
            {
                Log(ERaftLogType.Info, "We've already voted for you? Replying accept");
                SendMessage(new RaftRequestVoteResponse(message.From, NodeName, ClusterName, _currentTerm, true));
            }
            else
            {
                Log(ERaftLogType.Info, "We've already voted for someone else in this term ({0}). Replying reject.", _votedFor);
                SendMessage(new RaftRequestVoteResponse(message.From, NodeName, ClusterName, _currentTerm, false));
            }
        }
        private void HandleRaftRequestVoteResponse(RaftBaseMessage message)
        {
            if (message.Term < _currentTerm)
            {
                Log(ERaftLogType.Debug, "Discarding message, the message's term is less than ours");
                return;
            }

            RaftRequestVoteResponse requestVoteResponseMessage = (RaftRequestVoteResponse)message;

            if (!requestVoteResponseMessage.Granted)
            {
                Log(ERaftLogType.Info, "Discarding, they rejected our request");
                return;
            }

            Log(ERaftLogType.Info, "{0} accepted our request, we have their vote", message.From);
            _nodesInfo[message.From].VoteGranted = true;
            if (CheckForVoteMajority())
            {
                Log(ERaftLogType.Info, "This vote got us to majority. Changing to leader");
                if (_leaderName == "")
                {
                    _onWaitingToJoinCluster.Set();
                }

                _leaderName = NodeName;
                ChangeStateToLeader();
            }
            else
            {
                Log(ERaftLogType.Info, "This vote didn't get us to majority. Continuing waiting...");
            }
        }

        private void FailAppendEntryTasks(bool leadershipLost, bool onShutdown)
        {
            if (leadershipLost && onShutdown) throw new ArgumentException("Both values cannot be set");
            if (!leadershipLost && !onShutdown) throw new ArgumentException("Both values cannot be set");

            lock (_appendEntryTasks)
            {
                Log(ERaftLogType.Info, "There are {0} outstanding append entry tasks", _appendEntryTasks.Count);

                if (_appendEntryTasks.Count == 0)
                {
                    return;
                }

                int countOfAppendEntryTasks = _appendEntryTasks.Count;
                _countdownAppendEntryFailures = new CountdownEvent(countOfAppendEntryTasks);

                if (leadershipLost) _onLeadershipLost.Set();
                if (onShutdown) _onShutdown.Set();

                Log(ERaftLogType.Info, "Waiting for append entry tasks to complete");
                _countdownAppendEntryFailures.Wait();
                _countdownAppendEntryFailures = null;
            }
        }
        private bool CheckForVoteMajority()
        {
            Log(ERaftLogType.Trace, "Checking to see if we have vote majority for term {0}", _currentTerm);
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                if (!node.Value.VoteGranted) continue;
                Log(ERaftLogType.Trace, "Node {0} has granted us their vote", node.Key);
                total += 1;
            }

            int majorityMinimal = _maxNodes / 2 + 1;
            Log(ERaftLogType.Trace, "Minimal majority required {0}, total voted for us {1}", majorityMinimal, total);
            return total >= majorityMinimal;
        }
        private bool CheckForCommitMajority(int index)
        {
            Log(ERaftLogType.Trace, "Checking to see if we have commit majority for index {0}", index);
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                Log(ERaftLogType.Trace, "Node {0}'s match index is {1}", node.Key, node.Value.MatchIndex);
                if (node.Value.MatchIndex < index) continue;
                Log(ERaftLogType.Trace, "Node {0} is at least as up to date as this", node.Key);
                total += 1;
            }

            int majorityMinimal = _maxNodes / 2 + 1;
            Log(ERaftLogType.Trace, "Minimal majority required {0}, total as up to date {1}", majorityMinimal, total);
            return total >= majorityMinimal;
        }
        private void HandleNewTerm(RaftBaseMessage message)
        {
            Log(ERaftLogType.Info, "Heard from node ({0}) with greater term({1}) than ours({2}). Changing to follower.", message.From, message.Term, _currentTerm);
            UpdateTerm(message.Term);
            _leaderName = message.From;
            ChangeStateToFollower();
        }
        private void FollowerUpdateCommitIndex(int leaderCommitIndex)
        {
            int newCommitIndex = Math.Min(leaderCommitIndex, _distributedLog.LatestIndex);
            Log(ERaftLogType.Debug, "Updating commit index to {0}, leader's is {1}", newCommitIndex, leaderCommitIndex);
            int oldCommitIndex = _distributedLog.CommitIndex;
            _distributedLog.CommitUpToIndex(newCommitIndex);
            Log(ERaftLogType.Debug, "Running OnNewCommitedEntry. Starting from {0}, going to and including {1}", oldCommitIndex + 1, newCommitIndex);
            for (int i = oldCommitIndex + 1; i <= newCommitIndex; i++)
            {
                OnNewCommitedEntry?.Invoke(this, _distributedLog.GetEntry(i).ToTuple());
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
        private void UpdateTerm(int newTerm)
        {
            Log(ERaftLogType.Trace, "Setting term to {0} from {1}", newTerm, _currentTerm);
            _currentTerm = newTerm;
            _votedFor = "";
            foreach (KeyValuePair<string, NodeInfo> node in _nodesInfo)
            {
                node.Value.VoteGranted = false;
            }
        }
        private void RecalculateTimeoutValue()
        {
            Log(ERaftLogType.Trace, "Previous timeout value {0}", _timeoutValue);
            _timeoutValue = Rand.Next(_timeoutValueMin, _timeoutValueMax + 1);
            Log(ERaftLogType.Trace, "New timeout value {0}", _timeoutValue);
        }

        protected virtual void OnMessageReceived(object sender, BaseMessage message)
        {
            Log(ERaftLogType.Debug, "Received message. From: {0}, Type: {1}", message.From, message.GetType());
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

            Log(ERaftLogType.Trace, "Message has passed checked. Queuing for processing. GUID: {0}", raftBaseMessage.MessageGuid);

            _raftMessageQueue.Enqueue(raftBaseMessage);
        }
        private void SendMessage(RaftBaseMessage message)
        {
            Log(ERaftLogType.Debug, "Sending message. To: {0}, type: {1}", message.To, message.GetType());
            Log(ERaftLogType.Trace, "Sending message contents: {0}", message.ToString());
            _networking.SendMessage(message);
        }
        public void ManualAddPeer(string name, IPEndPoint endPoint)
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

        private void Log(ERaftLogType logType, string format, params object[] args)
        {
            string messagePrepend = $"{NodeName} (Status={_currentState.ToString()}) - ";
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

        public bool HasJoinedCluster()
        {
            return _currentState == ERaftState.Follower ||
                   _currentState == ERaftState.Candidate ||
                   _currentState == ERaftState.Leader;
        }
        public bool IsUASRunning()
        {
            return _currentState == ERaftState.Leader;
        }

        public int NumberOfCommits()
        {
            return _distributedLog.CommitIndex + 1;
        }
        public TValue ReadEntryValue(TKey key)
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
        public TValue[] ReadEntryValueHistory(TKey key)
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

        public void Dispose()
        {
            if(isDisposed) return;
            isDisposed = true;

            Log(ERaftLogType.Trace, "Disposing {0}, previous status: {1}", NodeName, _currentState);

            if (_currentState == ERaftState.Leader)
            {
                Log(ERaftLogType.Trace, "We were leader, sending message out to stop UAS");
                OnStopUAS?.Invoke(this, EStopUasReason.ClusterStop);
                Log(ERaftLogType.Info, "Setting all the outstanding append entry tasks to failed");
                FailAppendEntryTasks(false, true);
            }

            Log(ERaftLogType.Trace, "Shutting down background thread, if running");
            _onShutdown.Set();
            if (_backgroundThread.IsAlive)
            {
                _backgroundThread.Join();
            }

            Log(ERaftLogType.Trace, "Background thread completed shutdown, if running");

            Log(ERaftLogType.Trace, "Disposing network, if required");
            _networking?.Dispose();
            Log(ERaftLogType.Trace, "Network disposed, if required");

            _currentState = ERaftState.Stopped;
        }
    }
}
