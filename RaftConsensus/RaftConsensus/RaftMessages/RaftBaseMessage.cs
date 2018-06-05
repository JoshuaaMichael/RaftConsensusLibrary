using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftBaseMessage : BaseMessage
    {
        public string ClusterName { get; set; }

        public RaftBaseMessage() { }

        public RaftBaseMessage(string to, string from, string clusterName)
            : base(to, from) { ClusterName = clusterName; }

        public override string ToString()
        {
            return string.Format("Message Contents: Type:{0}, To: {1}, From: {2}", MessageType, To, From);
        }
    }
}
