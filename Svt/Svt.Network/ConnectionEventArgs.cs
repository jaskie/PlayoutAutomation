using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Network
{
    public class ConnectionEventArgs : EventArgs
    {
        internal ConnectionEventArgs(string host, int port, bool connected)
        {
            Hostname = host;
            Port = port;
            Connected = connected;
            Exception = null;
        }
        internal ConnectionEventArgs(string host, int port, bool connected, Exception exception)
        {
            Hostname = host;
            Port = port;
            Connected = connected;
            Exception = exception;
        }

        public string Hostname { get; private set; }
        public int Port { get; private set; }
        public bool Connected { get; private set; }
        public Exception Exception { get; private set; }
    }

    public class ClientConnectionEventArgs : ConnectionEventArgs
    {
		public RemoteHostState Client { get; set; }

        internal ClientConnectionEventArgs(string host, int port, bool connected, Exception ex, bool remote)
            : base(host, port, connected, ex)
        {
            Remote = remote;
        }

        public bool Remote { get; private set; }
    }
}
