using TAS.Common.Interfaces;

namespace TAS.Server.Model
{
    public class Crosspoint
    {
        public IRouterPort InPort { get; }
        public IRouterPort OutPort { get; }

        public Crosspoint(short inPort, short outPort)
        {
            InPort = new RouterPort(inPort);
            OutPort = new RouterPort(outPort);
        }
    }
}
