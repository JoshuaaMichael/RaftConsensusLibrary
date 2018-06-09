using System;

namespace TeamDecided.RaftConsensus.Networking.Exceptions
{
    public class UDPNetworkingReceiveFailureException : UDPNetworkingException
    {
        public UDPNetworkingReceiveFailureException(string errorMessage)
            : base(errorMessage) { }

        public UDPNetworkingReceiveFailureException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException) { }
    }
}
