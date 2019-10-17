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
        private SemaphoreSlim _semaphoreInputList = new SemaphoreSlim(0);
       
        public event EventHandler<RouterEventArgs> OnInputPortChangeReceived;

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
                               
            _routerCommunicator.RequestOutputPorts();                        
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
                return _routerCommunicator.Connect(_device.IP, _device.Port).Result;
            }
            catch
            {
                return false;
            }
            
        }       
             
        public bool SwitchInput(IRouterPortState inPort)
        {
            return _routerCommunicator.SwitchInput(new RouterPort(inPort.InputID), _outputPorts);
        }

        public bool SwitchInput(RouterPort inPort)
        {
            return _routerCommunicator.SwitchInput(inPort, _outputPorts);
        }

        public async Task<IEnumerable<RouterPort>> GetInputPorts(bool requestCurrentInput = false)
        {
            _inputPorts = null;
            _routerCommunicator.RequestInputPorts();

            if (_inputPorts == null)
                await _semaphoreInputList.WaitAsync();
            
            if (requestCurrentInput == true)
                _routerCommunicator.RequestCurrentInputPort();

            return _inputPorts;
        }

        private void Communicator_OnOutputPortsListReceived(object sender, RouterEventArgs e)
        {                       
            _outputPorts = e.RouterPorts.Where(param => _device.OutputPorts.Contains(param.ID));            
        }

        private void Communicator_OnInputPortChangeReceived(object sender, RouterEventArgs e)
        {
            var changedIn = e.RouterPorts.Where(p => _outputPorts.Any(q => q.ID == p.ID)).FirstOrDefault();

            if (changedIn == null)
                return;

            OnInputPortChangeReceived?.Invoke(this, new RouterEventArgs(e.RouterPorts));
        }

        private void Communicator_OnInputPortsListReceived(object sender, RouterEventArgs e)
        {
            _inputPorts = new List<RouterPort>(e.RouterPorts);
            _semaphoreInputList.Release();            
        }

        public void Dispose()
        {
            _routerCommunicator.OnInputPortsListReceived -= Communicator_OnInputPortsListReceived;
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.Dispose();
            Debug.WriteLine("Router Plugin Disposed");
        }

        
    }
}
