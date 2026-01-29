using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;

namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update013 : UpdateBase
    {
        public override void Update()
        {
            BeginTransaction();
            try
            {
                UpdateEngines();
                UpdateServers();
                UpdateUsers();
                UpdateGroups();
                using (var cmd = new DbCommandRedundant("ALTER TABLE aco CHANGE COLUMN Config Config JSON", Connection))
                    cmd.ExecuteNonQuery();
                CommitTransaction();
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        private void UpdateEngines()
        {
            var engines = new List<_Update013.Engine>();
            using (var cmd = new DbCommandRedundant("SELECT idEngine, Config FROM engine", Connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var sr = new StringReader(reader.GetString("Config"));
                    var serializer = new XmlSerializer(typeof(_Update013.Engine));
                    var engine = (_Update013.Engine)serializer.Deserialize(sr);
                    engine.Id = reader.GetUInt64("idEngine");
                    engines.Add(engine);
                }
            }
            foreach (var engine in engines)
            {
                using (var cmd = new DbCommandRedundant("UPDATE engine SET Config=@Config where idEngine=@idEngine", Connection))
                {
                    cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                    cmd.Parameters.AddWithValue("@Config", JsonConvert.SerializeObject(engine));
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new ApplicationException($"Update failed for engine {engine.Id}");
                }
            }
            using (var cmd = new DbCommandRedundant("ALTER TABLE engine CHANGE COLUMN Config Config JSON", Connection))
                cmd.ExecuteNonQuery();
        }


        private void UpdateServers()
        {
            var servers = new List<_Update013.CasparServer>();
            using (var cmd = new DbCommandRedundant("SELECT idServer, Config FROM server", Connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var sr = new StringReader(reader.GetString("Config"));
                    var serializer = new XmlSerializer(typeof(_Update013.CasparServer));
                    var server = (_Update013.CasparServer)serializer.Deserialize(sr);
                    server.Id = reader.GetUInt64("idServer");
                    servers.Add(server);
                }
            }
            foreach (var server in servers)
            {
                using (var cmd = new DbCommandRedundant("UPDATE server SET Config=@Config WHERE idServer=@idServer", Connection))
                {
                    cmd.Parameters.AddWithValue("@idServer", server.Id);
                    cmd.Parameters.AddWithValue("@Config", JsonConvert.SerializeObject(server));
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new ApplicationException($"Update failed for server {server.Id}");
                }
            }
            using (var cmd = new DbCommandRedundant("ALTER TABLE server CHANGE COLUMN Config Config JSON", Connection))
                cmd.ExecuteNonQuery();
        }

        private void UpdateUsers()
        {
            var acos = new List<_Update013.User>();
            using (var cmd = new DbCommandRedundant("SELECT idACO, Config FROM aco WHERE typACO=@typACO", Connection))
            {
                cmd.Parameters.AddWithValue("@typACO", (int)SecurityObjectType.User);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var sr = new StringReader(reader.GetString("Config"));
                        var serializer = new XmlSerializer(typeof(_Update013.User));
                        var aco = (_Update013.User)serializer.Deserialize(sr);
                        aco.Id = reader.GetUInt64("idACO");
                        acos.Add(aco);
                    }
                }
            }
            foreach (var aco in acos)
            {
                using (var cmd = new DbCommandRedundant("UPDATE aco SET Config=@Config WHERE idACO=@idACO", Connection))
                {
                    cmd.Parameters.AddWithValue("@idACO", aco.Id);
                    cmd.Parameters.AddWithValue("@Config", JsonConvert.SerializeObject(aco));
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new ApplicationException($"Update failed for user {aco.Id}");
                }
            }
        }

        private void UpdateGroups()
        {
            var acos = new List<_Update013.Group>();
            using (var cmd = new DbCommandRedundant("SELECT idACO, Config FROM aco WHERE typACO=@typACO", Connection))
            {
                cmd.Parameters.AddWithValue("@typACO", (int)SecurityObjectType.Group);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var sr = new StringReader(reader.GetString("Config"));
                        var serializer = new XmlSerializer(typeof(_Update013.Group));
                        var aco = (_Update013.Group)serializer.Deserialize(sr);
                        aco.Id = reader.GetUInt64("idACO");
                        acos.Add(aco);
                    }
                }
            }
            foreach (var aco in acos)
            {
                using (var cmd = new DbCommandRedundant("UPDATE aco SET Config=@Config WHERE idACO=@idACO", Connection))
                {
                    cmd.Parameters.AddWithValue("@idACO", aco.Id);
                    cmd.Parameters.AddWithValue("@Config", JsonConvert.SerializeObject(aco));
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new ApplicationException($"Update failed for group {aco.Id}");
                }
            }
        }
    }


namespace _Update013
    {

        public class CasparServerChannel : TAS.Common.Interfaces.IPlayoutServerChannelProperties
        {
            public ulong Id { get; set; }
            public string ChannelName { get; set; }
            public double MasterVolume { get; set; }
            public string LiveDevice { get; set; }
            public string PreviewUrl { get; set; }
            public int AudioChannelCount { get; set; }
        }

        public class CasparRecorder : TAS.Common.Interfaces.IRecorderProperties
        {
            public int Id { get; set; }
            public string RecorderName { get; set; }
            public int DefaultChannel { get; set; }
        }

        public class CasparServer : TAS.Common.Interfaces.IPlayoutServerProperties
        {
            [XmlIgnore, JsonIgnore]
            public ulong Id { get; set; }
            public string ServerAddress { get; set; }
            public int OscPort { get; set; }
            public string MediaFolder { get; set; }
            public bool IsMediaFolderRecursive { get; set; }
            public string AnimationFolder { get; set; }
            public TServerType ServerType { get; set; }
            public TMovieContainerFormat MovieContainerFormat { get; set; }
            public CasparServerChannel[] Channels { get; set; }
            public CasparRecorder[] Recorders { get; set; }
            [XmlIgnore, JsonIgnore]
            public IDictionary<string, int> FieldLengths { get; set; }
            public void Delete()
            {
                throw new NotImplementedException();
            }
            public void Save()
            {
                throw new NotImplementedException();
            }
        }

        public class Engine : TAS.Common.Interfaces.IEngineProperties
        {
            [XmlIgnore, JsonIgnore]
            public ulong Id { get; set; }
            public string EngineName { get; set; }
            public TVideoFormat VideoFormat { get; set; }
            public bool EnableCGElementsForNewEvents { get; set; }
            public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }
            public bool StudioMode { get; set; }
            public ServerHost Remote { get; set; }
            public TAspectRatioControl AspectRatioControl { get; set; }
            public int TimeCorrection { get; set; }
            public int CGStartDelay { get; set; }
        }

        public class ServerHost
        {
            [XmlAttribute]
            public ushort ListenPort { get; set; }
        }

        public class User
        {
            [XmlIgnore, JsonIgnore]
            public ulong Id { get; set; }
            public string Name { get; set; }
            public bool IsAdmin { get; set; }
            public AuthenticationSource AuthenticationSource { get; set; }
            public string AuthenticationObject { get; set; }
            public ulong[] Groups { get; set; }
        }

        public class Group
        {
            [XmlIgnore, JsonIgnore]
            public ulong Id { get; set; }
            public string Name { get; set; }
        }

    }
}
