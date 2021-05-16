using TAS.Client.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EngineVideoSwitchViewModel : ViewModelBase
    {
        public EngineVideoSwitchViewModel(IVideoSwitch videoSwitch)
        {
            VideoSwitch = videoSwitch;
            VideoSwitch.Connect();
            VideoSwitch.PropertyChanged += VideoSwitch_PropertyChanged;

            CommandChangeSource = new UiCommand(ChangeSource, CanChangeSource);
        }

        private bool CanChangeSource(object obj)
        {
            return IsConnected;
        }

        private void ChangeSource(object obj)
        {
            using (var switcherVm = new VideoSwitchViewModel(VideoSwitch))
            {
                WindowManager.Current.ShowDialog(switcherVm, resources._caption_Switcher);
            }
        }

        private void VideoSwitch_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {                
                case nameof(VideoSwitch.SelectedSource):
                    NotifyPropertyChanged(nameof(SelectedSource));
                    break;
                case nameof(VideoSwitch.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    InvalidateRequerySuggested();
                    break;
            }
        }       

        public IVideoSwitchPort SelectedSource => VideoSwitch.SelectedSource;        

        public bool IsConnected => VideoSwitch.IsConnected;

        public IVideoSwitch VideoSwitch { get; }

        public UiCommand CommandChangeSource { get; }

        protected override void OnDispose()
        {
            VideoSwitch.PropertyChanged -= VideoSwitch_PropertyChanged;
        }        
    }
}
