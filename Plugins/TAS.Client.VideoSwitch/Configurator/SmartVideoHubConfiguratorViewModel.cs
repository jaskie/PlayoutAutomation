using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public class SmartVideoHubConfiguratorViewModel : ConfiguratorViewModelBase
    {       
        private string _ipAddress;
        private bool _preload;
        private List<PortInfo> _ports;
        private readonly SmartVideoHub _smartVideoHub;

        public SmartVideoHubConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _smartVideoHub = engine.VideoSwitch as SmartVideoHub ?? new SmartVideoHub();
            _smartVideoHub.PropertyChanged += SmartVideoHub_PropertyChanged;
            CommandAddPort = new UiCommand(AddOutputPort, CanAddPort);
            CommandDeletePort = new UiCommand(DeleteOutputPort);
            Load();
        }

        public override string PluginName => "BMD Smart Video Hub router";

        public override IPlugin Model => _smartVideoHub;

        private void SmartVideoHub_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IVideoSwitch.SelectedSource))
                NotifyPropertyChanged(nameof(SelectedSource));
            else if (e.PropertyName == nameof(IVideoSwitch.IsConnected))
                NotifyPropertyChanged(nameof(IsConnected));
        }

        private void DeleteOutputPort(object obj)
        {
            if (!(obj is PortInfo port))
                return;
            _ports.Remove(port);
            Ports.Refresh();
        }

        private bool CanAddPort(object obj)
        {
            return IsEnabled;
        }

        private void AddOutputPort(object obj)
        {
            var lastItem = _ports.LastOrDefault();
            _ports.Add(new PortInfo((short)(lastItem == null ? 0 : lastItem.Id + 1), String.Empty));
            IsModified = true;
            Ports.Refresh();
        }


        protected override bool CanConnect(object obj)
        {
            if (IpAddress?.Length > 0)
                return true;

            return false;
        }

        protected override void Connect(object obj)
        {
            _smartVideoHub.IpAddress = IpAddress;
            _smartVideoHub.OutputPorts = _ports.Select(p => p.Id).ToArray();
            _smartVideoHub.Connect();
        }

        protected override void Disconnect(object obj)
        {
            base.Disconnect(obj);
        }

        public override void Load()
        {
            _ports = new List<PortInfo>();
            Ports = CollectionViewSource.GetDefaultView(_ports);

            IpAddress = _smartVideoHub.IpAddress;
            Preload = _smartVideoHub.Preload;
            if (_smartVideoHub.OutputPorts != null)
                foreach (var port in _smartVideoHub.OutputPorts)
                    _ports.Add(new PortInfo(port, null));
            Ports.Refresh();

            IsModified = false;
        }

        protected override void OnDispose()
        {
            _smartVideoHub.PropertyChanged -= SmartVideoHub_PropertyChanged;
        }

        public override void Save()
        {
            _smartVideoHub.IpAddress = _ipAddress;
            _smartVideoHub.IsEnabled = IsEnabled;
            _smartVideoHub.Preload = _preload;
            _smartVideoHub.OutputPorts = _ports.Select(p => p.Id).ToArray();
            Engine.VideoSwitch = _smartVideoHub;
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && _ipAddress?.Length > 0)
                return true;

            return false;
        }

        public UiCommand CommandAddPort { get; }
        public UiCommand CommandDeletePort { get; }
        public ICollectionView Ports { get; private set; }        
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }
        public bool Preload { get => _preload; set => SetField(ref _preload, value); }
        public IVideoSwitchPort SelectedSource
        {
            get => _smartVideoHub?.SelectedSource;
            set
            {
                if (_smartVideoHub?.Sources == value)
                    return;

                if (value == null)
                    return;
                _smartVideoHub?.SetSource(value.PortId);
            }
        }
    }
}
