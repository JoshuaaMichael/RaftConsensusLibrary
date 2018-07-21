using System;
using System.Collections.Generic;
using System.Text;

namespace TeamDecided.RaftConsensus.Consensus
{
    class StateMessageWhitelistFilter<T>
    {
        private readonly Dictionary<T, List<Type>> _filter;

        public StateMessageWhitelistFilter()
        {
            _filter = new Dictionary<T, List<Type>>();
        }

        public void Add(T state, Type messageType)
        {
            if (!_filter.ContainsKey(state))
            {
                _filter.Add(state, new List<Type>());
            }

            if (!_filter[state].Contains(messageType))
            {
                _filter[state].Add(messageType);
            }
        }

        public bool Check(T state, Type messageType)
        {
            return _filter.ContainsKey(state) && _filter[state].Contains(messageType);
        }
    }
}
