using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftCommon.Logging;
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

        private Task backgroundThread;
        private ManualResetEvent onNotifyBackgroundThread;
        private ManualResetEvent onReceivedMessage;
        private ManualResetEvent onShutdown;
        private CountdownEvent onThreadsStarted;

        private ManualResetEvent onWaitingToJoinCluster;
        private int waitingToJoinClusterTimeout = 5000;
        private EJoinClusterResponse eJoinClusterResponse;
        private object eJoinClusterResponeLockObject;
        private int joiningClusterAttemptNumber;

        private Dictionary<int, ManualResetEvent> appendEntryTasks;
        private object appendEntryTasksLockObject;

        public event EventHandler StartUAS;
        public event EventHandler<EStopUASReason> StopUAS;
        public event EventHandler<Tuple<TKey, TValue>> OnNewCommitedEntry;
        private int onNewCommitedEntryNotifed;

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

            manuallyAddedClients = new List<Tuple<string, string, int>>();
            onNewCommitedEntryNotifed = -1;
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

                networking = new UDPNetworking();
                networking.Start(listeningPort);
                networking.OnMessageReceived += OnMessageReceive;
                FlushNetworkPeerBuffer();

                Log("Trying to join cluster - {0}", clusterName);
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    IPEndPoint ipEndPoint = networking.GetIPFromName(node.Key);
                    Log("I know: nodeName={0}, ipAddress={1}, port={2}", node.Key, ipEndPoint.Address.ToString(), ipEndPoint.Port);
                }

                this.clusterName = clusterName;

                lock (nodesInfoLockObject)
                {
                    if (nodesInfo.Count + 1 != maxNodes) //You aren't in the nodesInfo list
                    {
                        throw new InvalidOperationException("There are not enough nodes known yet");
                    }

                    onWaitingToJoinCluster = new ManualResetEvent(false);
                }

                currentState = ERaftState.FOLLOWER;
                StartThreads();
                ChangeStateToFollower();

                Task<EJoinClusterResponse> task = Task.Run(() =>
                {
                    if (onWaitingToJoinCluster.WaitOne(waitingToJoinClusterTimeout) == false) //The timeout occured
                    {
                        Log("Never found cluster in {0} millisecond timeout", waitingToJoinClusterTimeout);
                        lock (currentStateLockObject)
                        {
                            currentState = ERaftState.INITIALIZING;
                        }
                        networking.Dispose();
                        return EJoinClusterResponse.NO_RESPONSE;
                    }
                    else
                    {
                        Log("Notifying the user we've succesfully joined the cluster");
                        return EJoinClusterResponse.ACCEPT; //We found the cluster
                    }
                });

                return task;
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
            manuallyAddedClients.Add(new Tuple<string, string, int>(name, endPoint.Address.ToString(), endPoint.Port));
            lock (nodesInfoLockObject)
            {
                nodesInfo.Add(name, new NodeInfo(name));
            }
        }
        private void FlushNetworkPeerBuffer()
        {
            foreach (Tuple<string, string, int> peer in manuallyAddedClients)
            {
                if (!networking.HasPeer(peer.Item1))
                {
                    networking.ManualAddPeer(peer.Item1, new IPEndPoint(IPAddress.Parse(peer.Item2), peer.Item3));
                }
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
                        prevTerm = distributedLog.GetTermOfLastIndex();
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
            Log("Background thread initialised");
            ERaftState threadState = ERaftState.INITIALIZING;
            while (true)
            {
                int indexOuter = WaitHandle.WaitAny(waitHandles);
                if (indexOuter == 0) //We've been told to shutdown
                {
                    Log("Background thread has been told to shutdown");
                    return;
                }
                else if (indexOuter == 1) //We've been notified to update
                {
                    Log("Background thread has been notified to update");
                    onNotifyBackgroundThread.Reset();
                    lock (currentStateLockObject)
                    {
                        threadState = currentState;
                    }
                }
                //Next, execute the background logic of whichever state we're in

                if (threadState == ERaftState.FOLLOWER)
                {
                    Log("Background thread now running as Follower");
                    BackgroundThread_Follower(waitHandles); //We're a follower, so we'll be checking for timeouts from hearing append entry messages
                }
                else if (threadState == ERaftState.CANDIDATE)
                {
                    Log("Background thread now running as Candidate");
                    BackgroundThread_Candidate(waitHandles); //We're a candidate, so we'll be checking for timeouts from our attempt to become leader
                }
                else if (threadState == ERaftState.LEADER)
                {
                    Log("Background thread now running as Leader");
                    BackgroundThread_Leader(waitHandles); //We're a leader, so we'll be sending heart beats
                }
            }
        }
        private void BackgroundThread_Leader(WaitHandle[] waitHandles)
        {
            while (true)
            {
                //TODO: Do the math to figure out the next heartbeatTime, technically this way it'll be offset at like every timeout-1ms or something due to the effort when it goes in... technically
                int index = WaitHandle.WaitAny(waitHandles, heartbeatInterval);
                if (index == WaitHandle.WaitTimeout)
                {
                    Log("It's time to send out heartbeats.");
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
                                    if (distributedLog.GetLastIndex() >= node.Value.NextIndex)
                                    {
                                        int prevIndex = node.Value.NextIndex - 1;
                                        int prevTerm = distributedLog.GetTermOfIndex(prevIndex);
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
                else //We've been signaled. Told to shutdown or we've stopped being leader
                {
                    return;
                }
            }
        }
        private void BackgroundThread_Candidate(WaitHandle[] waitHandles)
        {
            if (WaitHandle.WaitAny(waitHandles, timeoutValue) == WaitHandle.WaitTimeout)
            {
                //We didn't hear from anyone, so we've got to go candidate again to try be leader... again
                Log("We didn't get voted in to be leader, time to try again");
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

            while (true)
            {
                int indexInner = WaitHandle.WaitAny(followerWaitHandles, timeoutValue);
                if (indexInner == WaitHandle.WaitTimeout)
                {
                    Log("Timing out to be candidate. We haven't heard from the leader in {0} milliseconds", timeoutValue);
                    ChangeStateToCandiate();
                    return;
                }
                else if (indexInner == onReceivedMessageIndex)
                {
                    onReceivedMessage.Reset();
                }
                else //We've been signaled. Told to shutdown, onNotifyBackgroundThread doesn't impact us really
                {
                    return;
                }
            }
        }

        private void ChangeStateToFollower()
        {
            Log("Changing state to Follower");
            if (currentState == ERaftState.LEADER)
            {
                Log("Notifying the UAS to stop");
                StopUAS?.Invoke(this, EStopUASReason.CLUSTER_LEADERSHIP_LOST);
            }

            currentState = ERaftState.FOLLOWER;
            RecalculateTimeoutValue();
            onNotifyBackgroundThread.Set();
        }
        private void ChangeStateToLeader()
        {
            Log("Changing state to Leader");
            currentState = ERaftState.LEADER;

            lock (distributedLogLockObject)
            {
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    node.Value.NextIndex = distributedLog.GetLastIndex() + 1;
                }

                onNotifyBackgroundThread.Set();
                Log("Letting everyone know about our new leadership");
                //Blast out to let everyone know about our victory
                foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                {
                    RaftAppendEntry<TKey, TValue> message = new RaftAppendEntry<TKey, TValue>(node.Key, nodeName, clusterName, ELogName.UAS_LOG, currentTerm, distributedLog.CommitIndex);
                    SendMessage(message);
                }
            }
            Log("Notifying to start UAS");
            StartUAS?.Invoke(this, null);
        }
        private void ChangeStateToCandiate()
        {
            Log("Changing state to Candidate");
            currentState = ERaftState.CANDIDATE;
            lock (currentTermLockObject)
            {
                currentTerm += 1;
            }
            onNotifyBackgroundThread.Set();
            RecalculateTimeoutValue();

            lock (nodesInfo)
            {
                Log("Requesting votes for leadership from everyone");
                lock (distributedLogLockObject)
                {
                    foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
                    {
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
                Log("Dropping packet. We don't know what it is. It's not a RaftBaseMessage. It's a {0}", message.GetType());
                return;
            }

            Log("Received new message: {0}, from {1}", ((RaftBaseMessage)message).GetType(), message.From);
            LogVerbose(message.ToString());

            if(((RaftBaseMessage)message).ClusterName != clusterName)
            {
                Log("Dropping packet. It's for the wrong cluster name");
                return;
            }

            if (message.MessageType == typeof(RaftAppendEntry<TKey, TValue>))
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

        private void HandleAppendEntry(RaftAppendEntry<TKey, TValue> message)
        {
            RaftAppendEntryResponse responseMessage;
            lock (currentStateLockObject)
            {
                if (currentState != ERaftState.LEADER && currentState != ERaftState.CANDIDATE && currentState != ERaftState.FOLLOWER)
                {
                    Log("Recieved AppendEntry from node {0}. We don't recieve these types of requests. How did you even get here?", message.From);
                    return; //We don't recieve these type of requests, drop it
                }

                lock (currentTermLockObject)
                {
                    if (message.Term > currentTerm)
                    {
                        Log("Heard from a node ({0}) with greater term({1}) than ours({2}). Changing to follower", message.From, message.Term, currentTerm);

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
                        Log("Recieved AppendEntry from node {0} for a previous term. Sending back a reject.", message.From);
                        responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, false, -1);
                        SendMessage(responseMessage);
                        return;
                    }

                    if (currentState == ERaftState.LEADER)
                    {
                        //TODO: Handle the forwarding of a item to commit through the leader
                        Log("Recieved AppendEntry from node {0}. Discarding as we're the leader.", message.From);
                        return;
                    }
                    else if (currentState == ERaftState.CANDIDATE)
                    {
                        Log("Recieved AppendEntry from the leader {0} of currentTerm. Going back to being a follower.", message.From);
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
                        //Else, continue down, this is the a typical message, therefore message.Term == currentTerm
                        if (leaderName == null)
                        {
                            FoundCluster(message.From, message.Term);
                            return;
                        }
                    }
                    else
                    {
                        Log("Recieved AppendEntry from node {0}. We don't recieve these types of requests. How did you even get here?", message.From);
                        return; //We don't recieve these type of requests, drop it
                    }
                }

                RecalculateTimeoutValue();
                onReceivedMessage.Set();

                lock (currentTermLockObject)
                {
                    lock (distributedLogLockObject)
                    {
                        //Check if this is a heart beat with no data
                        if (message.Entry == null)
                        {
                            Log("This is a heatbeat message from leader {0}", message.From);
                            //Check if we should move up our commit index, and respond to the leader
                            if (message.LeaderCommitIndex > distributedLog.CommitIndex)
                            {
                                Log("Heartbeat contained a request to update out commit index");
                                int newCommitIndex = Math.Min(message.LeaderCommitIndex, distributedLog.GetLastIndex());
                                Log("Updated commit index to {0}, leader's is {1}", newCommitIndex, message.LeaderCommitIndex);
                                distributedLog.CommitUpToIndex(newCommitIndex);
                            }
                            responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, true, distributedLog.GetLastIndex());
                            SendMessage(responseMessage);
                            return;
                        }
                        else
                        {
                            Log("This is a AppendEntry message from {0} has new entries to commit", message.From);
                            if (distributedLog.ConfirmPreviousIndex(message.PrevIndex, message.PrevTerm))
                            {
                                distributedLog.AppendEntry(message.Entry, message.PrevIndex);
                                Log("Confirmed previous index. Appended message");
                                if (message.LeaderCommitIndex > distributedLog.CommitIndex)
                                {
                                    int newCommitIndex = Math.Min(message.LeaderCommitIndex, distributedLog.GetLastIndex());
                                    Log("Updated commit index to {0}, leader's is {1}", newCommitIndex, message.LeaderCommitIndex);
                                    distributedLog.CommitUpToIndex(newCommitIndex);
                                }
                                Log("Responding to leader with the success of our append");
                                responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, true, distributedLog.GetLastIndex());
                                SendMessage(responseMessage);
                                return;
                            }
                            else
                            {
                                Log("Failed to add new entries because confirming previous index/term failed. Ours ({0}. {1}). Theirs ({2}, {3})", distributedLog.GetLastIndex(), distributedLog.GetTermOfLastIndex(), message.PrevIndex, message.PrevTerm);
                                responseMessage = new RaftAppendEntryResponse(message.From, nodeName, clusterName, message.LogName, currentTerm, false, distributedLog.GetLastIndex());
                                SendMessage(responseMessage);
                                return;
                            }
                        }
                    }
                }
            }
        }
        private void HandleAppendEntryResponse(RaftAppendEntryResponse message)
        {
            //TODO: If we're recieved this from a node which is out of date, respond with another RaftAppendEntry to keep going until done
            lock (currentStateLockObject)
            {
                //TODO: Whitelist states
                lock (currentTermLockObject)
                {
                    //Are we behind, and we've now got a new leader?
                    if (message.Term > currentTerm)
                    {
                        Log("Heard from a node ({0}) with greater term({1}) than ours({2}). Changing to follower", message.From, message.Term, currentTerm);

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
                        Log("Recieved AppendEntryResponse from node {0} for a previous term. Discarding.", message.From);
                        return;
                    }

                    if (currentState != ERaftState.LEADER)
                    {
                        return; //We don't recieve these type of requests, drop it
                    }
                    Log("Recieved AppendEntryResponse from node {0}. Seems legit so far.", message.From);

                    lock (nodesInfoLockObject)
                    {
                        NodeInfo nodeInfo = nodesInfo[message.From];
                        nodeInfo.UpdateLastReceived();
                        if (message.MatchIndex == nodeInfo.MatchIndex)
                        {
                            Log("It was just a heartbeat.");
                            return; //Heart beat, nothing more we need to do
                        }
                        Log("Setting {0}'s match index to {1} from {2}.", message.From, message.MatchIndex, nodeInfo.MatchIndex);
                        nodeInfo.MatchIndex = message.MatchIndex;
                        nodeInfo.NextIndex = message.MatchIndex + 1;

                        if (message.Success)
                        {
                            Log("The append entry was a success");
                            lock (distributedLogLockObject)
                            {
                                if (message.MatchIndex > distributedLog.CommitIndex)
                                {
                                    Log("Since we've got another commit, we should check if we're at majority now");
                                    //Now we've got another commit, have we reached majority now?
                                    if (CheckForCommitMajority(message.MatchIndex) && distributedLog.GetTermOfIndex(message.MatchIndex) == currentTerm)
                                    {
                                        Log("We've reached majority. Time to notify everyone to update.");
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
                                                                                            clusterName,
                                                                                            ELogName.UAS_LOG,
                                                                                            currentTerm,
                                                                                            distributedLog.CommitIndex);
                                                SendMessage(updateMessage);
                                            }
                                            for (int i = onNewCommitedEntryNotifed + 1; i <= message.MatchIndex; i++)
                                            {
                                                OnNewCommitedEntry?.Invoke(this, distributedLog[i].GetTuple());
                                            }
                                            onNewCommitedEntryNotifed = message.MatchIndex;
                                        }
                                    }
                                    else
                                    {
                                        Log("We've haven't reached majority yet");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log("This follower failed to append entry. Stepping back their next index");
                            //If a follower fails to insert into log, it means that the prev check failed, so we need to step backwards
                            nodesInfo[message.From].NextIndex--;
                            //Background thread
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
                    Log("Received message from {0}. We aren't in the correct state to process it. Discarding", message.From);
                    return; //We don't recieve these type of requests, drop it
                }

                lock (currentTermLockObject)
                {
                    if (message.Term > currentTerm)
                    {
                        Log("Received a RequestVote from {0}. Let's take a look.", message.From);
                        UpdateTerm(message.Term);

                        RecalculateTimeoutValue();
                        onReceivedMessage.Set();

                        if (currentState != ERaftState.FOLLOWER)
                        {
                            Log("We currently aren't a follower. Changing state to follower.");
                            leaderName = null;
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
                                        Log("Their log is at least as up to date as ours, replying accept");
                                        votedFor = message.From;
                                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, true);
                                    }
                                    else
                                    {
                                        Log("Their log is not at least as up to date as our, replying reject.");
                                        responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, false);
                                    }
                                }
                            }
                            else if (votedFor == message.From)
                            {
                                Log("We've already voted for you? Replying accept");
                                responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, true);
                            }
                            else //We've voted for someome else... akward
                            {
                                Log("We've already voted for someone else in this term ({0}). Akward. Replying rejefct.", votedFor);
                                responseMessage = new RaftRequestVoteResponse(message.From, nodeName, clusterName, currentTerm, false);
                            }
                        }
                    }
                    else //Same or old term
                    {
                        Log("This message is from the same or an old term, returning false");
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
                        Log("Heard from a node ({0}) with greater term({1}) than ours({2}). Changing to follower", message.From, message.Term, currentTerm);
                        UpdateTerm(message.Term);
                        leaderName = message.From;
                        ChangeStateToFollower();
                        return;
                    }
                    else if (message.Term < currentTerm)
                    {
                        Log("Recieved RaftRequestVoteResponse from node {0} for a previous term. Discarding.", message.From);
                        return; //This is not valid, discard
                    }
                }
                if (currentState == ERaftState.CANDIDATE && message.Granted)
                {
                    Log("They accepted our request, we have their vote.");
                    lock (currentTermLockObject) //Used by ChangeStateToLeader, maintaining lock ordering
                    {
                        lock (nodesInfoLockObject)
                        {
                            nodesInfo[message.From].VoteGranted = true;
                            if (CheckForVoteMajority())
                            {
                                Log("This vote got us to majority. Changing to leader");
                                if (leaderName == null)
                                {
                                    onWaitingToJoinCluster.Set();
                                }
                                leaderName = nodeName;
                                ChangeStateToLeader(); //This includes sending out the blast
                            }
                            else
                            {
                                Log("This vote didn't get us to majority. Going to continue waiting...");
                            }
                        }
                    }
                }
                else if (currentState == ERaftState.CANDIDATE && !message.Granted)
                {
                    Log("They rejected our request. Discarding.");
                }
                else
                {
                    Log("We are not in the correct state to process this message. Discarding.");
                }
            }
        }

        private void FoundCluster(string leader, int term)
        {
            Log("We've found the cluster! It's node {0} on term {1}", leader, term);
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
                timeoutValue = rand.Next(timeoutValueMin, timeoutValueMax + 1);
            }
        }
        private bool CheckForCommitMajority(int index)
        {
            int total = 1; //Initialised to 1 as leader counts
            foreach (KeyValuePair<string, NodeInfo> node in nodesInfo)
            {
                if (node.Value.MatchIndex >= index)
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
            lock (votedForLockObject)
            {
                votedFor = "";
            }
        }

        private void SendMessage(RaftBaseMessage message)
        {
            Log("Sending message: {0}, to {1}", message.GetType(), message.To);
            LogVerbose(message.ToString());
            networking.SendMessage(message);
        }

        private void Log(string format, params object[] args)
        {
            string messagePrepend = string.Format("{0} (Status={1}) - ", nodeName, currentState.ToString());
            RaftLogging.Instance.Info(messagePrepend + format, args);
        }
        private void LogVerbose(string format, params object[] args)
        {
            string messagePrepend = string.Format("{0} (Status={1}) - ", nodeName, currentState.ToString());
            RaftLogging.Instance.Debug(messagePrepend + format, args);
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
                nodes[i] = new RaftConsensus<TKey, TValue>("Node" + (i + 1), startPort + i);
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
                    }
                    if (previousStatus != ERaftState.INITIALIZING)
                    {
                        if (previousStatus == ERaftState.LEADER)
                        {
                            StopUAS?.Invoke(this, EStopUASReason.CLUSTER_STOP);
                        }
                        onShutdown.Set();
                        backgroundThread.Wait();
                        networking.Dispose();
                    }
                    else if (previousStatus == ERaftState.INITIALIZING && joiningClusterAttemptNumber > 0)
                    {
                        networking.Dispose();
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
