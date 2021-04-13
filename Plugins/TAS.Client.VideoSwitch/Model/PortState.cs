namespace TAS.Server.VideoSwitch.Model
{
    public class PortState
    {
        public PortState(short portId, bool isSignalPresent)
        {
            PortId = portId;
            IsSignalPresent = isSignalPresent;
        }

        public short PortId { get; }
        public bool IsSignalPresent { get; }
    }
}
