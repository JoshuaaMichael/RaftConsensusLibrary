using RaftConsensusLibrary.Consensus.States;
using RaftConsensusLibrary.Messages;
using System;
using UDPMessaging.Identification.PeerIdentification;

namespace RaftConsensusLibrary.Consensus
{
    public class RaftConsensusContext<T> : IRaftConsensus
    {
        internal RaftConsensusBaseState<T> FollowerState { get; }
        internal RaftConsensusBaseState<T> CandiateState { get; }
        internal RaftConsensusBaseState<T> LeaderState { get; }

        private RaftConsensusBaseState<T> _currentState;

        public IPeerIdentification Name { get; }

        public RaftConsensusContext(IPeerIdentification name)
        {
            FollowerState = new RaftConsensusStateFollower<T>(this);
            CandiateState = new RaftConsensusStateCandidate<T>(this);
            LeaderState = new RaftConsensusStateLeader<T>(this);

            _currentState = FollowerState;

            Name = name;
        }

        internal void SetState(RaftConsensusBaseState<T> nextState)
        {
            _currentState = nextState;
        }

        private void ProcessMessage(IRaftMessage message)
        {
            switch (message)
            {
                case RaftAppendEntryRequest<T> raftAppendEntryRequestMessage:
                    ProcessRaftAppendEntryRequest(raftAppendEntryRequestMessage);
                    break;
                case RaftRequestVoteRequest raftRequestVoteRequestMessage:
                    ProcessRaftRequestVoteRequest(raftRequestVoteRequestMessage);
                    return;
            }

            if (_currentState.IsAcceptedType(message.GetType()))
            {
                _currentState.ProcessMessage(message);
            }
        }

        private void ProcessRaftRequestVoteRequest(RaftRequestVoteRequest message)
        {
            throw new NotImplementedException();
        }

        private void ProcessRaftAppendEntryRequest(RaftAppendEntryRequest<T> message)
        {
            throw new NotImplementedException();
        }
    }
}
