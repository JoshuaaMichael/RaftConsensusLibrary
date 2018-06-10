using System;
using System.Collections.Generic;
using System.IO;

namespace TeamDecided.RaftConsensus.Common.Logging
{
    public sealed class RaftLogging : IRaftLogging
    {
        public static RaftLogging Instance { get; } = new RaftLogging();

        public event EventHandler<Tuple<ERaftLogType, string>> OnNewLogEntry;

        private readonly  object _methodLockObject = new object();

        private const string DefaultFilename = "debug.log";
        private string _loggingFileName = DefaultFilename;

        private List<string> _buffer;
        private int _linesToBufferCount;

        private ERaftLogType _logLevel;

        private RaftLogging() { }

        public void Log(ERaftLogType logType, string format, params object[] args)
        {
            lock (_methodLockObject)
            {
                if (logType < _logLevel) return;

                string message = string.Format(GetTimestampString() + format + Environment.NewLine, args);

                OnNewLogEntry?.Invoke(this, new Tuple<ERaftLogType, string>(logType, message));

                if (_buffer == null)
                {
                    File.AppendAllText(_loggingFileName, message);
                }
                else
                {
                    _buffer.Add(message.TrimEnd());
                    if (_buffer.Count == _linesToBufferCount)
                    {
                        FlushBuffer();
                    }
                }
            }
        }

        public ERaftLogType LogLevel
        {
            get
            {
                lock (_methodLockObject)
                {
                    return _logLevel;
                }
            }
            set
            {
                lock (_methodLockObject)
                {
                    _logLevel = value;
                }
            }
        }

        public string LogFilename
        {
            get
            {
                lock (_methodLockObject)
                {
                    return _loggingFileName;
                }
            }
            set
            {
                lock (_methodLockObject)
                {
                    _loggingFileName = value;
                }
            }
        }

        public void DeleteExistingLogFile()
        {
            lock (_methodLockObject)
            {
                File.Delete(_loggingFileName);
            }
        }

        public void EnableBuffer(int linesToBufferCount)
        {
            lock (_methodLockObject)
            {
                _linesToBufferCount = linesToBufferCount;
                _buffer = new List<string>(linesToBufferCount);
            }
        }

        public void FlushBuffer()
        {
            lock (_methodLockObject)
            {
                if (_buffer == null || _buffer.Count <= 0) return;
                File.AppendAllLines(_loggingFileName, _buffer);
                _buffer.Clear();
            }
        }

        private static string GetTimestampString()
        {
            return DateTime.Now.ToString("HH:mm:ss.ffff") + ": ";
        }
    }
}
