using System;
using System.Threading;

namespace UDPNetworking.Utilities.ProcessingThread
{
    internal class ProcessingHandleThread<T> : IProcessingThread
    {
        private readonly Thread _thread;
        private readonly ManualResetEvent _onStop;
        private readonly WaitHandle _waitConditionHandle;
        private readonly Func<T> _producer;
        private readonly Action<T> _action;
        private readonly Func<Exception, bool> _onException;

        protected bool DisposedValue; // To detect redundant calls

        internal ProcessingHandleThread(WaitHandle waitConditionHandle, Func<T> producer, Action<T> action, Func<Exception, bool> onException)
        {
            _thread = new Thread(Process);
            _onStop = new ManualResetEvent(false);
            _waitConditionHandle = waitConditionHandle;
            _producer = producer;
            _action = action;
            _onException = onException;
        }

        public void Start()
        {
            _thread.Start();
        }

        private void Process()
        {
            if (DisposedValue)
            {
                throw new InvalidOperationException("Class is currently not in a state it may start processing in");
            }

            WaitHandle[] resetEvents = new WaitHandle[2];
            resetEvents[0] = _onStop;
            resetEvents[1] = _waitConditionHandle;

            while (true)
            {
                try
                {
                    int index = WaitHandle.WaitAny(resetEvents);

                    if (index == 0)
                    {
                        return;
                    }

                    _action(_producer());
                }
                catch (Exception e)
                {
                    if (!_onException(e))
                    {
                        Stop();
                    }
                }
            }
        }

        public void Stop()
        {
            _onStop.Set();
            _thread.Join();
        }

        public void Dispose()
        {
            if (DisposedValue) return;

            Stop();

            DisposedValue = true;
        }
    }
}
