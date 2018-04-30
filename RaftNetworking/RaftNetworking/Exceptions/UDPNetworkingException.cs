using System;

namespace TeamDecided.RaftNetworking.Exceptions
{
    class UDPNetworkingException : Exception
    {
        public UDPNetworkingException(string errorMessage)
            : base(errorMessage) { }

        public UDPNetworkingException(string errorMessage, Exception innerException)
            :base(errorMessage, innerException) { }
    }
}
