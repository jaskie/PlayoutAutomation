using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.FtpClient;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Database.Interfaces;
using TAS.Server.Security;

namespace TAS.Server
{
    public static class EngineController
    {

        private static List<CasparServer> _servers;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static List<Engine> Engines { get; private set; }

        public static IDatabase Database { get; private set; }

        public static void Initialize()
        {
            FtpTrace.AddListener(new NLog.NLogTraceListener());
            Logger.Info("Engines initializing");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Database = DatabaseProviderLoader.LoadDatabaseProvider();
            Logger.Debug("Connecting to database");
            Database.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            _servers = Database.LoadServers<CasparServer>();
            _servers.ForEach(s =>
            {
                s.ChannelsSer.ForEach(c => c.Owner = s);
                s.RecordersSer.ForEach(r => r.SetOwner(s));
            });

            var authenticationService = new AuthenticationService(Database.Load<User>(), Database.Load<Group>());
            Engines = Database.LoadEngines<Engine>(ulong.Parse(ConfigurationManager.AppSettings["Instance"]));
            foreach (var e in Engines)
                e.Initialize(_servers, authenticationService);
            Logger.Debug("Engines initialized");
        }

        public static void ShutDown()
        {
            if (Engines != null)
                foreach (var e in Engines)
                    e.Dispose();
            Logger.Info("Engines shutdown completed");
            Database?.Close();
            Logger.Info("Database closed");
        }

        public static int GetConnectedClientCount() => Engines.Sum(e => e.Remote?.ClientCount ?? 0);
    }
}
