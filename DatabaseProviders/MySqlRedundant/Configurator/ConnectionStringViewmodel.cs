using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using TAS.Client.Common;

namespace TAS.Database.MySqlRedundant.Configurator
{
    public class ConnectionStringViewModel : OkCancelViewModelBase
    {
        private string _database;
        private uint _port;
        private string _server;
        private string _userId;
        private string _password;
        private string _characterSet;
        private MySqlSslMode _sslMode;
        private MySqlConnectionStringBuilder _mySqlConnectionStringBuilder;

        public ConnectionStringViewModel(string connectionString)
        {
            _mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder { ConnectionString = connectionString };
            Init();
        }

        protected override void OnDispose() { }

        public void Init()
        {
            UserID = _mySqlConnectionStringBuilder.UserID;
            Password = _mySqlConnectionStringBuilder.Password;
            Database = _mySqlConnectionStringBuilder.Database;
            SslMode = _mySqlConnectionStringBuilder.SslMode;
            Server = _mySqlConnectionStringBuilder.Server;
            Port = _mySqlConnectionStringBuilder.Port;
            CharacterSet = _mySqlConnectionStringBuilder.CharacterSet;
            IsModified = false;
        }

        public override bool Ok(object obj)
        {
            _mySqlConnectionStringBuilder.UserID = UserID;
            _mySqlConnectionStringBuilder.Password = Password;
            _mySqlConnectionStringBuilder.Database = Database;
            _mySqlConnectionStringBuilder.SslMode = SslMode;
            _mySqlConnectionStringBuilder.Server = Server;
            _mySqlConnectionStringBuilder.Port = Port;
            _mySqlConnectionStringBuilder.CharacterSet = CharacterSet;
            return true;
        }        

        public string ConnectionString => _mySqlConnectionStringBuilder.ConnectionString;

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
