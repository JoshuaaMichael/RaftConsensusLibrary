using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace TeamDecided.RaftConsensus.Common.Logging
{
    public sealed class RaftLogging : IRaftLogging, IDisposable
    {
        //Singleton
        public static RaftLogging Instance { get; } = new RaftLogging();

        //Member variables
        private readonly object _methodLockObject = new object();
        private ERaftLogType _logLevel;
        public bool WriteToFile;
        public bool WriteToNamedPipe;
        public bool WriteToEvent;
        private DateTime _startTime;
        private Stopwatch _stopwatch;

        //File handling
        private List<string> _buffer;
        private int _linesToBufferCount;
        public const string DefaultFilename = "debug.log";
        private string _loggingFileName;

        //Event handling
        public event EventHandler<Tuple<ERaftLogType, string>> OnNewLogEntry;

        //Named pipe handling
        private NamedPipeClientStream _namedPipe;


        private RaftLogging()
        {
            _startTime = DateTime.Now;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public void Log(ERaftLogType logType, string format, params object[] args)
        {
            lock (_methodLockObject)
            {
                if (logType < _logLevel) return;
                string message = string.Format(GetTimestampString() + format + Environment.NewLine, args);

                if (WriteToEvent)
                {
                    WriteEvent(logType, message);
                }

                if (WriteToNamedPipe)
                {
                    WriteNamedPipe(message);
                }

                if (WriteToFile)
                {
                    WriteFile(message);
                }
            }
        }

        private void WriteEvent(ERaftLogType logType, string message)
        {
            OnNewLogEntry?.Invoke(this, new Tuple<ERaftLogType, string>(logType, message));
        }

        private void WriteNamedPipe(string message)
        {
            SetupNamedPipe();
            if(!WriteToNamedPipe)  return;

            byte[] output = Encoding.UTF8.GetBytes(message);

            try
            {
                _namedPipe.Write(output, 0, output.Length);
                _namedPipe.Flush();
            }
            catch
            {
                SetupNamedPipe();
                _namedPipe.Write(output, 0, output.Length);
                _namedPipe.Flush();
            }
        }

        public void NamedPipeRequestNewFile()
        {
            //Just a random sequence pattern for both sides to detect a direct message
            WriteNamedPipe("###---###$$$MakeNewFile###---###$$$");
        }

        public void NamedPipeWriteToConsole(string message)
        {
            WriteNamedPipe("###---###$$$Message:" + message + "###---###$$$");
        }

        private void SetupNamedPipe()
        {
            if (_namedPipe == null)
            {
                _namedPipe = new NamedPipeClientStream(".", "RaftConsensusLogging", PipeDirection.Out,
                    PipeOptions.WriteThrough);
            }

            if (_namedPipe.IsConnected) return;
            try
            {
                _namedPipe.Connect(0);
            }
            catch (TimeoutException) //the server's named pipe is not running
            {
                WriteToNamedPipe = false;
                _namedPipe.Dispose();
                _namedPipe = null;
            }
        }

        private void WriteFile(string message)
        {
            if (_loggingFileName == null)
            {
                return;
            }

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
                if (_loggingFileName == null)
                {
                    return;
                }

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
                if (_loggingFileName == null)
                {
                    return;
                }

                if (_buffer == null || _buffer.Count <= 0) return;
                File.AppendAllLines(_loggingFileName, _buffer);
                _buffer.Clear();
            }
        }

        private string GetTimestampString()
        {
            return (_startTime + _stopwatch.Elapsed).ToString("HH:mm:ss.fff") + ": ";
        }

        public static string FlattenException(Exception exception)
        {
            StringBuilder stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }

        public void Dispose()
        {
            FlushBuffer();
            _namedPipe?.Close();
            _namedPipe?.Dispose();
            _namedPipe = null;
        }
    }
}
