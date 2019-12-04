using jNet.RPC;
using jNet.RPC.Client;
using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    class RouterPort : ProxyBase, IRouterPort
    {
        #pragma warning disable CS0649
        [JsonProperty(nameof(IRouterPort.PortId))]
        private short _portId;
        [JsonProperty(nameof(IRouterPort.PortName))]
        private string _portName;
        [JsonProperty(nameof(IRouterPort.IsSignalPresent))]
        private bool? _portIsSignalPresent;
        #pragma warning restore

        public short PortId => _portId;
        public string PortName => _portName;
        public bool? IsSignalPresent => _portIsSignalPresent;

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
