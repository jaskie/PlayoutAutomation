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

namespace TAS.Server.Router
{
    public class RouterController : IRouter, IRouterPortState
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();       

        private IRouterCommunicator _routerCommunicator;
        private RouterDevice _device;
        private IEnumerable<RouterPort> _inputPorts;
        private IEnumerable<RouterPort> _outputPorts;        
        private SemaphoreSlim _semaphoreSignalPresence = new SemaphoreSlim(0);
        private CancellationTokenSource cTokenSourceSignalPresence = new CancellationTokenSource();
                
        public event EventHandler<RouterEventArgs> OnInputPortChange;
        public event EventHandler<RouterEventArgs> OnInputSignalPresenceListReceived;
        public event EventHandler<RouterEventArgs> OnInputPortListChange;    

        #region IRouterPortState
        private int _inputID;

        public int InputID
        {
            get => _inputID;
            set => _inputID = value;
        }
        #endregion

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

       

        private void Init()
        {
            if (!Connect())
                return;
           
            _routerCommunicator.OnOutputPortsListReceived += Communicator_OnOutputPortsListReceived;
            _routerCommunicator.OnInputPortsListReceived += Communicator_OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived += Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnInputSignalPresenceListReceived += Communicator_OnInputSignalPresenceListReceived;
                               
            _routerCommunicator.RequestOutputPorts();                        
        }

        private void Communicator_OnOutputPortsListReceived(object sender, RouterEventArgs e)
        {
            _outputPorts = e.RouterPorts.Where(param => _device.OutputPorts.Contains(param.ID));
        }

        private void KeepUpdatingSignalPresence()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (cTokenSourceSignalPresence.IsCancellationRequested)
                        throw new OperationCanceledException(cTokenSourceSignalPresence.Token);

                    RequestSignalPresenceStates();
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

        public void RequestInputPorts()
        {
            _routerCommunicator.RequestInputPorts();
        }

        private void Communicator_OnInputPortsListReceived(object sender, RouterEventArgs e)
        {
            if (_inputPorts == null)
            {
                _inputPorts = new List<RouterPort>(e.RouterPorts);
                OnInputPortListChange?.Invoke(this, new RouterEventArgs(e.RouterPorts.ToList()));
                Task.Run(() => KeepUpdatingSignalPresence(), cTokenSourceSignalPresence.Token);
            }
            else
            {
                foreach(var port in _inputPorts)
                {
                    if (e.RouterPorts.FirstOrDefault(param => param.ID == port.ID && param.Name == port.Name) != null)
                        continue;
                    OnInputPortListChange?.Invoke(this, new RouterEventArgs(e.RouterPorts.ToList()));
                    break;
                }
            }
            
        }

        public void RequestSignalPresenceStates()
        {
            _routerCommunicator.RequestSignalPresence();
        }

        private void Communicator_OnInputSignalPresenceListReceived(object sender, RouterEventArgs e)
        {
            OnInputSignalPresenceListReceived?.Invoke(this, new RouterEventArgs(e.RouterPorts.ToList()));
            _semaphoreSignalPresence.Release();
        }

        public void RequestCurrentInputPort()
        {
            _routerCommunicator.RequestCurrentInputPort();
        }

        private void Communicator_OnInputPortChangeReceived(object sender, RouterEventArgs e)
        {
            var changedIn = e.RouterPorts.Where(p => _outputPorts.Any(q => q.ID == p.ID)).FirstOrDefault();

            if (changedIn == null)
                return;

            OnInputPortChange?.Invoke(this, new RouterEventArgs(e.RouterPorts));
        }

        public void SwitchInput(IRouterPortState inPort)
        {
            _routerCommunicator.SwitchInput(new RouterPort(inPort.InputID), _outputPorts);
        }

        public void SwitchInput(RouterPort inPort)
        {
            _routerCommunicator.SwitchInput(inPort, _outputPorts);
        }             

        public void Dispose()
        {
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
