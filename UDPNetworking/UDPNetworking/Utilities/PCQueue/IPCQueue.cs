using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Utilities.PCQueue
{
    public interface IPCQueue<T>
    {
        void Enqueue(T item);
        T Dequeue();
        int Count();
        void Clear();
    }
}
