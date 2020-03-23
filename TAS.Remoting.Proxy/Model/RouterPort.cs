using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    class RouterPort : ProxyObjectBase, IRouterPort
    {
        #pragma warning disable CS0649
        [DtoField(nameof(IRouterPort.PortId))]
        private short _portId;
        [DtoField(nameof(IRouterPort.PortName))]
        private string _portName;
        [DtoField(nameof(IRouterPort.IsSignalPresent))]
        private bool? _portIsSignalPresent;
        #pragma warning restore

        public short PortId => _portId;
        public string PortName => _portName;
        public bool? IsSignalPresent => _portIsSignalPresent;

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
