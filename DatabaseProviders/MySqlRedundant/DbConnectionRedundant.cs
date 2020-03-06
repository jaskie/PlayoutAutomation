using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MySql.Data.MySqlClient;
using TAS.Common;

namespace TAS.Database.MySqlRedundant
{
    
    [DesignerCategory("Code")]
    public class DbConnectionRedundant : DbConnection
    {
        private Timer _idleTimeTimerPrimary;
        private Timer _idleTimeTimerSecondary;
        
        #region static methods
        public static void TestConnect(string connectionString)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
            }
        }

        public static bool CreateEmptyDatabase(string connectionString, string collate)
        {
            var csb = new MySqlConnectionStringBuilder(connectionString);
            var databaseName = csb.Database;
            var charset = csb.CharacterSet;
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;
            csb.Remove("Database");
            csb.Remove("CharacterSet");
            using (var connection = new MySqlConnection(csb.ConnectionString))
            {
                connection.Open();
                using (var createCommand = new MySqlCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET = {charset} COLLATE = {collate};", connection))
                {
                    if (createCommand.ExecuteNonQuery() == 1)
                    {
                        using (var useCommand = new MySqlCommand($"use {databaseName};", connection))
                        {
                            useCommand.ExecuteNonQuery();
                            using (var scriptReader = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Database.MySqlRedundant.database.sql")))
                            {
                                var createStatements = scriptReader.ReadToEnd();
                                var createScript = new MySqlScript(connection, createStatements);
                                if (createScript.Execute() > 0)
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        public static void DropDatabase(string connectionString)
        {
            var csb = new MySqlConnectionStringBuilder(connectionString);
            var databaseName = csb.Database;
            csb.Remove("Database");
            using (var connection = new MySqlConnection(csb.ConnectionString))
            {
                connection.Open();
                using (var dropCommand = new MySqlCommand($"DROP DATABASE `{databaseName}`;", connection))
                    dropCommand.ExecuteNonQuery();
            }
        }

        public static void CloneDatabase(string connectionStringSource, string connectionStringDestination)
        {
            var backupFile = Path.GetTempFileName();
            var csb = new MySqlConnectionStringBuilder(connectionStringDestination);
            var databaseName = csb.Database;
            var charset = csb.CharacterSet;
            if (string.IsNullOrWhiteSpace(databaseName))
                return;
            csb.Remove("Database");
            csb.Remove("CharacterSet");
            try
            {
                using (var conn = new MySqlConnection(connectionStringSource))
                {
                    using (var cmd = new MySqlCommand())
                    {
                        using (var mb = new MySqlBackup(cmd) )
                        {
                            mb.ExportInfo.MaxSqlLength = 1024 * 1024; // 1M
                            cmd.Connection = conn;
                            conn.Open();
                            mb.ExportToFile(backupFile);
                            conn.Close();
                        }
                    }
                }
                //file ready
                using (var conn = new MySqlConnection(csb.ConnectionString))
                {
                    conn.Open();
                    using (var createCommand = new MySqlCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET = {charset};", conn))
                    {
                        if (createCommand.ExecuteNonQuery() != 1)
                            return;
                        using (var useCommand = new MySqlCommand($"use {databaseName};", conn))
                        {
                            useCommand.ExecuteNonQuery();
                            using (var cmd = new MySqlCommand())
                            {
                                using (var mb = new MySqlBackup(cmd))
                                {
                                    cmd.Connection = conn;
                                    mb.ImportFromFile(backupFile);
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
                ConnectionPrimary = new MySqlConnection(connectionStringPrimary);
                ConnectionPrimary.StateChange += _connection_StateChange;
            }
            if (!string.IsNullOrWhiteSpace(connectionStringSecondary))
            {
                ConnectionSecondary = new MySqlConnection(connectionStringSecondary);
                ConnectionSecondary.StateChange += _connection_StateChange;
            }
        }

        private readonly object _stateLock = new object();

        private void _connection_StateChange(object sender, StateChangeEventArgs e)
        {
            lock (_stateLock)
            {
                var newState = StateRedundant;
                if (sender == ConnectionPrimary)
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
                if (sender == ConnectionSecondary)
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
                if ((ConnectionPrimary != null || ConnectionSecondary != null)
                    && (ConnectionPrimary == null || ConnectionPrimary.State == ConnectionState.Open)
                    && (ConnectionSecondary == null || ConnectionSecondary.State == ConnectionState.Open))
                    newState = (newState & ~ConnectionStateRedundant.Broken) | ConnectionStateRedundant.Open;
                StateRedundant = newState;
            }
        }

        public override void Open()
        {
            if (ConnectionPrimary != null)
            {
                TimeSpan timeout = TimeSpan.FromSeconds(60);
                _idleTimeTimerPrimary = new Timer(_idleTimeTimerCallback, ConnectionPrimary, timeout, timeout);
                try
                {
                    ConnectionPrimary.Open();
                }
                catch
                {
                    StateRedundant = _stateRedundant | ConnectionStateRedundant.BrokenPrimary;
                    _tryOpenSecondary();
                    if (ConnectionSecondary == null || ConnectionSecondary.State != ConnectionState.Open)
                        throw;
                }
            }
            _tryOpenSecondary();
        }

        public override void Close()
        {
            if (ConnectionPrimary != null)
                lock (ConnectionPrimary)
                {
                    _idleTimeTimerPrimary.Dispose();
                    _idleTimeTimerPrimary = null;
                    ConnectionPrimary.Close();
                    ConnectionPrimary.StateChange -= _connection_StateChange;
                }
            if (ConnectionSecondary != null)
                lock (ConnectionSecondary)
                {
                    _idleTimeTimerSecondary.Dispose();
                    _idleTimeTimerSecondary = null;
                    ConnectionSecondary.Close();
                    ConnectionSecondary.StateChange -= _connection_StateChange;
                }
        }


        public bool ExecuteScript(string script)
        {
            MySqlScript scriptPrimary = null;
            MySqlScript scriptSecondary = null;
            try
            {
                if (ConnectionPrimary != null)
                    scriptPrimary = new MySqlScript(ConnectionPrimary, script);
                if (ConnectionSecondary != null)
                    scriptSecondary = new MySqlScript(ConnectionSecondary, script);
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
            if (!(o is MySqlConnection connection))
                return;
            try
            {
                lock (this)
                {
                    var isConnected = connection.Ping();
                    if (isConnected)
                        return;
                    connection.Close();
                    connection.Open();
                }
            }
            catch { }
        }

        private void _tryOpenSecondary()
        {
            if (ConnectionSecondary == null)
                return;
            TimeSpan timeout = TimeSpan.FromSeconds(ConnectionSecondary.ConnectionTimeout);
            _idleTimeTimerSecondary = new Timer(_idleTimeTimerCallback, ConnectionSecondary, timeout, timeout);
            try
            {
                ConnectionSecondary.Open();
            }
            catch { }
        }

        public override DataTable GetSchema(string collection)
        {
            return ConnectionPrimary?.State == ConnectionState.Open
                ? ConnectionPrimary.GetSchema(collection)
                : ConnectionSecondary?.State == ConnectionState.Open
                    ? ConnectionSecondary.GetSchema(collection)
                    : null;
        }

        public override string ConnectionString
        {
            get => throw new NotImplementedException();

            set => throw new NotImplementedException();
        }

        public override string Database => throw new NotImplementedException();

        public override string DataSource => throw new NotImplementedException();

        public override string ServerVersion => throw new NotImplementedException();

        public override ConnectionState State => ConnectionPrimary.State;

        private ConnectionStateRedundant _stateRedundant;
        public ConnectionStateRedundant StateRedundant
        {
            get => _stateRedundant;
            internal set
            {
                if (value == _stateRedundant)
                    return;
                var oldState = _stateRedundant;
                _stateRedundant = value;
                StateRedundantChange?.Invoke(this, new RedundantConnectionStateEventArgs(oldState, value));
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

        internal MySqlConnection ConnectionPrimary { get; }
        internal MySqlConnection ConnectionSecondary { get; }

        public ConnectionState ConnectionStatePrimary => ConnectionPrimary.State;
        public ConnectionState ConnectionStateSecondary => ConnectionSecondary.State;

        public event EventHandler<RedundantConnectionStateEventArgs> StateRedundantChange;
    }
    


}

