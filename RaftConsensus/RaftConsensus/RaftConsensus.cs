using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftCommon;
using TeamDecided.RaftCommon.Logging;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftConsensus.Interfaces;
using TeamDecided.RaftConsensus.RaftMessages;
using TeamDecided.RaftNetworking;
using TeamDecided.RaftNetworking.Interfaces;
using TeamDecided.RaftNetworking.Messages;

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

namespace TeamDecided.RaftConsensus
{
    public class RaftConsensus<TKey, TValue> : IConsensus<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        private static Random rand = new Random();
        private string clusterName;
        private int maxNodes;
        private ERaftState currentState;
        private object currentStateLockObject;
        private int currentTerm;
        private string votedFor;
        private object votedForLockObject;
        private object currentTermLockObject;
        private Dictionary<string, NodeInfo> nodesInfo;
        private object nodesInfoLockObject;
        private IUDPNetworking networking;
        private List<Tuple<string, string, int>> manuallyAddedClients;
        private int listeningPort;
        private string nodeName;
        private string leaderName;

        private List<Tuple<string, IPEndPoint>> manuallyAddedPeers;

        private RaftDistributedLog<TKey, TValue> distributedLog;
        private object distributedLogLockObject;

        #region Timeout values
        private const int networkLatency = 50; //ms
        private int heartbeatInterval = networkLatency * 3;
        private int timeoutValueMin = 10 * networkLatency;
        private int timeoutValueMax = 5 * 10 * networkLatency;
        private int timeoutValue; //The actual timeout value chosen
        private object timeoutValueLockObject;
        #endregion

        private Thread backgroundThread;
        private ManualResetEvent onNotifyBackgroundThread;
        private ManualResetEvent onReceivedMessage;
        private ManualResetEvent onShutdown;
        private CountdownEvent onThreadsStarted;

        private ManualResetEvent onWaitingToJoinCluster;
        private int waitingToJoinClusterTimeout = 5000;
        private object eJoinClusterResponeLockObject;
        private int joiningClusterAttemptNumber;

        private Dictionary<int, ManualResetEvent> appendEntryTasks;
        private object appendEntryTasksLockObject;

        public event EventHandler StartUAS;
        public event EventHandler<EStopUASReason> StopUAS;
        public event EventHandler<Tuple<TKey, TValue>> OnNewCommitedEntry;

        private bool disposedValue = false; // To detect redundant calls

        public RaftConsensus(string nodeName, int listeningPort)
        {
            currentState = ERaftState.INITIALIZING;
            currentStateLockObject = new object();
            currentTerm = 0;
            votedFor = "";
            votedForLockObject = new object();
            currentTermLockObject = new object();
            nodesInfo = new Dictionary<string, NodeInfo>();
            nodesInfoLockObject = new object();
            this.listeningPort = listeningPort;
            this.nodeName = nodeName;
            manuallyAddedPeers = new List<Tuple<string, IPEndPoint>>();
            distributedLog = new RaftDistributedLog<TKey, TValue>();
            distributedLogLockObject = new object();

            timeoutValueLockObject = new object();

            backgroundThread = new Thread(new ThreadStart(BackgroundThread));
            onNotifyBackgroundThread = new ManualResetEvent(false);
            onReceivedMessage = new ManualResetEvent(false);
            onShutdown = new ManualResetEvent(false);
            onThreadsStarted = new CountdownEvent(1);
            eJoinClusterResponeLockObject = new object();
            joiningClusterAttemptNumber = 0;

            appendEntryTasks = new Dictionary<int, ManualResetEvent>();
            appendEntryTasksLockObject = new object();

            manuallyAddedClients = new List<Tuple<string, string, int>>();
        }

        public Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, int maxNodes, bool useEncryption)
        {
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.INITIALIZING)
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

                Log(ERaftLogType.INFO, "Starting networking stack");

                if(useEncryption)
                {
                    networking = new UDPNetworkingSecure(clusterPassword);
                }
                else
                {
                    networking = new UDPNetworking();
                }

                networking.SetClientName(nodeName);
                networking.Start(listeningPort);
                networking.OnMessageReceived += OnMessageReceive;
                FlushNetworkPeerBuffer();

                this.maxNodes = maxNodes;

                Log(ERaftLogType.INFO, "Trying to join cluster - {0}", clusterName);
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    IPEndPoint ipEndPoint = networking.GetIPFromName(node.Key);
                    Log(ERaftLogType.DEBUG, "I know: nodeName={0}, ipAddress={1}, port={2}", node.Key, ipEndPoint.Address.ToString(), ipEndPoint.Port);
                }

                this.clusterName = clusterName;

                lock (nodesInfoLockObject)
                {
                    if (nodesInfo.Count + 1 != maxNodes) //You aren't in the nodesInfo list
                    {
                        ThrowInvalidOperationException("There are not enough nodes known yet");
                    }

                    onWaitingToJoinCluster = new ManualResetEvent(false);
                }

                currentState = ERaftState.FOLLOWER;
                Log(ERaftLogType.INFO, "Set state to follower");
                StartBackgroundThread();
                ChangeStateToFollower();

                Task<EJoinClusterResponse> task = Task.Run(() =>
                {
                    if (onWaitingToJoinCluster.WaitOne(waitingToJoinClusterTimeout) == false) //The timeout occured
                    {
                        Log(ERaftLogType.WARN, "Never found cluster in {0} millisecond timeout", waitingToJoinClusterTimeout);
                        Log(ERaftLogType.INFO, "Shutdown background thread");
                        onShutdown.Dispose();
                        lock (currentStateLockObject)
                        {
                            Log(ERaftLogType.INFO, "Set state to initializing");
                            currentState = ERaftState.INITIALIZING;
                        }
                        Log(ERaftLogType.INFO, "Disposing networking");
                        networking.Dispose();
                        this.maxNodes = 0;
                        Log(ERaftLogType.INFO, "Returning no response message from join attempt");
                        return EJoinClusterResponse.NO_RESPONSE;
                    }
                    else
                    {
                        Log(ERaftLogType.INFO, "Notifying the user we've succesfully joined the cluster");
                        return EJoinClusterResponse.ACCEPT; //We found the cluster
                    }
                });

                return task;
            }
        }

        private void StartBackgroundThread()
        {
            Log(ERaftLogType.INFO, "Starting background thread");
            backgroundThread.Start();
            onThreadsStarted.Wait();
            Log(ERaftLogType.INFO, "Started background thread");
        }

        public string GetClusterName()
        {
            if (clusterName == "")
            {
                ThrowInvalidOperationException("Cluster name not set yet, please join a cluster or create a cluster");
            }
            return clusterName;
        }

        private void ThrowInvalidOperationException(string exceptionMessage)
        {
            Log(ERaftLogType.WARN, exceptionMessage);
            throw new InvalidOperationException(exceptionMessage);
        }
        private void ThrowArgumentException(string exceptionMessage)
        {
            Log(ERaftLogType.WARN, exceptionMessage);
            throw new ArgumentException(exceptionMessage);
        }

        public string GetNodeName()
        {
            return nodeName;
        }
        public TValue ReadEntryValue(TKey key)
        {
            lock (distributedLogLockObject)
            {
                try
                {
                    return distributedLog[key].Value;
                }
                catch (KeyNotFoundException e)
                {
                    Log(ERaftLogType.WARN, "Failed to ReadEntryValue for key: {0}", key);
                    throw e;
                }
            }
        }
        public TValue[] ReadEntryValueHistory(TKey key)
        {
            lock (distributedLogLockObject)
            {
                try
                {
                    return distributedLog.GetValueHistory(key);
                }
                catch (KeyNotFoundException e)
                {
                    Log(ERaftLogType.WARN, "Failed to ReadEntryValueHistory for key: {0}", key);
                    throw e;
                }
            }
        }
        public void ManualAddPeer(string name, IPEndPoint endPoint)
        {
            Log(ERaftLogType.DEBUG, "Manually adding peer {0}'s IP details. Address {1}, port {2}", name, endPoint.Address.ToString(), endPoint.Port);
            manuallyAddedClients.Add(new Tuple<string, string, int>(name, endPoint.Address.ToString(), endPoint.Port));
            lock (nodesInfoLockObject)
            {
                nodesInfo.Add(name, new NodeInfo(name));
            }
        }
        private void FlushNetworkPeerBuffer()
        {
            Log(ERaftLogType.INFO, "Flushing network peer buffer");
            Log(ERaftLogType.DEBUG, "Buffer has {0} entries", manuallyAddedClients.Count);

            int succesfulAdd = 0;
            foreach (Tuple<string, string, int> peer in manuallyAddedClients)
            {
                if (!networking.HasPeer(peer.Item1))
                {
                    Log(ERaftLogType.TRACE, "Adding node. Node name {0}, node IP {1}, node port {2}", peer.Item1, peer.Item2, peer.Item3);
                    networking.ManualAddPeer(peer.Item1, new IPEndPoint(IPAddress.Parse(peer.Item2), peer.Item3));
                    succesfulAdd += 1;
                }
            }

            Log(ERaftLogType.DEBUG, "Added {0}/{1} entries into UDPNetworking", succesfulAdd, manuallyAddedClients.Count);
            Log(ERaftLogType.INFO, "Flushed network peer buffer");
        }
        public Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value)
        {
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.LEADER)
                {
                    ThrowInvalidOperationException("You may only append entries when your UAS is active");
                }

                RaftLogEntry<TKey, TValue> entry;
                lock (currentTermLockObject)
                {
                    entry = new RaftLogEntry<TKey, TValue>(key, value, currentTerm);
                    Log(ERaftLogType.INFO, "Attempting to append new entry to log");
                    Log(ERaftLogType.DEBUG, entry.ToString());

                    int prevIndex;
                    int prevTerm;
                    int commitIndex;
                    ManualResetEvent waitEvent;
                    int currentLastIndex;

                    lock (distributedLogLockObject)
                    {
                        prevIndex = distributedLog.GetLastIndex();
                        Log(ERaftLogType.TRACE, "Previous index: {0}", prevIndex);
                        prevTerm = distributedLog.GetTermOfLastIndex();
                        Log(ERaftLogType.TRACE, "Previous term: {0}", prevTerm);
                        commitIndex = distributedLog.CommitIndex;
                        Log(ERaftLogType.TRACE, "Commit index: {0}", commitIndex);
                        distributedLog.AppendEntry(entry, distributedLog.GetLastIndex());
                        Log(ERaftLogType.TRACE, "Appended entry");
                        waitEvent = new ManualResetEvent(false);
                        currentLastIndex = distributedLog.GetLastIndex();
                        Log(ERaftLogType.TRACE, "Current last index: {0}", currentLastIndex);
                    }

                    lock (appendEntryTasksLockObject)
                    {
                        Log(ERaftLogType.TRACE, "Adding event for notifying of commit");
                        appendEntryTasks.Add(currentLastIndex, waitEvent);
                    }

                    Task<ERaftAppendEntryState> task = Task.Run(() =>
                    {
                        //TODO: Handle the case where you stop being leader, and it can tech fail
                        waitEvent.WaitOne();
                        Log(ERaftLogType.INFO, "Succesfully appended entry to log");
                        Log(ERaftLogType.DEBUG, "Heard back from commit attempt, success. {0}", entry.ToString());
                        return ERaftAppendEntryState.COMMITED;
                    });

                    return task;
                }
            }
        }
        public bool IsUASRunning()
        {
            lock (currentStateLockObject)
            {
                return currentState == ERaftState.LEADER;
            }
        }

        private void BackgroundThread()
        {
            WaitHandle[] waitHandles = new WaitHandle[] { onShutdown, onNotifyBackgroundThread };
            onThreadsStarted.Signal();
            Log(ERaftLogType.INFO, "Background thread initialised");
            ERaftState threadState = ERaftState.INITIALIZING;
            while (true)
            {
                int indexOuter = 0;
                try
                {
                    indexOuter = WaitHandle.WaitAny(waitHandles);
                }
                catch(Exception e)
                {
                    Log(ERaftLogType.FATAL, "Threw exception when waiting for events", e.ToString());
                }

                if (indexOuter == 0)
                {
                    Log(ERaftLogType.INFO, "Background thread has been told to shutdown");
                    return;
                }
                else if (indexOuter == 1)
                {
                    Log(ERaftLogType.DEBUG, "Background thread has been notified to update");
                    onNotifyBackgroundThread.Reset();
                    lock (currentStateLockObject)
                    {
                        threadState = currentState;
                    }
                }

                if (threadState == ERaftState.FOLLOWER)
                {
                    BackgroundThread_Follower(waitHandles);
                }
                else if (threadState == ERaftState.CANDIDATE)
                {
                    BackgroundThread_Candidate(waitHandles);
                }
                else if (threadState == ERaftState.LEADER)
                {
                    BackgroundThread_Leader(waitHandles);
                }
            }
        }
        private void BackgroundThread_Leader(WaitHandle[] waitHandles)
        {
            Log(ERaftLogType.DEBUG, "Background thread now running as Leader");
            while (true)
            {
                int index = WaitHandle.WaitAny(waitHandles, heartbeatInterval);
                if (index == WaitHandle.WaitTimeout)
                {
                    Log(ERaftLogType.DEBUG, "It's time to send out heartbeats.");
                    lock (currentTermLockObject)
                    {
                        lock (nodesInfoLockObject)
                        {
                            lock (distributedLogLockObject)
                            {
                                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                                {
                                    RaftAppendEntry<TKey, TValue> heartbeatMessage;
                                    if (distributedLog.GetLastIndex() >= node.Value.NextIndex)
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
                                            prevTerm = distributedLog.GetTermOfIndex(prevIndex);
                                        }

                                        Log(ERaftLogType.TRACE, "Previous index: {0}", prevIndex);
                                        Log(ERaftLogType.TRACE, "Previous term: {0}", prevTerm);

                                        heartbeatMessage =
                                            new RaftAppendEntry<TKey, TValue>(node.Key,
                                                                                    nodeName,
                                                                                    clusterName,
                                                                                    ELogName.UAS_LOG,
                                                                                    currentTerm,
                                                                                    prevIndex,
                                                                                    prevTerm,
                                                                                    distributedLog.CommitIndex,
                                                                                    distributedLog[node.Value.NextIndex]);
                                    }
                                    else
                                    {
                                        heartbeatMessage = new RaftAppendEntry<TKey, TValue>(node.Key, nodeName, clusterName, ELogName.UAS_LOG, currentTerm, distributedLog.CommitIndex);
                                    }
                                    SendMessage(heartbeatMessage);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log(ERaftLogType.DEBUG, "Leader thread has been signaled, {0}", index);
                    return;
                }
            }
        }
        private void BackgroundThread_Candidate(WaitHandle[] waitHandles)
        {
            Log(ERaftLogType.DEBUG, "Background thread now running as Candidate");
            int index = WaitHandle.WaitAny(waitHandles, timeoutValue);
            if (index == WaitHandle.WaitTimeout)
            {
                Log(ERaftLogType.INFO, "We didn't get voted in to be leader, time to try again");
                ChangeStateToCandiate();
                return;
            }
            else
            {
                Log(ERaftLogType.DEBUG, "Candidate thread has been signaled, {0}", index);
                return;
            }
        }
        private void BackgroundThread_Follower(WaitHandle[] waitHandles)
        {
            Log(ERaftLogType.DEBUG, "Background thread now running as Follower");
            //Add onReceivedMessage to the array of waithandle to wait on, don't edit original array
            WaitHandle[] followerWaitHandles = new WaitHandle[waitHandles.Length + 1];
            Array.Copy(waitHandles, followerWaitHandles, waitHandles.Length);
            int onReceivedMessageIndex = waitHandles.Length;
            followerWaitHandles[onReceivedMessageIndex] = onReceivedMessage;

            while (true)
            {
                int indexInner = WaitHandle.WaitAny(followerWaitHandles, timeoutValue);
                if (indexInner == WaitHandle.WaitTimeout)
                {
                    Log(ERaftLogType.INFO, "Timing out to be candidate. We haven't heard from the leader in {0} milliseconds", timeoutValue);
                    ChangeStateToCandiate();
                    return;
                }
                else if (indexInner == onReceivedMessageIndex)
                {
                    onReceivedMessage.Reset();
                }
                else //We've been signaled. Told to shutdown, onNotifyBackgroundThread doesn't impact us really
                {
                    Log(ERaftLogType.DEBUG, "Follower thread has been signaled. {0}", indexInner);
                    return;
                }
            }
        }

        private void ChangeStateToFollower()
        {
            Log(ERaftLogType.INFO, "Changing state to Follower");
            if (currentState == ERaftState.LEADER)
            {
                Log(ERaftLogType.INFO, "Notifying the UAS to stop");
                StopUAS?.Invoke(this, EStopUASReason.CLUSTER_LEADERSHIP_LOST);
            }

            currentState = ERaftState.FOLLOWER;
            RecalculateTimeoutValue();
            onNotifyBackgroundThread.Set();
        }
        private void ChangeStateToLeader()
        {
            Log(ERaftLogType.INFO, "Changing state to Leader");
            currentState = ERaftState.LEADER;

            lock (distributedLogLockObject)
            {
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    node.Value.NextIndex = distributedLog.GetLastIndex() + 1;
                }

                onNotifyBackgroundThread.Set();
                Log(ERaftLogType.DEBUG, "Letting everyone know about our new leadership");
                //Blast out to let everyone know about our victory
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    RaftAppendEntry<TKey, TValue> message = new RaftAppendEntry<TKey, TValue>(node.Key, nodeName, clusterName, ELogName.UAS_LOG, currentTerm, distributedLog.CommitIndex);
                    SendMessage(message);
                }
            }
            Log(ERaftLogType.INFO, "Notifying to start UAS");
            StartUAS?.Invoke(this, null);
        }
        private void ChangeStateToCandiate()
        {
            Log(ERaftLogType.INFO, "Changing state to Candidate");

            if(currentState == ERaftState.STOPPED)
            {
                Log(ERaftLogType.TRACE, "How did you get here?");
            }

            currentState = ERaftState.CANDIDATE;
            lock (currentTermLockObject)
            {
                currentTerm += 1;
            }
            onNotifyBackgroundThread.Set();
            RecalculateTimeoutValue();

            lock (nodesInfo)
            {
                Log(ERaftLogType.INFO, "Requesting votes for leadership from everyone");
                lock (distributedLogLockObject)
                {
                    foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                    {
                        node.Value.VoteGranted = false;
                        RaftRequestVote message = new RaftRequestVote(node.Key, nodeName, clusterName, currentTerm, distributedLog.GetLastIndex(), distributedLog.GetTermOfLastIndex());
                        SendMessage(message);
                    }
                }
            }
        }

        private void OnMessageReceive(object sender, BaseMessage message)
        {
            if (!message.GetType().IsSubclassOf(typeof(RaftBaseMessage)) && !(message.GetType() == typeof(RaftBaseMessage)))
            {
                Log(ERaftLogType.WARN, "Dropping message. We don't know what it is. It's not a RaftBaseMessage. It's a {0}", message.GetType());
                Log(ERaftLogType.TRACE, "Received message contents: {0}", message.ToString());
                return;
            }

            Log(ERaftLogType.DEBUG, "Received message. From: {0}, type: {1}", message.From, message.GetType());
            Log(ERaftLogType.TRACE, "Received message contents: {0}", message.ToString());

            if (((RaftBaseMessage)message).ClusterName != clusterName)
            {
                Log(ERaftLogType.WARN, "Dropping message. It's for the wrong cluster name");
                Log(ERaftLogType.DEBUG, "The cluster name they're attempting to join is {0}", ((RaftBaseMessage)message).ClusterName);
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
                Log(ERaftLogType.WARN, "Dropping message. This is a RaftBaseMessage we don't support. Type: {0}", message.GetType());
            }
        }

        private void HandleAppendEntry(RaftAppendEntry<TKey, TValue> message)
        {
            RaftAppendEntryResponse responseMessage;
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.LEADER && currentState != ERaftState.CANDIDATE && currentState != ERaftState.FOLLOWER)
                {
                    Log(ERaftLogType.DEBUG, "Recieved AppendEntry from node {0}. We don't recieve these types of requests", message.From);
                    return;
                }

                lock (currentTermLockObject)
                {
                    if (message.Term > currentTerm)
                    {
                        Log(ERaftLogType.INFO, "Heard from node ({0}) with greater term({1}) than ours({2}). Changing to follower.", message.From, message.Term, currentTerm);

                        if(leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                            return;
                        }

                        UpdateTerm(message.Term);
                        leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }

                    if (message.Term < currentTerm)
                    {
                        Log(ERaftLogType.DEBUG, "Recieved AppendEntry from node {0} for a previous term. Sending back a reject.", message.From);
                        responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, false, -1);
                        SendMessage(responseMessage);
                        return;
                    }

                    if (currentState == ERaftState.LEADER)
                    {
                        Log(ERaftLogType.DEBUG, "Recieved AppendEntry from node {0}. Discarding as we're the leader.", message.From);
                        return;
                    }
                    else if (currentState == ERaftState.CANDIDATE)
                    {
                        Log(ERaftLogType.INFO, "Recieved AppendEntry from the leader {0} of currentTerm. Going back to being a follower.", message.From);
                        if (leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                        }
                        leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }
                    else if (currentState == ERaftState.FOLLOWER)
                    {
                        if (leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                            return;
                        }
                    }
                }

                RecalculateTimeoutValue();
                onReceivedMessage.Set();

                lock (currentTermLockObject)
                {
                    lock (distributedLogLockObject)
                    {
                        if (message.Entry == null) //Check if this is a heart beat with no data
                        {
                            Log(ERaftLogType.DEBUG, "This is a heatbeat message from leader {0}", message.From);
                            //Check if we should move up our commit index, and respond to the leader
                            if (message.LeaderCommitIndex > distributedLog.CommitIndex)
                            {
                                Log(ERaftLogType.INFO, "Heartbeat contained a request to update out commit index");
                                FollowerUpdateCommitIndex(message.LeaderCommitIndex);
                            }
                            responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, true, distributedLog.GetLastIndex());
                            SendMessage(responseMessage);
                            return;
                        }
                        else
                        {
                            Log(ERaftLogType.INFO, "This is a AppendEntry message from {0} has new entries to commit", message.From);

                            if (distributedLog.GetLastIndex() > message.PrevIndex)
                            {
                                Log(ERaftLogType.DEBUG, "We received a message but our latestIndex ({0}) was greater than their PrevIndex ({0}). Truncating the rest of the log forward for safety", distributedLog.GetLastIndex(), message.PrevIndex);
                                distributedLog.TruncateLog(message.PrevIndex + 1);
                            }

                            if (distributedLog.GetLastIndex() == message.PrevIndex)
                            {
                                if (distributedLog.ConfirmPreviousIndex(message.PrevIndex, message.PrevTerm))
                                {
                                    distributedLog.AppendEntry(message.Entry, message.PrevIndex);
                                    Log(ERaftLogType.DEBUG, "Confirmed previous index. Appended message");
                                    if (message.LeaderCommitIndex > distributedLog.CommitIndex)
                                    {
                                        FollowerUpdateCommitIndex(message.LeaderCommitIndex);
                                    }
                                    Log(ERaftLogType.INFO, "Responding to leader with the success of our append");
                                    responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, true, distributedLog.GetLastIndex());
                                    SendMessage(responseMessage);
                                }
                            }
                            else if (distributedLog.GetLastIndex() < message.PrevIndex)
                            {
                                Log(ERaftLogType.DEBUG, "Got entry we weren't ready for, replying with false. Our previous index {0}, their previous index {1}", distributedLog.GetLastIndex(), message.PrevIndex);
                                responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, false, distributedLog.GetLastIndex());
                                SendMessage(responseMessage);
                            }
                        }
                    }
                }
            }
        }
        private void HandleAppendEntryResponse(RaftAppendEntryResponse message)
        {
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.LEADER)
                {
                    Log(ERaftLogType.DEBUG, "Recieved AppendEntry from node {0}. We don't recieve these types of requests", message.From);
                    return;
                }
                lock (currentTermLockObject)
                {
                    //Are we behind, and we've now got a new leader?
                    if (message.Term > currentTerm)
                    {
                        Log(ERaftLogType.INFO, "Heard from a node ({0}) with greater term({1}) than ours({2}). Changing to follower", message.From, message.Term, currentTerm);

                        if (leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                            return;
                        }

                        UpdateTerm(message.Term);
                        leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }

                    if (message.Term < currentTerm)
                    {
                        Log(ERaftLogType.DEBUG, "Recieved AppendEntryResponse from node {0} for a previous term. Discarding.", message.From);
                        return;
                    }

                    if (currentState != ERaftState.LEADER)
                    {
                        return; //We don't recieve these type of requests, drop it
                    }
                    Log(ERaftLogType.TRACE, "Recieved AppendEntryResponse from node {0}", message.From);

                    lock (nodesInfoLockObject)
                    {
                        NodeInfo nodeInfo = nodesInfo[message.From];
                        nodeInfo.UpdateLastReceived();

                        if (message.MatchIndex == nodeInfo.MatchIndex && message.Success)
                        {
                            if (message.MatchIndex != nodeInfo.NextIndex - 1)
                            {
                                nodeInfo.MatchIndex = message.MatchIndex;
                                nodeInfo.NextIndex = message.MatchIndex + 1;
                                Log(ERaftLogType.DEBUG, "We've been told to step back their log. Their match is {0}, their new NextIndex is {1}", nodeInfo.MatchIndex, nodeInfo.NextIndex);
                            }
                            else
                            {
                                Log(ERaftLogType.DEBUG, "It was just a heartbeat");
                            }
                            return;
                        }
                        Log(ERaftLogType.INFO, "Setting {0}'s match index to {1} from {2}.", message.From, message.MatchIndex, nodeInfo.MatchIndex);
                        nodeInfo.MatchIndex = message.MatchIndex;
                        nodeInfo.NextIndex = message.MatchIndex + 1;

                        if (message.Success)
                        {
                            Log(ERaftLogType.INFO, "The append entry was a success");
                            lock (distributedLogLockObject)
                            {
                                if (message.MatchIndex > distributedLog.CommitIndex)
                                {
                                    Log(ERaftLogType.DEBUG, "Since we've got another commit, we should check if we're at majority now");
                                    //Now we've got another commit, have we reached majority now?
                                    if (CheckForCommitMajority(message.MatchIndex) && distributedLog.GetTermOfIndex(message.MatchIndex) == currentTerm)
                                    {
                                        Log(ERaftLogType.INFO, "We've reached majority. Time to notify everyone to update.");
                                        //We have! Update our log. Notify everyone to update their logs
                                        lock (appendEntryTasksLockObject)
                                        {
                                            int oldCommitIndex = distributedLog.CommitIndex;
                                            distributedLog.CommitUpToIndex(message.MatchIndex);
                                            appendEntryTasks[message.MatchIndex].Set();
                                            appendEntryTasks.Remove(message.MatchIndex);

                                            foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                                            {
                                                RaftAppendEntry<TKey, TValue> updateMessage =
                                                    new RaftAppendEntry<TKey, TValue>(node.Key,
                                                                                            nodeName,
                                                                                            clusterName,
                                                                                            ELogName.UAS_LOG,
                                                                                            currentTerm,
                                                                                            distributedLog.CommitIndex);
                                                SendMessage(updateMessage);
                                            }
                                            Log(ERaftLogType.DEBUG, "Running OnNewCommitedEntry. Starting from {0}, going to and including {1}", oldCommitIndex + 1, message.MatchIndex);
                                            for (int i = oldCommitIndex + 1; i <= message.MatchIndex; i++)
                                            {
                                                OnNewCommitedEntry?.Invoke(this, distributedLog[i].GetTuple());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log(ERaftLogType.INFO, "We've haven't reached majority yet");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log(ERaftLogType.DEBUG, "This follower failed to append entry. Stepping back their next index");
                            if(nodesInfo[message.From].NextIndex > 0)
                            {
                                nodesInfo[message.From].NextIndex--;
                            }
                        }
                    }
                }
            }
        }
        private void HandleCallElection(RaftRequestVote message)
        {
            RaftRequestVoteResponse responseMessage;
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.LEADER && currentState != ERaftState.CANDIDATE && currentState != ERaftState.FOLLOWER)
                {
                    Log(ERaftLogType.DEBUG, "Received message from {0}. We aren't in the correct state to process it. Discarding", message.From);
                    return;
                }

                lock (currentTermLockObject)
                {
                    if (message.Term > currentTerm)
                    {
                        Log(ERaftLogType.INFO, "Received a RequestVote from {0}. Let's take a look.", message.From);
                        UpdateTerm(message.Term);

                        RecalculateTimeoutValue();
                        onReceivedMessage.Set();

                        if (currentState != ERaftState.FOLLOWER)
                        {
                            Log(ERaftLogType.INFO, "We currently aren't a follower. Changing state to follower.");
                            leaderName = null;
                            ChangeStateToFollower();
                        }

                        lock (votedForLockObject)
                        {
                            if (votedFor == "") //We haven't voted for anyone
                            {
                                lock (distributedLogLockObject)
                                {
                                    int logLatestIndex = distributedLog.GetLastIndex();
                                    if (message.LastLogIndex >= logLatestIndex && message.LastTermIndex >= distributedLog.GetTermOfIndex(logLatestIndex))
                                    {
                                        Log(ERaftLogType.INFO, "Their log is at least as up to date as ours, replying accept");
                                        votedFor = message.From;
                                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, true);
                                    }
                                    else
                                    {
                                        Log(ERaftLogType.INFO, "Their log is not at least as up to date as our, replying reject.");
                                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, false);
                                    }
                                }
                            }
                            else if (votedFor == message.From)
                            {
                                Log(ERaftLogType.INFO, "We've already voted for you? Replying accept");
                                responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, true);
                            }
                            else
                            {
                                Log(ERaftLogType.INFO, "We've already voted for someone else in this term ({0}). Akward. Replying rejefct.", votedFor);
                                responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, false);
                            }
                        }
                    }
                    else
                    {
                        Log(ERaftLogType.INFO, "This message is from the same or an old term, returning false");
                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, false);
                    }
                }
            }
            SendMessage(responseMessage);
        }
        private void HandleCallElectionResponse(RaftRequestVoteResponse message)
        {
            lock (currentStateLockObject)
            {
                lock (currentTermLockObject)
                {
                    if (message.Term > currentTerm)
                    {
                        Log(ERaftLogType.INFO, "Heard from a node ({0}) with greater term({1}) than ours({2}). Changing to follower", message.From, message.Term, currentTerm);
                        UpdateTerm(message.Term);
                        leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }
                    else if (message.Term < currentTerm)
                    {
                        Log(ERaftLogType.DEBUG, "Recieved RaftRequestVoteResponse from node {0} for a previous term. Discarding.", message.From);
                        return;
                    }
                }
                if (currentState == ERaftState.CANDIDATE && message.Granted)
                {
                    Log(ERaftLogType.INFO, "{0} accepted our request, we have their vote", message.From);
                    lock (currentTermLockObject) //Used by ChangeStateToLeader, maintaining lock ordering
                    {
                        lock (nodesInfoLockObject)
                        {
                            nodesInfo[message.From].VoteGranted = true;
                            if (CheckForVoteMajority())
                            {
                                Log(ERaftLogType.INFO, "This vote got us to majority. Changing to leader");
                                if (leaderName == null)
                                {
                                    onWaitingToJoinCluster.Set();
                                }
                                leaderName = nodeName;
                                ChangeStateToLeader(); //This includes sending out the blast
                            }
                            else
                            {
                                Log(ERaftLogType.INFO, "This vote didn't get us to majority. Going to continue waiting...");
                            }
                        }
                    }
                }
                else if (currentState == ERaftState.CANDIDATE && !message.Granted)
                {
                    Log(ERaftLogType.INFO, "They rejected our request. Discarding.");
                }
                else
                {
                    Log(ERaftLogType.DEBUG, "We are not in the correct state to process this message. Discarding.");
                }
            }
        }

        private void FollowerUpdateCommitIndex(int leaderCommitIndex)
        {
            int newCommitIndex = Math.Min(leaderCommitIndex, distributedLog.GetLastIndex());
            Log(ERaftLogType.DEBUG, "Updating commit index to {0}, leader's is {1}", newCommitIndex, leaderCommitIndex);
            int oldCommitIndex = distributedLog.CommitIndex;
            distributedLog.CommitUpToIndex(newCommitIndex);
            Log(ERaftLogType.DEBUG, "Running OnNewCommitedEntry. Starting from {0}, going to and including {1}", oldCommitIndex + 1, newCommitIndex);
            for (int i = oldCommitIndex + 1; i <= newCommitIndex; i++)
            {
                OnNewCommitedEntry?.Invoke(this, distributedLog[i].GetTuple());
            }
        }
        private void FoundCluster(string leader, int term)
        {
            Log(ERaftLogType.INFO, "We've found the cluster! It's node {0} on term {1}", leader, term);
            leaderName = leader;
            UpdateTerm(term);
            onWaitingToJoinCluster.Set();
            RecalculateTimeoutValue();
            onNotifyBackgroundThread.Set();
        }

        private void RecalculateTimeoutValue()
        {
            lock (timeoutValueLockObject)
            {
                Log(ERaftLogType.TRACE, "Previous timeout value {0}", timeoutValue);
                timeoutValue = rand.Next(timeoutValueMin, timeoutValueMax + 1);
                Log(ERaftLogType.TRACE, "New timeout value {0}", timeoutValue);
            }
        }
        private bool CheckForCommitMajority(int index)
        {
            Log(ERaftLogType.TRACE, "Checking to see if we have commit majority for index {0}", index);
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
            {
                Log(ERaftLogType.TRACE, "Node {0}'s match index is {1}", node.Key, node.Value.MatchIndex);
                if (node.Value.MatchIndex >= index)
                {
                    Log(ERaftLogType.TRACE, "Node {0} is at least as up to date as this", node.Key);
                    total += 1;
                }
            }

            int majorityMinimal = (maxNodes / 2) + 1;
            Log(ERaftLogType.TRACE, "Minimal majority required {0}, total as up to date {1}", majorityMinimal, total);
            return (total >= majorityMinimal);
        }
        private bool CheckForVoteMajority()
        {
            Log(ERaftLogType.TRACE, "Checking to see if we have vote majority for term {0}", currentTerm);
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
            {
                if (node.Value.VoteGranted)
                {
                    Log(ERaftLogType.TRACE, "Node {0} has granted us their vote", node.Key);
                    total += 1;
                }
            }

            int majorityMinimal = (maxNodes / 2) + 1;
            Log(ERaftLogType.TRACE, "Minimal majority required {0}, total voted for us {1}", majorityMinimal, total);
            return (total >= majorityMinimal);
        }
        private void UpdateTerm(int newTerm)
        {
            lock (nodesInfoLockObject)
            {
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    node.Value.VoteGranted = false;
                }
            }
            Log(ERaftLogType.TRACE, "Setting term to {0} from {1}", newTerm, currentTerm);
            currentTerm = newTerm;
            lock (votedForLockObject)
            {
                votedFor = "";
            }
        }

        private void SendMessage(RaftBaseMessage message)
        {
            Log(ERaftLogType.DEBUG, "Sending message. To: {0}, type: {1}", message.To, message.GetType());
            Log(ERaftLogType.TRACE, "Sending message contents: {0}", message.ToString());
            networking.SendMessage(message);
        }

        private void Log(ERaftLogType logType, string format, params object[] args)
        {
            string messagePrepend = string.Format("{0} (Status={1}) - ", nodeName, currentState.ToString());
            RaftLogging.Instance.Log(logType, messagePrepend + format, args);
        }

        #region Get/set timeout/heartbeat values
        public int GetHeartbeatInterval()
        {
            return heartbeatInterval;
        }

        public void SetHeartbeatInterval(int value)
        {
            lock (currentStateLockObject)
            {
                if (currentState == ERaftState.INITIALIZING)
                {
                    heartbeatInterval = value;
                }
                else
                {
                    ThrowInvalidOperationException("You may not set this value while service is running, only before it starts");
                }
            }
        }

        public int GetWaitingForJoinClusterTimeout()
        {
            return waitingToJoinClusterTimeout;
        }

        public void SetWaitingForJoinClusterTimeout(int value)
        {
            lock (currentStateLockObject)
            {
                if (currentState == ERaftState.INITIALIZING)
                {
                    waitingToJoinClusterTimeout = value;
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    ERaftState previousStatus;
                    lock (currentStateLockObject)
                    {
                        previousStatus = currentState;
                        currentState = ERaftState.STOPPED;
                        Log(ERaftLogType.TRACE, "Disposing {0}, previous status: {1}", nodeName, previousStatus);
                    }
                    if (previousStatus != ERaftState.INITIALIZING)
                    {
                        Log(ERaftLogType.TRACE, "We've also got to do some cleanup");
                        if (previousStatus == ERaftState.LEADER)
                        {
                            Log(ERaftLogType.TRACE, "We were leader, sending message out to stop UAS");
                            StopUAS?.Invoke(this, EStopUASReason.CLUSTER_STOP);
                        }
                        Log(ERaftLogType.TRACE, "Shutting down background thread");
                        onShutdown.Set();
                        backgroundThread.Join();
                        Log(ERaftLogType.TRACE, "Background thread completed shutdown. Disposing network.");
                        networking.Dispose();
                        Log(ERaftLogType.TRACE, "Network disposed");
                    }
                    else if (previousStatus == ERaftState.INITIALIZING && joiningClusterAttemptNumber > 0)
                    {
                        Log(ERaftLogType.TRACE, "Just had to dispose the network");
                        networking.Dispose();
                        Log(ERaftLogType.TRACE, "Network disposed");
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
