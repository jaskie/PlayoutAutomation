using Newtonsoft.Json;
using System.Collections.Generic;
using ComponentModelRPC;
using ComponentModelRPC.Client;
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

        public IList<IRouterPort> InputPorts
        {
            get => _inputPorts;
            set => Set(value);
        }
       
        public IRouterPort SelectedInputPort 
        {
            get => _selectedInputPort;
            set => Set(value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => Set(value);
        }

        public void SelectInput(int inputId)
        {
            Invoke(parameters: new object[] { inputId });
        }

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
