using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using UDPNetworking.Identification;
using UDPNetworking.Identification.MessageTypeIdentification;
using UDPNetworking.Identification.MessageVersionIdentification;
using UDPNetworking.Identification.PeerIdentification;

namespace UDPNetworking.Messages
{
    public interface IBaseMessage : ISerializable
    {
        IPeerIdentification To { get; set; }
        IPeerIdentification From { get; set; }
        IMessageTypeIdentification Type { get; set; }
        IMessageVersionIdentification Version { get; set; }
    }
}
