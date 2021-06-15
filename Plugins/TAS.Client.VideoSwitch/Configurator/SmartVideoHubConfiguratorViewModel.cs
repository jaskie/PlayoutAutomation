using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public class SmartVideoHubConfiguratorViewModel : ConfiguratorViewModelBase
    {       
        private bool _preload;
        private readonly SmartVideoHub _smartVideoHub;

        public SmartVideoHubConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _smartVideoHub = engine.VideoSwitch as SmartVideoHub ?? new SmartVideoHub();
            _smartVideoHub.PropertyChanged += SmartVideoHub_PropertyChanged;
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

        protected override bool CanConnect() => IpAddress?.Length > 0;

        protected override void Connect()
        {
            _smartVideoHub.IpAddress = IpAddress;
            _smartVideoHub.OutputPorts = Ports.Select(p => p.Id).ToArray();
            _smartVideoHub.Connect(CancellationToken.None);
        }

        protected override void Disconnect()
        {
            _smartVideoHub.Disconnect();
        }

        public override void Load()
        {
            base.Load();
            Ports.Clear();
            foreach (var source in _smartVideoHub.Sources.Select(p => new PortInfo(p.Id, p.Name)))
                Ports.Add(source);
            IpAddress = _smartVideoHub.IpAddress;
            Preload = _smartVideoHub.Preload;
            IsModified = false;
        }

        protected override void OnDispose()
        {
            _smartVideoHub.PropertyChanged -= SmartVideoHub_PropertyChanged;
            _smartVideoHub.Dispose();
        }

        public override void Save()
        {
            base.Save();
            _smartVideoHub.IpAddress = IpAddress;
            _smartVideoHub.IsEnabled = IsEnabled;
            _smartVideoHub.Preload = _preload;
            _smartVideoHub.OutputPorts = Ports.Select(p => p.Id).ToArray();
            Engine.VideoSwitch = _smartVideoHub;
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && IpAddress?.Length > 0)
                return true;

            return false;
        }

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
                _smartVideoHub?.SetSource(value.Id);
            }
        }
    }
}
