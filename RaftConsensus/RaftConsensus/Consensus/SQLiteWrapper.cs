using System;
using System.Data.SQLite;
using System.IO;
using TeamDecided.RaftConsensus.Common.Logging;

namespace TeamDecided.RaftConsensus.Consensus
{
    internal class SQLiteWrapper : IDisposable
    {
        private SQLiteConnection _db;

        public SQLiteWrapper(string filename)
        {
            if (filename.Length == 0)
            {
                _db = new SQLiteConnection("Data Source=:memory:;");
            }
            else
            {
                if (!File.Exists(filename))
                {
                    SQLiteConnection.CreateFile(filename);
                }

                _db = new SQLiteConnection($"Data Source={filename}");
            }

            _db.Open();
        }

        public void Dispose()
        {
            _db.Close();
            _db.Dispose();
            _db = null;
            GC.Collect();
        }

        public int ExecuteNonQuery(string commandStr, params object[] param)
        {
            try
            {
                return PrepareCommand(_db, commandStr, param).ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                RaftLogging.Instance.Log(ERaftLogType.Fatal, "Exception caught: {0}", RaftLogging.FlattenException(e));
                throw;
            }
        }

        public object ExecuteScalar(string commandStr, params object[] param)
        {
            try
            {
                return PrepareCommand(_db, commandStr, param).ExecuteScalar();
            }
            catch (SQLiteException e)
            {
                RaftLogging.Instance.Log(ERaftLogType.Fatal, "Exception caught: {0}", RaftLogging.FlattenException(e));
                throw;
            }
        }

        public int ExecuteScalarInt(string commandStr, params object[] param)
        {
            return Convert.ToInt32(ExecuteScalar(commandStr, param));
        }

        public SQLiteDataReader ExecuteReader(string commandStr, params object[] param)
        {
            try
            {
                return PrepareCommand(_db, commandStr, param).ExecuteReader();
            }
            catch (SQLiteException e)
            {
                RaftLogging.Instance.Log(ERaftLogType.Fatal, "Exception caught: {0}", RaftLogging.FlattenException(e));
                throw;
            }
        }

        private static SQLiteCommand PrepareCommand(SQLiteConnection db, string commandStr, params object[] param)
        {
            SQLiteCommand command = new SQLiteCommand(commandStr)
            {
                Connection = db
            };

            if (param.Length > 0)
            {
                AddParameters(command, param);
            }

            return command;
        }

        private static void AddParameters(SQLiteCommand command, object[] param)
        {
            for (int i = 0; i < param.Length; i++)
            {
                SQLiteParameter sqliteParameter = new SQLiteParameter()
                {
                    ParameterName = "$param" + i,
                    Value = param[i]
                };
                command.Parameters.Add(sqliteParameter);
            }
        }
    }
}
