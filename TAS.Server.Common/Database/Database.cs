using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Data;
using System.Diagnostics;
using TAS.Server.Interfaces;
using TAS.Common;
using System.Xml;
using System.Xml.Serialization;

namespace TAS.Server.Database
{
    public static class Database
    {
        static DbConnectionRedundant _connection;

        public static void Open(string connectionStringPrimary, string connectionStringSecondary)
        {
            _connection = new DbConnectionRedundant(connectionStringPrimary, connectionStringSecondary);
            _connection.Open();
        }

        #region Configuration Functions
        public static bool TestConnect(string connectionString)
        {
            return DbConnectionRedundant.TestConnect(connectionString);
        }

        public static void Close()
        {
            _connection.Close();
        }

        public static bool CreateEmptyDatabase(string connectionString, string collate)
        {
            return DbConnectionRedundant.CreateEmptyDatabase(connectionString, collate);
        }

        public static bool CloneDatabase(string connectionStringSource, string connectionStringDestination)
        {
            DbConnectionRedundant.CloneDatabase(connectionStringSource, connectionStringDestination);
            return DbConnectionRedundant.TestConnect(connectionStringDestination);
        }

        #endregion //Configuration functions

        #region IPlayoutServer

        public static List<T> DbLoadServers<T>() where T : IPlayoutServerConfig
        {
            List<T> servers = new List<T>();
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM server;", _connection);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
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
            return servers;
        }

        public static void DbInsertServer<T>(this T server) where T : IPlayoutServerConfig
        {
            lock (_connection)
            {
                {
                    DbCommandRedundant cmd = new DbCommandRedundant(@"INSERT INTO server set typServer=0, Config=@Config", _connection);
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

        public static void DbUpdateServer<T>(this T server) where T : IPlayoutServerConfig
        {
            lock (_connection)
            {

                DbCommandRedundant cmd = new DbCommandRedundant("UPDATE server SET Config=@Config WHERE idServer=@idServer;", _connection);
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

        public static void DbDeleteServer<T>(this T server) where T : IPlayoutServerConfig
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("DELETE FROM server WHERE idServer=@idServer;", _connection);
                cmd.Parameters.AddWithValue("@idServer", server.Id);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion //IPlayoutServer

        #region IEngine

        public static List<T> DbLoadEngines<T>(ulong? instance = null) where T : IEngineConfig
        {
            List<T> engines = new List<T>();
            lock (_connection)
            {
                DbCommandRedundant cmd;
                if (instance == null)
                    cmd = new DbCommandRedundant("SELECT * FROM engine;", _connection);
                else
                {
                    cmd = new DbCommandRedundant("SELECT * FROM engine where Instance=@Instance;", _connection);
                    cmd.Parameters.AddWithValue("@Instance", instance);
                }
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
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
            return engines;
        }

        public static void DbInsertEngine<T>(this T engine) where T : IEngineConfig
        {
            lock (_connection)
            {
                {
                    DbCommandRedundant cmd = new DbCommandRedundant(@"INSERT INTO engine set Instance=@Instance, idServerPGM=@idServerPGM, ServerChannelPGM=@ServerChannelPGM, idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config;", _connection);
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

        public static void DbUpdateEngine<T>(this T engine) where T : IEngineConfig
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant(@"UPDATE engine set Instance=@Instance, idServerPGM=@idServerPGM, ServerChannelPGM=@ServerChannelPGM, idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config where idEngine=@idEngine", _connection);
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

        public static void DbDeleteEngine<T>(this T engine) where T : IEngineConfig
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("DELETE FROM engine WHERE idEngine=@idEngine;", _connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion //IEngine

        #region ArchiveDirectory
        public static List<T> DbLoadArchiveDirectories<T>() where T : IArchiveDirectoryConfig, new()
        {
            List<T> directories = new List<T>();
            lock (_connection)
            {
                DbCommandRedundant cmd;
                cmd = new DbCommandRedundant("SELECT * FROM archive;", _connection);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dir = new T();
                        dir.idArchive = dataReader.GetUInt64("idArchive");
                        dir.Folder = dataReader.GetString("Folder");
                    }
                    dataReader.Close();
                }
            }
            return directories;
        }


        #endregion // ArchiveDirectory
    }
}
