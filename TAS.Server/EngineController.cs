using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.FtpClient;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Server.Media;

namespace TAS.Server
{
    public class EngineController
    {
        public double ReferenceLoudnessLevel => _referenceLoudnessLevel;

        private EngineController()
        {
            if (!double.TryParse(ConfigurationManager.AppSettings["ReferenceLoudnessLevel"], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out _referenceLoudnessLevel))
                _referenceLoudnessLevel = -23;
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly double _referenceLoudnessLevel;

        public static EngineController Current { get; } = new EngineController();

        public IReadOnlyCollection<CasparServer> Servers;

        public IngestDirectory[] IngestDirectories { get; set; }

        public IReadOnlyCollection<Engine> Engines { get; private set; }

        public IReadOnlyCollection<ArchiveDirectory> ArchiveDirectories { get; private set; }

        public void InitializeEngines()
        {
            FtpTrace.AddListener(new NLog.NLogTraceListener());
            Logger.Info("Engines initializing");
            Servers = DatabaseProvider.Database.LoadServers<CasparServer>();
            foreach (var s in Servers)
            {
                Array.ForEach(s.ChannelsSer, c => c.Owner = s);
                Array.ForEach(s.RecordersSer, r => r.SetOwner(s));
            }
            Engines = DatabaseProvider.Database.LoadEngines<Engine>(ulong.Parse(ConfigurationManager.AppSettings["Instance"]));
            LoadArchiveDirectories();
            foreach (var e in Engines)
                e.Initialize(Servers);
            Logger.Debug("Engines initialized");
            Parallel.ForEach(Engines, async e => 
                await ((MediaManager)e.MediaManager).Initialize(ArchiveDirectories.FirstOrDefault(a => a.IdArchive == e.IdArchive)));
            Logger.Debug("All media managers initialized");
        }

        private void LoadArchiveDirectories()
        {
            ArchiveDirectories = DatabaseProvider.Database.LoadArchiveDirectories<ArchiveDirectory>();
            foreach (var archiveDirectory in ArchiveDirectories)
                archiveDirectory?.RefreshVolumeInfo();
        }

        public void LoadIngestDirectories()
        {
            Logger.Debug("Loading ingest directories");
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), ConfigurationManager.AppSettings["IngestFolders"]);
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                var reader = new XmlSerializer(typeof(IngestDirectory[]), new XmlRootAttribute(nameof(IngestDirectories)));
                using (var file = new StreamReader(fileName))
                    IngestDirectories = ((IngestDirectory[])reader.Deserialize(file));
                foreach (var d in IngestDirectories)
                    Task.Run(() => d.Initialize());
            }
            else IngestDirectories = new IngestDirectory[0];
            Logger.Debug("IngestDirectories loaded");
        }

        public void ShutDown()
        {
            if (Engines != null)
                foreach (var e in Engines)
                    e.Dispose();
            if (Servers != null)
                foreach (var s in Servers)
                    s.Dispose();
            if (DatabaseProvider.Database != null)
            {
                DatabaseProvider.Database.Close();
                Logger.Info("Database closed");
            }
            FileManager.Current.Shutdown();
        }

        public int GetConnectedClientCount() => Engines.Sum(e => e.Remote?.ClientCount ?? 0);
    }
}
