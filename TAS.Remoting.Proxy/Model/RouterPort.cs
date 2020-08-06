using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    class RouterPort : ProxyObjectBase, IVideoSwitchPort
    {
        #pragma warning disable CS0649
        [DtoMember(nameof(IVideoSwitchPort.PortId))]
        private short _portId;
        [DtoMember(nameof(IVideoSwitchPort.PortName))]
        private string _portName;
        [DtoMember(nameof(IVideoSwitchPort.IsSignalPresent))]
        private bool? _portIsSignalPresent;
        #pragma warning restore

        public short PortId => _portId;
        public string PortName => _portName;
        public bool? IsSignalPresent => _portIsSignalPresent;

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
