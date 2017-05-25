using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

namespace TAS.Server
{
    public static class EngineController
    {

        private static List<CasparServer> _servers;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(EngineController));

        public static List<Engine> Engines;

        public static void Initialize()
        {
            Logger.Info("Engines initializing");
            Logger.Debug("Connecting to database");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Common.Database.Database.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            _servers = Common.Database.Database.DbLoadServers<CasparServer>();
            _servers.ForEach(s =>
            {
                s.ChannelsSer.ForEach(c => c.Owner = s);
                s.RecordersSer.ForEach(r => r.SetOwner(s));
            });
            
            Engines = Common.Database.Database.DbLoadEngines<Engine>(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]));
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
