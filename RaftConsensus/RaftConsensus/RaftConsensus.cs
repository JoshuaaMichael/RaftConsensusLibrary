using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftConsensus.Interfaces;
using TeamDecided.RaftConsensus.RaftMessages;
using TeamDecided.RaftNetworking;
using TeamDecided.RaftNetworking.Interfaces;
using TeamDecided.RaftNetworking.Messages;

/* TODO: Correct naming convention issue between value/entry for the log
 * Add stopping cluster ability
 *      - Need to commit out a stop message to a majority, not like a vote
 *      - If they reach majority they tell everyone to stop
 *      - If you're not part of the majority, or you're left behind, you time out after 2 minutes with a "where did my cluster go" message
 * Add ability to send multiple log entries in a packet, well be careful of packet size
 */

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
        private int listeningPort;
        private string nodeName;
        private string leaderName;

        private List<Tuple<string, IPEndPoint>> manuallyAddedPeers;

        private RaftDistributedLog<TKey, TValue> distributedLog;
        private object distributedLogLockObject;

        #region Timeout values
        private const int networkLatency = 100; //ms
        private int heartbeatInterval = networkLatency * 3;
        private int timeoutValueMin = 10 * networkLatency;
        private int timeoutValueMax = 2 * 10 * networkLatency;
        private int timeoutValue; //The actual timeout value chosen
        private object timeoutValueLockObject;
        private DateTime lastReceivedMessage;
        private object lastReceivedMessageLock;
        #endregion

        private Task backgroundThread;
        private ManualResetEvent onNotifyBackgroundThread;
        private ManualResetEvent onReceivedMessage;
        private ManualResetEvent onShutdown;
        private CountdownEvent onThreadsStarted;

        private ManualResetEvent onWaitingToJoinCluster;
        private const int waitingToJoinClusterTimeout = 200000; //ms
        private EJoinClusterResponse eJoinClusterResponse;
        private object eJoinClusterResponeLockObject;
        private int joiningClusterAttemptNumber;

        private Dictionary<int, ManualResetEvent> appendEntryTasks;
        private object appendEntryTasksLockObject;

        public event EventHandler StartUAS;
        public event EventHandler<EStopUASReason> StopUAS;

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
            lastReceivedMessageLock = new object();

            backgroundThread = new Task(BackgroundThread, TaskCreationOptions.LongRunning);
            onNotifyBackgroundThread = new ManualResetEvent(false);
            onReceivedMessage = new ManualResetEvent(false);
            onShutdown = new ManualResetEvent(false);
            onThreadsStarted = new CountdownEvent(1);
            eJoinClusterResponse = EJoinClusterResponse.NOT_YET_SET;
            eJoinClusterResponeLockObject = new object();
            joiningClusterAttemptNumber = 0;

            appendEntryTasks = new Dictionary<int, ManualResetEvent>();
            appendEntryTasksLockObject = new object();

            networking = new UDPNetworking();
            networking.Start(listeningPort);
            networking.OnMessageReceived += OnMessageReceive;
        }

        public Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, int maxNodes)
        {
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.INITIALIZING)
                {
                    throw new InvalidOperationException("You may only join, or attempt to join, one cluster at a time");
                }
                if (string.IsNullOrWhiteSpace(clusterName))
                {
                    throw new ArgumentException("clusterName must not be blank");
                }
                if (string.IsNullOrWhiteSpace(clusterPassword))
                {
                    throw new ArgumentException("clusterPassword must not be blank");
                }
                lock (nodesInfoLockObject)
                {
                    if (nodesInfo.Count + 1 != maxNodes) //You aren't in the nodesInfo list
                    {
                        throw new InvalidOperationException("There are not enough nodes known yet");
                    }

                    StartThreads();

                    onWaitingToJoinCluster = new ManualResetEvent(false);

                    currentState = ERaftState.ATTEMPTING_TO_JOIN_CLUSTER;
                    joiningClusterAttemptNumber += 1;

                    foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                    {
                        RaftJoinCluster message = new RaftJoinCluster(node.Key, nodeName, clusterName, joiningClusterAttemptNumber);
                        networking.SendMessage(message);
                    }
                }

                Task<EJoinClusterResponse> task = Task.Run(() =>
                {
                    if (onWaitingToJoinCluster.WaitOne(waitingToJoinClusterTimeout) == false) //The timeout occured
                    {
                        lock (currentStateLockObject)
                        {
                            currentState = ERaftState.INITIALIZING;
                        }
                        onWaitingToJoinCluster = null;
                        return EJoinClusterResponse.NO_RESPONSE;
                    }
                    onWaitingToJoinCluster = null;
                    lock (eJoinClusterResponeLockObject)
                    {
                        return eJoinClusterResponse;
                    }
                });

                return task;
            }
        }
        public void CreateCluster(string clusterName, string clusterPassword, int maxNodes)
        {
            //TODO: clusterPassword will be used by IUDPNetworking when it's implementing the UDPNetworkingSecure
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.INITIALIZING)
                {
                    throw new InvalidOperationException("You may only create one cluster at a time, and you may only do it before joining a cluster");
                }
                if (string.IsNullOrWhiteSpace(clusterName))
                {
                    throw new ArgumentException("clusterName must not be blank");
                }
                if (string.IsNullOrWhiteSpace(clusterPassword))
                {
                    throw new ArgumentException("clusterPassword must not be blank");
                }
                if (!(maxNodes >= 3 && maxNodes % 2 == 1))
                {
                    throw new ArgumentException("Number of maxNodes must be greater than or equal to 3, and then also be an odd number");
                }
                lock (nodesInfoLockObject)
                {
                    if (nodesInfo.Count + 1 != maxNodes) //You aren't in the nodesInfo list
                    {
                        throw new InvalidOperationException("There are not enough nodes known yet");
                    }
                }

                this.clusterName = clusterName;
                this.maxNodes = maxNodes;

                currentState = ERaftState.LEADER;
                onNotifyBackgroundThread.Set();

                StartThreads();
                StartUAS?.Invoke(this, null);
            }
        }

        private void StartThreads()
        {
            backgroundThread.Start();
            onThreadsStarted.Wait();
        }

        public string GetClusterName()
        {
            if (clusterName == "")
            {
                throw new InvalidOperationException("Cluster name not set yet, please join a cluster or create a cluster");
            }
            return clusterName;
        }
        public string GetNodeName()
        {
            return nodeName;
        }
        public TValue ReadEntryValue(TKey key)
        {
            lock (distributedLogLockObject)
            {
                return distributedLog.GetValue(key);
            }
        }
        public TValue[] ReadEntryValueHistory(TKey key)
        {
            lock (distributedLogLockObject)
            {
                return distributedLog.GetValueHistory(key);
            }
        }
        public void ManualAddPeer(string name, IPEndPoint endPoint)
        {
            networking.ManualAddPeer(name, endPoint);
            lock (nodesInfoLockObject)
            {
                nodesInfo.Add(name, new NodeInfo(name));
            }
        }
        public Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value)
        {
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.LEADER)
                {
                    throw new InvalidOperationException("You may only append entries when your UAS is active");
                }

                RaftLogEntry<TKey, TValue> entry;
                lock (currentTermLockObject)
                {
                    entry = new RaftLogEntry<TKey, TValue>(key, value, currentTerm);


                    int prevIndex;
                    int prevTerm;
                    int commitIndex;
                    ManualResetEvent waitEvent;
                    int currentLastIndex;

                    lock (distributedLogLockObject)
                    {
                        prevIndex = distributedLog.GetLastIndex();
                        prevTerm = distributedLog.GetTermOfLastCommit();
                        commitIndex = distributedLog.CommitIndex;
                        distributedLog.AppendEntry(entry, distributedLog.GetLastIndex()); //TODO: Clean up this append
                        waitEvent = new ManualResetEvent(false);
                        currentLastIndex = distributedLog.GetLastIndex();
                    }

                    lock (appendEntryTasksLockObject)
                    {
                        appendEntryTasks.Add(currentLastIndex, waitEvent);
                    }

                    Task<ERaftAppendEntryState> task = Task.Run(() =>
                    {
                        //TODO: Handle the case where you stop being leader, and it can tech fail
                        waitEvent.WaitOne();
                        return ERaftAppendEntryState.COMMITED;
                    });

                    lock (nodesInfoLockObject)
                    {
                        foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                        {
                            RaftAppendEntry<TKey, TValue> message =
                                new RaftAppendEntry<TKey, TValue>(node.Key,
                                                                        nodeName,
                                                                        ELogName.UAS_LOG,
                                                                        currentTerm,
                                                                        prevIndex,
                                                                        prevTerm,
                                                                        commitIndex,
                                                                        entry);
                            networking.SendMessage(message);
                        }
                    }

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
            ERaftState threadState = ERaftState.INITIALIZING;
            while (true)
            {
                int indexOuter = WaitHandle.WaitAny(waitHandles);
                if (indexOuter == 0) //We've been told to shutdown
                {
                    return;
                }
                else if (indexOuter == 1) //We've been notified to update
                {
                    onNotifyBackgroundThread.Reset();
                    lock (currentStateLockObject)
                    {
                        threadState = currentState;
                    }
                }
                //Next, execute the background logic of whichever state we're in

                if (threadState == ERaftState.FOLLOWER)
                {
                    BackgroundThread_Follower(waitHandles); //We're a follower, so we'll be checking for timeouts from hearing append entry messages
                }
                else if (threadState == ERaftState.CANDIDATE)
                {
                    BackgroundThread_Candidate(waitHandles); //We're a candidate, so we'll be checking for timeouts from our attempt to become leader
                }
                else if(threadState == ERaftState.LEADER)
                {
                    BackgroundThread_Leader(waitHandles); //We're a leader, so we'll be sending heart beats
                }
            }
        }
        private void BackgroundThread_Leader(WaitHandle[] waitHandles)
        {
            while (true)
            {
                //TODO: Do the math to figure out the next heartbeatTime, technically this way it'll be offset at like every timeout-1ms or something due to the effort when it goes in... technically
                if (WaitHandle.WaitAny(waitHandles, heartbeatInterval) == WaitHandle.WaitTimeout)
                {
                    //Time to send heart beats
                    lock (currentTermLockObject)
                    {
                        lock (nodesInfoLockObject)
                        {
                            lock (distributedLogLockObject)
                            {
                                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                                {
                                    RaftAppendEntry<TKey, TValue> heartbeatMessage;
                                    if (distributedLog.GetLastIndex() > node.Value.NextIndex)
                                    {
                                        int prevIndex = node.Value.NextIndex - 1;
                                        int prevTerm = distributedLog.GetTermOfIndex(prevIndex);
                                        heartbeatMessage =
                                            new RaftAppendEntry<TKey, TValue>(node.Key,
                                                                                    nodeName,
                                                                                    ELogName.UAS_LOG,
                                                                                    currentTerm,
                                                                                    prevIndex,
                                                                                    prevTerm,
                                                                                    distributedLog.CommitIndex,
                                                                                    distributedLog[node.Value.NextIndex]);
                                    }
                                    else
                                    {
                                        heartbeatMessage = new RaftAppendEntry<TKey, TValue>(node.Key, nodeName, ELogName.UAS_LOG, currentTerm, distributedLog.CommitIndex);
                                    }
                                    networking.SendMessage(heartbeatMessage);
                                }
                            }
                        }
                    }
                }
                else //We've been signaled. Told to shutdown or we've stopped being leader
                {
                    return;
                }
            }
        }
        private void BackgroundThread_Candidate(WaitHandle[] waitHandles)
        {
            if(WaitHandle.WaitAny(waitHandles, timeoutValue) == WaitHandle.WaitTimeout)
            {
                //We didn't hear from anyone, so we've got to go candidate again to try be leader... again
                ChangeStateToCandiate();
                return;
            }
            else //We've been signaled. Told to shutdown or we're no longer candidate
            {
                return;
            }
        }
        private void BackgroundThread_Follower(WaitHandle[] waitHandles)
        {
            //Add onReceivedMessage to the array of waithandle to wait on, don't edit original array
            WaitHandle[] followerWaitHandles = new WaitHandle[waitHandles.Length + 1];
            Array.Copy(waitHandles, followerWaitHandles, waitHandles.Length);
            int onReceivedMessageIndex = waitHandles.Length;
            followerWaitHandles[onReceivedMessageIndex] = onReceivedMessage;

            int timeoutValueTemp;
            lock (timeoutValueLockObject)
            {
                timeoutValueTemp = timeoutValue;
            }
            while (true)
            {
                int indexInner = WaitHandle.WaitAny(waitHandles, timeoutValueTemp);
                if(indexInner == WaitHandle.WaitTimeout)
                {
                    ChangeStateToCandiate();
                    return;
                }
                else if(indexInner == onReceivedMessageIndex)
                {
                    onReceivedMessage.Reset();
                    lock (lastReceivedMessageLock)
                    {
                        lock (timeoutValueLockObject)
                        {
                            timeoutValueTemp = timeoutValue - (DateTime.Now - lastReceivedMessage).Milliseconds;
                        }
                    }
                }
                else //We've been signaled. Told to shutdown, onNotifyBackgroundThread doesn't impact us really
                {
                    return;
                }
            }
        }

        private void ChangeStateToFollower()
        {
            if(currentState == ERaftState.LEADER)
            {
                StopUAS?.Invoke(this, EStopUASReason.CLUSTER_LEADERSHIP_LOST);
            }

            lock (lastReceivedMessageLock)
            {
                lastReceivedMessage = DateTime.Now;
            }
            currentState = ERaftState.FOLLOWER;
            RecalculateTimeoutValue();
            onNotifyBackgroundThread.Set();
        }
        private void ChangeStateToLeader()
        {
            currentState = ERaftState.LEADER;

            lock (distributedLogLockObject)
            {
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    node.Value.NextIndex = distributedLog.GetLastIndex() + 1;
                }

                onNotifyBackgroundThread.Set();

                //Blast out to let everyone know about our victory
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    RaftAppendEntry<TKey, TValue> message = new RaftAppendEntry<TKey, TValue>(node.Key, nodeName, ELogName.UAS_LOG, currentTerm, distributedLog.CommitIndex);
                    networking.SendMessage(message);
                }
            }

            StartUAS?.Invoke(this, null);
        }
        private void ChangeStateToCandiate()
        {
            currentState = ERaftState.CANDIDATE;
            lock(currentTermLockObject)
            {
                currentTerm += 1;
            }
            onNotifyBackgroundThread.Set();
            //Detect if we even know a majority of nodes at the moment, if we don't then don't even bother
            //Set the random value for the timeout
        }

        private void OnMessageReceive(object sender, BaseMessage message)
        {
            if (!message.GetType().IsSubclassOf(typeof(RaftBaseMessage)) && !(message.GetType() == typeof(RaftBaseMessage)))
            {
                //TODO: Logging
                //We've received a message we don't support
                return;
            }

            if (message.MessageType == typeof(RaftJoinCluster))
            {
                HandleJoinCluster((RaftJoinCluster)message);
                return;
            }
            else if (message.MessageType == typeof(RaftJoinClusterResponse))
            {
                HandleJoinClusterResponse((RaftJoinClusterResponse)message);
                return;
            }
            else if (message.MessageType == typeof(RaftAppendEntry<TKey, TValue>))
            {
                HandleAppendEntry((RaftAppendEntry<TKey, TValue>)message);
                return;
            }
            else if (message.MessageType == typeof(RaftAppendEntryResponse))
            {
                HandleAppendEntryResponse((RaftAppendEntryResponse)message);
                return;
            }
            else if (message.MessageType == typeof(RaftRequestVote))
            {
                HandleCallElection((RaftRequestVote)message);
                return;
            }
            else if (message.MessageType == typeof(RaftRequestVoteResponse))
            {
                HandleCallElectionResponse((RaftRequestVoteResponse)message);
                return;
            }
            else
            {
                //TOOD: Logging
                //We've received a RaftBaseMessage message we don't support
                return;
            }
        }

        private void HandleJoinCluster(RaftJoinCluster message)
        {
            RaftJoinClusterResponse responseMessage;
            lock(currentStateLockObject)
            {
                if(currentState == ERaftState.LEADER)
                {
                    lock (nodesInfoLockObject)
                    {
                        if (message.ClusterName != clusterName)
                        {
                            responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.JoinClusterAttempt, message.ClusterName, EJoinClusterResponse.REJECT_WRONG_CLUSTER_NAME);
                        }
                        else if (nodesInfo.ContainsKey(message.From)) //If we've already talked to you
                        {
                            responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.JoinClusterAttempt, message.ClusterName, EJoinClusterResponse.ACCEPT);
                        }
                        else if (nodesInfo.Count >= maxNodes - 1)
                        {
                            responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.JoinClusterAttempt, message.ClusterName, EJoinClusterResponse.REJECT_CLUSTER_FULL);
                        }
                        else
                        {
                            responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.JoinClusterAttempt, message.ClusterName, EJoinClusterResponse.ACCEPT);
                        }
                    }
                }
                else if (currentState == ERaftState.FOLLOWER || currentState == ERaftState.CANDIDATE)
                {
                    responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.JoinClusterAttempt, message.ClusterName, networking.GetIPFromName(leaderName).Address.ToString(), networking.GetIPFromName(leaderName).Port);
                }
                else if (currentState == ERaftState.ATTEMPTING_TO_JOIN_CLUSTER || currentState == ERaftState.ATTEMPTING_TO_START_CLUSTER)
                {
                    responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.JoinClusterAttempt, message.ClusterName, EJoinClusterResponse.REJECT_LEADER_UNKNOWN);
                }
                else if(currentState == ERaftState.INITIALIZING)
                {
                    return; //Discard message, can't do anything with it yet
                }
                else
                {
                    throw new InvalidOperationException("How did you even get here?");
                }
            }
            networking.SendMessage(responseMessage);
        }
        private void HandleJoinClusterResponse(RaftJoinClusterResponse message)
        {
            lock(currentStateLockObject)
            {
                if(currentState != ERaftState.ATTEMPTING_TO_JOIN_CLUSTER)
                {
                    return; //We don't need this, we've already found the leader
                }

                if (message.JoinClusterAttempt != joiningClusterAttemptNumber)
                {
                    return; //Discard
                }

                if (message.JoinClusterResponse == EJoinClusterResponse.ACCEPT)
                {
                    //Resolve the servers actual name from what we set it as initially
                    IPEndPoint serverEndpoint = networking.GetIPFromName(message.From);
                    networking.RemovePeer(message.From);
                    networking.ManualAddPeer(message.From, serverEndpoint);

                    //Set our current leader name for reference
                    leaderName = message.From;

                    lock (eJoinClusterResponeLockObject)
                    {
                        eJoinClusterResponse = message.JoinClusterResponse;
                    }
                    onWaitingToJoinCluster.Set();

                    ChangeStateToFollower();
                }
                else
                {
                    //We've been unsuccesful
                    //Set the value of the response for the client, and notify the Task
                    lock (eJoinClusterResponeLockObject)
                    {
                        eJoinClusterResponse = message.JoinClusterResponse;
                    }
                    onWaitingToJoinCluster.Set();
                }
            }
        }
        private void HandleAppendEntry(RaftAppendEntry<TKey, TValue> message)
        {
            RaftAppendEntryResponse responseMessage;
            lock (currentStateLockObject)
            {
                if(currentState == ERaftState.LEADER)
                {
                    lock (currentTermLockObject)
                    {
                        if (message.Term > currentTerm)
                        {
                            UpdateTerm(message.Term);
                            leaderName = message.From;
                            ChangeStateToFollower();
                            return;
                        }
                        //Not even going to return false
                        //TODO: Handle the forwarding of a item to commit through the leader
                        //else if (message.Term == currentTerm) { }
                    }
                }
                else if(currentState == ERaftState.CANDIDATE)
                {
                    lock (currentTermLockObject)
                    {
                        if (message.Term >= currentTerm)
                        {
                            UpdateTerm(message.Term);
                            leaderName = message.From;
                            ChangeStateToFollower();
                            return;
                        }
                        else //message.Term < currentTerm
                        {
                            responseMessage = new RaftAppendEntryResponse(message.From, nodeName, message.LogName, currentTerm, false, -1);
                            networking.SendMessage(responseMessage);
                            return;
                        }
                    }
                }
                else if (currentState == ERaftState.FOLLOWER)
                {
                    lock (currentTermLockObject)
                    {
                        if (message.Term > currentTerm)
                        {
                            UpdateTerm(message.Term);
                            leaderName = message.From;
                            ChangeStateToFollower();
                            return;
                        }
                        else if(message.Term < currentTerm)
                        {
                            responseMessage = new RaftAppendEntryResponse(message.From, nodeName, message.LogName, currentTerm, false, -1);
                            networking.SendMessage(responseMessage);
                            return;
                        }
                        //Else, continue down, this is the a typical message, therefore message.Term == currentTerm
                    }
                }
                else
                {
                    return; //We don't recieve these type of requests, drop it
                }

                RecalculateTimeoutValue();
                lock(lastReceivedMessageLock)
                {
                    lastReceivedMessage = DateTime.Now;
                }
                onReceivedMessage.Set();

                if (message.LogName == ELogName.UAS_LOG)
                {
                    lock (currentTermLockObject)
                    {
                        lock (distributedLogLockObject)
                        {
                            //Check if this is a heart beat with no data
                            if (message.Entry == null)
                            {
                                //Check if we should move up our commit index, and respond to the leader
                                if (message.LeaderCommitIndex > distributedLog.CommitIndex)
                                {
                                    int newCommitIndex = Math.Min(message.LeaderCommitIndex, distributedLog.GetLastIndex());
                                    distributedLog.CommitUpToIndex(newCommitIndex);
                                }
                                responseMessage = new RaftAppendEntryResponse(message.From, nodeName, message.LogName, currentTerm, true, distributedLog.GetLastIndex());
                                networking.SendMessage(responseMessage);
                                return;
                            }
                            else
                            {
                                if (distributedLog.ConfirmPreviousIndex(message.PrevIndex, message.PrevTerm))
                                {
                                    distributedLog.AppendEntry(message.Entry, message.PrevIndex);

                                    if (message.LeaderCommitIndex > distributedLog.CommitIndex)
                                    {
                                        int newCommitIndex = Math.Min(message.LeaderCommitIndex, distributedLog.GetLastIndex());
                                        distributedLog.CommitUpToIndex(newCommitIndex);
                                    }
                                    responseMessage = new RaftAppendEntryResponse(message.From, nodeName, message.LogName, currentTerm, true, distributedLog.GetLastIndex());
                                    networking.SendMessage(responseMessage);
                                    return;
                                }
                                else
                                {
                                    responseMessage = new RaftAppendEntryResponse(message.From, nodeName, message.LogName, currentTerm, false, distributedLog.GetLastIndex());
                                    networking.SendMessage(responseMessage);
                                    return;
                                }
                            }
                        }
                    }
                }
                //TODO: Implement node log
            }
        }
        private void HandleAppendEntryResponse(RaftAppendEntryResponse message)
        {
            //TODO: If we're recieved this from a node which is out of date, respond with another RaftAppendEntry to keep going until done
            lock (currentStateLockObject)
            {
                lock (currentTermLockObject)
                {
                    //Are we behind, and we've now got a new leader?
                    if (message.Term > currentTerm)
                    {
                        UpdateTerm(message.Term);
                        leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }
                }
                if (currentState != ERaftState.LEADER)
                {
                    return; //We don't recieve these type of requests, drop it
                }
                lock (currentTermLockObject)
                {
                    lock (nodesInfoLockObject)
                    {
                        NodeInfo nodeInfo = nodesInfo[message.From];
                        nodeInfo.UpdateLastReceived();
                        if (message.MatchIndex == nodeInfo.MatchIndex)
                        {
                            return; //Heart beat, nothing more we need to do
                        }
                        nodeInfo.MatchIndex = message.MatchIndex;
                        nodeInfo.NextIndex = message.MatchIndex + 1;

                        if (message.Success)
                        {
                            lock (distributedLogLockObject)
                            {
                                if (message.MatchIndex > distributedLog.CommitIndex)
                                {
                                    //Now we've got another commit, have we reached majority now?
                                    if (CheckForCommitMajority(message.MatchIndex))
                                    {
                                        if (distributedLog.GetTermOfIndex(message.MatchIndex) == currentTerm) //An additional check from the paper
                                        {
                                            //We have! Update our log. Notify everyone to update their logs
                                            lock (appendEntryTasksLockObject)
                                            {
                                                distributedLog.CommitUpToIndex(message.MatchIndex);
                                                appendEntryTasks[message.MatchIndex].Set();
                                                appendEntryTasks.Remove(message.MatchIndex);
                                                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                                                {
                                                    RaftAppendEntry<TKey, TValue> updateMessage =
                                                        new RaftAppendEntry<TKey, TValue>(node.Key,
                                                                                                nodeName,
                                                                                                ELogName.UAS_LOG,
                                                                                                currentTerm,
                                                                                                distributedLog.CommitIndex);
                                                    networking.SendMessage(updateMessage);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //If a follower fails to insert into log, it means that the prev check failed, so we need to step backwards
                            nodesInfo[message.From].NextIndex--;
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
                    return; //We don't recieve these type of requests, drop it
                }

                lock (currentTermLockObject)
                {
                    if (message.Term > currentTerm)
                    {
                        UpdateTerm(message.Term);

                        RecalculateTimeoutValue();
                        lock (lastReceivedMessageLock)
                        {
                            lastReceivedMessage = DateTime.Now;
                        }
                        onReceivedMessage.Set();

                        if (currentState != ERaftState.FOLLOWER)
                        {
                            ChangeStateToFollower();
                        }

                        lock (votedForLockObject)
                        {
                            if (votedFor == "") //We haven't voted for anyone
                            {
                                lock (distributedLogLockObject)
                                {
                                    //If they're at least as up to date we'll vote for them
                                    int logLatestIndex = distributedLog.GetLastIndex();
                                    if (message.LastLogIndex >= logLatestIndex && message.LastTermIndex >= distributedLog.GetTermOfIndex(logLatestIndex))
                                    {
                                        votedFor = message.From;
                                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, currentTerm, true);
                                    }
                                    else
                                    {
                                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, currentTerm, false);
                                    }
                                }
                            }
                            else if (votedFor == message.From)
                            {
                                responseMessage = new RaftRequestVoteResponse(message.From, nodeName, currentTerm, true);
                            }
                            else //We've voted for someome else... akward
                            {
                                responseMessage = new RaftRequestVoteResponse(message.From, nodeName, currentTerm, false);
                            }
                        }
                    }
                    else //Same or old term
                    {
                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, currentTerm, false);
                    }
                }
            }
            networking.SendMessage(responseMessage);
        }
        private void HandleCallElectionResponse(RaftRequestVoteResponse message)
        {
            lock (currentStateLockObject)
            {
                lock(currentTermLockObject)
                {
                    if(message.Term > currentTerm)
                    {
                        UpdateTerm(message.Term);
                        leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }
                    else if (message.Term < currentTerm)
                    {
                        return; //This is not valid, discard
                    }
                }

                if (currentState == ERaftState.CANDIDATE && message.Granted)
                {
                    lock (currentTermLockObject) //Used by ChangeStateToLeader, maintaining lock ordering
                    {
                        lock (nodesInfoLockObject)
                        {
                            nodesInfo[message.From].VoteGranted = true;
                            if (CheckForVoteMajority())
                            {
                                leaderName = nodeName;
                                ChangeStateToLeader(); //This includes sending out the blast
                            }
                        }
                    }
                }
            }
        }

        private void RecalculateTimeoutValue()
        {
            lock (timeoutValueLockObject)
            {
                timeoutValue = rand.Next(timeoutValueMin, timeoutValueMax + 1);
            }
        }
        private bool CheckForCommitMajority(int index)
        {
            int total = 1; //Initialised to 1 as leader counts
            foreach(KeyValuePair<string, NodeInfo> node in nodesInfo)
            {
                if(node.Value.MatchIndex == index)
                {
                    total += 1;
                }
            }

            int majorityMinimal = (maxNodes / 2) + 1;
            return (total >= majorityMinimal);
        }
        private bool CheckForVoteMajority()
        {
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
            {
                if (node.Value.VoteGranted)
                {
                    total += 1;
                }
            }

            int majorityMinimal = (maxNodes / 2) + 1;
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
            currentTerm = newTerm;
            lock(votedForLockObject)
            {
                votedFor = "";
            }
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
                    throw new InvalidOperationException("You may not set this value while service is running, only before it starts");
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
                nodes[i] = new RaftConsensus<TKey, TValue>(Guid.NewGuid().ToString(), startPort + i);
            }

            return nodes;
        }

        public void SetIUDPNetworking(IUDPNetworking udpNetworking)
        {
            lock (currentStateLockObject)
            {
                if (currentState == ERaftState.INITIALIZING)
                {
                    networking = udpNetworking;
                }
                else
                {
                    throw new InvalidOperationException("You may not set this value while service is running, only before it starts");
                }
            }
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
                        onShutdown.Set();
                    }
                    if (previousStatus != ERaftState.INITIALIZING)
                    {
                        StopUAS?.Invoke(this, EStopUASReason.CLUSTER_STOP);
                        networking.Dispose();
                        backgroundThread.Wait();
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
