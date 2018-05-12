using System;
using System.Collections.Generic;
using System.Threading;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftConsensus.RaftMessages;
using TeamDecided.RaftNetworking.Interfaces;

namespace TeamDecided.RaftConsensus
{
    public class RaftConsensus<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        string clusterName;
        ERaftState currentState;
        int currentTerm;
        Dictionary<string, NodeInfo> nodesInfo;
        IUDPNetworking networking;
        string nodeName;
        string leaderNode;

        RaftDistributedLog<TKey, TValue> distributedLog;
        RaftDistributedLog<string, RaftNodeNetworkInfo> nodeLog;

        int messageReplyTimeout;
        int messageReplyTimeoutRandom;
        int heartbeatInterval;
        int nodeNetworkInfoHeartbeatInterval;

        Thread backgroundWorkerThread;
        ManualResetEvent onChangeState;
        bool isUASRunning;

        public RaftConsensus() { }

        private void ChangeStateToFollower() { throw new NotImplementedException(); }
        private void ChangeStateToLeader() { throw new NotImplementedException(); }
        private void ChangeStateToCandiate() { throw new NotImplementedException(); }

        private ERaftState GetCurrentState() { throw new NotImplementedException(); }

        private void OnMessageReceive(RaftBaseMessage message) { throw new NotImplementedException(); }
        private void HandleJoinCluster(RaftJoinCluster message) { throw new NotImplementedException(); }
        private void HandleJoinClusterResponse(RaftAppendEntryResponse message) { throw new NotImplementedException(); }
        private void HandleAppendEntry(RaftAppendEntry<TKey, TValue> message) { throw new NotImplementedException(); }
        private void HandleAppendEntryResult(RaftAppendEntryResponse message) { throw new NotImplementedException(); }
        private void HandleCallElection(RaftRequestVote message) { throw new NotImplementedException(); }
        private void HandleCallElectionResponse(RaftRequestVoteResponse message) { throw new NotImplementedException(); }

        public int GetMessageReplyTimeout()
        {
            return messageReplyTimeout;
        }

        public void SetMessageReplyTimeout(int value)
        {
            messageReplyTimeout = value;
        }

        public int GetMessageReplyTimeoutRandom()
        {
            return messageReplyTimeoutRandom;
        }

        public void SetMessageReplyTimeoutRandom(int value)
        {
            messageReplyTimeoutRandom = value;
        }

        public int GetHeartbeatInterval()
        {
            return heartbeatInterval;
        }

        public void SetHeartbeatInterval(int value)
        {
            heartbeatInterval = value;
        }

        public int GetNodeNetworkInfoHeartbeatInterval()
        {
            return nodeNetworkInfoHeartbeatInterval;
        }

        public void SetNodeNetworkInfoHeartbeatInterval(int value)
        {
            nodeNetworkInfoHeartbeatInterval = value;
        }

        public bool IsUASRunning()
        {
            return isUASRunning;
        }
    }
}
