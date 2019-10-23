using System;

namespace TAS.Remoting.Client
{
    public class ProxyBaseEventArgs: EventArgs
    {
        public ProxyBaseEventArgs(ProxyBase proxy)
        {
            Proxy = proxy;
        }

        public ProxyBase Proxy { get; }
    }
}
