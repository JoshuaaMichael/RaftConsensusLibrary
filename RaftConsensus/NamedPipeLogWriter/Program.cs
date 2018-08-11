using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeamDecided.RaftConsensus.NamedPipeLogWriter
{
    internal class Program
    {
        private const string InstructionFlag = "###---###$$$";
        private const string LogFilename = "debug-{0}-{1}.log";
        private static readonly ManualResetEvent OnClose = new ManualResetEvent(false);

        private const string NamedPipePrependName = "RaftConsensus";
        private const int DefaultNumberOfLogBuffers = 3;
        private static int _numberOfLogBuffers;
        private static List<LoggingBuffer> _loggingBuffers;
        private static List<string> _logFilenames;
        private static List<Task> _tasks;

        private static void Main(string[] args)
        {
            _numberOfLogBuffers = args.Length > 0 ? int.Parse(args[0]) : DefaultNumberOfLogBuffers;

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Console.WriteLine("Closing Window");
                OnClose.Set();
            };

            _loggingBuffers = new List<LoggingBuffer>();
            _logFilenames = new List<string>();
            _tasks = new List<Task>();

            string dateTimeString = GetLogFileDateTimeString();

            for (int i = 0; i < _numberOfLogBuffers; i++)
            {
                var temp = i;
                _loggingBuffers.Add(new LoggingBuffer());
                _logFilenames.Add(string.Format(LogFilename, temp, dateTimeString));
                _tasks.Add(Task.Run(() => ThreadPerNamedPipe(temp)));
            }

            while (WaitHandle.WaitAny(new WaitHandle[] {OnClose}, 500) == WaitHandle.WaitTimeout)
            {
                FlushAllBuffers();
            }

            foreach (Task task in _tasks)
            {
                task.Wait();
            }
        }

        private static void FlushAllBuffers()
        {
            for (int i = 0; i < _loggingBuffers.Count; i++)
            {
                FlushBuffer(i);
            }
        }

        private static void FlushBuffer(int pipeNumber)
        {
            LoggingBuffer temp = _loggingBuffers[pipeNumber];

            lock (temp)
            {
                if (!temp.HasEverReceivedMessages || temp.Count == 0) return;

                WriteToConsole(pipeNumber, $"Flushing buffer. {temp.Count} entries");

                File.AppendAllText(_logFilenames[pipeNumber], temp.ToString());
            }
        }

        private static void AddToBuffer(int pipeNumber, string message, bool isHeader = false)
        {
            lock (_loggingBuffers[pipeNumber])
            {
                _loggingBuffers[pipeNumber].AddToBuffer(message, isHeader);
            }
        }

        private static string GetLogFileDateTimeString()
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }

        private static void StartNewFile(int pipeNumber)
        {
            lock (_loggingBuffers[pipeNumber])
            {
                if (_loggingBuffers[pipeNumber].HasEverReceivedMessages)
                {
                    AddToBuffer(pipeNumber, "##############################################" + Environment.NewLine);
                    AddToBuffer(pipeNumber, "##################ENDING LOG##################" + Environment.NewLine);
                    AddToBuffer(pipeNumber, "##############################################" + Environment.NewLine);
                    FlushBuffer(pipeNumber);
                }

                _logFilenames[pipeNumber] = string.Format(LogFilename, pipeNumber, GetLogFileDateTimeString());

                AddToBuffer(pipeNumber, "##############################################" + Environment.NewLine, true);
                AddToBuffer(pipeNumber, "###############STARTING NEW LOG###############" + Environment.NewLine, true);
                AddToBuffer(pipeNumber, "##############################################" + Environment.NewLine, true);
            }

        }

        private static void ThreadPerNamedPipe(int pipeNumber)
        {
            while (true)
            {
                StartNewFile(pipeNumber);
                NamedPipeServerStream server = new NamedPipeServerStream(NamedPipePrependName + pipeNumber);
                try
                {
                    WriteToConsole(pipeNumber, "Waiting for connection");
                    server.WaitForConnection();
                    WriteToConsole(pipeNumber, "Got connection!");

                    while (true)
                    {
                        byte[] buffer = new byte[4096];
                        int length = server.Read(buffer, 0, buffer.Length);

                        if (length == 0)
                        {
                            throw new Exception("Client left");
                        }

                        byte[] chunk = new byte[length];
                        Array.Copy(buffer, chunk, length);
                        string line = Encoding.UTF8.GetString(chunk);

                        if (!line.StartsWith(InstructionFlag) || !line.EndsWith(InstructionFlag))
                        {
                            AddToBuffer(pipeNumber, line);
                            continue;
                        }

                        string message = line.Substring(InstructionFlag.Length);
                        message = message.Substring(0, message.Length - InstructionFlag.Length);

                        int index = message.IndexOf(":",
                            StringComparison.Ordinal); //Checks if message contains an instruction with a parameter

                        if (index == -1)
                        {
                            switch (message)
                            {
                                case "MakeNewFile":
                                    throw new Exception("Breaking to start new file");
                            }
                        }
                        else
                        {
                            string instruction = message.Substring(0, index);
                            string parameter = message.Substring(index + 1);

                            switch (instruction)
                            {
                                case "Message":
                                    WriteToConsole(pipeNumber, parameter);
                                    break;
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    WriteToConsole(pipeNumber, "Caught exception on background thread - " + e.Message);
                }
                finally
                {
                    FlushBuffer(pipeNumber);
                    server?.Disconnect();
                    server?.Dispose();
                }
            }
        }

        private static void WriteToConsole(int pipeNumber, string message)
        {
            Console.WriteLine("{0}{1} - {2}", NamedPipePrependName, pipeNumber, message);
        }
    }
}