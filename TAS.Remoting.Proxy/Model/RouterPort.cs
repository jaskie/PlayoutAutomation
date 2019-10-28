using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;

namespace TAS.Remoting.Model
{
    class RouterPort : ProxyBase, IRouterPort
    {
        #pragma warning disable CS0649
        [JsonProperty(nameof(IRouterPort.PortID))]
        private int _portID;
        [JsonProperty(nameof(IRouterPort.PortName))]
        private string _portName;
        [JsonProperty(nameof(IRouterPort.PortIsSignalPresent))]
        private bool? _portIsSignalPresent;
        #pragma warning restore

        public int PortID { get => _portID; set => Set(value); }
        public string PortName { get => _portName; set => Set(value); }
        public bool? PortIsSignalPresent { get => _portIsSignalPresent; set => Set(value); }

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
