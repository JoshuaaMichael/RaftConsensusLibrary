using System;

namespace TeamDecided.RaftConsensus.Networking.Exceptions
{
    public class UdpNetworkingReceiveFailureException : UdpNetworkingException
    {
        public UdpNetworkingReceiveFailureException(string errorMessage)
            : base(errorMessage) { }

        public UdpNetworkingReceiveFailureException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException) { }
    }
}
