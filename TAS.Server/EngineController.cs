using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using TAS.Data;
using TAS.Server.Interfaces;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace TAS.Server
{
    public class EngineController: IDisposable
    {
        public readonly List<IPlayoutServer> Servers;
        public readonly List<Engine> Engines;
        [Import]
        ILocalDevices _localGPIDevices = null;
        public readonly CompositionContainer ServerContainer;

        public EngineController()
        {
            try
            {
                DirectoryCatalog catalog = new DirectoryCatalog(".", "TAS.Server.*.dll");
                ServerContainer = new CompositionContainer(catalog);
                ServerContainer.ComposeExportedValue("LocalDevicesConfigurationFile", ConfigurationManager.AppSettings["LocalDevices"]);
                ServerContainer.SatisfyImportsOnce(this);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            if (_localGPIDevices != null)
                _localGPIDevices.Initialize();

            Debug.WriteLine(this, "Initializing database connector");
            ConnectionStringSettings connectionStringPrimary = ConfigurationManager.ConnectionStrings["tasConnectionString"];
            ConnectionStringSettings connectionStringSecondary = ConfigurationManager.ConnectionStrings["tasConnectionStringSecondary"];
            DatabaseConnector.Initialize(connectionStringPrimary == null ? string.Empty : connectionStringPrimary.ConnectionString,
                                         connectionStringSecondary == null ? string.Empty : connectionStringSecondary.ConnectionString);
            Servers = DatabaseConnector.DbLoadServers();
            Engines = DatabaseConnector.DbLoadEngines(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]));
            foreach (Engine E in Engines)
            {
                IGpi engineGpi = _localGPIDevices == null ? null : _localGPIDevices.Select(E.Id); 
                E.Initialize(Servers, engineGpi);
            }
            Debug.WriteLine(this, "Created");
        }

        private bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
                DoDispose();
        }

        protected void DoDispose()
        {
            _disposed = true;
            foreach (Engine E in Engines)
                E.Dispose();
            if (_localGPIDevices != null)
                _localGPIDevices.Dispose();
        }
    }
}
