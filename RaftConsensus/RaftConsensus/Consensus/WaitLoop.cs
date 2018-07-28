using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;

namespace TeamDecided.RaftConsensus.Consensus
{
    internal class WaitLoop
    {
        private readonly List<ManualResetEvent> _waitHandles;
        private readonly List<Func<ManualResetEvent, int, bool>> _funcs;

        private Func<Exception, bool> _exceptionFunc;
        private Func<bool> _timeoutFunc;
        public int TimeoutMs;

        public WaitLoop()
        {
            _waitHandles = new List<ManualResetEvent>();
            _funcs = new List<Func<ManualResetEvent, int, bool>>();
        }

        public void RegisterAction(Func<ManualResetEvent, int, bool> func, ManualResetEvent handle)
        {
            if (_waitHandles.Contains(handle))
            {
                throw new ArgumentException("Cannot add a wait handle which has already been added");
            }

            _waitHandles.Add(handle);
            _funcs.Add(func);
        }

        public void RegisterExceptionFunc(Func<Exception, bool> func)
        {
            _exceptionFunc = func;
        }

        public void RegisterTimeoutFunc(Func<bool> func, int timeoutMs)
        {
            TimeoutMs = timeoutMs >= 0 ? timeoutMs : throw new ArgumentException("timeoutMs must be >= 0");
            _timeoutFunc = func;
        }

        public void Run()
        {
            WaitHandle[] waitHandles = _waitHandles.ToArray();
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                int index;
                try
                {
                    stopwatch.Restart();
                    index = _timeoutFunc != null
                        ? WaitHandle.WaitAny(waitHandles, TimeoutMs)
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
                            if (_funcs[index](_waitHandles[index], (int)stopwatch.ElapsedMilliseconds)) return;
                            continue;
                        }
                }
            }
        }
    }
}
