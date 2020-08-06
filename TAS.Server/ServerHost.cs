using System;
using System.Xml.Serialization;
using TAS.Database.Common;

namespace TAS.Server
{
    public class ServerHost: IDisposable
    {
        private jNet.RPC.Server.ServerHost _serverHost;

        [XmlAttribute, Hibernate]
        public ushort ListenPort { get; set; }
        public int ClientCount => _serverHost?.ClientCount ?? 0;       

        public void Dispose()
        {
            _serverHost?.Dispose();
        }

        internal void Initialize(Engine engine, jNet.RPC.Server.IPrincipalProvider principalProvider)
        {
            _serverHost = new jNet.RPC.Server.ServerHost { ListenPort = ListenPort };
            _serverHost.Initialize(engine, principalProvider);
        }

        internal void UnInitialize()
        {
            _serverHost?.UnInitialize();
        }
    }
}
