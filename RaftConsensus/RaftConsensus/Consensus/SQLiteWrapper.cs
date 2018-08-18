using System;
using Microsoft.Data.Sqlite;
using TeamDecided.RaftConsensus.Common.Logging;

namespace TeamDecided.RaftConsensus.Consensus
{
    internal class SQLiteWrapper : IDisposable
    {
        private readonly SqliteConnection _db;

        public SQLiteWrapper()
        {
            _db = new SqliteConnection("Data Source=:memory:;");
            _db.Open();
        }

        public SQLiteWrapper(string filename)
        {
            _db = new SqliteConnection($"Filename={filename}");
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
            catch (SqliteException e)
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
            catch (SqliteException e)
            {
                RaftLogging.Instance.Log(ERaftLogType.Fatal, "Exception caught: {0}", RaftLogging.FlattenException(e));
                throw;
            }
        }

        public int ExecuteScalarInt(string commandStr, params object[] param)
        {
            return Convert.ToInt32(ExecuteScalar(commandStr, param));
        }

        public SqliteDataReader ExecuteReader(string commandStr, params object[] param)
        {
            try
            {
                return PrepareCommand(_db, commandStr, param).ExecuteReader();
            }
            catch (SqliteException e)
            {
                RaftLogging.Instance.Log(ERaftLogType.Fatal, "Exception caught: {0}", RaftLogging.FlattenException(e));
                throw;
            }
        }

        private static SqliteCommand PrepareCommand(SqliteConnection db, string commandStr, params object[] param)
        {
            SqliteCommand command = new SqliteCommand(commandStr)
            {
                Connection = db
            };

            if (param.Length > 0)
            {
                AddParameters(command, param);
            }

            return command;
        }

        private static void AddParameters(SqliteCommand command, object[] param)
        {
            for (int i = 0; i < param.Length; i++)
            {
                SqliteParameter sqliteParameter = new SqliteParameter()
                {
                    ParameterName = "$param" + i,
                    Value = param[i]
                };
                command.Parameters.Add(sqliteParameter);
            }
        }
    }
}
