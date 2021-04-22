using jNet.RPC;
using jNet.RPC.Client;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    [DtoType(typeof(IVideoSwitcher))]
    public class VideoSwitcher : ProxyObjectBase, IVideoSwitcher
    {
        [DtoMember(nameof(IVideoSwitcher.Sources))]
        private IList<IVideoSwitchPort> _sources;

        [DtoMember(nameof(IVideoSwitcher.SelectedSource))]
        private IVideoSwitchPort _selectedSource;

        [DtoMember(nameof(IVideoSwitcher.IsConnected))]
        private bool _isConnected;

        [DtoMember(nameof(IVideoSwitcher.IsEnabled))]
        private bool _isEnabled;

        [DtoMember(nameof(IVideoSwitcher.Preload))]
        private bool _preload;

        [DtoMember(nameof(IVideoSwitcher.DefaultEffect))]
        private VideoSwitcherTransitionStyle _defaultEffect;

        public VideoSwitcherTransitionStyle DefaultEffect => _defaultEffect;

        public IList<IVideoSwitchPort> Sources => _sources;

        public IVideoSwitchPort SelectedSource => _selectedSource;

        public bool IsConnected => _isConnected;

        public bool Preload => _preload;

        public bool IsEnabled { get => _isEnabled; set => Set(value); }

        public event EventHandler Started;

        public bool Connect()
        {
            return Query<bool>();
        }        

        public void PreloadSource(int sourceId)
        {
            Invoke(parameters: new object[] { sourceId });
        }

        public void SetSource(int sourceId)
        {
            Invoke(parameters: new object[] { sourceId });
        }

        public void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect)
        {
            Invoke(parameters: new object[] { videoSwitchEffect });
        }

        public void Take()
        {
            Invoke();
        }

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
