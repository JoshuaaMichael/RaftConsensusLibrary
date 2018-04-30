using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftNetworking.Enums;
using TeamDecided.RaftNetworking.Exceptions;
using TeamDecided.RaftNetworking.Interfaces;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking
{
    class UDPNetworking : IUDPNetworking
    {
        public event EventHandler<BaseMessage> OnMessageReceived;
        private Queue<BaseMessage> newMessagesReceived;
        private object newMessagesReceivedLockObject;
        private ManualResetEvent onMessageReceive;

        private Queue<BaseMessage> newMessagesToSend;
        private object newMessagesToSendLockObject;
        private ManualResetEvent onMessageToSend;

        private ManualResetEvent onNetworkingStop;

        public event EventHandler<string> OnNewConnectedPeer;
        private Queue<string> newConnectedPeers;
        private object newConnectedPeersLockObject;
        private ManualResetEvent onNewConnectedPeer;
        private Dictionary<string, IPEndPoint> peers;
        private object peersLockObject;

        public event EventHandler<UDPNetworkingReceiveFailureException> OnMessageReceivedFailure;
        private Queue<UDPNetworkingReceiveFailureException> newMessageReceiveFailures;
        private object newMessageReceiveFailuresLockObject;
        private ManualResetEvent onMessageReceiveFailure;

        public event EventHandler<UDPNetworkingSendFailureException> OnMessageSendFailure;
        private Queue<UDPNetworkingSendFailureException> newMessageSendFailures;
        private object newMessageSendFailuresLockObject;
        private ManualResetEvent onMessageSendFailure;

        private UdpClient udpClient;

        private EUDPNetworkingStatus status;
        private object statusLockObject;

        private Task listeningThread;
        private Task sendingThread;
        private Task processingThread;

        public void Start(int port)
        {
            throw new NotImplementedException();
        }

        public void Start(IPEndPoint endPoint)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        protected void StartThreads()
        {

        }

        protected void StopThreads()
        {

        }

        public void SendMessage(BaseMessage message)
        {
            throw new NotImplementedException();
        }

        public int CountPeers()
        {
            throw new NotImplementedException();
        }

        public string[] GetPeers()
        {
            throw new NotImplementedException();
        }

        public EUDPNetworkingStatus GetStatus()
        {
            throw new NotImplementedException();
        }

        public bool HasPeer(string peerName)
        {
            throw new NotImplementedException();
        }

        public void ManualAddPeer(string peerName, IPEndPoint endPoint)
        {
            throw new NotImplementedException();
        }

        public void RemovePeer(string peerName)
        {
            throw new NotImplementedException();
        }
    }
}
