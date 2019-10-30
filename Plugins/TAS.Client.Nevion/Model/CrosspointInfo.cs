namespace TAS.Server.Model
{
    internal class CrosspointInfo
    {
        public CrosspointInfo(short inPort, short outPort)
        {
            InPort = inPort;
            OutPort = outPort;
        }

        public short InPort { get; }

        public short OutPort { get; }

    }
}
