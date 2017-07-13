using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using TAS.Server.Common.Database;
using TAS.Server.Security;

namespace TAS.Server
{
    public static class EngineController
    {

        private static List<CasparServer> _servers;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(EngineController));

        public static List<Engine> Engines;
        public static AuthenticationService AuthenticationService;

        public static void Initialize()
        {
            Logger.Info("Engines initializing");
            Logger.Debug("Connecting to database");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Database.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            _servers = Database.DbLoadServers<CasparServer>();
            _servers.ForEach(s =>
            {
                s.ChannelsSer.ForEach(c => c.Owner = s);
                s.RecordersSer.ForEach(r => r.SetOwner(s));
            });
            
            AuthenticationService = new AuthenticationService(Database.DbLoadUsers<User>(), null);
            Engines = Database.DbLoadEngines<Engine>(ulong.Parse(ConfigurationManager.AppSettings["Instance"]));
            foreach (var e in Engines)
                e.Initialize(_servers);
            Logger.Debug("Engines initialized");
        }

        public static void ShutDown()
        {
            if (Engines != null)
                foreach (var e in Engines)
                    e.Dispose();
            Logger.Info("Engines shutdown");
        }
    }
}
