using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Exceptions
{
    public class UDPNetworkingException : Exception
    {
        public UDPNetworkingException(string errorMessage)
            : base(errorMessage) { }

        public UDPNetworkingException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException) { }
    }
}
