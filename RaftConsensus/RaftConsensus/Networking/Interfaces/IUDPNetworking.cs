using System;
using System.Net;
using TeamDecided.RaftConsensus.Networking.Enums;
using TeamDecided.RaftConsensus.Networking.Exceptions;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking.Interfaces
{
    public interface IUdpNetworking : IDisposable
    {
        void Start(int port);
        void Start(IPEndPoint endPoint);
        EUDPNetworkingStatus GetStatus();

        void SendMessage(BaseMessage message);
        event EventHandler<BaseMessage> OnMessageReceived;
        event EventHandler<UdpNetworkingReceiveFailureException> OnMessageReceivedFailure;
        event EventHandler<UdpNetworkingSendFailureException> OnMessageSendFailure;
        event EventHandler<string> OnNewConnectedPeer;

        string GetClientName();
        void SetClientName(string clientName);
        string[] GetPeers();
        void ManualAddPeer(string peerName, IPEndPoint endPoint);
        IPEndPoint GetIpFromName(string peerName);
        bool HasPeer(string peerName);
        void RemovePeer(string peerName);
        int CountPeers();
    }
}
