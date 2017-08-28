using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using TAS.Database;
using TAS.Server.Security;

namespace TAS.Server
{
    public static class EngineController
    {

        private static List<CasparServer> _servers;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(EngineController));

        public static List<Engine> Engines { get; private set; }
        public static AuthenticationService AuthenticationService { get; private set; }

        public static void Initialize()
        {
            Logger.Info("Engines initializing");
            Logger.Debug("Connecting to database");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Db.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            _servers = Db.DbLoadServers<CasparServer>();
            _servers.ForEach(s =>
            {
                s.ChannelsSer.ForEach(c => c.Owner = s);
                s.RecordersSer.ForEach(r => r.SetOwner(s));
            });
            
            AuthenticationService = new AuthenticationService(Db.DbLoad<User>(), Db.DbLoad<Group>());
            Engines = Db.DbLoadEngines<Engine>(ulong.Parse(ConfigurationManager.AppSettings["Instance"]));
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
