using RaftConsensusLibrary.Messages;
using System;

namespace RaftConsensusLibrary.Consensus.States
{
    internal class RaftConsensusStateLeader<T> : RaftConsensusBaseState<T>
    {
        public RaftConsensusStateLeader(RaftConsensusContext<T> context)
            : base(context, typeof(RaftAppendEntryResponse)) { }

        internal override void ProcessMessage(IRaftMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
