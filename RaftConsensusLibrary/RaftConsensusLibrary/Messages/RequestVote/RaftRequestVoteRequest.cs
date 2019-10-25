using RaftConsensusLibrary.Messages.Enums;

namespace RaftConsensusLibrary.Messages
{
    internal class RaftRequestVoteRequest : RaftBaseMessage
    {
        public int LastLogIndex { get; set; }
        public int LastLogTerm { get; set; }

        public RaftRequestVoteRequest(int term)
            : base(ERaftMessageType.RequestVoteRequest, term) { }
    }
}
