using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    public class NodeIPDictionary
    {
        private readonly Dictionary<string, IPEndPoint> _dict;
 
        public NodeIPDictionary()
        {
            _dict = new Dictionary<string, IPEndPoint>();
        }

        public bool AddOrUpdateNode(string nodeName, IPEndPoint ipEndPoint)
        {
            lock (_dict)
            {
                if (_dict.ContainsKey(nodeName))
                {
                    if (!_dict[nodeName].Equals(ipEndPoint))
                    {
                        _dict[nodeName] = ipEndPoint;
                    }

                    return false;
                }

                _dict.Add(nodeName, ipEndPoint);
                return true;
            }
        }

        public IPEndPoint GetNodeIPEndPoint(string nodeName)
        {
            lock (_dict)
            {
                return (_dict.ContainsKey(nodeName) ? _dict[nodeName] : null);
            }
        }

        public string[] GetNodes()
        {
            lock (_dict)
            {
                return _dict.Keys.ToArray();
            }
        }

        public bool HasNode(string nodeName)
        {
            lock (_dict)
            {
                return _dict.ContainsKey(nodeName);
            }
        }

        public bool RemoveNode(string nodeName)
        {
            lock (_dict)
            {
                return _dict.Remove(nodeName);
            }
        }

        public int Count
        {
            get
            {
                lock (_dict)
                {
                    return _dict.Count;
                }
            }
        }

        public IPEndPoint this[string nodeName] => GetNodeIPEndPoint(nodeName);
    }
}
