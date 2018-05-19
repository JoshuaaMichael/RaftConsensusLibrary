using System;
using System.Net;
using TeamDecided.RaftNetworking.Enums;
using TeamDecided.RaftNetworking.Exceptions;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking.Interfaces
{
    public interface IUDPNetworking : IDisposable
    {
        void Start(int port);
        void Start(IPEndPoint endPoint);
        EUDPNetworkingStatus GetStatus();

        void SendMessage(BaseMessage message);
        event EventHandler<BaseMessage> OnMessageReceived;
        event EventHandler<UDPNetworkingReceiveFailureException> OnMessageReceivedFailure;
        event EventHandler<UDPNetworkingSendFailureException> OnMessageSendFailure;
        event EventHandler<string> OnNewConnectedPeer;

        string GetClientName();
        string[] GetPeers();
        void ManualAddPeer(string peerName, IPEndPoint endPoint);
        IPEndPoint GetIPFromName(string peerName);
        bool HasPeer(string peerName);
        void RemovePeer(string peerName);
        int CountPeers();
    }
}
