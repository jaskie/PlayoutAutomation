using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using TAS.Server.Router.Helpers;
using TAS.Server.Router.Model;
using TAS.Server.Router.RouterCommunicators;
using TAS.Common;
using TAS.Common.Interfaces;
using System.Threading;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using TAS.Remoting.Server;

namespace TAS.Server.Router
{
    public class RouterController : DtoBase, IRouter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();       

        private IRouterCommunicator _routerCommunicator;
        private RouterDevice _device;
        
        private IEnumerable<IRouterPort> _outputPorts;        
        private SemaphoreSlim _semaphoreSignalPresence = new SemaphoreSlim(0);
        private CancellationTokenSource cTokenSourceSignalPresence = new CancellationTokenSource();

        private IRouterPort _selectedInputPort;
        public IRouterPort SelectedInputPort
        {
            get => _selectedInputPort;
            set => SetField(ref _selectedInputPort, value);
        }

        private IList<IRouterPort> _inputPorts;
        public IList<IRouterPort> InputPorts
        {
            get => _inputPorts;
            set => SetField(ref _inputPorts, value);
        }

        public RouterController()
        {                        
            _device = DataStore.Load<RouterDevice>("RouterDevice");

            if (_device == null)
            {
                Debug.WriteLine("Błąd deserializacji XML");
                return;
            }
            Task.Run(() => Init());            
        }

        public void SelectInput(int inPort)
        {
            _routerCommunicator.SelectInput(inPort, _outputPorts);
        }

        private void Init()
        {
            if (!Connect())
                return;
           
            _routerCommunicator.OnOutputPortsListReceived += Communicator_OnOutputPortsListReceived;
            _routerCommunicator.OnInputPortsListReceived += Communicator_OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived += Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnInputSignalPresenceListReceived += Communicator_OnInputSignalPresenceListReceived;
            _routerCommunicator.OnRouterConnectionStateChanged += _routerCommunicator_OnRouterConnectionStateChanged;

            _routerCommunicator.RequestInputPorts();
            _routerCommunicator.RequestOutputPorts();                        
        }

        private void _routerCommunicator_OnRouterConnectionStateChanged(object sender, EventArgs<bool> e)
        {
            if (e.Item) return;

            InputPorts = null;
            SelectedInputPort = null;

            Task.Run(() => Init());
        }

        private void Communicator_OnOutputPortsListReceived(object sender, EventArgs<IEnumerable<IRouterPort>> e)
        {
            _outputPorts = e.Item.Where(param => _device.OutputPorts.Contains(param.PortID));
        }

        private void KeepUpdatingSignalPresence()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (cTokenSourceSignalPresence.IsCancellationRequested)
                        throw new OperationCanceledException(cTokenSourceSignalPresence.Token);

                    _routerCommunicator.RequestSignalPresence();
                    await _semaphoreSignalPresence.WaitAsync();
                    await Task.Delay(3000);
                }
            });            
        }

        private bool Connect()
        {        
            try
            {
                switch (_device.Type)
                {
                    case Model.Enums.Router.Nevion:
                        {
                            Debug.WriteLine("Nevion communicator registered");
                            _routerCommunicator = new NevionCommunicator(_device);
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
                return _routerCommunicator.Connect(_device.IP, _device.Port);
            }
            catch
            {
                return false;
            }
            
        }            

        private void Communicator_OnInputPortsListReceived(object sender, EventArgs<IEnumerable<IRouterPort>> e)
        {
            if (_inputPorts == null)
            {
                InputPorts = new List<IRouterPort>(e.Item);               
                Task.Run(() => KeepUpdatingSignalPresence(), cTokenSourceSignalPresence.Token);
            }
            else
            {
                foreach(var port in _inputPorts)
                {
                    if (e.Item.FirstOrDefault(param => param.PortID == port.PortID && param.PortName == port.PortName) != null)
                        continue;
                    InputPorts = new List<IRouterPort>(e.Item);
                    break;
                }
            }
            _routerCommunicator.RequestCurrentInputPort();
        }        

        private void Communicator_OnInputSignalPresenceListReceived(object sender, EventArgs<IEnumerable<IRouterPort>> e)
        {
            foreach (var port in _inputPorts)
                port.PortIsSignalPresent = e.Item.FirstOrDefault(param => param.PortID == port.PortID).PortIsSignalPresent;

            _semaphoreSignalPresence.Release();
        }

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<IEnumerable<IRouterPort>> e)
        {
            var changedIn = e.Item.Where(p => _outputPorts.Any(q => q.PortID == p.PortID)).FirstOrDefault();

            if (changedIn == null)
                return;

            SelectedInputPort = InputPorts.FirstOrDefault(param => param.PortID == changedIn.PortID);
        }

        public new void Dispose()
        {
            base.Dispose();
            cTokenSourceSignalPresence.Cancel();
            _routerCommunicator.OnInputPortsListReceived -= Communicator_OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnInputSignalPresenceListReceived -= Communicator_OnInputSignalPresenceListReceived;
            _routerCommunicator.Dispose();
            Debug.WriteLine("Router Plugin Disposed");
        }        
    }
}
