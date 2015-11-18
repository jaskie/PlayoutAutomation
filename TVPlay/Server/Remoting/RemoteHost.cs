using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;
using WebSocketSharp.Server;

namespace TAS.Server.Remoting
{
    public class RemoteHost : IDisposable, IRemoteHostConfig
    {
        [XmlAttribute]
        public string EndpointAddress { get; set; }
        [XmlIgnore]
        public TAS.Server.Engine Engine { get; private set; }
        WebSocketServer _server;
        public bool Initialize(TAS.Server.Engine engine)
        {
            if (string.IsNullOrEmpty(EndpointAddress))
                return false;
            try
            {
                _server = new WebSocketServer(string.Format("ws://{0}", EndpointAddress));
                _server.AddWebSocketService<MediaManagerBehavior>("/MediaManager", () => new MediaManagerBehavior(engine.MediaManager as MediaManager));
                //_server.AddWebSocketService<EngineBehavior>("/Engine", () => new EngineBehavior(engine));
                _server.Start();
                return true;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e, "Initialization of RemoteHost error");
            }
            return false;
        }

        bool _disposed = false;
        void _doDispose(bool disposed)
        {
            if (!disposed)
            {
                _disposed = true;
                _server.Stop();
            }
        }

        public void Dispose()
        {
            lock (_server)
            {
                _doDispose(_disposed);
            }
        }

        internal void UnInitialize(Server.Engine engine)
        {
            _server.Stop();
        }
    }
}
