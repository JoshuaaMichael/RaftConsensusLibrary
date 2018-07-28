using System;
using System.Net;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Exceptions;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking.Interfaces
{
    public interface IUDPNetworking : IDisposable
    {
        void Start(int port);
        void Start(IPEndPoint endPoint);
        void Stop();
        EUDPNetworkingStatus GetStatus();

        void SendMessage(BaseMessage message);
        event EventHandler<BaseMessage> OnMessageReceived;
        event EventHandler<UdpNetworkingReceiveFailureException> OnMessageReceivedFailure;
        event EventHandler<UdpNetworkingSendFailureException> OnMessageSendFailure;
        event EventHandler<string> OnNewConnectedPeer;

        string ClientName { get; set; }
        string[] GetPeers();
        void ManualAddPeer(string peerName, IPEndPoint endPoint);
        IPEndPoint GetIPFromName(string peerName);
        bool HasPeer(string peerName);
        void RemovePeer(string peerName);
        int CountPeers();
    }
}
