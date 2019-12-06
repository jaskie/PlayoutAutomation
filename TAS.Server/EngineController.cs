using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.FtpClient;
using System.Xml.Serialization;
using TAS.Common.Database;
using TAS.Common.Database.Interfaces;
using TAS.Server.Media;

namespace TAS.Server
{
    public class EngineController
    {

        private EngineController()
        { }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static EngineController Current { get; } = new EngineController();

        public List<CasparServer> Servers;

        public List<IngestDirectory> IngestDirectories { get; set; }

        public List<Engine> Engines { get; private set; }

        public IDatabase Database { get; private set; }

        public void InitializeEngines()
        {
            FtpTrace.AddListener(new NLog.NLogTraceListener());
            Logger.Info("Engines initializing");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Database = DatabaseProviderLoader.LoadDatabaseProvider();
            Logger.Debug("Connecting to database");
            Database.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            Servers = Database.LoadServers<CasparServer>();
            Servers.ForEach(s =>
            {
                s.ChannelsSer.ForEach(c => c.Owner = s);
                s.RecordersSer.ForEach(r => r.SetOwner(s));
            });

            Engines = Database.LoadEngines<Engine>(ulong.Parse(ConfigurationManager.AppSettings["Instance"]));
            foreach (var e in Engines)
                e.Initialize(Servers);
            Logger.Debug("Engines initialized");
        }

        public void LoadIngestDirectories()
        {
            Logger.Debug("Loading ingest directories");
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), ConfigurationManager.AppSettings["IngestFolders"]);
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                var reader = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute(nameof(IngestDirectories)));
                using (var file = new StreamReader(fileName))
                    IngestDirectories = ((List<IngestDirectory>)reader.Deserialize(file)).ToList();
                foreach (var d in IngestDirectories)
                    d.Initialize();
            }
            else IngestDirectories = new List<IngestDirectory>();
            Logger.Debug("IngestDirectories loaded");
        }

        public void ShutDown()
        {
            Engines?.ForEach(e => e.Dispose());
            Logger.Info("Engines shutdown completed");
            Database?.Close();
            Logger.Info("Database closed");
            Servers?.ForEach(s => s.Dispose());
        }

        public int GetConnectedClientCount() => Engines.Sum(e => e.Remote?.ClientCount ?? 0);
    }
}
