using jNet.RPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public abstract class RouterBase : jNet.RPC.Server.ServerObjectBase, IVideoSwitch
    {
        #region Configuration        
        [Hibernate]
        public bool IsEnabled { get; set; }
        [Hibernate]
        public string IpAddress { get; set; }
        [Hibernate]
        public string Login { get; set; }
        [Hibernate]
        public string Password { get; set; }
        [Hibernate]
        public short[] OutputPorts { get; set; }
        [Hibernate]
        public bool Preload { get; set; }                
        [DtoMember, Hibernate]
        public List<IVideoSwitchPort> Sources { get; set; } = new List<IVideoSwitchPort>();
        #endregion

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IVideoSwitchPort _selectedInputPort;
        private IRouterCommunicator _communicator;
        
        private void SetupDevice(PortInfo[] ports)
        {
            if (ports == null)
            {
                if (Sources.Count == 0)
                    return;

                ports = new PortInfo[Sources.Count];
                for (int i = 0; i < Sources.Count; ++i)
                    ports[i] = new PortInfo(Sources[i].Id, Sources[i].Name);
            }                

            foreach (var port in ports)
            {
                if (Sources.FirstOrDefault(inPort => inPort.Id == port.Id && inPort.Name != port.Name) is RouterPort foundPort)
                    foundPort.Name = port.Name;
                else if (Sources.All(inPort => inPort.Id != port.Id))
                    Sources.Add(new RouterPort(port.Id, port.Name));
            }

            NotifyPropertyChanged(nameof(Sources));
            var selectedInput = Communicator.GetSelectedSource();

            if (selectedInput == null)                            
                return;
            

            if (SelectedSource == null || SelectedSource.Id != selectedInput.InPort)
                SelectedSource = Sources.FirstOrDefault(port => port.Id == selectedInput.InPort);
        }

        protected IRouterCommunicator Communicator { get
            {
                if (_communicator is null)
                {
                    _communicator = CreateCommunicator();
                    _communicator.SourceChanged += Communicator_SourceChanged;
                    _communicator.PropertyChanged += Communicator_PropertyChanged;
                }
                return _communicator;
            }
        }

        private void Communicator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IRouterCommunicator.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    break;
            }
        }

        protected abstract void Communicator_SourceChanged(object sender, EventArgs<CrosspointInfo> e);

        protected abstract IRouterCommunicator CreateCommunicator();

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
        public bool IsConnected => _communicator?.IsConnected ?? false;

        public event EventHandler Started;

        public bool Connect()
        {
            try
            {
                Communicator.Connect(IpAddress);
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

        public void Disconnect()
        {
            Communicator.Disconnect();
            NotifyPropertyChanged(nameof(IsConnected));
        }

        public void RaiseGpiStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }        

        public void SetSource(int sourceId)
        {
            Communicator.SetSource(sourceId);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            if (_communicator is null)
                return;
            _communicator.SourceChanged -= Communicator_SourceChanged;
            _communicator.PropertyChanged -= Communicator_PropertyChanged;
            _communicator.Dispose();
        }

    }
}
