using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking.Helpers;
using TeamDecided.RaftConsensus.Networking.Messages;
using TeamDecided.RaftConsensus.Consensus;

namespace TeamDecided.RaftConsensus.Tests.Networking.Helpers
{
    [TestFixture]
    internal class RaftPCQueueTests
    {
        private RaftPCQueue<int> _queue;
        [SetUp]
        public void BeforeTest()
        {
            _queue = new RaftPCQueue<int>();
        }

        [Test]
        public void HappyDaySingleThreaded()
        {
            _queue.Enqueue(1);
            _queue.Enqueue(2);
            _queue.Enqueue(3);

            _queue.Flag.WaitOne();

            _queue.Dequeue();
        }

        [Test]
        public void HappyDayDualThreaded()
        {
            const int numberOfValues = 100;
            List<int> dequeuedValues = new List<int>();

            Task t = Task.Run(() =>
            {
                WaitHandle[] handles = {_queue.Flag};
                while (true)
                {
                    int index = WaitHandle.WaitAny(handles, 1000);

                    if (index == WaitHandle.WaitTimeout)
                    {
                        break;
                    }

                    int val = _queue.Dequeue();
                    dequeuedValues.Add(val);
                }
            });

            for (int i = 0; i < numberOfValues; i++)
            {
                _queue.Enqueue(i);
                Thread.Sleep(50);
            }

            t.Wait();

            Assert.AreEqual(numberOfValues, dequeuedValues.Count);
        }

        [Test]
        public void HappyDayDualThreadedWithWaitLoop()
        {
            const int numberOfValues = 100;
            const int timeoutValue = 1000;
            List<int> dequeuedValues = new List<int>();

            WaitLoop waitLoop = new WaitLoop();

            waitLoop.RegisterTimeoutFunc(() => true, timeoutValue);

            waitLoop.RegisterAction((manualResetEvent, elapsedWait) =>
            {
                int val = _queue.Dequeue();
                dequeuedValues.Add(val);
                return false;
            }, _queue.Flag);

            Task t = Task.Run(() =>
            {
                waitLoop.Run();
            });

            for (int i = 0; i < numberOfValues; i++)
            {
                _queue.Enqueue(i);
                Thread.Sleep(50);
            }

            t.Wait();

            Assert.AreEqual(numberOfValues, dequeuedValues.Count);
        }

        [Test]
        public void UT_ClearMessageQueue_QueueEmpty()
        {
            int numberOfValues = 10;

            for (int i = 0; i < numberOfValues; i++)
            {
                _queue.Enqueue(i);
            }

            Assert.AreEqual(numberOfValues, _queue.Count());
            _queue.Clear();

            Assert.Zero(_queue.Count());

        }

    }
}
