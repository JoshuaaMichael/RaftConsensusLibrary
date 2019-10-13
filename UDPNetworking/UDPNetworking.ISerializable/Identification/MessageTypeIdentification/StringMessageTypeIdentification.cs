using System;
using System.Runtime.Serialization;
using UDPNetworking.Extensions;

namespace UDPNetworking.Identification.MessageTypeIdentification
{
    [Serializable()]
    public class StringMessageTypeIdentification : IMessageTypeIdentification
    {
        private readonly string _identification;
        private const string IdentificationSerialisationStr = "_identification";

        public StringMessageTypeIdentification(string identification)
        {
            _identification = identification;
        }

        protected StringMessageTypeIdentification(SerializationInfo info, StreamingContext ctxt)
        {
            _identification = info.GetValue<string>(IdentificationSerialisationStr);
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case string s:
                    return s == _identification;
                case StringMessageTypeIdentification spi:
                    return spi._identification == _identification;
            }

            return false;
        }

        public bool Equals(IMessageTypeIdentification obj)
        {
            return Equals((object)obj);
        }

        public override int GetHashCode()
        {
            return _identification.GetHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue<string>(IdentificationSerialisationStr, _identification);
        }

        public object GetIdentification()
        {
            return _identification;
        }
    }
}
