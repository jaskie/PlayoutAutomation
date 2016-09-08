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
        public static readonly List<CasparServer> Servers;
        public static readonly List<Engine> Engines;
        static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(EngineController));

        static EngineController()
        {
            Logger.Info("Application starting");
            Logger.Debug("Connecting to database");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Database.Database.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            Servers = Database.Database.DbLoadServers<CasparServer>();
            Servers.ForEach(s => s.Channels.ForEach(c => c.OwnerServer = s));
            Engines = Database.Database.DbLoadEngines<Engine>(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]));
            foreach (Engine e in Engines)
                e.Initialize(Servers);
            Debug.WriteLine("EngineController Created");
            Logger.Debug("Finished creating engines");
        }

        public static void ShutDown()
        {
            if (Engines != null)
                foreach (Engine e in Engines)
                    e.Dispose();
            Logger.Info("Application shutdown");
        }
    }
}
