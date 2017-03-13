using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using TAS.Server.Interfaces;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

namespace TAS.Server
{
    public static class EngineController
    {
        public static List<Engine> Engines;

        static List<CasparServer> _servers;
        static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(EngineController));

        public static void Initialize()
        {
            Logger.Info("Engines initializing");
            Logger.Debug("Connecting to database");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Database.Database.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            _servers = Database.Database.DbLoadServers<CasparServer>();
            _servers.ForEach(s =>
            {
                s._channels.ForEach(c => c.ownerServer = s);
                s._recorders.ForEach(r => r.ownerServer = s);
            });
            Engines = Database.Database.DbLoadEngines<Engine>(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]));
            foreach (Engine e in Engines)
                e.Initialize(_servers);
            Logger.Debug("Engines initialized");
        }

        public static void ShutDown()
        {
            if (Engines != null)
                foreach (Engine e in Engines)
                    e.Dispose();
            Logger.Info("Engines shutdown");
        }
    }
}
