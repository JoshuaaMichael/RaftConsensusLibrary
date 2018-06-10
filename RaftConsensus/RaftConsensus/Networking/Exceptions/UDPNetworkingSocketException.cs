using System;

namespace TeamDecided.RaftConsensus.Networking.Exceptions
{
    public class UdpNetworkingSocketException : UdpNetworkingException
    {
        public UdpNetworkingSocketException(string errorMessage)
            : base(errorMessage) { }

        public UdpNetworkingSocketException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException) { }
    }
}
