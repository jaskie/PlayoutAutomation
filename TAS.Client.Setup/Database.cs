using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Data;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using TAS.Server.Interfaces;
using TAS.Common;
using System.Xml;
using System.Xml.Serialization;

namespace TAS.Server.Common
{
    public static class Database
    {
        static MySqlConnection _connection;
        static Timer _idleTimeTimer;
        static bool _connect()
        {
            bool _connectionResult = _connection.State == ConnectionState.Open;
            if (!_connectionResult)
            {
                _connection.Open();
                _connectionResult = _connection.State == ConnectionState.Open;
            }
            Debug.WriteLineIf(!_connectionResult, _connection.State, "Not connected");
            return _connectionResult;
        }

        public static void Initialize(string connectionString)
        {
            _connection = new MySqlConnection(connectionString);
            _idleTimeTimer = new Timer(_idleTimeTimerCallback, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            Debug.WriteLine(_connection, "Created");
        }

        internal static void Uninitize()
        {
            lock (_connection)
            {
                _idleTimeTimer.Dispose();
                _idleTimeTimer = null;
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        private static void _idleTimeTimerCallback(object o)
        {
            lock (_connection)
                if (!_connection.Ping())
                {
                    _connection.Close();
                    _connect();
                }
        }

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
                                using (StreamReader scriptReader = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Client.Setup.database.sql")))
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

        internal static List<T> DbLoadServers<T>() where T: IPlayoutServer
        {
            List<T> servers = new List<T>();
            if (_connect())
            {
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM Server;", _connection);
                lock (_connection)
                {
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            XmlRootAttribute rootAttribute;
                            switch ((TServerType)dataReader.GetInt32("typServer"))
                            {
                                case TServerType.Caspar:
                                    rootAttribute = new XmlRootAttribute("CasparServer");
                                    break;
                                default:
                                    throw new NotImplementedException("Server type out of range");
                            }
                            StringReader reader = new StringReader(dataReader.GetString("Config"));
                            XmlSerializer serializer = new XmlSerializer(typeof(T), rootAttribute);
                            T server = (T)serializer.Deserialize(reader);
                            server.Id = dataReader.GetUInt64("idServer");
                            server.ServerType = (TServerType)dataReader.GetInt32("typServer");
                            
                            //XmlNode channelsNode = configXml.SelectSingleNode(@"CasparServer/Channels");
                            //Debug.WriteLine("Adding Caspar channels");
                            //newServer.Channels = SerializationHelper.Deserialize<List<CasparServerChannel>>(channelsNode.OuterXml, "Channels").ConvertAll<PlayoutServerChannel>(pc => (PlayoutServerChannel)pc);
                            servers.Add(server);
                        }
                        dataReader.Close();
                    }
                }
            }
            return servers;
        }


    }
}
