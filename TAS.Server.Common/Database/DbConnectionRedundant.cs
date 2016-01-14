using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using MySql.Data.MySqlClient;
using System.IO;

namespace TAS.Server.Database
{

    [DesignerCategory("Code")]
    public class DbConnectionRedundant: DbConnection
    {
        MySqlConnection _connectionPrimary;
        Timer _idleTimeTimerPrimary;
        MySqlConnection _connectionSecondary;
        Timer _idleTimeTimerSecondary;

        #region static methods
        public static bool TestConnect(string connectionString)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch { }
            }
            return false;
        }

        public static bool CreateEmptyDatabase(string connectionString, string collate)
        {
            MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(connectionString);
            string databaseName = csb.Database;
            string charset = csb.CharacterSet;
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;
            csb.Remove("Database");
            csb.Remove("CharacterSet");
            using (MySqlConnection connection = new MySqlConnection(csb.ConnectionString))
            {
                connection.Open();
                using (var createCommand = new MySqlCommand(string.Format("CREATE DATABASE {0} CHARACTER SET = {1} COLLATE = {2};", databaseName, charset, collate), connection))
                {
                    if (createCommand.ExecuteNonQuery() == 1)
                    {
                        using (var useCommand = new MySqlCommand(string.Format("use {0};", databaseName), connection))
                        {
                            useCommand.ExecuteNonQuery();
                            using (StreamReader scriptReader = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Server.Common.Database.database.sql")))
                            {
                                string createStatements = scriptReader.ReadToEnd();
                                MySqlScript createScript = new MySqlScript(connection, createStatements);
                                if (createScript.Execute() > 0)
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        public static void CloneDatabase(string connectionStringSource, string connectionStringDestination)
        {
            string backupFile = Path.GetTempFileName();
            MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(connectionStringDestination);
            string databaseName = csb.Database;
            string charset = csb.CharacterSet;
            if (string.IsNullOrWhiteSpace(databaseName))
                return;
            csb.Remove("Database");
            csb.Remove("CharacterSet");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionStringSource))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();
                            mb.ExportToFile(backupFile);
                            conn.Close();
                        }
                    }
                }
                //file ready
                using (MySqlConnection conn = new MySqlConnection(csb.ConnectionString))
                {
                    conn.Open();
                    using (var createCommand = new MySqlCommand(string.Format("CREATE DATABASE {0} CHARACTER SET = {1};", databaseName, charset), conn))
                    {
                        if (createCommand.ExecuteNonQuery() == 1)
                        {
                            using (var useCommand = new MySqlCommand(string.Format("use {0};", databaseName), conn))
                            {
                                useCommand.ExecuteNonQuery();
                                using (MySqlCommand cmd = new MySqlCommand())
                                {
                                    using (MySqlBackup mb = new MySqlBackup(cmd))
                                    {
                                        cmd.Connection = conn;
                                        mb.ImportFromFile(backupFile);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                File.Delete(backupFile);
            }
        }

        #endregion // static methods

        public DbConnectionRedundant(string connectionStringPrimary, string connectionStringSecondary)
        {
            if (!string.IsNullOrWhiteSpace(connectionStringPrimary))
            {
                _connectionPrimary = new MySqlConnection(connectionStringPrimary);
                _idleTimeTimerPrimary = new Timer(_idleTimeTimerCallback, _connectionPrimary, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            }
            if (!string.IsNullOrWhiteSpace(connectionStringSecondary))
            {
                _connectionSecondary = new MySqlConnection(connectionStringSecondary);
                _idleTimeTimerSecondary = new Timer(_idleTimeTimerCallback, _connectionSecondary, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            }
        }

        public override void Open()
        {
            if (_connectionPrimary != null)
            {
                _idleTimeTimerPrimary = new Timer(_idleTimeTimerCallback, _connectionPrimary, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
                _connect(_connectionPrimary);
            }
            if (_connectionSecondary != null)
            {
                _idleTimeTimerSecondary = new Timer(_idleTimeTimerCallback, _connectionSecondary, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
                _connect(_connectionSecondary);
            }
        }

        public override void Close()
        {
            if (_connectionPrimary != null)
                lock (_connectionPrimary)
                {
                    _idleTimeTimerPrimary.Dispose();
                    _idleTimeTimerPrimary = null;
                    _connectionPrimary.Close();
                }
            if (_connectionSecondary != null)
                lock (_connectionSecondary)
                {
                    _idleTimeTimerSecondary.Dispose();
                    _idleTimeTimerSecondary = null;
                    _connectionSecondary.Close();
                }
        }

        public new void Dispose()
        {
            Close();
        }

        private static void _idleTimeTimerCallback(object o)
        {
            lock (o)
            {
                MySqlConnection connection = o as MySqlConnection;
                if (connection != null
                    && !connection.Ping())
                {
                    connection.Close();
                    _connect(connection);
                }
            }
        }

        private static bool _connect(MySqlConnection connection)
        {
            bool connectionResult = connection.State == ConnectionState.Open;
            if (!connectionResult)
            {
                connection.Open();
                connectionResult = connection.State == ConnectionState.Open;
            }
            Debug.WriteLineIf(!connectionResult, connection.State, "Not connected");
            return connectionResult;
        }

        public override string ConnectionString
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override string Database
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string DataSource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string ServerVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ConnectionState State
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            return new DbCommandRedundant(this);
        }

        internal MySqlConnection ConnectionPrimary { get { return _connectionPrimary; } }
        internal MySqlConnection ConnectionSecondary { get { return _connectionSecondary; } }

        public ConnectionState ConnectionStatePrimary { get { return _connectionPrimary.State; } }
        public ConnectionState ConnectionStateSecondary { get { return _connectionSecondary.State; } }

        private bool _isConnectionSync = true;
        public bool IsConnetionSync
        {
            get { return _isConnectionSync; }
            internal set
            {
                _isConnectionSync = value;
            }
        }

    }
}
