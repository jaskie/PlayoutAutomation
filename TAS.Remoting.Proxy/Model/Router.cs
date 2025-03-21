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
        private IRouterPort[] _inputPorts;

        [DtoMember(nameof(IRouter.SelectedInputPort))]
        private IRouterPort _selectedInputPort;

        [DtoMember(nameof(IRouter.IsConnected))]
        private bool _isConnected;

#pragma warning restore

        public IRouterPort[] InputPorts => _inputPorts;

        public IRouterPort SelectedInputPort => _selectedInputPort;

        public bool IsConnected => _isConnected;

        /// <summary>
        /// method used only on the server side
        /// </summary>
        public bool SwitchOnPreload => throw new NotImplementedException();

        ///<inheritdoc/>
        public void SelectInputPort(int inputId, bool instant)
        {
            Invoke(parameters: new object[] { inputId, instant });
        }

    }
}
