using System.Collections.Generic;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineRouterViewModel : ViewModelBase
    {

        private IRouterPort _selectedInputPort;

        public EngineRouterViewModel(IRouter router)
        {
            Router = router;
            Router.PropertyChanged += Router_PropertyChanged;
            _selectedInputPort = Router.SelectedInputPort;
        }

        public IList<IRouterPort> InputPorts => Router.InputPorts;

        public IRouterPort SelectedInputPort 
        { 
            get => _selectedInputPort; 
            set
            {
                if (!SetField(ref _selectedInputPort, value))
                    return;
                if (value == null)
                    return;
                Router.SelectInputPort(value.PortId, true);
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
                    _selectedInputPort = Router.SelectedInputPort;
                    NotifyPropertyChanged(nameof(SelectedInputPort));
                    break;
                case nameof(Router.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    break;
            }
        }

    }
}
