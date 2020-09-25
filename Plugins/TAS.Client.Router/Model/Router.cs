using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using System.Linq;
using TAS.Server.VideoSwitch.Communicators;
using jNet.RPC;
using TAS.Database.Common;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TAS.Server.VideoSwitch.Model
{	    
    public class Router : RouterBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private IVideoSwitchPort _selectedInputPort;
          

        public event EventHandler Started;
        public event PropertyChangedEventHandler PropertyChanged;

        public Router(CommunicatorType type = CommunicatorType.Unknown) : base(type)
        {            
            Communicator.SourceChanged += Communicator_OnInputPortChangeReceived;                                  
        }
       
        

        [DtoMember]
        public IVideoSwitchPort SelectedSource
        {
            get => _selectedInputPort;
            private set
            {
                if (value == _selectedInputPort)
                    return;
                _selectedInputPort = value;
                NotifyPropertyChanged();                
            }
        }

        [DtoMember, Hibernate]
        public IList<IVideoSwitchPort> Sources { get; } = new List<IVideoSwitchPort>();

        [DtoMember]
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (value == _isConnected)
                    return;
                _isConnected = value;
                NotifyPropertyChanged();
            }
        }             

        public void SetSource(int inPort)
        {
            Communicator.SetSource(inPort);
        }

        public async Task<bool> ConnectAsync()
        {
            if (Communicator == null)
                return false;

            try
            {
                IsConnected = await Communicator.ConnectAsync();
                if (IsConnected)
                {
                    SetupDevice(Communicator.Sources);
                    return true;
                }                                   
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return false;
        }

        private async void SetupDevice(PortInfo[] ports)
        {
            if (ports == null)
                return;

            foreach (var port in ports)
            {
                if (Sources.FirstOrDefault(inPort => inPort.PortId == port.Id && inPort.PortName != port.Name) is RouterPort foundPort)
                    foundPort.PortName = port.Name;
                else if (Sources.All(inPort => inPort.PortId != port.Id))
                    Sources.Add(new RouterPort(port.Id, port.Name));
            }

            NotifyPropertyChanged(nameof(Sources));            
            var selectedInput = await Communicator.GetSelectedSource();

            if (selectedInput == null)
            {
                //ParseInputMeta(ports);
                return;
            }                

            if (SelectedSource == null || SelectedSource.PortId != selectedInput.InPort)            
                SelectedSource = Sources.FirstOrDefault(port => port.PortId == selectedInput.InPort);
        }          

        //private void Communicator_OnRouterPortStateReceived(object sender, EventArgs<PortState[]> e)
        //{
        //    foreach (var port in Sources)
        //        ((RouterPort)port).IsSignalPresent = e.Value?.FirstOrDefault(param => param.PortId == port.PortId)?.IsSignalPresent;
        //}

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<CrosspointInfo> e)
        {
            if (Type != CommunicatorType.Ross && Type != CommunicatorType.Atem)
            {
                if (OutputPorts.Length == 0)
                    return;

                var port = OutputPorts[0];
                var changedIn = e.Value.OutPort == port ? e.Value : null;
                if (changedIn == null)
                    return;

                SelectedSource = Sources.FirstOrDefault(param => param.PortId == changedIn.InPort);
            }
            else
            {
                if (e.Value.InPort == GpiPort?.Id)
                    Started?.Invoke(this, EventArgs.Empty);

                SelectedSource = Sources.FirstOrDefault(param => param.PortId == e.Value.InPort);
            }            
        }        

        internal void GpiStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);            
        }
      

        

        public void Dispose()
        {
            Communicator.SourceChanged -= Communicator_OnInputPortChangeReceived;
            Communicator.ConnectionChanged -= Communicator_OnRouterConnectionStateChanged;            
            Communicator.Dispose();
        }
    }
}
