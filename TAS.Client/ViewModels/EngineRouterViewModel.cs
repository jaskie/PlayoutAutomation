using System.Collections.Generic;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineRouterViewModel : ViewModelBase
    {
        public EngineRouterViewModel(IVideoSwitch router)
        {
            Router = router;
            Router.Connect();
            Router.PropertyChanged += Router_PropertyChanged;
        }

        public IList<IVideoSwitchPort> InputPorts => Router.InputPorts;

        private IVideoSwitchPort _selectedInputPort
        {
            get => Router.SelectedInputPort;
            set
            {
                if (Router.InputPorts == value)
                    return;

                if (value == null)
                    return;

                Router.SelectInput(value.PortId);
            }
        }

        public IVideoSwitchPort SelectedInputPort 
        { 
            get => _selectedInputPort; 
            set 
            {
                _selectedInputPort = value;                    
                NotifyPropertyChanged();                                    
            } 
        }

        public bool IsConnected => Router.IsConnected;

        public IVideoSwitch Router { get; }

        protected override void OnDispose()
        {
            Router.PropertyChanged -= Router_PropertyChanged;
        }

        private void Router_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Router.InputPorts):
                    NotifyPropertyChanged(nameof(InputPorts));
                    break;
                case nameof(Router.SelectedInputPort):
                    NotifyPropertyChanged(nameof(SelectedInputPort));
                    break;
                case nameof(Router.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    break;
            }
        }

    }
}
