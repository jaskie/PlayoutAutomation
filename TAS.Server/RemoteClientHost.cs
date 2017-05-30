using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;
using TAS.Remoting.Server;
using TAS.Server.Common.Interfaces;
using WebSocketSharp.Server;

namespace TAS.Server
{
    public class RemoteClientHost : IDisposable, IRemoteHostConfig
    {
        [XmlAttribute]
        public ushort ListenPort { get; set; }
        private WebSocketServer _server;
        private static ISerializationBinder ServerBinder = new ServerSerializationBinder();
        private static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(RemoteClientHost));

        public bool Initialize(Engine engine)
        {
            if (ListenPort < 1024)
                return false;
            try
            {
                _server = new WebSocketServer(ListenPort);
                _server.AddWebSocketService("/Engine", () => new CommunicationBehavior(engine) { Binder = ServerBinder });
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

        bool _disposed = false;
        void _doDispose(bool disposed)
        {
            if (!disposed)
            {
                _disposed = true;
                UnInitialize();
            }
        }

        public void Dispose()
        {
            _doDispose(_disposed);
        }

        internal void UnInitialize()
        {
            _server?.Stop();
        }
    }
}
