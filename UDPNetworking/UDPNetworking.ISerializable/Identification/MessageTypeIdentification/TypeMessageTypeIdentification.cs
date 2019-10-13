using System;
using System.Runtime.Serialization;

namespace UDPNetworking.Identification.MessageTypeIdentification
{
    [Serializable()]
    public class TypeMessageTypeIdentification : StringMessageTypeIdentification
    {
        public TypeMessageTypeIdentification(Type type)
            :base(type.FullName) { }

        protected TypeMessageTypeIdentification(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt) { }
    }
}
