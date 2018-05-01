using System;

namespace TeamDecided.RaftNetworking.Exceptions
{
    public class UDPNetworkingSocketException : UDPNetworkingException
    {
        public UDPNetworkingSocketException(string errorMessage)
            : base(errorMessage) { }

        public UDPNetworkingSocketException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException) { }
    }
}
