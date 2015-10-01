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

        internal static void Uninitialize()
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

        #region Configuration Functions
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
        #endregion //Configuration functions

        #region IPlayoutServer

        internal static List<T> DbLoadServers<T>() where T: IPlayoutServerConfig
        {
            List<T> servers = new List<T>();
            lock (_connection)
            {
                if (_connect())
                {

                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM server;", _connection);
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            StringReader reader = new StringReader(dataReader.GetString("Config"));
                            XmlSerializer serializer = new XmlSerializer(typeof(T));
                            T server = (T)serializer.Deserialize(reader);
                            server.Id = dataReader.GetUInt64("idServer");
                            servers.Add(server);
                        }
                        dataReader.Close();
                    }
                }
            }
            return servers;
        }

        internal static void DbInsertServer<T>(this T server) where T: IPlayoutServerConfig
        {
            lock (_connection)
            {
                if (_connect())
                {
                    {
                        MySqlCommand cmd = new MySqlCommand(@"INSERT INTO server set typServer=0, Config=@Config", _connection);
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        using (var writer = new StringWriter())
                        {
                            serializer.Serialize(writer, server);
                            cmd.Parameters.AddWithValue("@Config", writer.ToString());
                        }
                        cmd.ExecuteNonQuery();
                        server.Id = (ulong)cmd.LastInsertedId;
                    }
                }
            }

        }

        internal static void DbUpdateServer<T>(this T server) where T : IPlayoutServerConfig
        {
            lock (_connection)
            {
                if (_connect())
                {

                    MySqlCommand cmd = new MySqlCommand("UPDATE server SET Config=@Config WHERE idServer=@idServer;", _connection);
                    cmd.Parameters.AddWithValue("@idServer", server.Id);
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, server);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.ExecuteNonQuery();

                }
            }
        }

        internal static void DbDeleteServer<T>(this T server) where T : IPlayoutServerConfig
        {
            lock (_connection)
            {
                if (_connect())
                {

                    MySqlCommand cmd = new MySqlCommand("DELETE FROM server WHERE idServer=@idServer;", _connection);
                    cmd.Parameters.AddWithValue("@idServer", server.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion //IPlayoutServer

        #region IEngine

        internal static List<T> DbLoadEngines<T>(ulong? instance = null) where T : IEngineConfig
        {
            List<T> engines = new List<T>();
            lock (_connection)
            {
                if (_connect())
                {
                    MySqlCommand cmd;
                    if (instance == null)
                        cmd = new MySqlCommand("SELECT * FROM engine;", _connection);
                    else
                    {
                        cmd = new MySqlCommand("SELECT * FROM engine where Instance=@Instance;", _connection);
                        cmd.Parameters.AddWithValue("@Instance", instance);
                    }
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            StringReader reader = new StringReader(dataReader.GetString("Config"));
                            XmlSerializer serializer = new XmlSerializer(typeof(T));
                            T engine = (T)serializer.Deserialize(reader);
                            engine.Id = dataReader.GetUInt64("idEngine");
                            engine.IdServerPGM = dataReader.GetUInt64("idServerPGM");
                            engine.ServerChannelPGM = dataReader.GetInt32("ServerChannelPGM");
                            engine.IdServerPRV = dataReader.GetUInt64("idServerPRV");
                            engine.ServerChannelPRV = dataReader.GetInt32("ServerChannelPRV");
                            engine.IdArchive = dataReader.GetUInt64("IdArchive");
                            engine.Instance = dataReader.GetUInt64("Instance");
                            engines.Add(engine);
                        }
                        dataReader.Close();
                    }
                }
            }
            return engines;
        }

        internal static void DbInsertEngine<T>(this T engine) where T : IEngineConfig
        {
            lock (_connection)
            {
                if (_connect())
                {
                    {
                        MySqlCommand cmd = new MySqlCommand(@"INSERT INTO engine set Instance=@Instance, idServerPGM=@idServerPGM, ServerChannelPGM=@ServerChannelPGM, idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config;", _connection);
                        cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                        cmd.Parameters.AddWithValue("@idServerPGM", engine.IdServerPGM);
                        cmd.Parameters.AddWithValue("@ServerChannelPGM", engine.ServerChannelPGM);
                        cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                        cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                        cmd.Parameters.AddWithValue("@IdArchive", engine.IdArchive);
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        using (var writer = new StringWriter())
                        {
                            serializer.Serialize(writer, engine);
                            cmd.Parameters.AddWithValue("@Config", writer.ToString());
                        }
                        cmd.ExecuteNonQuery();
                        engine.Id = (ulong)cmd.LastInsertedId;
                    }
                }
            }
        }

        internal static void DbUpdateEngine<T>(this T engine) where T : IEngineConfig
        {
            lock (_connection)
            {
                if (_connect())
                {

                    MySqlCommand cmd = new MySqlCommand(@"UPDATE engine set Instance=@Instance, idServerPGM=@idServerPGM, ServerChannelPGM=@ServerChannelPGM, idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config where idEngine=@idEngine", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                    cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                    cmd.Parameters.AddWithValue("@idServerPGM", engine.IdServerPGM);
                    cmd.Parameters.AddWithValue("@ServerChannelPGM", engine.ServerChannelPGM);
                    cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                    cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                    cmd.Parameters.AddWithValue("@IdArchive", engine.IdArchive);
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, engine);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }

        internal static void DbDeleteEngine<T>(this T engine) where T : IEngineConfig
        {
            lock (_connection)
            {
                if (_connect())
                {

                    MySqlCommand cmd = new MySqlCommand("DELETE FROM engine WHERE idEngine=@idEngine;", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion //IEngine
    }
}
