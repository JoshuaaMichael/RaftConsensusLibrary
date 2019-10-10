using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UDPNetworking.Exceptions;
using UDPNetworking.Identification.PeerIdentification;
using UDPNetworking.Messages;
using UDPNetworking.Utilities;

namespace UDPNetworking.Networking
{
    public abstract class BaseUDPNetworking : IUDPNetworking
    {
        public event EventHandler<IBaseMessage> OnMessageReceived;
        public event EventHandler<ReceiveFailureException> OnMessageReceivedFailure;
        public event EventHandler<SendFailureException> OnMessageSendFailure;
        public event EventHandler<IPeerIdentification> OnNewConnectedPeer;

        protected bool DisposedValue; // To detect redundant calls

        private EUDPNetworkingStatus _status;
        private readonly object _statusLockObject;

        protected BaseUDPNetworking(IPeerIdentification peerName)
        {
            PeerName = peerName;

            _status = EUDPNetworkingStatus.Initialized;
            _statusLockObject = new object();
        }

        public IPeerIdentification PeerName { get; }

        public void Start(IPEndPoint endPoint)
        {
            lock (_statusLockObject)
            {
                if (_status != EUDPNetworkingStatus.Initialized)
                {
                    throw new InvalidOperationException("Library is currently not in a state it may start in");
                }
                _status = EUDPNetworkingStatus.Starting;
            }

            ProcessingThread<>
        }

        public void Stop()
        {
            lock (_statusLockObject)
            {
                if (_status == EUDPNetworkingStatus.Running)
                {
                    //TODO: Stop
                }
                _status = EUDPNetworkingStatus.Stopped;
            }
        }

        public void SendMessage(IBaseMessage message)
        {
            throw new NotImplementedException();
        }

        public EUDPNetworkingStatus GetStatus()
        {
            lock (_statusLockObject)
            {
                return _status;
            }
        }

        public void Dispose()
        {
            if (DisposedValue) return;
            Stop();
            DisposedValue = true;
        }
    }
}
