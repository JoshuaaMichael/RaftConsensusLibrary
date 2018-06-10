using System;

namespace TeamDecided.RaftConsensus.Networking.Exceptions
{
    public class UdpNetworkingException : Exception
    {
        public UdpNetworkingException(string errorMessage)
            : base(errorMessage) { }

        public UdpNetworkingException(string errorMessage, Exception innerException)
            :base(errorMessage, innerException) { }
    }
}
