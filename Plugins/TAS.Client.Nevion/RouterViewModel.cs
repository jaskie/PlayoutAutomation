using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using TAS.Client.Common;
using TAS.Client.Router.Helpers;
using TAS.Client.Router.Model;
using TAS.Client.Router.RouterCommunicators;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Router
{
    public class RouterViewModel : ViewModelBase, IRouter
    {
        public UserControl View { get; }
        private IRouterCommunicator _routerCommunicator;

        private RouterDevice _device;
        public RouterDevice Device 
        { 
            get => _device; 
            set => SetField(ref _device, value); 
        }

        public bool? IsInputSignalPresent { get; set; }

        private ObservableCollection<RouterPort> _inputPorts = new ObservableCollection<RouterPort>();
        public ObservableCollection<RouterPort> InputPorts { get => _inputPorts; set => SetField(ref _inputPorts, value); }

        private IEnumerable<RouterPort> _outputPorts;

        private RouterPort _selectedInputPort = new RouterPort();
        public RouterPort SelectedInputPort { get => _selectedInputPort;
            set 
            {
                if (!SetField(ref _selectedInputPort, value))
                    return;

                if (_outputPorts == null)
                    return;
                _routerCommunicator.SwitchInput(value, _outputPorts);
            }
        }

        
        public RouterViewModel()
        {
            View = new RouterView();
            View.DataContext = this;
            Device = DataStore.Load<RouterDevice>("RouterDevice");

            if (Device == null)
            {
                Debug.WriteLine("Błąd deserializacji XML");
                return;
            }
            Task.Run(() => Init());            
        }

        private void Init()
        {
            if (!Connect())
                return;           

            _routerCommunicator.OnOutputPortsListReceived += OnOutputPortsListReceived;
            _routerCommunicator.OnInputPortsListReceived += OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived += OnInputPortChangeReceived;

           
            _routerCommunicator.RequestInputPorts();           
            _routerCommunicator.RequestOutputPorts();            
            _routerCommunicator.RequestCurrentInputPort();
        }

        private void OnOutputPortsListReceived(object sender, RouterEventArgs e)
        {
            _outputPorts = e.RouterPorts.Where(param => _device.OutputPorts.Contains(param.ID));
        }

        private void OnInputPortChangeReceived(object sender, RouterEventArgs e)
        {           
            var changedIn = e.RouterPorts.Where(p=> _outputPorts.Any(q => q.ID == p.ID)).FirstOrDefault();

            if (changedIn == null)
                return;

            _selectedInputPort = _inputPorts.Where(p => p.ID == changedIn.ID).FirstOrDefault();           
            NotifyPropertyChanged(nameof(SelectedInputPort));
        }

        private void OnInputPortsListReceived(object sender, RouterEventArgs e)
        {           
            InputPorts = new ObservableCollection<RouterPort>(e.RouterPorts);
        }

        private bool Connect()
        {        
            try
            {
                switch (Device.Type)
                {
                    case Model.Enums.Router.Nevion:
                        {
                            Debug.WriteLine("Nevion communicator registered");
                            _routerCommunicator = new NevionCommunicator(Device);
                            break;
                        }
                    case Model.Enums.Router.Blackmagic:
                        {
                            _routerCommunicator = new BlackmagicSVHCommunicator();
                            break;
                        }
                    default:
                        return false;
                }
                return _routerCommunicator.Connect(Device.IP, Device.Port).Result;
            }
            catch
            {
                return false;
            }
            
        }       
             
        public bool SwitchInput(RouterPort inPort)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RouterPort> GetInputPorts()
        {
            return _inputPorts;
        }

        protected override void OnDispose()
        {
            _routerCommunicator.OnInputPortsListReceived -= OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived -= OnInputPortChangeReceived;
            _routerCommunicator.OnInputPortChangeReceived -= OnInputPortChangeReceived;
            _routerCommunicator.Dispose();
            Debug.WriteLine("Router Plugin Disposed");
        }       
    }
}
