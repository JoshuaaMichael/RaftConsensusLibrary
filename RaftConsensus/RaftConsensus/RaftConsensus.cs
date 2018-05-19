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

namespace TeamDecided.RaftConsensus
{
    public class RaftConsensus<TKey, TValue> : IDisposable, IConsensus<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        private string clusterName;
        private int maxNodes;
        private ERaftState currentState;
        private object currentStateLockObject;
        private int currentTerm;
        private object currentTermLockObject;
        private Dictionary<string, NodeInfo> nodesInfo;
        private IUDPNetworking networking;
        private int listeningPort;
        private string nodeName;
        private string leaderName;

        private List<Tuple<string, IPEndPoint>> manuallyAddedPeers;

        private RaftDistributedLog<TKey, TValue> distributedLog;
        private object distributedLogLockObject;
        private RaftDistributedLog<string, RaftNodeNetworkInfo> nodeLog;

        #region Timeout values
        //Common timeout value
        private const int networkLatency = 15; //ms
        //Leader: Sends heartbearts every x milliseconds
        private int heartbeatInterval = 100;
        private int nodeNetworkInfoHeartbeatInterval = 250; //Don't need to heartbeat for updates in IP as often
        //Follower: Waits to hear back from a leader with it's candidate timeout value
        //Candiate: Waits to hear back from followers to elect it as a leader, times out when not hearing back
        private int candidateTimeoutValue; //How long the follower will wait to hear from a leader before becoming candidate, set randomly each time entering follower state
        private int candidateTimeoutValueMin = 10 * networkLatency;
        private int candidateTimeoutValueMax = 2 * 10 * networkLatency;
        #endregion

        private Task backgroundWorkerThread;
        private ManualResetEvent onChangeState;

        private ManualResetEvent onWaitingToJoinCluster;
        private const int waitingToJoinClusterTimeout = 2000; //ms
        private EJoinClusterResponse eJoinClusterResponse;
        private object eJoinClusterResponeLockObject;
        private int joiningClusterAttemptNumber;

        private Dictionary<int, ManualResetEvent> appendEntryTasks;
        private object appendEntryTasksLockObject;

        private HashSet<string> joinClusterResponseFroms;

        public event EventHandler StartUAS;
        public event EventHandler<EStopUASReason> StopUAS;
        public event EventHandler<Tuple<TKey, TValue>> OnNewLogEntry;

        private bool disposedValue = false; // To detect redundant calls

        public RaftConsensus(string nodeName, int listeningPort)
        {
            currentState = ERaftState.INITIALIZING;
            currentStateLockObject = new object();
            currentTerm = 0;
            currentTermLockObject = new object();
            nodesInfo = new Dictionary<string, NodeInfo>();
            this.listeningPort = listeningPort;
            this.nodeName = nodeName;
            manuallyAddedPeers = new List<Tuple<string, IPEndPoint>>();
            distributedLog = new RaftDistributedLog<TKey, TValue>();
            distributedLogLockObject = new object();
            nodeLog = new RaftDistributedLog<string, RaftNodeNetworkInfo>();

            backgroundWorkerThread = new Task(BackgroundWorker, TaskCreationOptions.LongRunning);
            onChangeState = new ManualResetEvent(false);
            onWaitingToJoinCluster = new ManualResetEvent(false);
            eJoinClusterResponse = EJoinClusterResponse.NOT_YET_SET;
            eJoinClusterResponeLockObject = new object();
            joiningClusterAttemptNumber = 0;

            appendEntryTasks = new Dictionary<int, ManualResetEvent>();
            appendEntryTasksLockObject = new object();

            joinClusterResponseFroms = new HashSet<string>();
        }

        public Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword)
        {
            lock (currentStateLockObject)
            {
                if(currentState != ERaftState.INITIALIZING)
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
                if (manuallyAddedPeers.Count == 0)
                {
                    throw new InvalidOperationException("There are no nodes to talk to to enter the cluster");
                }

                currentState = ERaftState.ATTEMPTING_TO_JOIN_CLUSTER;
                joiningClusterAttemptNumber += 1;

                for (int i = 0; i < manuallyAddedPeers.Count; i++)
                {
                    RaftJoinCluster message = new RaftJoinCluster(manuallyAddedPeers[i].Item1, nodeName, clusterName, manuallyAddedPeers[i].Item1, joiningClusterAttemptNumber);
                    networking.SendMessage(message);
                }

                Task<EJoinClusterResponse> task = Task.Run(() =>
                {
                    if(onWaitingToJoinCluster.WaitOne(waitingToJoinClusterTimeout) == false) //The timeout occured
                    {
                        lock(currentStateLockObject)
                        {
                            currentState = ERaftState.INITIALIZING;
                        }
                        return EJoinClusterResponse.NO_RESPONSE;
                    }

                    lock(eJoinClusterResponeLockObject)
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
                if (maxNodes >= 3 && maxNodes % 2 == 1)
                {
                    throw new ArgumentException("Number of maxNodes must be greater than or equal to 3, and then also be an odd number");
                }

                this.clusterName = clusterName;
                this.maxNodes = maxNodes;

                networking = new UDPNetworking();
                networking.Start(listeningPort);
                networking.OnMessageReceived += OnMessageReceive;

                currentState = ERaftState.LEADER;
            }
        }

        public string GetClusterName()
        {
            if(clusterName == "")
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
            lock(distributedLogLockObject)
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
        public void ManualAddPeer(IPEndPoint endPoint)
        {
            // dictionary - gen'd guid, ipendpoint
            // we do manual add peer
            // then, later when we talk to them, we fire off the request to talk, we include our guid for THEM in the message
            // the respond with the guid we gave them, and their real name
            // we match their real name to the guid in our dictionary, and delete the dict entry, leaving it up to IUDPNetworking to handle

            string guid = Guid.NewGuid().ToString();
            manuallyAddedPeers.Add(new Tuple<string, IPEndPoint>(guid, endPoint));
            networking.ManualAddPeer(guid, endPoint);
        }
        public Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value)
        {
            lock(currentStateLockObject)
            {
                if (currentState != ERaftState.LEADER)
                {
                    throw new InvalidOperationException("You may only append entries when your UAS is active");
                }
            }

            RaftLogEntry<TKey, TValue> entry;
            lock (currentTermLockObject)
            {
                entry = new RaftLogEntry<TKey, TValue>(key, value, currentTerm);
            }
                
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
                distributedLog.AppendEntry(entry);
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

            string[] peers = networking.GetPeers();
            for (int i = 0; i < peers.Length; i++)
            {
                RaftAppendEntry<TKey, TValue> message = 
                    new RaftAppendEntry<TKey, TValue>(peers[i],
                                                            nodeName,
                                                            ELogName.UAS_LOG,
                                                            currentTerm,
                                                            prevIndex,
                                                            prevTerm,
                                                            commitIndex);
                networking.SendMessage(message);
            }

            return task;
        }
        public bool IsUASRunning()
        {
            lock(currentStateLockObject)
            {
                return currentState == ERaftState.LEADER;
            }
        }

        private void BackgroundWorker()
        {
            //Follower/candidate: Wait for candiate time outs
            //Leader: Wait to send out heart beats
            //Leader: Check append entry list to see if there are entries to send out
            //Initialised: Check for new connect to cluster requests to send off
        }

        private void ChangeStateToFollower()
        {
            //Check if we're coming from initialised, if we are, we need to kick off our thread
            throw new NotImplementedException();
        }
        private void ChangeStateToLeader() { throw new NotImplementedException(); }
        private void ChangeStateToCandiate() { throw new NotImplementedException(); }

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
                    if(message.ClusterName != clusterName)
                    {
                        responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.To, message.JoinClusterAttempt, EJoinClusterResponse.REJECT_WRONG_CLUSTER_NAME);
                    }
                    else if (joinClusterResponseFroms.Contains(message.From)) //If we've already talked to you
                    {
                        responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.To, message.JoinClusterAttempt, EJoinClusterResponse.ACCEPT);
                    }
                    else if(joinClusterResponseFroms.Count >= maxNodes - 1)
                    {
                        responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.To, message.JoinClusterAttempt, EJoinClusterResponse.REJECT_CLUSTER_FULL);
                    }
                    else
                    {
                        responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.To, message.JoinClusterAttempt, EJoinClusterResponse.ACCEPT);
                        joinClusterResponseFroms.Add(message.From);
                    }
                }
                else if (currentState == ERaftState.FOLLOWER || currentState == ERaftState.CANDIDATE)
                {
                    responseMessage = new RaftJoinClusterResponse(message.From, nodeName, message.To, message.JoinClusterAttempt, networking.GetIPFromName(leaderName));
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



            //Are we waiting for this response?
            //Is it one that we're currently still waiting for, or was it from a previous attempt?
            //If success, make sure we set the cluster name

            //Lookup in the dict for the reference name, and we'll resolve that conflict in IUDPNetworking
            //Woohoo, we're now in the cluster... 
            //Change into a follower
            //Handle errors, and give back the reject reason to the handle who asked us to join the cluster

            //You need to set the value of eJoinClusterResponse, and flick the flag if we're succesful
            
            //Set leader name


            throw new NotImplementedException();
        }
        private void HandleAppendEntry(RaftAppendEntry<TKey, TValue> message)
        {
            //Check has the term changed
            //Enter the data into self if we're happy
            //Send a RaftAppendEntryResponse
            throw new NotImplementedException();
        }
        private void HandleAppendEntryResponse(RaftAppendEntryResponse message)
        {
            //check if this was from a newer term, then change to follower
            // check if this notice mean's we've reached majority stored and we can lookup and notify the Task that we registered to this
            //if it means it, let the task know
            //if it doesn't know, just simply add it to the index match for the node

            //if it's majority, you should lookup the dict of the index of the commit and let the person who committed it know

            throw new NotImplementedException();
        }
        private void HandleCallElection(RaftRequestVote message)
        {
            //check if this comes from a newer term then I'm on, if it isn't reply no
            //increase our current term to that one, it must be the latest
            //if it is check if we've voted in that term
            //return yes otherwise, and change into a follower if we're not already
            throw new NotImplementedException();
        }
        private void HandleCallElectionResponse(RaftRequestVoteResponse message)
        {
            //check if it comes from a newer term than I'm on, if it is revert to follower and give them my vote
            //otherwise if it's no, live with, someone else is also out there, when we fail we'll do the random thing again with time
            //otherwise if it's yes, stack it up/record it and we'll see if we win
            throw new NotImplementedException();
        }

        #region Get/set timeout/heartbeat values
        public int GetCandiateTimeoutMin()
        {
            return candidateTimeoutValueMin;
        }

        public void SetCandiateTimeoutMin(int value)
        {
            lock (currentStateLockObject)
            {
                if (currentState == ERaftState.INITIALIZING)
                {
                    candidateTimeoutValueMin = value;
                }
                else
                {
                    throw new InvalidOperationException("You may not set this value while service is running, only before it starts");
                }
            }
        }

        public int GetCandiateTimeoutMax()
        {
            return candidateTimeoutValueMax;
        }

        public void SetCandiateTimeoutMax(int value)
        {
            lock (currentStateLockObject)
            {
                if (currentState == ERaftState.INITIALIZING)
                {
                    candidateTimeoutValueMax = value;
                }
                else
                {
                    throw new InvalidOperationException("You may not set this value while service is running, only before it starts");
                }
            }
        }

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

        public int GetNodeNetworkInfoHeartbeatInterval()
        {
            return nodeNetworkInfoHeartbeatInterval;
        }

        public void SetNodeNetworkInfoHeartbeatInterval(int value)
        {
            lock (currentStateLockObject)
            {
                if (currentState == ERaftState.INITIALIZING)
                {
                    nodeNetworkInfoHeartbeatInterval = value;
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
                        onChangeState.Set();
                    }
                    if (previousStatus != ERaftState.INITIALIZING)
                    {
                        networking.Dispose();
                        backgroundWorkerThread.Wait();
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
