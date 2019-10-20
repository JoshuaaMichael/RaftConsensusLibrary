using RaftConsensusLibrary.Messages.Enums;

namespace RaftConsensusLibrary.Messages
{
    internal class RaftRequestVote : RaftBaseMessage
    {
        public int LastLogIndex { get; set; }
        public int LastLogTerm { get; set; }

        public RaftRequestVote(int term)
            : base(ERaftMessageType.RequestVoteRequest, term) { }
    }
}
