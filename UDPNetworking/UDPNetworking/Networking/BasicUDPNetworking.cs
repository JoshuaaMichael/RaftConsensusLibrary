using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UDPNetworking.Identification.PeerIdentification;
using UDPNetworking.PeerManagement;
using UDPNetworking.Serialisation;

namespace UDPNetworking.Networking
{
    public class BasicUDPNetworking :  BaseUDPNetworking
    {
        public BasicUDPNetworking(IPeerIdentification peerName, IPeerManager peerManager, ISerializer serializer, IPEndPoint ipEndPoint) 
            : base(peerName, peerManager, serializer, ipEndPoint) { }
    }
}
