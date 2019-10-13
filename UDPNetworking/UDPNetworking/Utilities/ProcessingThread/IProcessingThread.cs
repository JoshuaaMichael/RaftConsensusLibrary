using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Utilities.ProcessingThread
{
    internal interface IProcessingThread : IDisposable
    {
        void Start();
        void Stop();
    }
}
