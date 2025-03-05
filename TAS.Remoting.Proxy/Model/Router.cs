using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class Router : ProxyObjectBase, IRouter
    {
#pragma warning disable CS0649 

        [DtoMember(nameof(IRouter.InputPorts))]
        private IList<IRouterPort> _inputPorts;

        [DtoMember(nameof(IRouter.SelectedInputPort))]
        private IRouterPort _selectedInputPort;

        [DtoMember(nameof(IRouter.IsConnected))]
        private bool _isConnected;

#pragma warning restore

        public IList<IRouterPort> InputPorts => _inputPorts;

        public IRouterPort SelectedInputPort => _selectedInputPort;

        public bool IsConnected => _isConnected;

        public void SelectInputPort(int inputId)
        {
            Invoke(parameters: new object[] { inputId });
        }

    }
}
