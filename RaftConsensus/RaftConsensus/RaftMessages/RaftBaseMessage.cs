using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftBaseMessage : BaseMessage
    {
        public RaftBaseMessage(string to, string from)
            : base(to, from) { }
    }
}
