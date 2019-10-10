using System.Collections.Generic;
using System.Threading;

namespace UDPNetworking.Utilities.PCQueue
{
    internal class PCQueue<T> : IPCQueue<T>
    {
        private readonly Queue<T> _queue;
        public ManualResetEvent Flag { get; }

        public PCQueue()
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
