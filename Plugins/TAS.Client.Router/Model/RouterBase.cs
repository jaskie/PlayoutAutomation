using jNet.RPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.VideoSwitch.Communicators;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public abstract class RouterBase : IRouter
    {
        #region Configuration        
        [Hibernate]
        public bool IsEnabled { get; set; }
        [Hibernate]
        public string IpAddress { get; set; }
        [Hibernate]
        public CommunicatorType Type { get; set; }
        [Hibernate]
        public int Level { get; set; }
        [Hibernate]
        public string Login { get; set; }
        [Hibernate]
        public string Password { get; set; }
        [Hibernate]
        public short[] OutputPorts { get; set; }
        [Hibernate]
        public bool Preload { get; set; }                
        [DtoMember, Hibernate]
        public IList<IVideoSwitchPort> Sources { get; set; } = new List<IVideoSwitchPort>();
        #endregion

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _isConnected;
        private bool _isDisposed = false;
        private IVideoSwitchPort _selectedInputPort;
        
        public RouterBase(CommunicatorType type)
        {
            Type = type;
            switch (type)
            {
                case CommunicatorType.Nevion:
                    Communicator = new NevionCommunicator(this);
                    break;
                case CommunicatorType.BlackmagicSmartVideoHub:
                    Communicator = new BlackmagicSmartVideoHubCommunicator(this);
                    break;
                case CommunicatorType.Atem:
                    Communicator = new AtemCommunicator(this);
                    break;
                case CommunicatorType.Ross:
                    Communicator = new RossCommunicator(this);
                    break;
                default:
                    return;
            }
            Communicator.ConnectionChanged += Communicator_ConnectionChanged;
        }
        private void Communicator_ConnectionChanged(object sender, EventArgs<bool> e)
        {
            IsConnected = e.Value;
            if (e.Value)
                return;

            _ = ConnectAsync();
        }                

        private async void SetupDevice(PortInfo[] ports)
        {
            if (ports == null)
            {
                if (Sources.Count == 0)
                    return;

                ports = new PortInfo[Sources.Count];
                for (int i = 0; i < Sources.Count; ++i)
                    ports[i] = new PortInfo(Sources[i].PortId, Sources[i].PortName);
            }
                

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
                return;
            

            if (SelectedSource == null || SelectedSource.PortId != selectedInput.InPort)
                SelectedSource = Sources.FirstOrDefault(port => port.PortId == selectedInput.InPort);
        }

        protected IRouterCommunicator Communicator { get; private set; }                

        [DtoMember]
        public IVideoSwitchPort SelectedSource
        {
            get => _selectedInputPort;
            protected set
            {
                if (value == _selectedInputPort)
                    return;
                _selectedInputPort = value;
                NotifyPropertyChanged();
            }
        }        

        [DtoMember]
        public bool IsConnected 
        { 
            get => _isConnected; 
            set
            {
                if (_isConnected == value)
                    return;
                _isConnected = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Started;

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

        public void RaiseGpiStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }        

        public void SetSource(int sourceId)
        {
            Communicator.SetSource(sourceId);
        }

        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                Communicator.ConnectionChanged -= Communicator_ConnectionChanged;
                Communicator.Dispose();
            }
            
            _isDisposed = true;
        }         

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
