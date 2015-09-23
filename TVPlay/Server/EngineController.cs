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
        public readonly List<PlayoutServer> Servers;
        public readonly List<Engine> Engines;
        [Import]
        ILocalDevices _localGPIDevices;
        public readonly CompositionContainer ServerContainer;

        public EngineController()
        {
            try
            {
                DirectoryCatalog catalog = new DirectoryCatalog(".", "TAS.Server.*.dll");
                ServerContainer = new CompositionContainer(catalog);
                ServerContainer.ComposeExportedValue("LocalDevicesConfigurationFile", ConfigurationManager.AppSettings["LocalSettings"]);
                ServerContainer.SatisfyImportsOnce(this);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            if (_localGPIDevices != null)
                _localGPIDevices.Initialize();

            Debug.WriteLine(this, "Initializing database connector");
            DatabaseConnector.Initialize();
            Servers = DatabaseConnector.DbLoadServers();
            Engines = DatabaseConnector.DbLoadEngines(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]), Servers);
            foreach (Engine E in Engines)
            {
                IGpi engineGpi = _localGPIDevices == null ? null : ((ILocalDevices)_localGPIDevices).Select(E.IdEngine); // (localSettings != null) ? localSettings.EngineBindings.FirstOrDefault(e => e.IdEngine == E.IdEngine) : null;
                E.Initialize(engineGpi);
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
            foreach (Engine E in Engines)
            {
                E.SaveAllEvents();
                E.Dispose();
            }
            //if (localSettings != null)
            //    localSettings.Dispose();
        }
    }
}
