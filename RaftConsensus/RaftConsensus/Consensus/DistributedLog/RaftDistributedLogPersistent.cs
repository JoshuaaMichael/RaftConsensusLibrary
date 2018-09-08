using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace TeamDecided.RaftConsensus.Consensus.DistributedLog
{
    public class RaftDistributedLogPersistent<TKey, TValue> : IDisposable, IRaftDistributedLog<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        private readonly SQLiteWrapper _db;

        public RaftDistributedLogPersistent()
        {
            _db = new SQLiteWrapper();
            SQLCreateTablesIfNotExist();
            SQLCreateSettingsIfNotExist();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public int CommitIndex
        {
            get => SQLGetCommitIndex();
            set => SQLUpdateCommitIndex(value);
        }

        public int LatestIndex => SQLGetCountOfEntries() - 1;

        public int LatestIndexTerm => SQLGetLatestIndexTerm();

        public void AppendEntry(RaftLogEntry<TKey, TValue> entry)
        {
            SQLAddEntry(LatestIndex + 1, entry.Key.GetHashCode(), JsonConvert.SerializeObject(entry), entry.Term);
        }

        public bool AppendEntry(RaftLogEntry<TKey, TValue> entry, int prevIndex, int prevTerm)
        {
            if (prevIndex < LatestIndex)
            {
                Truncate(prevIndex);
            }

            if (!ConfirmPreviousIndex(prevIndex, prevTerm)) return false;

            AppendEntry(entry);
            return true;
        }

        public void CommitUpToIndex(int index)
        {
            if (index < -1 || index > LatestIndex)
            {
                throw new ArgumentException("Invalid value for index");
            }

            SQLUpdateCommitIndex(index);
        }

        public bool ConfirmPreviousIndex(int prevIndex, int prevTerm)
        {
            int latest = LatestIndex;

            if (prevIndex == -1 && latest == -1) { return true; } //No preexisting entries yet

            if (prevIndex != latest) { return false; }

            return SQLLookupTermForIndex(prevIndex) == prevTerm;
        }

        public bool ContainsKey(TKey key)
        {
            return SQLGetEntry(key) != null;
        }

        public RaftLogEntry<TKey, TValue> GetEntry(int index)
        {
            return SQLGetEntry(index);
        }

        public RaftLogEntry<TKey, TValue> GetEntry(TKey key)
        {
            return SQLGetEntry(key) ?? throw new KeyNotFoundException("Key was not found in the database");
        }

        public RaftLogEntry<TKey, TValue>[] GetEntryHistory(TKey key)
        {
            return SQLGetEntryHistory(key);
        }

        public int GetTerm(int index)
        {
            return SQLLookupTermForIndex(index);
        }

        public TValue GetValue(int index)
        {
            return GetEntry(index).Value;
        }

        public TValue GetValue(TKey key)
        {
            return GetEntry(key).Value;
        }

        public TValue[] GetValueHistory(TKey key)
        {
            RaftLogEntry<TKey, TValue>[] entries = GetEntryHistory(key);

            TValue[] arr = new TValue[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                arr[i] = entries[i].Value;
            }

            return arr;
        }

        public void Truncate(int index)
        {
            SQLTruncate(index);
        }

        private void SQLCreateTablesIfNotExist()
        {
            _db.ExecuteNonQuery("CREATE TABLE Entry (`index` INTERGER NOT NULL, `key` INTERGER NOT NULL, value TEXT NOT NULL, term INTERGER NOT NULL, PRIMARY KEY (`index`))");
            _db.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS Entry_key ON Entry (key)");
            _db.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Settings (commitIndex INTERGER NOT NULL)");
        }

        private void SQLCreateSettingsIfNotExist()
        {
            if (_db.ExecuteScalarInt("SELECT COUNT(*) FROM Settings") == 0)
            {
                _db.ExecuteNonQuery("INSERT INTO Settings (commitIndex) VALUES (-1)");
            }
        }

        public void SQLUpdateCommitIndex(int index)
        {
            _db.ExecuteNonQuery($"UPDATE Settings Set commitIndex={index} WHERE rowid != -1");
        }

        private void SQLAddEntry(int index, int key, string value, int term)
        {
            _db.ExecuteNonQuery("INSERT INTO Entry (`index`, key, value, term) VALUES ($param0, $param1, $param2, $param3)", index, key, value, term);
        }

        private int SQLGetCommitIndex()
        {
            return _db.ExecuteScalarInt("SELECT commitIndex FROM Settings");
        }

        private int SQLGetCountOfEntries()
        {
            return _db.ExecuteScalarInt("SELECT COUNT(*) FROM Entry");
        }

        public void SQLTruncateAllData()
        {
            _db.ExecuteNonQuery("DELETE FROM Entry");
            SQLUpdateCommitIndex(-1);
        }

        private int SQLGetLatestIndexTerm()
        {
            const string command =
                "SELECT " +
                "CASE " +
                "WHEN ((SELECT COUNT(*) FROM Entry) = 0)" +
                "THEN -1 " +
                "ELSE (SELECT Term FROM Entry order by `index` DESC) " +
                "END Value";

            return _db.ExecuteScalarInt(command);
        }

        private int SQLLookupTermForIndex(int index)
        {
            return _db.ExecuteScalarInt("SELECT term FROM Entry WHERE `index` = " + index);
        }

        private RaftLogEntry<TKey, TValue> SQLGetEntry(int index)
        {
            const string command = "SELECT value FROM Entry WHERE `index`=$param0";

            SQLiteDataReader sqliteDataReader = _db.ExecuteReader(command, index);

            return !sqliteDataReader.Read() ? throw new Exception("Failed to find entry"): JsonConvert.DeserializeObject<RaftLogEntry<TKey, TValue>>((string) sqliteDataReader[0]);
        }

        private void SQLTruncate(int index)
        {
            _db.ExecuteNonQuery("DELETE FROM Entry WHERE `index` > " + index);
        }

        private RaftLogEntry<TKey, TValue>[] SQLGetEntryHistory(TKey key)
        {
            const string command = "SELECT value FROM Entry WHERE key=$param0 ORDER BY `index` ASC";

            SQLiteDataReader sqliteDataReader = _db.ExecuteReader(command, key.GetHashCode());

            List<RaftLogEntry<TKey, TValue>> entries = new List<RaftLogEntry<TKey, TValue>>();

            while (sqliteDataReader.Read())
            {
                string json = (string)sqliteDataReader[0];
                RaftLogEntry<TKey, TValue> entry = JsonConvert.DeserializeObject<RaftLogEntry<TKey, TValue>>(json);

                if (entry.Key.Equals(key))
                {
                    entries.Add(entry);
                }
            }

            return entries.ToArray();
        }

        public RaftLogEntry<TKey, TValue> SQLGetEntry(TKey key)
        {
            const string queryBase = "SELECT `index`, value FROM Entry WHERE key=$param0{0} ORDER BY `index` DESC LIMIT 1";
            int index = 0;

            for (int i = 0; ; i++)
            {
                string query = string.Format(queryBase, i == 0 ? "" : " AND `index` < " + index);

                SQLiteDataReader sqliteDataReader = _db.ExecuteReader(query, key.GetHashCode());

                if (!sqliteDataReader.Read())
                {
                    return null;
                }

                string json = (string)sqliteDataReader[1];
                RaftLogEntry<TKey, TValue> entry = JsonConvert.DeserializeObject<RaftLogEntry<TKey, TValue>>(json);

                if (entry.Key.Equals(key))
                {
                    return entry;
                }

                index = Convert.ToInt32(sqliteDataReader[0]);
            }
        }
    }
}
