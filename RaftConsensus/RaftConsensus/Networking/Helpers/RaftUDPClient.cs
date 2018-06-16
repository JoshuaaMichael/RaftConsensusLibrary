using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Common.Logging;
using TeamDecided.RaftConsensus.Networking.Exceptions;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    internal class RaftUDPClient : IDisposable
    {
        private UdpClient _udpClient;
        private IPEndPoint _ipEndPoint;
        private int _port;

        public NodeIPDictionary NodeIPs { get; }

        private const int MaxPacketSize = 65507;

        private readonly ManualResetEvent _isSocketReady;
        private readonly object _isRebuildingLockObject;
        private bool _isRebuilding;

        private UdpNetworkingSendFailureException _sendMessageException;

        private bool _disposedValue;

        public RaftUDPClient()
        {
            NodeIPs = new NodeIPDictionary();
            _isSocketReady = new ManualResetEvent(true);
            _isRebuildingLockObject = new object();
            _isRebuilding = false;
        }

        public void Start(int port)
        {
            if (_udpClient != null)
            {
                throw new InvalidOperationException("Can only call start once");
            }

            _port = port;
            Init();
        }

        public void Start(IPEndPoint ipEndPoint)
        {
            if (_udpClient != null)
            {
                throw new InvalidOperationException("Can only call start once");
            }

            _ipEndPoint = ipEndPoint;
            Init();
        }

        private void Init()
        {
            _udpClient = _ipEndPoint == null ? new UdpClient(_port) : new UdpClient(_ipEndPoint);
            DisableIcmpUnreachable();
        }

        private void DisableIcmpUnreachable()
        {
            const uint iocIn = 0x80000000;
            const uint iocVendor = 0x18000000;
            const uint sioUdpConnreset = iocIn | iocVendor | 12;
            _udpClient.Client.IOControl(unchecked((int)sioUdpConnreset), new[] { Convert.ToByte(false) }, null);
        }

        public bool Send(BaseMessage message)
        {
            byte[] messageToSend = message.Serialize();

            if (messageToSend.Length > MaxPacketSize)
            {
                _sendMessageException = new UdpNetworkingSendFailureException("Message is too large to send", message);
                return false;
            }

            if (message.IPEndPoint == null && message.To != null)
            {
                message.IPEndPoint = NodeIPs[message.To];
            }

            if (message.IPEndPoint == null)
            {
                _sendMessageException = new UdpNetworkingSendFailureException("Failed to convert recipient to IPAddress", message);
                return false;
            }

            try
            {
                Task<int> sendMessageTask = _udpClient.SendAsync(messageToSend, messageToSend.Length, message.IPEndPoint);
                sendMessageTask.Wait();

                if (sendMessageTask.Result > 0) return true;
                _sendMessageException = new UdpNetworkingSendFailureException("Failed to send message", message);
                return false;
            }
            catch (Exception e)
            {
                RaftLogging.Instance.Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", RaftLogging.FlattenException(e));
                RebuildUdpClient();
                return false;
            }
        }

        public UdpNetworkingSendFailureException GetSendFailureException()
        {
            UdpNetworkingSendFailureException temp = _sendMessageException;
            _sendMessageException = null;
            return temp;
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            try
            {
                _isSocketReady.WaitOne();
                return _udpClient.ReceiveAsync();
            }
            catch (Exception e)
            {
                RaftLogging.Instance.Log(ERaftLogType.Debug, "Caught exception. Dumping exception string: {0}", RaftLogging.FlattenException(e));
                RebuildUdpClient();
                return null;
            }
        }

        private void RebuildUdpClient()
        {
            lock (_isRebuildingLockObject)
            {
                if (_isRebuilding)
                {
                    return; //It's currently rebuilding
                }
                _isRebuilding = true;
                _isSocketReady.Reset();
            }

            _udpClient.Dispose();

            Init();

            lock (_isRebuildingLockObject)
            {
                _isRebuilding = false;
                _isSocketReady.Set();
            }
        }

        public void Dispose()
        {
            if (_disposedValue) return;
            _udpClient.Dispose();
            _disposedValue = true;
        }
    }
}
