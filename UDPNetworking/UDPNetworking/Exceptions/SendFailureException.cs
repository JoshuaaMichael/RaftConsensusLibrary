using System;
using UDPNetworking.Messages;

namespace UDPNetworking.Exceptions
{
    public class SendFailureException : UDPNetworkingException
    {
        private readonly IBaseMessage _message;

        public SendFailureException(string errorMessage, IBaseMessage message)
            : base(errorMessage) { _message = message; }

        public SendFailureException(string errorMessage, Exception innerException, IBaseMessage message)
            : base(errorMessage, innerException) { _message = message; }

        public IBaseMessage GetMessage()
        {
            return _message;
        }
    }
}
