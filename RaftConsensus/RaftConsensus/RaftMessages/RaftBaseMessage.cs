using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftBaseMessage : BaseMessage
    {
        public RaftBaseMessage() { }

        public RaftBaseMessage(string to, string from)
            : base(to, from) { }

        public override string ToString()
        {
            return string.Format("Message Contents: Type:{0}, To: {1}, From: {2}", MessageType, To, From);
        }
    }
}
