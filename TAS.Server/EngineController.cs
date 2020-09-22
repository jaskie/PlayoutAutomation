using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.FtpClient;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Server.Media;

namespace TAS.Server
{
    public class EngineController
    {
        private EngineController()
        {            
            if (double.TryParse(ConfigurationManager.AppSettings["ReferenceLoudnessLevel"], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var referenceLoudnessLevel))
                ReferenceLoudnessLevel = referenceLoudnessLevel;
            else
                ReferenceLoudnessLevel = -23;
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static EngineController Current { get; } = new EngineController();

        public ReadOnlyCollection<CasparServer> Servers;

        public IngestDirectory[] IngestDirectories { get; set; }

        public ReadOnlyCollection<Engine> Engines { get; private set; }

        public ReadOnlyCollection<ArchiveDirectory> ArchiveDirectories { get; private set; }

        public double ReferenceLoudnessLevel { get; }

        public void InitializeEngines()
        {
            FtpTrace.AddListener(new NLog.NLogTraceListener());
            Logger.Info("Engines initializing");
            Servers = DatabaseProvider.Database.LoadServers<CasparServer>();
            foreach (var s in Servers)
            {
                s.ChannelsSer.ForEach(c => c.Owner = s);
                s.RecordersSer.ForEach(r => r.SetOwner(s));
            }
            Engines = DatabaseProvider.Database.LoadEngines<Engine>(ulong.Parse(ConfigurationManager.AppSettings["Instance"]));
            LoadArchiveDirectories();
            foreach (var e in Engines)
                e.Initialize(Servers);
            foreach (var e in Engines)
                ((MediaManager) e.MediaManager).Initialize(
                    ArchiveDirectories.FirstOrDefault(a => a.IdArchive == e.IdArchive));
            Logger.Debug("Engines initialized");
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
            Logger.Info("Engines shutdown completed");
            if (Servers != null)
                foreach (var s in Servers)
                    s.Dispose();
            DatabaseProvider.Database?.Close();
            Logger.Info("Database closed");
        }

        public int GetConnectedClientCount() => Engines.Sum(e => e.Remote?.ClientCount ?? 0);
    }
}
