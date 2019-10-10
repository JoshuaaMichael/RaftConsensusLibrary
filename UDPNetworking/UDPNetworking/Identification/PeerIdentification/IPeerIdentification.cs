using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Identification.PeerIdentification
{
    public interface IPeerIdentification : IIdentification
    {
        bool Equals(IPeerIdentification obj);
    }
}
