using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.VideoSwitch.Communicators;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    internal class VideoSwitcher : IVideoSwitcher, IDisposable
    {
        private IVideoSwitchCommunicator _communicator;

        public VideoSwitcher(CommunicatorType type = CommunicatorType.Unknown)
        {
            Type = type;
            switch (type)
            {               
                
                default:
                    return;
            }
            _communicator.SourceChanged += Communicator_OnInputPortChangeReceived;
            _communicator.ConnectionChanged += Communicator_OnRouterConnectionStateChanged;
        }

        private void Communicator_OnRouterConnectionStateChanged(object sender, EventArgs<bool> e)
        {
            throw new NotImplementedException();
        }

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<CrosspointInfo> e)
        {
            throw new NotImplementedException();
        }

        public VideoSwitchEffect DefaultEffect => throw new NotImplementedException();

        public IList<IVideoSwitchPort> Sources => throw new NotImplementedException();

        public IVideoSwitchPort SelectedSource => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public bool Preload => throw new NotImplementedException();

        public bool IsEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [Hibernate]
        public CommunicatorType Type { get; set; }
        [Hibernate]
        public VideoSwitchEffect DefaultEffect { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Started;

        public Task<bool> ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void PreloadSource()
        {
            throw new NotImplementedException();
        }

        public void SetMixEffect(VideoSwitchEffect videoSwitchEffect)
        {
            ((IVideoSwitchCommunicator)_communicator).SetMixEffect(videoSwitchEffect);
        }

        public void SetSource(int inputId)
        {
            throw new NotImplementedException();
        }

        public void Take()
        {
            if (!(_communicator is IVideoSwitchCommunicator videoSwitch))
                return;
            videoSwitch.Take();
        }

        internal void GpiStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        protected override void DoDispose()
        {
            _communicator.SourceChanged -= Communicator_OnInputPortChangeReceived;
            _communicator.ConnectionChanged -= Communicator_OnRouterConnectionStateChanged;
            _communicator.Dispose();
        }
    }
}
