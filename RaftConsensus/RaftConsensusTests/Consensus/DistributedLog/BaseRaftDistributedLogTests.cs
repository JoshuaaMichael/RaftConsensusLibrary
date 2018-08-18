using System;
using System.Collections.Generic;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Consensus;
using TeamDecided.RaftConsensus.Consensus.DistributedLog;

namespace TeamDecided.RaftConsensus.Tests.Consensus.DistributedLog
{
    [TestFixture]
    internal abstract class BaseRaftDistributedLogTests
    {
        protected IRaftDistributedLog<string, string> Log;

        private const int ManyKeys = 3;
        private const int ManyValues = 3;

        [SetUp]
        public virtual void BeforeEachTest()
        {
        }

        [Test]
        public void IT_AppendToEmptyLog()
        {
            Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            Assert.AreEqual(0, Log.LatestIndex);
            Assert.AreEqual(1, Log.LatestIndexTerm);
        }

        [Test]
        public void IT_AppendManyValuesToEmptyLog()
        {
            string key = Guid.NewGuid().ToString();
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(key, Guid.NewGuid().ToString(), 1));
            }
            Assert.AreEqual(ManyValues - 1, Log.LatestIndex);
            Assert.AreEqual(1, Log.LatestIndexTerm);
        }

        [Test]
        public void IT_AppendManyNewKeyValuesToEmptyLog()
        {
            for (int i = 0; i < ManyKeys; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }
            Assert.AreEqual(ManyKeys - 1, Log.LatestIndex);
            Assert.AreEqual(1, Log.LatestIndexTerm);
        }

        [Test]
        public void IT_AppendRequireingTruncate()
        {
            Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 2));

            Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 0, 1);

            Assert.IsTrue(Log.LatestIndex == 1);
            Assert.IsTrue(Log.LatestIndexTerm == 3);
        }

        [Test]
        public void IT_AppendUsingCorrectPrevIndexPrevTerm()
        {
            for (int i = 0; i < ManyKeys; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i), i - 1, i - 1);
            }

            Assert.AreEqual(ManyKeys - 1, Log.LatestIndex);
        }

        [Test]
        public void IT_AppendUsingCorrectPrevIndexIncorrectPrevTerm()
        {
            Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 2));

            bool result0 = Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 0);
            bool result1 = Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 1);
            bool result3 = Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 3);
            bool result2 = Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 3), 1, 2);

            Assert.IsFalse(result0);
            Assert.IsFalse(result1);
            Assert.IsFalse(result3);
            Assert.IsTrue(result2);
            Assert.AreEqual(2, Log.LatestIndex);
        }

        [Test]
        public void IT_ConfirmContainsKey()
        {
            string key = Guid.NewGuid().ToString();

            Log.AppendEntry(new RaftLogEntry<string, string>(key, Guid.NewGuid().ToString(), 1));

            Assert.IsTrue(Log.ContainsKey(key));
            Assert.IsFalse(Log.ContainsKey(key + "Z"));
        }

        [Test]
        public void IT_GetEntryByKey()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            RaftLogEntry<string, string> sut = Log.GetEntry(key);

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
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            RaftLogEntry<string, string> sut = Log.GetEntry(key);

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

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            Assert.Throws<KeyNotFoundException>(() => { Log.GetEntry(value); });
        }

        [Test]
        public void IT_GetValueByKey()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            string sut = Log.GetValue(key);

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
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            string sut = Log.GetValue(key);

            Assert.AreEqual(value, sut);
        }

        [Test]
        public void IT_GetValueByWrongKeyThrowsException()
        {
            string key = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            int term = 1;

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            Assert.Throws<KeyNotFoundException>(() => { Log.GetValue(value); });
        }

        [Test]
        public void IT_GetEntryHistoryByKeyAfterManyEntries()
        {
            string key = Guid.NewGuid().ToString();
            string[] value = new string[5];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = Guid.NewGuid().ToString();
                Log.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            RaftLogEntry<string, string>[] valueHistory = Log.GetEntryHistory(key);

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
                Log.AppendEntry(new RaftLogEntry<string, string>(key, value[i], 1));
            }

            string[] valueHistory = Log.GetValueHistory(key);

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
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            RaftLogEntry<string, string> sut = Log.GetEntry(ManyValues);

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
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, term));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
            }

            string sut = Log.GetValue(ManyValues);

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
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i));
            }

            Log.AppendEntry(new RaftLogEntry<string, string>(key, value, ManyValues + 1));

            //Append some after
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i + ManyValues + 1));
            }

            int sut = Log.GetTerm(ManyValues);

            Assert.AreEqual(ManyValues + 1, sut);
        }

        [Test]
        public void IT_CommitUpToIndex()
        {
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i));
            }

            Log.CommitUpToIndex(ManyValues - 1);

            Assert.AreEqual(ManyValues - 1, Log.CommitIndex);
        }

        [Test]
        public void IT_CommitUpToIndexTooLargeCommitIndexThrows()
        {
            for (int i = 0; i < ManyValues; i++)
            {
                Log.AppendEntry(new RaftLogEntry<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), i));
            }

            Assert.Throws<ArgumentException>(() => { Log.CommitUpToIndex(ManyValues); });
        }
    }
}
