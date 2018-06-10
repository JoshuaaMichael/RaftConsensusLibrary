using System;
using Newtonsoft.Json;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftBaseMessage : BaseMessage
    {
        public string ClusterName { get; set; }
        public string MessageGuid { get; set; }

        protected RaftBaseMessage() { } //This is needed for Json.NET reasons

        [JsonConstructor]
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
