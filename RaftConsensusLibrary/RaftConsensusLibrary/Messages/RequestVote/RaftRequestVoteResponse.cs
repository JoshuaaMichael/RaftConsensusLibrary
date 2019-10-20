using RaftConsensusLibrary.Messages.Enums;

namespace RaftConsensusLibrary.Messages
{
    internal class RaftRequestVoteResponse : RaftBaseMessage
    {
        public bool VoteGranted { get; set; }

        public RaftRequestVoteResponse(int term)
            : base(ERaftMessageType.RequestVoteResponse, term) { }
    }
}
