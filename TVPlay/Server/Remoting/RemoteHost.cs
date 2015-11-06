using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;

namespace TAS.Server.Remoting
{
    public class RemoteHost : IDisposable, IRemoteHostConfig
    {
        [XmlAttribute]
        public string EndpointAddress { get; set; }
        [XmlIgnore]
        public TAS.Server.Engine Engine { get; private set; }
        ServiceHost _host;
        public bool Initialize(TAS.Server.Engine engine)
        {
            if (string.IsNullOrEmpty(EndpointAddress))
                return false;
            try
            {
                _host = new ServiceHost(engine.MediaManager);
                var service = engine.MediaManager;
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None, true);
                _host.AddServiceEndpoint(typeof(IMediaManagerContract), binding, string.Format(@"net.tcp://{0}/MediaManager", EndpointAddress));
                _host.Open();
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
                _host.Close();
            }
        }

        public void Dispose()
        {
            lock (_host)
            {
                _doDispose(_disposed);
            }
        }

        internal void UnInitialize(Server.Engine engine)
        {
            _host.Close();
        }
    }
}
