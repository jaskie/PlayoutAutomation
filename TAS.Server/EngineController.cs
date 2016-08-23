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

namespace TAS.Server
{
    public static class EngineController
    {
        public static readonly List<CasparServer> Servers;
        public static readonly List<Engine> Engines;

        [Import]
        static ILocalDevices _localGPIDevices = null;
        [Import]
        static IEnumerable<IEnginePlugin> _enginePlugins = null;

        public static readonly CompositionContainer ServerContainer;

        static EngineController()
        {
            try
            {
                DirectoryCatalog catalog = new DirectoryCatalog(".", "TAS.Server.*.dll");
                ServerContainer = new CompositionContainer(catalog);
                ServerContainer.ComposeExportedValue("LocalDevicesConfigurationFile", ConfigurationManager.AppSettings["LocalDevices"]);
                _localGPIDevices = ServerContainer.GetExportedValueOrDefault<ILocalDevices>();
                _enginePlugins = ServerContainer.GetExportedValues<IEnginePlugin>();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            if (_localGPIDevices != null)
                _localGPIDevices.Initialize();

            Debug.WriteLine("Initializing database connector");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            Database.Database.Open(connectionStringPrimary?.ConnectionString, connectionStringSecondary?.ConnectionString);
            Servers = Database.Database.DbLoadServers<CasparServer>();
            Servers.ForEach(s => s.Channels.ForEach(c => c.OwnerServer = s));
            Engines = Database.Database.DbLoadEngines<Engine>(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]));
            foreach (Engine e in Engines)
            {
                IGpi engineGpi = _localGPIDevices == null ? null : _localGPIDevices.Select(e.Id);
                e.Initialize(Servers, engineGpi);
                foreach (var plugin in _enginePlugins) 
                    plugin.Initialize(e);
            }
            Debug.WriteLine("EngineController Created");
        }

        public static void ShutDown()
        {
            if (Engines != null)
                foreach (Engine e in Engines)
                    e.Dispose();
        }
    }
}
