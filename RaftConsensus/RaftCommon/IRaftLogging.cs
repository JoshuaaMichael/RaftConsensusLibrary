using System;

namespace TeamDecided.RaftCommon.Logging
{
    public interface IRaftLogging
    {
        void Log(ERaftLogType logType, string format, params object[] args);
        event EventHandler<Tuple<ERaftLogType, string>> OnNewLogEntry;
    }
}
