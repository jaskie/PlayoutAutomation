using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    [DtoClass(nameof(IRouter))]
    public class Router : ProxyObjectBase, IRouter
    {
#pragma warning disable CS0649 

        [DtoMember(nameof(IRouter.Sources))]
        private IList<IVideoSwitchPort> _inputPorts;

        [DtoMember(nameof(IRouter.SelectedSource))]
        private IVideoSwitchPort _selectedInputPort;

        [DtoMember(nameof(IRouter.IsConnected))]
        private bool _isConnected;       

        [DtoMember(nameof(IRouter.IsEnabled))]
        private bool _isEnabled;
        [DtoMember(nameof(IRouter.Preload))]
        private bool _preload;
        [DtoMember(nameof(IRouter.DefaultEffect))]
        private VideoSwitchEffect _defaultEffect;

#pragma warning restore

        public IList<IVideoSwitchPort> Sources => _inputPorts;

        public IVideoSwitchPort SelectedSource => _selectedInputPort;

        public bool IsConnected => _isConnected;       

        public bool IsEnabled { get => _isEnabled; set => Set(value); }

        public bool Preload => _preload;

        public VideoSwitchEffect DefaultEffect => _defaultEffect;

        public event EventHandler Started;

        public async Task<bool> ConnectAsync()
        {
            return Query<bool>();
        }

        public void SetSource(int inputId)
        {
            Invoke(parameters: new object[] { inputId });
        }

        public void SetTransitionEffect(VideoSwitchEffect videoSwitchEffect)
        {
            Invoke(parameters: new object[] { videoSwitchEffect });
        }

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
