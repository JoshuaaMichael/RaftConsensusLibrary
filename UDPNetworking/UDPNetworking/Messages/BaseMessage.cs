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
    public abstract class BaseMessage : IBaseMessage
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("To", To.GetIdentification(), typeof(object));
            info.AddValue("From", To.GetIdentification(), typeof(object));
            info.AddValue("Type", To.GetIdentification(), typeof(object));
            info.AddValue("Version", To.GetIdentification(), typeof(object));
        }

        public IPeerIdentification To { get; set; }
        public IPeerIdentification From { get; set; }
        public IMessageTypeIdentification Type { get; set; }
        public IMessageVersionIdentification Version { get; set; }
    }
}
