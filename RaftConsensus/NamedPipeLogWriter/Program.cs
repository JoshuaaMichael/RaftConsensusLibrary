using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeamDecided.RaftConsensus.NamedPipeLogWriter
{
    internal class Program
    {
        private static readonly StringBuilder Buffer = new StringBuilder();
        private static int _bufferedLineCount;

        private const string InstructionFlag = "###---###$$$";
        private const string LogFilename = "debug-{0}.log";
        private static string _currentLogFile;
        private static readonly ManualResetEvent OnClose = new ManualResetEvent(false);

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Console.WriteLine("Closing Window");
                OnClose.Set();
                FlushBuffer();
            };

            File.Delete(LogFilename);

            Task.Run(() => BackgroundThread());

            while (WaitHandle.WaitAny(new WaitHandle[] {OnClose}, 500) == WaitHandle.WaitTimeout)
            {
                FlushBuffer();
            }
        }

        private static void FlushBuffer()
        {
            lock (Buffer)
            {
                if (_bufferedLineCount == 0) return;
                Console.WriteLine("Flushing buffer. {0} entries", _bufferedLineCount);
                File.AppendAllText(_currentLogFile, Buffer.ToString());
                Buffer.Clear();
                _bufferedLineCount = 0;
            }
        }

        private static void AddToBuffer(string message)
        {
            lock (Buffer)
            {
                Buffer.Append(message);
                _bufferedLineCount += 1;
            }
        }

        private static void StartNewFile()
        {
            if (_currentLogFile != null) //Not first time running
            {
                AddToBuffer("##############################################" + Environment.NewLine);
                AddToBuffer("##################ENDING LOG##################" + Environment.NewLine);
                AddToBuffer("##############################################" + Environment.NewLine);
                FlushBuffer();
            }

            _currentLogFile = string.Format(LogFilename, DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            AddToBuffer("##############################################" + Environment.NewLine);
            AddToBuffer("###############STARTING NEW LOG###############" + Environment.NewLine);
            AddToBuffer("##############################################" + Environment.NewLine);
            FlushBuffer();
        }

        private static void BackgroundThread()
        {
            while (true)
            {
                StartNewFile();
                NamedPipeServerStream server = new NamedPipeServerStream("RaftConsensusLogging");
                try
                {
                    Console.WriteLine("Waiting for connection...");
                    server.WaitForConnection();

                    Console.WriteLine("Got connection!");
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


                        if (line.StartsWith(InstructionFlag) && line.EndsWith(InstructionFlag)) //This is an message with an instruction
                        {
                            string message = line.Substring(InstructionFlag.Length);
                            message = message.Substring(0, message.Length - InstructionFlag.Length);

                            int index = message.IndexOf(":"); //Checks if message contains an instruction with a parameter

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
                                        Console.WriteLine(parameter);
                                        break;
                                }
                            }
                        }

                        AddToBuffer(line);
                    }
                }
                catch
                {
                    Console.WriteLine("Caught exception on background thread");
                }
                finally
                {
                    server.Disconnect();
                    server.Dispose();
                }
            }
        }
    }
}
