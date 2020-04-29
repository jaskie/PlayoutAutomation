using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using TAS.Client.Common;

namespace TAS.Database.MySqlRedundant.Configurator
{
    public class ConnectionStringViewmodel : EditViewmodelBase<MySqlConnectionStringBuilder>, IOkCancelViewModel
    {
        private string _database;
        private uint _port;
        private string _server;
        private string _userId;
        private string _password;
        private string _characterSet;
        private MySqlSslMode _sslMode;

        public ConnectionStringViewmodel(string connectionString) : base(new MySqlConnectionStringBuilder { ConnectionString = connectionString }) { }

        protected override void OnDispose() { }

        public bool Ok(object obj)
        {
            Update();
            return true;
        }

        public void Cancel(object obj)
        {
            
        }

        public bool CanOk(object obj)
        {
            return IsModified;
        }

        public bool CanCancel(object obj)
        {
            return true;
        }

        public string ConnectionString => Model.ConnectionString;

        public string Server
        {
            get => _server;
            set => SetField(ref _server, value);
        }

        public uint Port
        {
            get => _port;
            set => SetField(ref _port, value);
        }

        public string Database
        {
            get => _database;
            set => SetField(ref _database, value);
        }

        public string UserID
        {
            get => _userId;
            set => SetField(ref _userId, value);
        }

        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }

        public string CharacterSet
        {
            get => _characterSet;
            set => SetField(ref _characterSet, value);
        }

        public IEnumerable<string> CharacterSets { get; } = new List<string> { "utf8" };

        public MySqlSslMode SslMode { get => _sslMode; set => SetField(ref _sslMode, value); }

        public Array SslModes { get; } = Enum.GetValues(typeof(MySqlSslMode));

    }
}
