using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UDPNetworking.Identification.PeerIdentification;

namespace UDPNetworking.PeerManagement
{
    public interface IPeerManager
    {
        bool AddOrUpdatePeer(IPeerIdentification peerIdentification, IPEndPoint ipEndPoint);
        IPEndPoint GetPeerIPEndPoint(IPeerIdentification peerIdentification);
        IPEndPoint this[IPeerIdentification peerIdentification] { get; }
    }
}
