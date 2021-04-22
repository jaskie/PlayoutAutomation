using System.Collections.Generic;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class SwitcherViewModel : OkCancelViewModelBase
    {
        private IVideoSwitchPort _selectedInputPort;
        public SwitcherViewModel(IVideoSwitch router)
        {
            Router = router;            
            Router.PropertyChanged += Router_PropertyChanged;

            _selectedInputPort = Router.SelectedSource;
            NotifyPropertyChanged(nameof(SelectedSource));
        }

        private void Router_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Router.Sources):
                    NotifyPropertyChanged(nameof(Sources));
                    break;
                case nameof(Router.SelectedSource):
                    NotifyPropertyChanged(nameof(SelectedSource));
                    break;
                case nameof(Router.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    break;
            }
        }

        public IVideoSwitchPort SelectedSource
        {
            get => _selectedInputPort;
            set
            {
                if (Router.Sources == value)
                    return;

                if (value == null)
                    return;

                SetField(ref _selectedInputPort, value);
            }
        }

        public override bool CanOk(object obj)
        {
            if (Router.SelectedSource?.PortId != _selectedInputPort?.PortId && IsConnected)
                return true;
            return false;
        }

        public override bool Ok(object obj)
        {
            Router.SetSource(_selectedInputPort.PortId);
            return true;
        }

        public bool IsConnected => Router.IsConnected;
        public IVideoSwitch Router { get; }
        public IList<IVideoSwitchPort> Sources => Router.Sources;
    }
}
