using System;
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

        private static bool doDebug = false;
        private static bool doError = false;
        private static bool doFatal = false;
        private static bool doInfo = false;
        private static bool doTrace = false;
        private static bool doWarn = false;

        public void Debug(string format, params object[] args)
        {
            lock (methodLock)
            {
                lock (verbositySelection)
                {
                    if (doDebug)
                    {
                        File.AppendAllText(loggingFileName, string.Format(GetTimestampString() + format + Environment.NewLine, args));
                    }
                }
            }
        }

        public void Error(string format, params object[] args)
        {
            lock (methodLock)
            {
                lock (verbositySelection)
                {
                    if (doError)
                    {
                        File.AppendAllText(loggingFileName, string.Format(GetTimestampString() + format + Environment.NewLine, args));
                    }
                }
            }
        }

        public void Fatal(string format, params object[] args)
        {
            lock (methodLock)
            {
                lock (verbositySelection)
                {
                    if (doFatal)
                    {
                        File.AppendAllText(loggingFileName, string.Format(GetTimestampString() + format + Environment.NewLine, args));
                    }
                }
            }
        }

        public void Info(string format, params object[] args)
        {
            lock (methodLock)
            {
                lock (verbositySelection)
                {
                    if (doInfo)
                    {
                        File.AppendAllText(loggingFileName, string.Format(GetTimestampString() + format + Environment.NewLine, args));
                    }
                }
            }
        }

        public void Trace(string format, params object[] args)
        {
            lock (methodLock)
            {
                lock (verbositySelection)
                {
                    if (doTrace)
                    {
                        File.AppendAllText(loggingFileName, string.Format(GetTimestampString() + format + Environment.NewLine, args));
                    }
                }
            }
        }

        public void Warn(string format, params object[] args)
        {
            lock (methodLock)
            {
                lock (verbositySelection)
                {
                    if (doWarn)
                    {
                        File.AppendAllText(loggingFileName, string.Format(GetTimestampString() + format + Environment.NewLine, args));
                    }
                }
            }
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
