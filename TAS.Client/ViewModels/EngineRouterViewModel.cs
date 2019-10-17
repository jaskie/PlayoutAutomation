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
            _router.OnInputPortChangeReceived += _router_OnInputPortChangeReceived;
            Task.Run(() => InputPorts = new ObservableCollection<RouterPort>(_router.GetInputPorts(true).Result));
        }

        private void _router_OnInputPortChangeReceived(object sender, RouterEventArgs e)
        {            
            _selectedInputPort = InputPorts.FirstOrDefault(param=>param.ID == e.RouterPorts.FirstOrDefault().ID);
            NotifyPropertyChanged(nameof(SelectedInputPort));
        }

        protected override void OnDispose()
        {
            
        }
    }
}
