using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Identification.MessageTypeIdentification
{
    public interface IMessageTypeIdentification : IIdentification
    {
        bool Equals(IMessageTypeIdentification obj);
    }
}
