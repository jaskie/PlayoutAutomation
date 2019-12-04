using Newtonsoft.Json;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class Router : ProxyBase, IRouter
    {
#pragma warning disable CS0649 

        [JsonProperty(nameof(IRouter.InputPorts))]
        private IList<IRouterPort> _inputPorts;

        [JsonProperty(nameof(IRouter.SelectedInputPort))]
        private IRouterPort _selectedInputPort;

        [JsonProperty(nameof(IRouter.IsConnected))]
        private bool _isConnected;

#pragma warning restore

        public IList<IRouterPort> InputPorts => _inputPorts;

        public IRouterPort SelectedInputPort => _selectedInputPort;

        public bool IsConnected => _isConnected;

        public void SelectInput(int inputId)
        {
            Invoke(parameters: new object[] { inputId });
        }

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
