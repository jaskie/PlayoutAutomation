using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.Configuration;

namespace TAS.Server
{
    public enum TServerType { Caspar = 0 };
    class EngineController: IDisposable
    {
        public readonly List<PlayoutServer> Servers;
        public List<Engine> Engines { get; private set; }
        public EngineController()
        {
            Debug.WriteLine(this, "Creating");

            DatabaseConnector.Initialize();
            DatabaseConnector.Connect();
            Servers = DatabaseConnector.ServerLoadServers();
            Engines = DatabaseConnector.EngineLoadEngines(UInt64.Parse(ConfigurationManager.AppSettings["Instance"]), Servers);
            foreach (Engine E in Engines)
            {
                E.Initialize();
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
        }
    }
}
