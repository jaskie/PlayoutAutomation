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
using TAS.Server.Common;

namespace TAS.Server.Database
{


    [DesignerCategory("Code")]
    public class DbConnectionRedundant : DbConnection
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
                using (var createCommand = new MySqlCommand(string.Format("CREATE DATABASE `{0}` CHARACTER SET = {1} COLLATE = {2};", databaseName, charset, collate), connection))
                {
                    if (createCommand.ExecuteNonQuery() == 1)
                    {
                        using (var useCommand = new MySqlCommand(string.Format("use {0};", databaseName), connection))
                        {
                            useCommand.ExecuteNonQuery();
                            using (StreamReader scriptReader = new StreamReader(DbSchema.GetSchemaDefinitionStream()))
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

        public static bool DropDatabase(string connectionString)
        {
            MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(connectionString);
            string databaseName = csb.Database;
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;
            csb.Remove("Database");
            using (MySqlConnection connection = new MySqlConnection(csb.ConnectionString))
            {
                connection.Open();
                using (var dropCommand = new MySqlCommand(string.Format("DROP DATABASE `{0}`;", databaseName), connection))
                {
                    if (dropCommand.ExecuteNonQuery() > 0)
                        return true;
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
                    using (var createCommand = new MySqlCommand(string.Format("CREATE DATABASE `{0}` CHARACTER SET = {1};", databaseName, charset), conn))
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
                _connectionPrimary.StateChange += _connection_StateChange;
            }
            if (!string.IsNullOrWhiteSpace(connectionStringSecondary))
            {
                _connectionSecondary = new MySqlConnection(connectionStringSecondary);
                _connectionSecondary.StateChange += _connection_StateChange;
            }
        }

        private object stateLock = new object();
        private void _connection_StateChange(object sender, StateChangeEventArgs e)
        {
            lock (stateLock)
            {
                ConnectionStateRedundant newState = StateRedundant;
                if (sender == _connectionPrimary)
                {
                    switch (e.CurrentState)
                    {
                        case ConnectionState.Open:
                            newState &= ~ConnectionStateRedundant.BrokenPrimary;
                            newState |= ConnectionStateRedundant.OpenPrimary;
                            break;
                        case ConnectionState.Broken:
                        case ConnectionState.Closed:
                            newState |= ConnectionStateRedundant.BrokenPrimary;
                            newState &= ~ConnectionStateRedundant.OpenPrimary;
                            break;
                    }
                }
                if (sender == _connectionSecondary)
                {
                    switch (e.CurrentState)
                    {
                        case ConnectionState.Open:
                            newState &= ~ConnectionStateRedundant.BrokenSecondary;
                            newState |= ConnectionStateRedundant.OpenSecondary;
                            break;
                        case ConnectionState.Broken:
                        case ConnectionState.Closed:
                            newState |= ConnectionStateRedundant.BrokenSecondary;
                            newState &= ~ConnectionStateRedundant.OpenSecondary;
                            break;
                    }
                }
                if ((newState & (ConnectionStateRedundant.BrokenPrimary | ConnectionStateRedundant.BrokenSecondary)) > 0)
                    newState = (newState & ~ConnectionStateRedundant.Open) | ConnectionStateRedundant.Broken;
                if ((_connectionPrimary != null || _connectionSecondary != null)
                    && (_connectionPrimary == null || _connectionPrimary.State == ConnectionState.Open)
                    && (_connectionSecondary == null || _connectionSecondary.State == ConnectionState.Open))
                    newState = (newState & ~ConnectionStateRedundant.Broken) | ConnectionStateRedundant.Open;
                StateRedundant = newState;
            }
        }

        private void _tryReconnect(MySqlConnection connection)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                _connect(connection);
            });
        }

        public override void Open()
        {
            if (_connectionPrimary != null)
            {
                TimeSpan timeout = TimeSpan.FromSeconds(_connectionPrimary.ConnectionTimeout);
                _idleTimeTimerPrimary = new Timer(_idleTimeTimerCallback, _connectionPrimary, timeout, timeout);
                if (!_connect(_connectionPrimary))
                    StateRedundant = _stateRedundant | ConnectionStateRedundant.BrokenPrimary;
            }
            if (_connectionSecondary != null)
            {
                TimeSpan timeout = TimeSpan.FromSeconds(_connectionSecondary.ConnectionTimeout);
                _idleTimeTimerSecondary = new Timer(_idleTimeTimerCallback, _connectionSecondary, timeout, timeout);
                if (!_connect(_connectionSecondary))
                    StateRedundant = _stateRedundant | ConnectionStateRedundant.BrokenSecondary;
            }
        }

        public override void Close()
        {
            if (_connectionPrimary != null)
                lock (_connectionPrimary)
                {
                    _idleTimeTimerPrimary.Dispose();
                    _idleTimeTimerPrimary = null;
                    _connectionPrimary.StateChange -= _connection_StateChange;
                    _connectionPrimary.Close();
                }
            if (_connectionSecondary != null)
                lock (_connectionSecondary)
                {
                    _idleTimeTimerSecondary.Dispose();
                    _idleTimeTimerSecondary = null;
                    _connectionSecondary.StateChange -= _connection_StateChange;
                    _connectionSecondary.Close();
                }
        }

        public new void Dispose()
        {
            Close();
        }

        public bool ExecuteScript(string script)
        {
            MySqlScript scriptPrimary = null;
            MySqlScript scriptSecondary = null;
            try
            {

                if (_connectionPrimary != null)
                    scriptPrimary = new MySqlScript(_connectionPrimary, script);
                if (_connectionSecondary != null)
                    scriptSecondary = new MySqlScript(_connectionSecondary, script);
                if ((scriptPrimary == null || scriptPrimary.Execute() > 0)
                    && (scriptSecondary == null || scriptSecondary.Execute() > 0)
                    && (scriptSecondary != null || scriptPrimary != null))
                    return true;
            }
            catch { }
            return false;
        }

        private void _idleTimeTimerCallback(object o)
        {
            lock (this)
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

        private bool _connect(MySqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                try
                {
                    connection.Open();
                }
                catch (MySqlException) { }
            }
            Debug.WriteLineIf(connection.State != ConnectionState.Open, connection.State, "Not connected");
            return connection.State == ConnectionState.Open;
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
            get { return _connectionPrimary.State; }
        }

        private ConnectionStateRedundant _stateRedundant;
        public ConnectionStateRedundant StateRedundant
        {
            get { return _stateRedundant; }
            internal set
            {
                if (value != _stateRedundant)
                {
                    var oldState = _stateRedundant;
                    _stateRedundant = value;
                    StateRedundantChange?.Invoke(this, new RedundantConnectionStateEventArgs(oldState, value));
                }
            }
        }

        internal DbTransactionRedundant ActiveTransaction;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new DbTransactionRedundant(this);
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

        public EventHandler<RedundantConnectionStateEventArgs> StateRedundantChange;
    }
    


}

