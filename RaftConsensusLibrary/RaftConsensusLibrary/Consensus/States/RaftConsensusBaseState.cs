using RaftConsensusLibrary.Messages;
using System;
using System.Linq;

namespace RaftConsensusLibrary.Consensus.States
{
    internal abstract class RaftConsensusBaseState<T>
    {
        protected RaftConsensusContext<T> Context;
        private readonly Type[] _acceptedTypes;

        protected RaftConsensusBaseState(RaftConsensusContext<T> context, params Type[] acceptedTypes)
        {
            Context = context;
            _acceptedTypes = acceptedTypes;
        }

        internal abstract void ProcessMessage(IRaftMessage message);

        internal bool IsAcceptedType(Type type)
        {
            return _acceptedTypes.Contains(type);
        }
    }
}
