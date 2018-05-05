using System;

namespace TeamDecided.RaftCommon.Disposable
{
    public interface IRaftDisposable : IDisposable
    {
        bool IsDisposed();
    }
}
