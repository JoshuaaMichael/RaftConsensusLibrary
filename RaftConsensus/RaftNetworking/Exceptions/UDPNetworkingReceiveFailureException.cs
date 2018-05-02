using System;

namespace TeamDecided.RaftNetworking.Exceptions
{
    public class UDPNetworkingReceiveFailureException : UDPNetworkingException
    {
        public UDPNetworkingReceiveFailureException(string errorMessage)
            : base(errorMessage) { }

        public UDPNetworkingReceiveFailureException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException) { }
    }
}
