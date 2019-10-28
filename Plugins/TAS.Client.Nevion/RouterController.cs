using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TAS.Server.Router.Helpers;
using TAS.Server.Router.Model;
using TAS.Server.Router.RouterCommunicators;
using TAS.Common;
using TAS.Common.Interfaces;
using System.Threading;
using System.Linq;
using TAS.Remoting.Server;
using Newtonsoft.Json;

namespace TAS.Server
{
    public class RouterController : DtoBase, IRouter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();       

        private IRouterCommunicator _routerCommunicator;
        private RouterDevice _device;

        private Task _keepAliveTask;      
        
        private IEnumerable<IRouterPort> _outputPorts;        
        private SemaphoreSlim _semaphoreRouterState = new SemaphoreSlim(0);
        private CancellationTokenSource cTokenSourceRouterState = new CancellationTokenSource();

        private IRouterPort _selectedInputPort;
        [JsonProperty]
        public IRouterPort SelectedInputPort
        {
            get => _selectedInputPort;
            set => SetField(ref _selectedInputPort, value);
        }

        private IList<IRouterPort> _inputPorts;        
        [JsonProperty]
        public IList<IRouterPort> InputPorts
        {
            get => _inputPorts;
            set => SetField(ref _inputPorts, value);
        }

        public RouterController(RouterDevice device)
        {
            _device = device;
            _ = Init();     
        }

        public void SelectInput(int inPort)
        {
            _routerCommunicator.SelectInput(inPort);
        }

        private async Task Init()
        {
            var isConnected = await Connect().ConfigureAwait(false);
            if (!isConnected)
                return;
           
            _routerCommunicator.OnOutputPortsListReceived += Communicator_OnOutputPortsListReceived;
            _routerCommunicator.OnInputPortsListReceived += Communicator_OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived += Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnRouterStateReceived += Communicator_OnRouterStateReceived;
            _routerCommunicator.OnRouterConnectionStateChanged += _routerCommunicator_OnRouterConnectionStateChanged;
            
            _routerCommunicator.RequestInputPorts();
            _routerCommunicator.RequestOutputPorts();                        
        }

        private void _routerCommunicator_OnRouterConnectionStateChanged(object sender, EventArgs<bool> e)
        {
            if (e.Item) 
                return;

            InputPorts = null;
            SelectedInputPort = null;
            if (!cTokenSourceRouterState.IsCancellationRequested)
                _ = Init();
        }

        private void Communicator_OnOutputPortsListReceived(object sender, EventArgs<IEnumerable<IRouterPort>> e)
        {
            _outputPorts = e.Item.Where(param => _device.OutputPorts.Contains(param.PortID));
        }

        private async Task KeepUpdatingRouterState()
        {
            while (true)
            {
                try
                {
                    if (cTokenSourceRouterState.IsCancellationRequested)
                        throw new OperationCanceledException(cTokenSourceRouterState.Token);

                    _routerCommunicator.RequestRouterState();
                    await _semaphoreRouterState.WaitAsync(cTokenSourceRouterState.Token);
                    await Task.Delay(3000);
                }
                catch (OperationCanceledException cancelEx)
                {
                    Debug.WriteLine("RouterStateUpdater task canceled.");
                    break;
                }
            }
        }

        private async Task<bool> Connect()
        {        
            try
            {
                switch (_device.Type)
                {
                    case Router.Model.Enums.Router.Nevion:
                        {
                            Debug.WriteLine("Nevion communicator registered");
                            _routerCommunicator = new NevionCommunicator(_device);
                            break;
                        }
                    case Router.Model.Enums.Router.Blackmagic:
                        {
                            _routerCommunicator = new BlackmagicSVHCommunicator(_device);
                            break;
                        }
                    default:
                        return false;
                }
                var isConnected = await _routerCommunicator.Connect().ConfigureAwait(false);
                return isConnected;
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
                _keepAliveTask = KeepUpdatingRouterState();
            }
            else
            {
                foreach(var port in e.Item)
                {
                    if (_inputPorts.FirstOrDefault(inPort => inPort.PortID == port.PortID && inPort.PortName == port.PortName) != null)
                        continue;
                    else if (_inputPorts.FirstOrDefault(inPort => inPort.PortID == port.PortID && inPort.PortName != port.PortName) != null)
                        _inputPorts.FirstOrDefault(inPort => inPort.PortID == port.PortID).PortName = port.PortName;
                    else if (_inputPorts.FirstOrDefault(inPort => inPort.PortID == port.PortID) == null)
                        _inputPorts.Add(port);                                      
                }
            }
            _routerCommunicator.RequestCurrentInputPort();
        }        

        private void Communicator_OnRouterStateReceived(object sender, EventArgs<IEnumerable<IRouterPort>> e)
        {
            foreach (var port in _inputPorts)
                port.PortIsSignalPresent = e.Item?.FirstOrDefault(param => param.PortID == port.PortID).PortIsSignalPresent;

            _semaphoreRouterState.Release();
        }

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<IEnumerable<Crosspoint>> e)
        {
            var changedIn = e.Item.Where(p => _outputPorts.Any(q => q.PortID == p.OutPort.PortID)).FirstOrDefault();

            if (changedIn == null)
                return;

            SelectedInputPort = InputPorts.FirstOrDefault(param => param.PortID == changedIn.InPort.PortID);
        }

        protected override void DoDispose()
        {
            base.Dispose();
            cTokenSourceRouterState.Cancel();
            _routerCommunicator.OnInputPortsListReceived -= Communicator_OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnRouterStateReceived -= Communicator_OnRouterStateReceived;
            _routerCommunicator.OnRouterConnectionStateChanged -= _routerCommunicator_OnRouterConnectionStateChanged;

            _keepAliveTask?.Wait();
            _routerCommunicator?.Dispose();
            Debug.WriteLine("Router Plugin Disposed");
        }        
    }
}
