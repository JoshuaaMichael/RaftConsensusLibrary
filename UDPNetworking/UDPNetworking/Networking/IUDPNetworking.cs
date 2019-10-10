using System;
using System.Net;
using UDPNetworking.Exceptions;
using UDPNetworking.Identification.PeerIdentification;
using UDPNetworking.Messages;

namespace UDPNetworking.Networking
{
    public interface IUDPNetworking : IDisposable
    {
        void Start(IPEndPoint endPoint);
        void Stop();
        EUDPNetworkingStatus GetStatus();

        void SendMessage(IBaseMessage message);
        event EventHandler<IBaseMessage> OnMessageReceived;
        event EventHandler<ReceiveFailureException> OnMessageReceivedFailure;
        event EventHandler<SendFailureException> OnMessageSendFailure;
        event EventHandler<IPeerIdentification> OnNewConnectedPeer;

        IPeerIdentification PeerName { get; }
    }
}
