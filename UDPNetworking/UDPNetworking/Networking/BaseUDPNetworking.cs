using System;
using System.Net;
using System.Threading.Tasks;
using UDPNetworking.Exceptions;
using UDPNetworking.Identification.PeerIdentification;
using UDPNetworking.Messages;
using UDPNetworking.PeerManagement;
using UDPNetworking.Serialisation;

namespace UDPNetworking.Networking
{
    public abstract class BaseUDPNetworking : IUDPNetworking
    {
        public event EventHandler<IBaseMessage> OnMessageReceived;
        public event EventHandler<ReceiveFailureException> OnMessageReceivedFailure;
        public event EventHandler<SendFailureException> OnMessageSendFailure;
        public event EventHandler<IPeerIdentification> OnNewConnectedPeer;

        public IPeerIdentification PeerName { get; }
        public IPeerManager PeerManager { get; }
        protected ISerializer Serializer;

        protected const int MaxPacketSize = 65507;

        protected UDPClient UDPClient;

        protected bool DisposedValue; // To detect redundant calls

        protected BaseUDPNetworking(IPeerIdentification peerName, IPeerManager peerManager, ISerializer serializer, IPEndPoint ipEndPoint)
        {
            PeerName = peerName;
            PeerManager = peerManager;
            Serializer = serializer;

            UDPClient = new UDPClient(ipEndPoint);
            UDPClient.OnMessageReceived += 
                (sender, tuple) => RecieveMessageAsync(tuple.Item1, tuple.Item2);
            UDPClient.OnMessageReceivedFailure +=
                (sender, exception) => OnMessageReceivedFailure?.Invoke(sender, exception);
            UDPClient.OnMessageSendFailure += 
                (sender, exception) => OnMessageSendFailure?.Invoke(sender, exception);
        }

        public async void SendMessageAsync(IBaseMessage message)
        {
            byte[] messageToSend = Serializer.Serialize(message);

            if (messageToSend.Length > MaxPacketSize)
            {
                OnMessageSendFailure?.Invoke(this, new SendFailureException("Message is too large to send", message));
                return;
            }

            IPEndPoint ipEndPoint = PeerManager.GetPeerIPEndPoint(message.To);

            if (ipEndPoint == null)
            {
                OnMessageSendFailure?.Invoke(this, new SendFailureException("Failed to convert recipient to IPEndPoint", message));
                return;
            }

            if (await UDPClient.SendAsync(messageToSend, ipEndPoint)) return;

            OnMessageSendFailure?.Invoke(this, new SendFailureException("Failed when sending message", message));
        }

        protected async void RecieveMessageAsync(byte[] messageBytes, IPEndPoint ipEndPoint)
        {
            await Task.Run(() =>
            {
                try
                {
                    IBaseMessage message = Serializer.Deserialize(messageBytes);

                    if (PeerManager.AddOrUpdatePeer(message.From, ipEndPoint))
                    {
                        OnNewConnectedPeer?.Invoke(this, message.From);
                    }

                    OnMessageReceived?.Invoke(this, message);
                }
                catch (Exception e)
                {
                    OnMessageReceivedFailure?.Invoke(this, new ReceiveFailureException("Failed deserialising new message", e));
                }
            });
        }

        public void Dispose()
        {
            if (DisposedValue) return;

            UDPClient?.Dispose();

            DisposedValue = true;
        }
    }
}
