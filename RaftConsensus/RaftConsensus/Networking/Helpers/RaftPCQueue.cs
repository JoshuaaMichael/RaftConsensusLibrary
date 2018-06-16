using System.Collections.Generic;
using System.Threading;

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    internal class RaftPCQueue<T>
    {
        private readonly Queue<T> _queue;
        private readonly ManualResetEvent _flag;
        public WaitHandle Flag => _flag;

        public RaftPCQueue()
        {
            _queue = new Queue<T>();
            _flag = new ManualResetEvent(false);
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
                _flag.Set();
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                if (_queue.Count == 1)
                {
                    _flag.Reset();
                }
                return _queue.Dequeue();
            }
        }
    }
}
