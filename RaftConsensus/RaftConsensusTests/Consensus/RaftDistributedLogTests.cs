using System;
using System.Collections.Generic;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Consensus;

namespace TeamDecided.RaftConsensus.Tests.Consensus
{
    [TestFixture]
    public class RaftDistributedLogTests
    {
        RaftDistributedLog<string, string> _raftDistributedLog;

        [SetUp]
        public void BeforeEachTest()
        {
            _raftDistributedLog = new RaftDistributedLog<string, string>();
        }

        [Test]
        public void IT_EnterKey_DoesntThrow()
        {
            _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
        }

        [Test]
        public void IT_GetValueFound_ReturnsValue()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value, 1));

            Assert.AreEqual(value, _raftDistributedLog.GetValue(key));
        }

        [Test]
        public void IT_GetValueHistory_ReturnsValues()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for(int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            string[] valueHistory = _raftDistributedLog.GetValueHistory(key);

            Assert.AreEqual(value.Length, valueHistory.Length);

            for (int i = 0; i < valueHistory.Length; i++)
            {
                Assert.AreEqual(value[0], valueHistory[0]);
            }
        }

        [Test]
        public void IT_GetValueNotFound_Throws()
        {
            _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { _raftDistributedLog.GetValue(keyToLookFor); });
        }

        [Test]
        public void IT_GetValueHistoryNotFound_Throws()
        {
            _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { _raftDistributedLog.GetValueHistory(keyToLookFor); });
        }

        [Test]
        public void IT_GetEntryFound_ReturnsValue()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value, 1));

            Assert.AreEqual(value, _raftDistributedLog.GetEntry(key).Value);
        }

        [Test]
        public void IT_GetEntryHistory_ReturnsValues()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            RaftLogEntry<string, string>[] valueHistory = _raftDistributedLog.GetEntryHistory(key);

            Assert.AreEqual(value.Length, valueHistory.Length);

            for (int i = 0; i < valueHistory.Length; i++)
            {
                Assert.AreEqual(value[0], valueHistory[0].Value);
            }
        }

        [Test]
        public void IT_GetEntryNotFound_Throws()
        {
            _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { _raftDistributedLog.GetEntry(keyToLookFor); });
        }

        [Test]
        public void IT_GetEntryHistoryNotFound_Throws()
        {
            _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));

            string keyToLookFor = Guid.NewGuid().ToString();

            Assert.Throws<KeyNotFoundException>(() => { _raftDistributedLog.GetEntryHistory(keyToLookFor); });
        }

        [Test]
        public void IT_GetValueFromCommitIndex_ReturnsValue()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                _raftDistributedLog.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            for (int i = 0; i < value.Length; i++)
            {
                Assert.AreEqual(value[i], _raftDistributedLog.GetValue(i));
            }
        }
    }
}
