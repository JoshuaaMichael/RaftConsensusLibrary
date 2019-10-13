using System;
using System.Net;
using UDPNetworking.Exceptions;
using UDPNetworking.Identification.PeerIdentification;
using UDPNetworking.Messages;
using UDPNetworking.PeerManagement;

namespace UDPNetworking.Networking
{
    public interface IUDPNetworking : IDisposable
    {
        void SendMessageAsync(IBaseMessage message);
        event EventHandler<IBaseMessage> OnMessageReceived;
        event EventHandler<ReceiveFailureException> OnMessageReceivedFailure;
        event EventHandler<SendFailureException> OnMessageSendFailure;
        event EventHandler<IPeerIdentification> OnNewConnectedPeer;

        IPeerManager PeerManager { get; }
        IPeerIdentification PeerName { get; }
    }
}
