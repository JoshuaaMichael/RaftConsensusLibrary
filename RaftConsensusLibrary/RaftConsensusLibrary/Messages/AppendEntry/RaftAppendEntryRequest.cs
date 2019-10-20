using RaftConsensusLibrary.Messages.Enums;
using RaftConsensusLibrary.RaftDistributedLog;

namespace RaftConsensusLibrary.Messages
{
    internal class RaftAppendEntryRequest<T> : RaftBaseMessage
    {
        public int PreviousLogIndex { get; set; }
        public int PreviousLogTerm { get; set; }
        public int LeaderCommitIndex { get; set; }
        public IRaftEntry<T>[] Entries { get; set; }

        public RaftAppendEntryRequest(int term)
            :base(ERaftMessageType.AppendEntryRequest, term) { }
    }
}
