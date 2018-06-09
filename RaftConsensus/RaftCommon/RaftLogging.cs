using System;
using System.Collections.Generic;
using System.IO;

namespace TeamDecided.RaftCommon.Logging
{
    public class RaftLogging : IRaftLogging
    {
        private static RaftLogging instance = null;
        private static readonly object instanceLock = new object();
        private static readonly object methodLock = new object();
        private static readonly object verbositySelection = new object();
        private const string defaultFilename = "debug.log";
        private string loggingFileName = defaultFilename;

        private int linesToBufferCount;
        private List<string> buffer;

        ERaftLogType logLevel;
        public event EventHandler<Tuple<ERaftLogType, string>> OnNewLogEntry;

        public void EnableBuffer(int linesToBufferCount)
        {
            this.linesToBufferCount = linesToBufferCount;
            buffer = new List<string>(linesToBufferCount);
        }

        public void FlushBuffer()
        {
            File.AppendAllLines(loggingFileName, buffer);
            buffer.Clear();
        }

        public void Log(ERaftLogType logType, string format, params object[] args)
        {
            if(logType >= logLevel)
            {
                string message = string.Format(GetTimestampString() + format + Environment.NewLine, args);
                OnNewLogEntry?.Invoke(this, new Tuple<ERaftLogType, string>(logType, message));
                if (buffer == null)
                {
                    lock (this)
                    {
                        File.AppendAllText(loggingFileName, message);
                    }
                }
                else
                {
                    lock (this)
                    {
                        buffer.Add(message.TrimEnd());
                        if (buffer.Count == linesToBufferCount)
                        {
                            FlushBuffer();
                        }
                    }
                }
            }
        }

        public void SetLogLevel(ERaftLogType logType)
        {
            lock (verbositySelection)
            {
                logLevel = logType;
            }
        }

        public static RaftLogging Instance
        {
            get
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new RaftLogging();
                    }

                    return instance;
                }
            }
        }

        public void OverwriteLoggingFile(string newFilename)
        {
            loggingFileName = newFilename;
        }

        public void DeleteExistingLogFile()
        {
            File.Delete(loggingFileName);
        }

        private string GetTimestampString()
        {
            return DateTime.Now.ToString("HH:mm:ss.ffff") + ": ";
        }
    }
}
