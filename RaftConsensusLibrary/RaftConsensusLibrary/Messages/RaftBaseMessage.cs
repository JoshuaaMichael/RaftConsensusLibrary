using RaftConsensusLibrary.Messages.Enums;
using UDPMessaging.Identification.MessageTypeIdentification;
using UDPMessaging.Identification.MessageVersionIdentification;
using UDPMessaging.Identification.PeerIdentification;

namespace RaftConsensusLibrary.Messages
{
    internal abstract class RaftBaseMessage : IRaftMessage
    {
        public IPeerIdentification To { get; set; }
        public IPeerIdentification From { get; set; }
        public IMessageTypeIdentification Type { get; }
        public IMessageVersionIdentification Version { get; set; }
        public int Term { get; }

        protected RaftBaseMessage(ERaftMessageType messageType, int term)
        {
            Type = new RaftMessageTypeIdentification(messageType);
            Term = term;
        }
    }
}
