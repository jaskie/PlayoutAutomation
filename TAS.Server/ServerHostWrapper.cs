using jNet.RPC.Server;
using System;
using System.Xml.Serialization;
using TAS.Database.Common;

namespace TAS.Server
{
    public class ServerHostWrapper: IDisposable
    {
        private ServerHost _serverHost;

        [XmlAttribute, Hibernate]
        public ushort ListenPort { get; set; }
        public int ClientCount => _serverHost?.ClientCount ?? 0;

        public void Dispose()
        {
            _serverHost?.Dispose();
        }

        internal void Initialize(Engine engine, IPrincipalProvider principalProvider)
        {
            _serverHost = new ServerHost(ListenPort, engine, principalProvider);
        }

        internal void UnInitialize()
        {
            _serverHost?.Dispose();
            _serverHost = null;
        }
    }
}
