using System;
using Newtonsoft.Json;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Consensus.RaftMessages
{
    public class RaftBaseMessage : BaseMessage
    {
        public int Term { get; protected set; }
        public string ClusterName { get; set; }
        public string MessageGuid { get; set; }

        protected RaftBaseMessage() { } //This is needed for Json.NET reasons

        [JsonConstructor]
        public RaftBaseMessage(string to, string from, string clusterName, int term)
            : base(to, from)
        {
            ClusterName = clusterName;
            Term = term;
            MessageGuid = Guid.NewGuid().ToString().Substring(24); //We only want the last set
        }

        public override string ToString()
        {
            return
                $"Message Contents: Type:{GetMessageType()}, GUID: {MessageGuid}, To: {To}, From: {From}, Term: {Term}";
        }
    }
}
