using RaftConsensusLibrary.Messages;
using System;

namespace RaftConsensusLibrary.Consensus
{
    internal class RaftConsensusStateFollower<T> : RaftConsensusBaseState<T>
    {
        public RaftConsensusStateFollower(RaftConsensusContext<T> context)
            : base(context, typeof(RaftAppendEntryRequest<T>)) { }

        internal override void ProcessMessage(IRaftMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
