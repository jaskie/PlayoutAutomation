using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Router.Model
{
    public class Crosspoint
    {
        public IRouterPort InPort { get; }
        public IRouterPort OutPort { get; }

        public Crosspoint(int inPort, int outPort)
        {
            InPort = new RouterPort(inPort);
            OutPort = new RouterPort(outPort);
        }
    }
}
