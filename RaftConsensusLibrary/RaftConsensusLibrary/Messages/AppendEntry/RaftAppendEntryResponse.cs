using RaftConsensusLibrary.Messages.Enums;

namespace RaftConsensusLibrary.Messages
{
    internal class RaftAppendEntryResponse : RaftBaseMessage
    {
        public bool Success { get; set; }

        public RaftAppendEntryResponse(int term)
            : base(ERaftMessageType.AppendEntryResponse, term) { }
    }
}
