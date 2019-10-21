using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Common
{
    public class RouterEventArgs
    {
        public IList<RouterPort> RouterPorts { get; }
        public string Response { get; }
        public bool IsConnected { get; }

        public RouterEventArgs(IList<RouterPort> routerPorts)
        {
            RouterPorts = routerPorts;
        }

        public RouterEventArgs(string response)
        {
            Response = response;
        }

        public RouterEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }
}
