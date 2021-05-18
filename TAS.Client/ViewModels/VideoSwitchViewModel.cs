using System.Collections.Generic;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class VideoSwitchViewModel : OkCancelViewModelBase
    {
        private IVideoSwitchPort _selectedInputPort;
        public VideoSwitchViewModel(IVideoSwitch videoSwitch)
        {
            VideoSwitch = videoSwitch;            
            VideoSwitch.PropertyChanged += VideoSwitch_PropertyChanged;

            _selectedInputPort = VideoSwitch.SelectedSource;
            NotifyPropertyChanged(nameof(SelectedSource));
        }

        private void VideoSwitch_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(VideoSwitch.Sources):
                    NotifyPropertyChanged(nameof(Sources));
                    break;
                case nameof(VideoSwitch.SelectedSource):
                    NotifyPropertyChanged(nameof(SelectedSource));
                    break;
                case nameof(VideoSwitch.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    break;
            }
        }

        public IVideoSwitchPort SelectedSource
        {
            get => _selectedInputPort;
            set
            {
                if (VideoSwitch.Sources == value)
                    return;

                if (value == null)
                    return;

                SetField(ref _selectedInputPort, value);
            }
        }

        public override bool CanOk(object obj)
        {
            if (VideoSwitch.SelectedSource?.Id != _selectedInputPort?.Id && IsConnected)
                return true;
            return false;
        }

        public override bool Ok(object obj)
        {
            VideoSwitch.SetSource(_selectedInputPort.Id);
            return true;
        }

        public bool IsConnected => VideoSwitch.IsConnected;
        public IVideoSwitch VideoSwitch { get; }
        public IList<IVideoSwitchPort> Sources => VideoSwitch.Sources;
    }
}
