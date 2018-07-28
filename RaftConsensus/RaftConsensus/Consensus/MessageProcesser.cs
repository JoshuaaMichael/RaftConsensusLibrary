using System;
using System.Collections.Generic;

namespace TeamDecided.RaftConsensus.Consensus
{
    internal class MessageProcesser<T>
    {
        public delegate void MessageProcessHandler(T message);

        private readonly Dictionary<Type, MessageProcessHandler> _processer;

        public MessageProcesser()
        {
            _processer = new Dictionary<Type, MessageProcessHandler>();
        }

        public void Register(Type type, MessageProcessHandler messageProcesser)
        {
            if (type != typeof(T) && !type.IsSubclassOf(typeof(T)))
            {
                throw new ArgumentException("Can only register types which are, or derived from, T");
            }

            _processer.Add(type, messageProcesser);
        }

        public void Process(T message)
        {
            _processer[message.GetType()](message);
        }
    }
}
