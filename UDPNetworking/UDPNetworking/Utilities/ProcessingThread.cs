using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UDPNetworking.Utilities
{
    internal abstract class ProcessingThread<T> : IDisposable
    {
        private readonly Thread _thread;
        private readonly ManualResetEvent _onStop;
        private readonly Func<Task<T>> _waitCondition;
        private readonly Action<T> _action;
        private readonly Func<Exception, bool> _onException;

        protected bool DisposedValue; // To detect redundant calls

        protected ProcessingThread(Func<Task<T>> waitCondition, Action<T> action, Func<Exception, bool> onException)
        {
            _thread = new Thread(Process);
            _onStop = new ManualResetEvent(false);
            _waitCondition = waitCondition;
            _action = action;
            _onException = onException;
        }

        public void Process()
        {
            if (DisposedValue)
            {
                throw new InvalidOperationException("Class is currently not in a state it may start processing in");
            }

            Task taskOnStop = Task.Run(() =>
            {
                _onStop.WaitOne();
            });

            while (true)
            {
                try
                {
                    Task<T> t = _waitCondition();


                    if (Task.WaitAny(taskOnStop, t) == 0)
                    {
                        return;
                    }

                    _action(t.Result);
                }
                catch (Exception e)
                {
                    if (_onException(e))
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

        protected abstract bool Wait(WaitHandle[] handles);
    }
}
