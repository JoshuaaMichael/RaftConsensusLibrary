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
        private const string LOG_FILE_NAME = @"C:\Users\Tori\Downloads\debug.log";
        private const ERaftLogType DEFAULT_ERAFT_LOG_TYPE= ERaftLogType.Debug;


        [SetUp]
        public void SetUp()
        {
            _logging = RaftLogging.Instance;
            _logging.LogFilename = LOG_FILE_NAME;
            _logging.LogLevel = DEFAULT_ERAFT_LOG_TYPE;
        }

        [Test]
        public void UT_GettersSetters()
        {
            Assert.AreEqual(_logging.LogFilename, LOG_FILE_NAME);
            Assert.AreEqual(_logging.LogLevel, DEFAULT_ERAFT_LOG_TYPE);
        }

        [TestCase(10)]
        public void UT_EnableBuffer(int bufferLines)
        {
            _logging.EnableBuffer(bufferLines);
            _logging.DeleteExistingLogFile();

            FileAssert.DoesNotExist(LOG_FILE_NAME);

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

            FileAssert.DoesNotExist(LOG_FILE_NAME);

            _logging.Log(DEFAULT_ERAFT_LOG_TYPE, messageFormat, bufferLines);

            countdown.Wait();

            FileAssert.Exists(LOG_FILE_NAME);
            lines = File.ReadAllLines(LOG_FILE_NAME);
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

            FileAssert.DoesNotExist(LOG_FILE_NAME);


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


            FileAssert.DoesNotExist(LOG_FILE_NAME);

            _logging.FlushBuffer();

            FileAssert.Exists(LOG_FILE_NAME);
            string[] lines = File.ReadAllLines(LOG_FILE_NAME);
            Assert.IsNotEmpty(lines);
            Assert.AreEqual(lines.Length, buffersize-1);
            string lastline = lines[lines.Length - 1].Split(' ')[1];
            Assert.AreEqual(lastline, message);


            //countdown.Wait();

            //FileAssert.Exists(LOG_FILE_NAME);
            //lines = File.ReadAllLines(LOG_FILE_NAME);
            //Assert.IsNotEmpty(lines);
        }
    }
}
