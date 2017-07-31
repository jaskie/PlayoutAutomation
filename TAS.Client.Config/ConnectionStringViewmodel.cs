using System.Collections.Generic;
using TAS.Client.Common;
using MySql.Data.MySqlClient;

namespace TAS.Client.Config
{
    public class ConnectionStringViewmodel : OkCancelViewmodelBase<MySqlConnectionStringBuilder>
    {
        private string _database;
        private uint _port;
        private string _server;
        private string _userId;
        private string _password;
        private string _characterSet;

        public ConnectionStringViewmodel(string connectionString) : base(new MySqlConnectionStringBuilder { ConnectionString = connectionString }, typeof(ConnectionStringView), "Edit connection parameters") { }

        protected override void OnDispose() { }

        public string ConnectionString => Model.ConnectionString;

        public string Server { get { return _server; } set { SetField(ref _server, value); } }

        public uint Port { get { return _port; } set { SetField(ref _port, value); } }

        public string Database { get { return _database; } set { SetField(ref _database, value); } }

        public string UserID { get { return _userId; } set { SetField (ref _userId, value);} }

        public string Password { get { return _password; } set { SetField(ref _password, value); } }

        public string CharacterSet { get { return _characterSet; } set { SetField(ref _characterSet, value); } }

        public IEnumerable<string> CharacterSets { get; } = new List<string> { "utf8" };
    }
}
