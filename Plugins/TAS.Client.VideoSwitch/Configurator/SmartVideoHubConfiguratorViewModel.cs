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
    public class SmartVideoHubConfiguratorViewModel : ConfiguratorViewModelBase<SmartVideoHub>
    {       
        private bool _preload;

        public SmartVideoHubConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            VideoSwitch.PropertyChanged += SmartVideoHub_PropertyChanged;
            Load();
        }

        public override string PluginName => "BMD Smart Video Hub router";

        private void SmartVideoHub_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IVideoSwitch.SelectedSource):
                    NotifyPropertyChanged(nameof(SelectedSource));
                    break;
                case nameof(IVideoSwitch.IsConnected):
                    NotifyPropertyChanged(nameof(IsConnected));
                    break;
                case nameof(SmartVideoHub.AllOutputs):
                    break;
            }
        }

        protected override bool CanConnect() => IpAddress?.Length > 0;

        protected override void Connect()
        {
            VideoSwitch.IpAddress = IpAddress;
            VideoSwitch.Outputs = OutputPorts.Select(p => p.Id).ToArray();
            VideoSwitch.Connect(CancellationToken.None);
        }

        protected override void Disconnect()
        {
            VideoSwitch.Disconnect();
        }

        public override void Load()
        {
            base.Load();
            if (!(VideoSwitch.Outputs is null))
                foreach (var source in VideoSwitch.Outputs.Select(p => new OutputPortViewModel(p, $"Port {p + 1}", true)))
                    OutputPorts.Add(source);
            IpAddress = VideoSwitch.IpAddress;
            Preload = VideoSwitch.Preload;
            IsModified = false;
        }

        protected override void OnDispose()
        {
            VideoSwitch.PropertyChanged -= SmartVideoHub_PropertyChanged;
            VideoSwitch.Dispose();
        }

        public override void Save()
        {
            base.Save();
            VideoSwitch.IpAddress = IpAddress;
            VideoSwitch.IsEnabled = IsEnabled;
            VideoSwitch.Preload = _preload;
            VideoSwitch.Outputs = OutputPorts.Where(p => p.IsSelected).Select(p => p.Id).ToArray();
            Engine.VideoSwitch = VideoSwitch;
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
            get => VideoSwitch?.SelectedSource;
            set
            {
                if (VideoSwitch?.SelectedSource == value)
                    return;
                VideoSwitch?.SetSource(value.Id);
            }
        }

        public override bool IsVideoSwitcher => false;
    }
}
