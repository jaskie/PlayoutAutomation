using TAS.Client.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EngineRouterViewModel : ViewModelBase
    {
        public EngineRouterViewModel(IVideoSwitch router)
        {
            Router = router;
            Router.ConnectAsync();
            Router.PropertyChanged += Router_PropertyChanged;

            CommandChangeSource = new UiCommand(ChangeSource, CanChangeSource);
        }

        private bool CanChangeSource(object obj)
        {
            return IsConnected;
        }

        private void ChangeSource(object obj)
        {
            using (var switcherVm = new SwitcherViewModel(Router))
            {
                WindowManager.Current.ShowDialog(switcherVm, resources._caption_Switcher);
            }
        }

        private void Router_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {                
                case nameof(Router.SelectedSource):
                    NotifyPropertyChanged(nameof(SelectedSource));
                    break;
                case nameof(Router.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    InvalidateRequerySuggested();
                    break;
            }
        }       

        public IVideoSwitchPort SelectedSource => Router.SelectedSource;        

        public bool IsConnected => Router.IsConnected;

        public IVideoSwitch Router { get; }

        public UiCommand CommandChangeSource { get; }

        protected override void OnDispose()
        {
            Router.PropertyChanged -= Router_PropertyChanged;
        }        
    }
}
