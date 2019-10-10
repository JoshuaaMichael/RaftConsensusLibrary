using System;
using System.Collections.Generic;
using System.Text;
using UDPNetworking.Messages;

namespace UDPNetworking.Exceptions
{
    public class SendFailureException : UDPNetworkingException
    {
        private readonly BaseMessage _message;

        public SendFailureException(string errorMessage, BaseMessage message)
            : base(errorMessage) { _message = message; }

        public SendFailureException(string errorMessage, Exception innerException, BaseMessage message)
            : base(errorMessage, innerException) { _message = message; }

        public BaseMessage GetMessage()
        {
            return _message;
        }
    }
}
