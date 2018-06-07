using System;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftConsensus.RaftMessages
{
    public class RaftBaseMessage : BaseMessage
    {
        public string ClusterName { get; set; }
        public string MessageGuid { get; set; }

        public RaftBaseMessage() { }

        public RaftBaseMessage(string to, string from, string clusterName)
            : base(to, from)
        {
            ClusterName = clusterName;
            MessageGuid = Guid.NewGuid().ToString().Substring(24); //We only want the last set
        }

        public override string ToString()
        {
            return string.Format("Message Contents: Type:{0}, GUID: {1}, To: {2}, From: {3}", MessageType, MessageGuid, To, From);
        }
    }
}
