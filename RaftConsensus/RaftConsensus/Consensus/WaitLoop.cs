using System;
using System.Collections.Generic;
using System.Threading;

namespace TeamDecided.RaftConsensus.Consensus
{
    class WaitLoop
    {
        private readonly List<ManualResetEvent> _waitHandles;
        private readonly List<Func<ManualResetEvent, bool>> _funcs;

        private Func<Exception, bool> _exceptionFunc;
        private Func<bool> _timeoutFunc;
        private int _timeoutMs;

        public WaitLoop()
        {
            _waitHandles = new List<ManualResetEvent>();
            _funcs = new List<Func<ManualResetEvent, bool>>();
        }

        public void RegisterAction(Func<ManualResetEvent, bool> func, params ManualResetEvent[] handles)
        {
            foreach (ManualResetEvent handle in handles)
            {
                if (_waitHandles.Contains(handle))
                {
                    throw new ArgumentException("Cannot add a wait handle which has already been added");
                }

                _waitHandles.Add(handle);
                _funcs.Add(func);
            }
        }

        public void RegisterExceptionFunc(Func<Exception, bool> func)
        {
            _exceptionFunc = func;
        }

        public void RegisterTimeoutFunc(Func<bool> func, int timeoutMs)
        {
            _timeoutMs = timeoutMs >= 0 ? timeoutMs : throw new ArgumentException("timeoutMs must be >= 0");
            _timeoutFunc = func;
        }

        public void UpdateTimeoutFuncTimeout(int timeoutMs)
        {
            _timeoutMs = timeoutMs;
        }

        public void Run()
        {
            WaitHandle[] waitHandles = _waitHandles.ToArray();
            while (true)
            {
                int index;
                try
                {
                    index = _timeoutFunc != null
                        ? WaitHandle.WaitAny(waitHandles, _timeoutMs)
                        : WaitHandle.WaitAny(waitHandles);
                }
                catch (Exception e)
                {
                    if (_exceptionFunc?.Invoke(e) == true) return;
                    continue;
                }

                switch (index)
                {
                    case WaitHandle.WaitTimeout when _timeoutFunc?.Invoke() == true:
                        return;
                    case WaitHandle.WaitTimeout:
                        continue;
                    default:
                        {
                            if (_funcs[index](_waitHandles[index])) return;
                            continue;
                        }
                }
            }
        }
    }
}
