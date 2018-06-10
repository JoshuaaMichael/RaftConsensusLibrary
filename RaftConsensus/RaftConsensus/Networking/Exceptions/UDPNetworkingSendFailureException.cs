using System;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking.Exceptions
{
    public class UdpNetworkingSendFailureException : UdpNetworkingException
    {
        BaseMessage _message;

        public UdpNetworkingSendFailureException(string errorMessage, BaseMessage message)
            :base(errorMessage) { this._message = message; }

        public UdpNetworkingSendFailureException(string errorMessage, Exception innerException, BaseMessage message)
            : base(errorMessage, innerException) { this._message = message; }

        public BaseMessage GetMessage()
        {
            return _message;
        }
    }
}
