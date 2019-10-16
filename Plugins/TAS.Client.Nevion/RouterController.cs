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

namespace TAS.Server.Router
{
    public class RouterController : IRouter, IRouterPortState
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public UserControl View { get; }

        private IRouterCommunicator _routerCommunicator;
        private RouterDevice _device;
        private IEnumerable<RouterPort> _inputPorts;
        private IEnumerable<RouterPort> _outputPorts;

        public event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnInputPortChangeReceived;

        #region IRouterPortState
        private byte _inputID;

        public byte InputID
        {
            get => _inputID;
            set => _inputID = value;
        }
        #endregion

        public RouterController()
        {
            View = new RouterView();
            View.DataContext = this;
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

           
            _routerCommunicator.RequestInputPorts();           
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

        public async Task<IEnumerable<RouterPort>> GetInputPorts()
        {
            _inputPorts = null;
            _routerCommunicator.RequestInputPorts();
            await Task.Run(() => { while (_inputPorts == null) ; });
            return _inputPorts;
        }

        private void Communicator_OnOutputPortsListReceived(object sender, RouterEventArgs e)
        {
            _routerCommunicator.RequestCurrentInputPort();
        }

        private void Communicator_OnInputPortChangeReceived(object sender, RouterEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Communicator_OnInputPortsListReceived(object sender, RouterEventArgs e)
        {
            _inputPorts = new List<RouterPort>(e.RouterPorts);
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
