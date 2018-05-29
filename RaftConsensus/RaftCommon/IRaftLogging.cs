using System;

namespace TeamDecided.RaftCommon.Logging
{
    public interface IRaftLogging
    {
        void Trace(string format, params object[] args);
        void Debug(string format, params object[] args);
        void Info(string format, params object[] args);
        void Warn(string format, params object[] args);
        void Error(string format, params object[] args);
        void Fatal(string format, params object[] args);

        event EventHandler<string> OnNewLineTrace;
        event EventHandler<string> OnNewLineDebug;
        event EventHandler<string> OnNewLineInfo;
        event EventHandler<string> OnNewLineWarn;
        event EventHandler<string> OnNewLineError;
        event EventHandler<string> OnNewLineFatal;
    }
}
