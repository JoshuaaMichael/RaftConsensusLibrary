using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Identification.MessageTypeIdentification
{
    public class StringMessageTypeIdentification : IMessageTypeIdentification
    {
        private readonly string _identification;

        public StringMessageTypeIdentification(string identifier)
        {
            _identification = identifier;
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

        public object GetIdentification()
        {
            return _identification;
        }
    }
}
