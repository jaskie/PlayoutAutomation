using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.FtpClient;
using System.Xml.Serialization;
using TAS.Common.Database;
using TAS.Common.Database.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;

namespace TAS.Server
{
    public class EngineController
    {

        private readonly double _referenceLoudnessLevel;

        private EngineController()
        {
            if (!double.TryParse(ConfigurationManager.AppSettings["ReferenceLoudnessLevel"], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _referenceLoudnessLevel))
                _referenceLoudnessLevel = -23;
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static EngineController Current { get; } = new EngineController();

        public List<CasparServer> Servers;

        public List<IngestDirectory> IngestDirectories { get; set; }

        public List<Engine> Engines { get; private set; }

        public ArchiveDirectory[] ArchiveDirectories { get; private set; }

        public IDatabase Database { get; private set; }

        public double ReferenceLoudnessLevel => _referenceLoudnessLevel;



        public void InitializeEngines()
        {
            FtpTrace.AddListener(new NLog.NLogTraceListener());
            Logger.Info("Engines initializing");
            ConnectionStringSettings connectionStringPrimary =
                ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary =
                ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
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
            LoadArchiveDirectories();
            foreach (var e in Engines)
                ((MediaManager) e.MediaManager).Initialize(
                    ArchiveDirectories.FirstOrDefault(a => a.IdArchive == e.IdArchive));
            InitializeMediaDirectories();
            Logger.Debug("Engines initialized");
        }

        private void InitializeMediaDirectories()
        {
            var initalizationList = new List<IMediaDirectory>();
            foreach (var mediaManager in Engines.Select(e => e.MediaManager))
            {
                if (mediaManager.MediaDirectoryPRI != null && !initalizationList.Contains(mediaManager.MediaDirectoryPRI))
                    initalizationList.Add(mediaManager.MediaDirectoryPRI);
                if (mediaManager.MediaDirectorySEC != null && !initalizationList.Contains(mediaManager.MediaDirectorySEC))
                    initalizationList.Add(mediaManager.MediaDirectorySEC);
                if (mediaManager.MediaDirectoryPRV != null && !initalizationList.Contains(mediaManager.MediaDirectoryPRV))
                    initalizationList.Add(mediaManager.MediaDirectoryPRV);
            }
            foreach (var mediaDirectory in initalizationList)
            {
                if (mediaDirectory is ServerDirectory serverDirectory)
                    serverDirectory.Initialize();
            }
        }

        private void LoadArchiveDirectories()
        {
            ArchiveDirectories = Engines.Where(e => e.IdArchive > 0)
                .Select(e => Database.LoadArchiveDirectory<ArchiveDirectory>(e.IdArchive)).ToArray();
            foreach (var archiveDirectory in ArchiveDirectories)
                archiveDirectory?.RefreshVolumeInfo();
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
