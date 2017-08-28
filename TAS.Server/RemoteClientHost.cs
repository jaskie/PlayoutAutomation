using System;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using TAS.Remoting.Server;
using TAS.Common.Interfaces;
using WebSocketSharp.Server;
using Newtonsoft.Json.Serialization;

namespace TAS.Server
{
    public class RemoteClientHost : IDisposable, IRemoteHostConfig
    {
        [XmlAttribute]
        private WebSocketServer _server;
        private int _disposed;

        private static readonly ISerializationBinder ServerBinder = new ServerSerializationBinder();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(RemoteClientHost));

        [XmlAttribute]
        public ushort ListenPort { get; set; }

        public bool Initialize(Engine engine)
        {
            if (ListenPort < 1024)
                return false;
            try
            {
                _server = new WebSocketServer(ListenPort);
                _server.AddWebSocketService("/Engine", () => new CommunicationBehavior(engine, engine.AuthenticationService) { Binder = ServerBinder });
                _server.Start();
                return true;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e, "Initialization of RemoteClientHost error");
                Logger.Error(e, "Initialization of RemoteClientHost error");
            }
            return false;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == default(int))
                UnInitialize();
        }

        public void UnInitialize()
        {
            _server?.Stop();
        }
    }
}
