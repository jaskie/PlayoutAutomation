using System.Collections.Generic;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineRouterViewModel : ViewModelBase
    {
        public EngineRouterViewModel(IRouter router)
        {
            Router = router;
            Router.PropertyChanged += Router_PropertyChanged;
        }

        public IList<IRouterPort> InputPorts => Router.InputPorts;

        private IRouterPort _selectedInputPort
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

        public IRouterPort SelectedInputPort 
        { 
            get => _selectedInputPort; 
            set
            {
                _selectedInputPort = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsConnected => Router.IsConnected;

        public IRouter Router { get; }

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
