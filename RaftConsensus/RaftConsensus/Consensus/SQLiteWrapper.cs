using System;
using System.Data.SQLite;
using TeamDecided.RaftConsensus.Common.Logging;

namespace TeamDecided.RaftConsensus.Consensus
{
    internal class SQLiteWrapper : IDisposable
    {
        private readonly SQLiteConnection _db;

        public SQLiteWrapper()
        {
            _db = new SQLiteConnection("Data Source=:memory:;");
            _db.Open();
        }

        public SQLiteWrapper(string filename)
        {
            _db = new SQLiteConnection($"Filename={filename}");
            _db.Open();
        }

        public void Dispose()
        {
            _db.Close();
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

        private object ExecuteScalar(string commandStr, params object[] param)
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
