using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TeamDecided.RaftConsensus.Tests
{
    [TestFixture]
    public class RaftDistributedLogTests
    {
        RaftDistributedLog<string, string> raftDistributedLog;

        [SetUp]
        public void BeforeEachTest()
        {
            raftDistributedLog = new RaftDistributedLog<string, string>();
        }

        [Test]
        public void IT_EnterKey_DoesntThrow()
        {
            raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
        }

        [Test]
        public void IT_GetValueFound_ReturnsValue()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value, 1));

            Assert.AreEqual(value, raftDistributedLog.GetValue(key));
        }

        [Test]
        public void IT_GetValueHistory_ReturnsValues()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for(int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            string[] valueHistory = raftDistributedLog.GetValueHistory(key);

            Assert.AreEqual(value.Length, valueHistory.Length);

            for (int i = 0; i < valueHistory.Length; i++)
            {
                Assert.AreEqual(value[0], valueHistory[0]);
            }
        }

        [Test]
        public void IT_GetValueNotFound_Throws()
        {
            raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { raftDistributedLog.GetValue(keyToLookFor); });
        }

        [Test]
        public void IT_GetValueHistoryNotFound_Throws()
        {
            raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { raftDistributedLog.GetValueHistory(keyToLookFor); });
        }

        [Test]
        public void IT_GetEntryFound_ReturnsValue()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value, 1));

            Assert.AreEqual(value, raftDistributedLog.GetEntry(key).Value);
        }

        [Test]
        public void IT_GetEntryHistory_ReturnsValues()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            RaftLogEntry<string, string>[] valueHistory = raftDistributedLog.GetEntryHistory(key);

            Assert.AreEqual(value.Length, valueHistory.Length);

            for (int i = 0; i < valueHistory.Length; i++)
            {
                Assert.AreEqual(value[0], valueHistory[0].Value);
            }
        }

        [Test]
        public void IT_GetEntryNotFound_Throws()
        {
            raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { raftDistributedLog.GetEntry(keyToLookFor); });
        }

        [Test]
        public void IT_GetEntryHistoryNotFound_Throws()
        {
            raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { raftDistributedLog.GetEntryHistory(keyToLookFor); });
        }

        [Test]
        public void IT_GetValueFromCommitIndex_ReturnsValue()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            for (int i = 0; i < value.Length; i++)
            {
                Assert.AreEqual(value[i], raftDistributedLog.GetValue(i));
            }
        }
    }
}
