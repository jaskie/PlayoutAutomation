using System;

namespace TAS.Remoting.Client
{
    internal class ProxyBaseEventArgs: EventArgs
    {
        public ProxyBaseEventArgs(ProxyBase proxy)
        {
            Proxy = proxy;
        }

        public ProxyBase Proxy { get; }
    }
}
