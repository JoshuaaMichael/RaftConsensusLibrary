using System;
using System.Collections.Generic;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Consensus;
using TeamDecided.RaftConsensus.Consensus.DistributedLog;

namespace TeamDecided.RaftConsensus.Tests.Consensus.DistributedLog
{
    [TestFixture]
    public class RaftDistributedLogTests
    {
        RaftDistributedLog<string, string> _log;

        private const int ManyKeys = 3;
        private const int ManyValues = 3;

        [SetUp]
        public void BeforeEachTest()
        {
            _log = new RaftDistributedLog<string, string>();
        }

        [Test]
        public void IT_AppendToEmptyLog()
        {
            _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            Assert.AreEqual(0, _log.LatestIndex);
            Assert.AreEqual(1, _log.LatestIndexTerm);
        }

        [Test]
        public void IT_AppendManyValuesToEmptyLog()
        {
            string key = Guid.NewGuid().ToString();
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(key, Guid.NewGuid().ToString(), 1));
            }
            Assert.AreEqual(ManyValues - 1, _log.LatestIndex);
            Assert.AreEqual(1, _log.LatestIndexTerm);
        }

        [Test]
        public void IT_AppendManyNewKeyValuesToEmptyLog()
        {
            for (int i = 0; i < ManyKeys; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }
            Assert.AreEqual(ManyKeys - 1, _log.LatestIndex);
            Assert.AreEqual(1, _log.LatestIndexTerm);
        }

        [Test]
        public void IT_AppendRequireingTruncate()
        {
            _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 2));

            _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 0, 1);

            Assert.IsTrue(_log.LatestIndex == 1);
            Assert.IsTrue(_log.LatestIndexTerm == 3);
        }

        [Test]
        public void IT_AppendUsingCorrectPrevIndexPrevTerm()
        {
            for (int i = 0; i < ManyKeys; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i), i - 1, i - 1);
            }

            Assert.AreEqual(ManyKeys - 1, _log.LatestIndex);
        }

        [Test]
        public void IT_AppendUsingCorrectPrevIndexIncorrectPrevTerm()
        {
            _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 2));

            bool result0 = _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 0);
            bool result1 = _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 1);
            bool result3 = _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 3);
            bool result2 = _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 2);

            Assert.IsFalse(result0);
            Assert.IsFalse(result1);
            Assert.IsFalse(result3);
            Assert.IsTrue(result2);
            Assert.AreEqual(2, _log.LatestIndex);
        }

        [Test]
        public void IT_ConfirmContainsKey()
        {
            string key = Guid.NewGuid().ToString();

            _log.AppendEntry(new RaftLogEntry<string, string>(key, Guid.NewGuid().ToString(), 1));

            Assert.IsTrue(_log.ContainsKey(key));
            Assert.IsFalse(_log.ContainsKey(key + "Z"));
        }

        [Test]
        public void IT_GetEntryByKey()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            RaftLogEntry<string, string> sut = _log.GetEntry(key);

            Assert.AreEqual(key, sut.Key);
            Assert.AreEqual(value, sut.Value);
            Assert.AreEqual(term, sut.Term);
        }

        [Test]
        public void IT_GetEntryByKeyAfterManyEntries()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            //Append some before
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            RaftLogEntry<string, string> sut = _log.GetEntry(key);

            Assert.AreEqual(key, sut.Key);
            Assert.AreEqual(value, sut.Value);
            Assert.AreEqual(term, sut.Term);
        }

        [Test]
        public void IT_GetEntryByWrongKeyThrowsException()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            Assert.Throws<KeyNotFoundException>(() => { _log.GetEntry(value); });
        }

        [Test]
        public void IT_GetValueByKey()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            string sut = _log.GetValue(key);

            Assert.AreEqual(value, sut);
        }

        [Test]
        public void IT_GetValueByKeyAfterManyEntries()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            //Append some before
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            string sut = _log.GetValue(key);

            Assert.AreEqual(value, sut);
        }

        [Test]
        public void IT_GetValueByWrongKeyThrowsException()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            Assert.Throws<KeyNotFoundException>(() => { _log.GetValue(value); });
        }

        [Test]
        public void IT_GetEntryHistoryByKeyAfterManyEntries()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                _log.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            RaftLogEntry<string, string>[] valueHistory = _log.GetEntryHistory(key);

            Assert.AreEqual(value.Length, valueHistory.Length);

            for (int i = 0; i < valueHistory.Length; i++)
            {
                Assert.AreEqual(value[0], valueHistory[0].Value);
            }
        }

        [Test]
        public void IT_GetValueHistoryByKeyAfterManyEntries()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                _log.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            string[] valueHistory = _log.GetValueHistory(key);

            Assert.AreEqual(value.Length, valueHistory.Length);

            for (int i = 0; i < valueHistory.Length; i++)
            {
                Assert.AreEqual(value[0], valueHistory[0]);
            }
        }

        [Test]
        public void IT_GetEntryFromIndex()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            //Append some before
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            RaftLogEntry<string, string> sut = _log.GetEntry(ManyValues);

            Assert.AreEqual(key, sut.Key);
            Assert.AreEqual(value, sut.Value);
            Assert.AreEqual(term, sut.Term);
        }

        [Test]
        public void IT_GetValueFromIndex()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            //Append some before
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            string sut = _log.GetValue(ManyValues);

            Assert.AreEqual(value, sut);
        }

        [Test]
        public void IT_GetTermFromIndex()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();

            //Append some before
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i));
            }

            _log.AppendEntry(new RaftLogEntry<string, string>(key, value, ManyValues + 1));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i + ManyValues + 1));
            }

            int sut = _log.GetTerm(ManyValues);

            Assert.AreEqual(ManyValues + 1, sut);
        }

        [Test]
        public void IT_CommitUpToIndex()
        {
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i));
            }

            _log.CommitUpToIndex(ManyValues - 1);

            Assert.AreEqual(ManyValues - 1, _log.CommitIndex);
        }

        [Test]
        public void IT_CommitUpToIndexTooLargeCommitIndexThrows()
        {
            for (int i = 0; i < ManyValues; i++)
            {
                _log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i));
            }

            Assert.Throws<ArgumentException>(() => { _log.CommitUpToIndex(ManyValues); });
        }
    }
}
