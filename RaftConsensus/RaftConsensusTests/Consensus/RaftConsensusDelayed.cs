using System;
using System.Threading;
using TeamDecided.RaftConsensus.Consensus;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Tests.Consensus
{
    class RaftConsensusDelayed<TKey, TValue> : RaftConsensus<TKey, TValue> where TValue : ICloneable where TKey : ICloneable
    {
        private readonly int _messageDelay;
        public RaftConsensusDelayed(string nodeName, int listeningPort, int messageDelay)
            : base(nodeName, listeningPort)
        {
            _messageDelay = messageDelay;
        }

        protected override void OnMessageReceived(object sender, BaseMessage message)
        {
            Thread.Sleep(_messageDelay);
            base.OnMessageReceived(sender, message);
        }
    }
}
