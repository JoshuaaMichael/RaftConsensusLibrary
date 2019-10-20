using RaftConsensusLibrary.Messages.Enums;
using UDPMessaging.Identification.MessageTypeIdentification;

namespace RaftConsensusLibrary.Messages
{
    internal class RaftMessageTypeIdentification : IMessageTypeIdentification
    {
        private readonly ERaftMessageType _messageType;

        public RaftMessageTypeIdentification(ERaftMessageType messageType)
        {
            _messageType = messageType;
        }

        public object GetIdentification()
        {
            return _messageType;
        }

        public bool Equals(IMessageTypeIdentification obj)
        {
            return Equals((object)obj);
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case RaftMessageTypeIdentification s:
                    return s._messageType == _messageType;
                case ERaftMessageType emt:
                    return emt == _messageType;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _messageType.GetHashCode();
        }
    }
}
