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

namespace TAS.Server
{
    public class EngineController: IDisposable
    {
        public readonly List<PlayoutServer> Servers;
        public readonly List<Engine> Engines;
        readonly LocalDevices localSettings;
        public EngineController()
        {
            Debug.WriteLine(this, "Creating LocalSettings");
            try
            {
                string settingsFileName = ConfigurationManager.AppSettings["LocalSettings"];
                if (!string.IsNullOrEmpty(settingsFileName) && File.Exists(settingsFileName))
                {
                    XmlSerializer reader = new XmlSerializer(typeof(LocalDevices));
                    StreamReader file = new System.IO.StreamReader(settingsFileName);
                    localSettings = (LocalDevices)reader.Deserialize(file);
                    file.Close();
                    if (localSettings != null)
                    {
                        Debug.WriteLine(this, "Initializing local settings");
                        localSettings.Initialize();
                    }
                }
            }
            catch (Exception e) { Debug.WriteLine(e); }

            Debug.WriteLine(this, "Initializing database connector");
            DatabaseConnector.Initialize();
            Servers = DatabaseConnector.DbLoadServers();
            Engines = DatabaseConnector.DbLoadEngines(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]), Servers);
            foreach (Engine E in Engines)
            {
                
                LocalGpiDeviceBinding engineSettings = (localSettings != null) ? localSettings.EngineBindings.FirstOrDefault(e => e.IdEngine == E.IdEngine): default(LocalGpiDeviceBinding);
                E.Initialize(engineSettings);
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
            if (localSettings != null)
                localSettings.Dispose();
        }
    }
}
