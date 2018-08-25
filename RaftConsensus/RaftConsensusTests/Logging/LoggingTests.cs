using System;
using System.IO;
using NUnit.Framework;
using System.Threading;
using TeamDecided.RaftConsensus.Common.Logging;

namespace TeamDecided.RaftConsensus.Tests.Logging
{
    [TestFixture]
    class LoggingTests
    {
        private RaftLogging _logging;
        private readonly string _logFileName = TestContext.CurrentContext.TestDirectory + @"\debug.log";
        private const ERaftLogType DEFAULT_ERAFT_LOG_TYPE= ERaftLogType.Debug;

        [SetUp]
        public void SetUp()
        {
            _logging = RaftLogging.Instance;
            _logging.WriteToEvent = true;
            _logging.WriteToFile = true;
            _logging.LogFilename = _logFileName;
            _logging.LogLevel = DEFAULT_ERAFT_LOG_TYPE;
        }

        [Test]
        public void UT_GettersSetters()
        {
            Assert.AreEqual(_logging.LogFilename, _logFileName);
            Assert.AreEqual(_logging.LogLevel, DEFAULT_ERAFT_LOG_TYPE);
        }

        [TestCase(10)]
        public void UT_EnableBuffer(int bufferLines)
        {
            _logging.EnableBuffer(bufferLines);
            _logging.DeleteExistingLogFile();

            FileAssert.DoesNotExist(_logFileName);

            string message = Guid.NewGuid().ToString();
            string messageFormat = message + ": {0}";
            CountdownEvent countdown = new CountdownEvent(bufferLines);

            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(message))
                {
                    countdown.Signal();
                }
            };

            //spam messages to log
            for (int i = 1; i < bufferLines ; i++)
            {
                _logging.Log(DEFAULT_ERAFT_LOG_TYPE, messageFormat, i);
            }

            string[] lines;

            FileAssert.DoesNotExist(_logFileName);

            _logging.Log(DEFAULT_ERAFT_LOG_TYPE, messageFormat, bufferLines);

            countdown.Wait();

            FileAssert.Exists(_logFileName);
            lines = File.ReadAllLines(_logFileName);
            Assert.IsNotEmpty(lines);

            string[] lastline = lines[lines.Length - 1].Split(' ');
            Assert.AreEqual(lastline[1].Substring(0, lastline[1].Length-1), message);
        }

        [TestCase(10)]
        public void UT_FlushBufferAppendsToLog(int buffersize)
        {
            _logging.EnableBuffer(buffersize);
            _logging.DeleteExistingLogFile();

            _logging.FlushBuffer();

            FileAssert.DoesNotExist(_logFileName);


            string message = Guid.NewGuid().ToString();

            CountdownEvent countdown = new CountdownEvent(buffersize);

            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(message))
                {
                    countdown.Signal();
                }
            };

            //spam messages to log
            for (int i = 1; i < buffersize; i++)
            {
                _logging.Log(DEFAULT_ERAFT_LOG_TYPE, "{0}", message);
            }


            FileAssert.DoesNotExist(_logFileName);

            _logging.FlushBuffer();

            FileAssert.Exists(_logFileName);
            string[] lines = File.ReadAllLines(_logFileName);
            Assert.IsNotEmpty(lines);
            Assert.AreEqual(lines.Length, buffersize-1);
            string lastline = lines[lines.Length - 1].Split(' ')[1];
            Assert.AreEqual(lastline, message);


            //countdown.Wait();

            //FileAssert.Exists(LOG_FILE_NAME);
            //lines = File.ReadAllLines(LOG_FILE_NAME);
            //Assert.IsNotEmpty(lines);
        }

        [Test]
        public void UT_WriteToPipe()
        {
            string message = Guid.NewGuid().ToString();

            _logging.WriteToFile = false;
            _logging.WriteToEvent = true;

            ManualResetEvent gotLogEntry = new ManualResetEvent(false);
            //CountdownEvent countdown = new CountdownEvent(10);
            bool caughtMessage = false;

            RaftLogging.Instance.OnNewLogEntry += (sender, tuple) =>
            {
                if (tuple.Item2.Contains(message))
                {
                    //countdown.AddCount();
                    //countdown.Signal();
                    gotLogEntry.Set();
                    caughtMessage = true;
                }
            };

            _logging.WriteToNamedPipe = true;
            _logging.NamedPipeName = "RaftConsensus0";
            _logging.NamedPipeRequestNewFile();

            for (int i = 0; i < 10; i++)
            {
                _logging.Log(ERaftLogType.Info, "{0} log entry", message);
            }

            _logging.FlushBuffer();

            gotLogEntry.WaitOne(5000);
            //countdown.Wait(5000);
            Assert.IsTrue(caughtMessage);
        }
    }
}
