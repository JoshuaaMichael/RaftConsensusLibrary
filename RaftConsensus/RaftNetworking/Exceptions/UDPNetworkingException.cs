using System;

namespace TeamDecided.RaftNetworking.Exceptions
{
    public class UDPNetworkingException : Exception
    {
        public UDPNetworkingException(string errorMessage)
            : base(errorMessage) { }

        public UDPNetworkingException(string errorMessage, Exception innerException)
            :base(errorMessage, innerException) { }
    }
}
