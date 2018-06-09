using System;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking.Exceptions
{
    public class UDPNetworkingSendFailureException : UDPNetworkingException
    {
        BaseMessage message;

        public UDPNetworkingSendFailureException(string errorMessage, BaseMessage message)
            :base(errorMessage) { this.message = message; }

        public UDPNetworkingSendFailureException(string errorMessage, Exception innerException, BaseMessage message)
            : base(errorMessage, innerException) { this.message = message; }

        public BaseMessage GetMessage()
        {
            return message;
        }
    }
}
