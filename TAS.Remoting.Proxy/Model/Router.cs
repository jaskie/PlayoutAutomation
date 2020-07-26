using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    [DtoClass(nameof(IRouter))]
    public class Router : ProxyObjectBase, IRouter
    {
#pragma warning disable CS0649 

        [DtoMember(nameof(IRouter.InputPorts))]
        private IList<IRouterPort> _inputPorts;

        [DtoMember(nameof(IRouter.SelectedInputPort))]
        private IRouterPort _selectedInputPort;

        [DtoMember(nameof(IRouter.IsConnected))]
        private bool _isConnected;

        [DtoMember(nameof(IRouter.OutputPorts))]
        private short[] _outputPorts;

        [DtoMember(nameof(IRouter.IpAddress))]
        private string _ipAddress;

        [DtoMember(nameof(IRouter.Login))]
        private string _login;

        [DtoMember(nameof(IRouter.Password))]
        private string _password;

        [DtoMember(nameof(IRouter.Level))]
        private int _level;

        [DtoMember(nameof(IRouter.IsEnabled))]
        private bool _isEnabled;

#pragma warning restore

        public IList<IRouterPort> InputPorts => _inputPorts;

        public IRouterPort SelectedInputPort => _selectedInputPort;

        public bool IsConnected => _isConnected;

        public short[] OutputPorts => _outputPorts;

        public string IpAddress => _ipAddress;

        public string Login => _login;

        public string Password => _password;

        public int Level => _level;

        public bool IsEnabled { get => _isEnabled; set => Set(value); }

        public void Connect()
        {
            Invoke();
        }

        public void SelectInput(int inputId)
        {
            Invoke(parameters: new object[] { inputId });
        }

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
