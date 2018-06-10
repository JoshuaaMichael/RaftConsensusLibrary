using System;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking.Exceptions
{
    public class UdpNetworkingSendFailureException : UdpNetworkingException
    {
        private readonly BaseMessage _message;

        public UdpNetworkingSendFailureException(string errorMessage, BaseMessage message)
            :base(errorMessage) { _message = message; }

        public UdpNetworkingSendFailureException(string errorMessage, Exception innerException, BaseMessage message)
            : base(errorMessage, innerException) { _message = message; }

        public BaseMessage GetMessage()
        {
            return _message;
        }
    }
}
