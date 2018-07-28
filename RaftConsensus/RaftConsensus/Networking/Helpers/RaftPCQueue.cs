using System.Collections.Generic;
using System.Threading;

//TODO: Add support for being used by more than just one thread for P and one for C

namespace TeamDecided.RaftConsensus.Networking.Helpers
{
    internal class RaftPCQueue<T>
    {
        private readonly Queue<T> _queue;
        public ManualResetEvent Flag { get; }

        public RaftPCQueue()
        {
            _queue = new Queue<T>();
            Flag = new ManualResetEvent(false);
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
                Flag.Set();
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                if (_queue.Count == 1)
                {
                    Flag.Reset();
                }
                return _queue.Dequeue();
            }
        }

        public int Count()
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }

        public void Clear()
        {
            lock (_queue)
            {
                Flag.Reset();
                _queue.Clear();
            }
        }
    }
}
