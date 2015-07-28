using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Server.Remoting
{
    public class RemoteHost : IDisposable
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
                _host = new ServiceHost(typeof(Engine));
                Engine service = new Engine();
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None, true);
                _host.AddServiceEndpoint(typeof(IEngine), binding, string.Format(@"net.tcp://{0}/Engine", EndpointAddress));
                _host.AddServiceEndpoint(typeof(IMediaManager), binding, string.Format(@"net.tcp://{0}/MediaManager", EndpointAddress));
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
