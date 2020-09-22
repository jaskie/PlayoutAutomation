using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using System.Linq;
using jNet.RPC.Server;
using TAS.Server.VideoSwitch.Model;
using TAS.Server.VideoSwitch.Communicators;
using jNet.RPC;
using TAS.Database.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Threading.Tasks;

namespace TAS.Server.VideoSwitch
{	
    [DtoClass(nameof(IVideoSwitch))]
    public class VideoSwitch : ServerObjectBase, IVideoSwitch, IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IRouterCommunicator _communicator;
        private IVideoSwitchPort _selectedInputPort;
        private bool _isConnected;        

        public event EventHandler Started;

        public VideoSwitch(VideoSwitchType type = VideoSwitchType.Unknown)
        {
            Type = type;
            switch (type)
            {
                case VideoSwitchType.Nevion:
                    _communicator = new NevionCommunicator(this);
                    Preload = true;
                    break;
                case VideoSwitchType.BlackmagicSmartVideoHub:
                    _communicator = new BlackmagicSmartVideoHubCommunicator(this);
                    Preload = true;
                    break;
                case VideoSwitchType.Atem:                    
                    _communicator = new AtemCommunicator(this);                    
                    Preload = false;
                    break;
                case VideoSwitchType.Ross:
                    _communicator = new RossCommunicator(this);
                    Preload = false;
                    break;
                default:
                    return;
            }
            _communicator.OnInputPortChangeReceived += Communicator_OnInputPortChangeReceived;
            _communicator.OnRouterPortsStatesReceived += Communicator_OnRouterPortStateReceived;
            _communicator.OnRouterConnectionStateChanged += Communicator_OnRouterConnectionStateChanged;            
        }
       
        #region Configuration
        
        [Hibernate]
        public bool IsEnabled { get; set; }

        [Hibernate]
        public string IpAddress { get; set; }        
        [Hibernate]
        public VideoSwitchType Type { get; set; }
        [Hibernate]
        public int Level { get; set; }
        [Hibernate]
        public string Login { get; set; }
        [Hibernate]
        public string Password { get; set; }
        [Hibernate]
        public short[] OutputPorts { get; set; }
        
        public bool Preload { get; }

        #endregion

        [DtoMember]
        public IVideoSwitchPort SelectedInputPort
        {
            get => _selectedInputPort;
            set => SetField(ref _selectedInputPort, value);
        }

        [DtoMember, Hibernate]
        public IList<IVideoSwitchPort> InputPorts { get; } = new List<IVideoSwitchPort>();

        [DtoMember]
        public bool IsConnected
        {
            get => _isConnected;
            private set => SetField(ref _isConnected, value);
        }

        [Hibernate]
        public VideoSwitchEffect DefaultEffect { get; set; }
        [Hibernate]
        public PortInfo GpiPort { get; set; }

        public void SelectInput(int inPort)
        {
            _communicator.SelectInput(inPort);
        }

        public async Task<bool> ConnectAsync()
        {
            if (_communicator == null)
                return false;

            try
            {
                IsConnected = await _communicator.Connect();
                if (IsConnected)
                {
                    ParseInputMeta(await _communicator.GetInputPorts());
                    return true;
                }                                   
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return false;
        }

        private async void ParseInputMeta(PortInfo[] ports)
        {
            if (ports == null)
                return;

            foreach (var port in ports)
            {
                if (InputPorts.FirstOrDefault(inPort => inPort.PortId == port.Id && inPort.PortName != port.Name) is RouterPort foundPort)
                    foundPort.PortName = port.Name;
                else if (InputPorts.All(inPort => inPort.PortId != port.Id))
                    InputPorts.Add(new RouterPort(port.Id, port.Name));
            }

            NotifyPropertyChanged(nameof(InputPorts));            
            var selectedInput = await _communicator.GetCurrentInputPort();

            if (selectedInput == null)
            {
                //ParseInputMeta(ports);
                return;
            }                

            if (SelectedInputPort == null || SelectedInputPort.PortId != selectedInput.InPort)            
                SelectedInputPort = InputPorts.FirstOrDefault(port => port.PortId == selectedInput.InPort);
        }

        private void Communicator_OnRouterConnectionStateChanged(object sender, EventArgs<bool> e)
        {
            IsConnected = e.Value;
            if (e.Value)
                return;            

            _ = ConnectAsync();           
        }        

        private void Communicator_OnRouterPortStateReceived(object sender, EventArgs<PortState[]> e)
        {
            foreach (var port in InputPorts)
                ((RouterPort)port).IsSignalPresent = e.Value?.FirstOrDefault(param => param.PortId == port.PortId)?.IsSignalPresent;
        }

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<CrosspointInfo> e)
        {
            if (Type != VideoSwitchType.Ross && Type != VideoSwitchType.Atem)
            {
                if (OutputPorts.Length == 0)
                    return;

                var port = OutputPorts[0];
                var changedIn = e.Value.OutPort == port ? e.Value : null;
                if (changedIn == null)
                    return;

                SelectedInputPort = InputPorts.FirstOrDefault(param => param.PortId == changedIn.InPort);
            }
            else
            {
                if (e.Value.InPort == GpiPort?.Id)
                    Started?.Invoke(this, EventArgs.Empty);

                SelectedInputPort = InputPorts.FirstOrDefault(param => param.PortId == e.Value.InPort);
            }
            
        }

        protected override void DoDispose()
        {
            _communicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _communicator.OnRouterPortsStatesReceived -= Communicator_OnRouterPortStateReceived;
            _communicator.OnRouterConnectionStateChanged -= Communicator_OnRouterConnectionStateChanged;
            _communicator.Dispose();
        }

        internal void GpiStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        public void SetTransitionEffect(VideoSwitchEffect videoSwitchEffect)
        {
            ((IVideoSwitchCommunicator)_communicator).SetTransitionEffect(videoSwitchEffect);
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum VideoSwitchType
        {
            Nevion,
            BlackmagicSmartVideoHub,
            Atem,
            Ross,
            Unknown
        }
    }
}
