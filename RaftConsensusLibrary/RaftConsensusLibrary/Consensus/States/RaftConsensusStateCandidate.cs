using RaftConsensusLibrary.Messages;
using System;

namespace RaftConsensusLibrary.Consensus.States
{
    internal class RaftConsensusStateCandidate<T> : RaftConsensusBaseState<T>
    {
        public RaftConsensusStateCandidate(RaftConsensusContext<T> context) 
            : base(context, typeof(RaftAppendEntryRequest<T>), typeof(RaftRequestVoteResponse)) { }

        internal override void ProcessMessage(IRaftMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
