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

        private bool doDebug = false;
        private bool doError = false;
        private bool doFatal = false;
        private bool doInfo = false;
        private bool doTrace = false;
        private bool doWarn = false;

        public event EventHandler<string> OnNewLineTrace;
        public event EventHandler<string> OnNewLineDebug;
        public event EventHandler<string> OnNewLineInfo;
        public event EventHandler<string> OnNewLineWarn;
        public event EventHandler<string> OnNewLineError;
        public event EventHandler<string> OnNewLineFatal;

        public void EnableBuffer(int linesToBufferCount)
        {
            this.linesToBufferCount = linesToBufferCount;
            buffer = new List<string>(linesToBufferCount);
        }

        private void WriteToLog(bool doLogLevel, EventHandler<string> onNewLineEvent, string format, params object[] args)
        {
            if(doLogLevel)
            {
                string message = string.Format(GetTimestampString() + format + Environment.NewLine, args);
                onNewLineEvent?.Invoke(this, message);
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

        public void FlushBuffer()
        {
            File.AppendAllLines(loggingFileName, buffer);
            buffer.Clear();
        }


        public void Debug(string format, params object[] args)
        {
            WriteToLog(doDebug, OnNewLineDebug, format, args);
        }

        public void Error(string format, params object[] args)
        {
            WriteToLog(doError, OnNewLineError, format, args);
        }

        public void Fatal(string format, params object[] args)
        {
            WriteToLog(doFatal, OnNewLineFatal, format, args);
        }

        public void Info(string format, params object[] args)
        {
            WriteToLog(doInfo, OnNewLineInfo, format, args);
        }

        public void Trace(string format, params object[] args)
        {
            WriteToLog(doTrace, OnNewLineTrace, format, args);
        }

        public void Warn(string format, params object[] args)
        {
            WriteToLog(doWarn, OnNewLineWarn, format, args);
        }

        public void SetDoDebug(bool targetValue = true)
        {
            lock(verbositySelection)
            {
                doDebug = targetValue;
            }
        }

        public void SetDoError(bool targetValue = true)
        {
            lock (verbositySelection)
            {
                doError = targetValue;
            }
        }

        public void SetDoFatal(bool targetValue = true)
        {
            lock (verbositySelection)
            {
                doFatal = targetValue;
            }
        }

        public void SetDoInfo(bool targetValue = true)
        {
            lock (verbositySelection)
            {
                doInfo = targetValue;

            }
        }

        public void SetDoTrace(bool targetValue = true)
        {
            lock (verbositySelection)
            {
                doTrace = targetValue;
            }
        }

        public void SetDoWarn(bool targetValue = true)
        {
            lock (verbositySelection)
            {
                doWarn = targetValue;
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
