using System;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking.Exceptions
{
    class UDPNetworkingSendFailureException : UDPNetworkingException
    {
        BaseMessage message;

        public UDPNetworkingSendFailureException(string errorMessage, BaseMessage message)
            :base(errorMessage)
        {

        }

        public UDPNetworkingSendFailureException(string errorMessage, Exception innerException, BaseMessage message)
            : base(errorMessage, innerException)
        {

        }
    }
}
