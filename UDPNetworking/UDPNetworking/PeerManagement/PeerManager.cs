using System.Collections.Generic;
using System.Net;
using UDPNetworking.Identification.PeerIdentification;

namespace UDPNetworking.PeerManagement
{
    internal class PeerManager : IPeerManager
    {
        private readonly Dictionary<IPeerIdentification, IPEndPoint> _peers;

        public PeerManager()
        {
            _peers = new Dictionary<IPeerIdentification, IPEndPoint>();
        }

        public bool AddOrUpdatePeer(IPeerIdentification peerIdentification, IPEndPoint ipEndPoint)
        {
            lock (_peers)
            {
                bool isNewPeer = !_peers.ContainsKey(peerIdentification);

                _peers[peerIdentification] = ipEndPoint;

                return isNewPeer;
            }
        }

        public IPEndPoint GetPeerIPEndPoint(IPeerIdentification peerIdentification)
        {
            lock (_peers)
            {
                return _peers.TryGetValue(peerIdentification, out IPEndPoint ipEndPoint) ? ipEndPoint : null;
            }
        }

        public IPEndPoint this[IPeerIdentification peerIdentification] => GetPeerIPEndPoint(peerIdentification);
    }
}
