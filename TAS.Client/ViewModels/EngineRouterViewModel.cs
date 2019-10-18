using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineRouterViewModel : ViewModelBase
    {
        private byte _inputID;
        private ObservableCollection<RouterPort> _inputPorts;
        private RouterPort _selectedInputPort;
        private bool? _isInputSignalPresent;

        public byte InputID { get => _inputID; set => _inputID = value; }
        public ObservableCollection<RouterPort> InputPorts { get => _inputPorts; set => SetField(ref _inputPorts, value); }
        public RouterPort SelectedInputPort
        {
            get => _selectedInputPort;
            set
            {
                if (!SetField(ref _selectedInputPort, value))
                    return;

                _router.SwitchInput(value);
            }
        }
        public bool? IsInputSignalPresent { get => _isInputSignalPresent; set => SetField(ref _isInputSignalPresent, value); }

        private IRouter _router;

        public EngineRouterViewModel(IRouter router)
        {
            _router = router;
            _router.OnInputPortListChange += _router_OnInputPortListChange;
            _router.OnInputPortChange += _router_OnInputPortChange;            
            _router.OnInputSignalPresenceListReceived += _router_OnInputSignalPresenceListReceived;

            _router.RequestInputPorts();                                 
        }

        private void _router_OnInputPortListChange(object sender, RouterEventArgs e)
        {
            InputPorts = new ObservableCollection<RouterPort>(e.RouterPorts);
            _router.RequestCurrentInputPort();            
        }

        private void _router_OnInputSignalPresenceListReceived(object sender, RouterEventArgs e)
        {
            foreach (var port in _inputPorts)            
                port.IsSignalPresent = e.RouterPorts.FirstOrDefault(param => param.ID == port.ID).IsSignalPresent;            
        }

        private void _router_OnInputPortChange(object sender, RouterEventArgs e)
        {
            if (_selectedInputPort?.ID == e.RouterPorts.FirstOrDefault().ID)
                return;

            _selectedInputPort = InputPorts.FirstOrDefault(param=>param.ID == e.RouterPorts.FirstOrDefault().ID);
            NotifyPropertyChanged(nameof(SelectedInputPort));
        }

        protected override void OnDispose()
        {
            _router.OnInputPortListChange -= _router_OnInputPortListChange;
            _router.OnInputPortChange -= _router_OnInputPortChange;
            _router.OnInputSignalPresenceListReceived -= _router_OnInputSignalPresenceListReceived;
        }
    }
}
