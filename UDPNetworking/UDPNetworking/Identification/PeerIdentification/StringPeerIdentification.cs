using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Identification.PeerIdentification
{
    public class StringPeerIdentification : IPeerIdentification
    {
        private readonly string _identifier;

        public StringPeerIdentification(string identifier)
        {
            _identifier = identifier;
        }

        public static StringPeerIdentification Generate()
        {
            return new StringPeerIdentification(Guid.NewGuid().ToString());
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case string s:
                    return s == _identifier;
                case StringPeerIdentification spi:
                    return spi._identifier == _identifier;
            }

            return false;
        }

        public bool Equals(IPeerIdentification obj)
        {
            return Equals((object)obj);
        }

        public override int GetHashCode()
        {
            return _identifier.GetHashCode();
        }

        public object GetIdentification()
        {
            return _identifier;
        }
    }
}
