using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public class RossConfiguratorViewModel : ConfiguratorViewModelBase
    {
        private string _ipAddress;
        private bool _preload;
        private VideoSwitcherTransitionStyle? _selectedTransitionType;
        private PortInfo _selectedGpiSource;
        private List<PortInfo> _ports;
        private List<PortInfo> _gpiSources;
        private readonly Ross _ross;

        public RossConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _ross = engine.VideoSwitch as Ross ?? new Ross();
            _ross.PropertyChanged += Ross_PropertyChanged;
            CommandRefreshSources = new UiCommand(RefreshGpiSources, CanRefreshGpiSources);
            CommandAddPort = new UiCommand(AddOutputPort, CanAddPort);
            CommandDeletePort = new UiCommand(DeleteOutputPort);
            Load();
        }

        public override string PluginName => "Ross video switcher";

        public override IPlugin Model => _ross;

        private void Ross_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private bool CanRefreshGpiSources(object obj)
        {
            if (_ports.Count > 0)
                return true;
            return false;
        }

        private void RefreshGpiSources(object obj)
        {
            _gpiSources = new List<PortInfo>()
            {
                new PortInfo(-1, "None")
            };

            foreach (var port in _ports)
                _gpiSources.Add(new PortInfo(port.Id, port.Name));

            GpiSources = CollectionViewSource.GetDefaultView(_gpiSources);
            NotifyPropertyChanged(nameof(GpiSources));

//            _selectedGpiSource = _gpiSources.FirstOrDefault(p => p.Id == _ross?. .GpiPort?.Id);
            NotifyPropertyChanged(nameof(SelectedGpiSource));
        }

        protected override bool CanConnect(object obj)
        {
            if (_ross == null && IpAddress?.Length > 0)
                return true;

            return false;
        }

        protected override void Connect(object obj)
        {
        }

        protected override void Disconnect(object obj)
        {
            base.Disconnect(obj);
        }

        public override void Load()
        {
            _ports = new List<PortInfo>();
            Ports = CollectionViewSource.GetDefaultView(_ports);

            _gpiSources = new List<PortInfo>()
            {
                new PortInfo(-1, "None")
            };
            GpiSources = CollectionViewSource.GetDefaultView(_gpiSources);

            SelectedGpiSource = _gpiSources.FirstOrDefault();

            IsModified = false;
        }

        protected override void OnDispose()
        {

        }

        public override void Save()
        {
            Engine.VideoSwitch = _ross;
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && _ipAddress?.Length > 0 && _selectedTransitionType != null)
                return true;

            return false;
        }

        public UiCommand CommandRefreshSources { get; }
        public UiCommand CommandAddPort { get; }
        public UiCommand CommandDeletePort { get; }        
        public ICollectionView Ports { get; private set; }
        public ICollectionView GpiSources { get; private set; }
        public VideoSwitcherTransitionStyle? SelectedTransitionType { get => _selectedTransitionType; set => SetField(ref _selectedTransitionType, value); }
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }
        public PortInfo SelectedGpiSource { get => _selectedGpiSource; set => SetField(ref _selectedGpiSource, value); }
        public List<VideoSwitcherTransitionStyle> TransitionTypes { get; } = new List<VideoSwitcherTransitionStyle>()
        {
            VideoSwitcherTransitionStyle.VFade,
            VideoSwitcherTransitionStyle.FadeAndTake,
            VideoSwitcherTransitionStyle.Mix,
            VideoSwitcherTransitionStyle.TakeAndFade,
            VideoSwitcherTransitionStyle.Cut,
            VideoSwitcherTransitionStyle.WipeLeft,
            VideoSwitcherTransitionStyle.WipeTop,
            VideoSwitcherTransitionStyle.WipeReverseLeft,
            VideoSwitcherTransitionStyle.WipeReverseTop,
            VideoSwitcherTransitionStyle.TakeAndFade,
            VideoSwitcherTransitionStyle.UFade
        };

        public bool Preload { get => _preload; set => SetField(ref _preload, value); }
        public IVideoSwitchPort SelectedSource
        {
            get => _ross.SelectedSource;
            set
            {
                if (_ross?.SelectedSource == value)
                    return;
                _ross?.SetSource(value.PortId);
            }
        }

    }
}
