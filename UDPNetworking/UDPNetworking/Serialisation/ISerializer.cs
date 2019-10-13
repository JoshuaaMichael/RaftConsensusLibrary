using System;
using System.Collections.Generic;
using System.Text;
using UDPNetworking.Messages;

namespace UDPNetworking.Serialisation
{
    public interface ISerializer
    {
        byte[] Serialize(IBaseMessage message);
        IBaseMessage Deserialize(byte[] messageBytes);
    }
}
