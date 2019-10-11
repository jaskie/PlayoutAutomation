using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;

namespace TAS.Client.Router.Model
{
    public class RouterEventArgs
    {
        public IList<RouterPort> RouterPorts { get; }
        public string Response { get; }

        public RouterEventArgs(IList<RouterPort> routerPorts)
        {
            RouterPorts = routerPorts;
        }

        public RouterEventArgs(string response)
        {
            Response = response;
        }
    }
}
