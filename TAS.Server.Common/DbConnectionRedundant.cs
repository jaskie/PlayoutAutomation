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

namespace TAS.Server.Common
{
    [DesignerCategory("Code")]
    public class DbConnectionRedundant: DbConnection
    {
        MySqlConnection _connectionPrimary;
        Timer _idleTimeTimerPrimary;
        MySqlConnection _connectionSecondary;
        Timer _idleTimeTimerSecondary;

        public DbConnectionRedundant(string connectionStringPrimary, string connectionStringSecondary)
        {
            if (!string.IsNullOrWhiteSpace(connectionStringPrimary))
                _connectionPrimary = new MySqlConnection(connectionStringPrimary);
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
            lock (_connectionPrimary)
            {
                _idleTimeTimerPrimary.Dispose();
                _idleTimeTimerPrimary = null;
                _connectionPrimary.Close();
            }
            lock (_connectionPrimary)
            {
                _idleTimeTimerPrimary.Dispose();
                _idleTimeTimerPrimary = null;
                _connectionPrimary.Close();
            }
        }

        public new void Dispose()
        {
            if (_connectionPrimary != null)
            {
                _connectionPrimary.Dispose();
                _connectionPrimary = null;
            }
            if (_connectionSecondary != null)
            {
                _connectionSecondary.Dispose();
                _connectionSecondary = null;
            }
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

    }
}
