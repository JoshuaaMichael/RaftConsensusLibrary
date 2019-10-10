using System;
using System.Collections.Generic;
using System.Text;
using UDPNetworking.Identification.PeerIdentification;

namespace UDPNetworking.Networking
{
    public class BasicUDPNetworking : BaseUDPNetworking
    {
        public BasicUDPNetworking(IPeerIdentification peerName) 
            : base(peerName)
        {
        }
    }
}
