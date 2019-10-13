using System;

namespace UDPNetworking.Exceptions
{
    public abstract class UDPNetworkingException : Exception
    {
        protected UDPNetworkingException(string errorMessage)
            : base(errorMessage) { }

        protected UDPNetworkingException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException) { }
    }
}
