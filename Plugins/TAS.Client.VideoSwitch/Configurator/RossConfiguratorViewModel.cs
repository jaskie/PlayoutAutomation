﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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
        private VideoSwitcherTransitionStyle _selectedTransitionType;
        private PortInfo _selectedGpiSource;
        private List<PortInfo> _ports;
        private List<PortInfo> _gpiSources;
        private readonly Ross _ross;
        private bool _isConnecting;

        public RossConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _ross = engine.VideoSwitch as Ross ?? new Ross();
            _ross.PropertyChanged += Ross_PropertyChanged;
            CommandRefreshSources = new UiCommand(RefreshGpiSources);
            CommandAddPort = new UiCommand(AddOutputPort);
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
            {
                NotifyPropertyChanged(nameof(IsConnected));
                InvalidateRequerySuggested();
            }
        }

        private void DeleteOutputPort(object obj)
        {
            if (!(obj is PortInfo port))
                return;
            _ports.Remove(port);
            Ports.Refresh();
        }

        private void AddOutputPort(object obj)
        {
            var lastItem = _ports.LastOrDefault();
            _ports.Add(new PortInfo((short)(lastItem == null ? 0 : lastItem.Id + 1), string.Empty));
            IsModified = true;
            Ports.Refresh();
        }

        private void RefreshGpiSources(object _ = null)
        {
            var gpiSources = new List<PortInfo>()
            {
                new PortInfo(-1, "None")
            };
            foreach (var port in _ports)
                gpiSources.Add(new PortInfo(port.Id, port.Name));
            GpiSources = gpiSources;
            _selectedGpiSource = GpiSources.FirstOrDefault(p => p.Id == _ross.GpiPort?.Id);
            NotifyPropertyChanged(nameof(SelectedGpiSource));
        }

        protected override bool CanConnect() => IpAddress?.Length > 0 && !_isConnecting && !IsConnected;

        protected async override void Connect()
        {
            _isConnecting = true;
            _ross.IpAddress = IpAddress;
            InvalidateRequerySuggested(); 
            await Task.Run(() =>
            {
                var connected  = _ross.Connect();
            });
            _isConnecting = false;
            InvalidateRequerySuggested();
        }

        protected override void Disconnect()
        {
            _ross.Disconnect();
        }

        protected override void OnDispose()
        {
            _ross.PropertyChanged -= Ross_PropertyChanged;
            _ross.Dispose();
        }

        public override void Load()
        {
            IpAddress = _ross.IpAddress;
            Preload = _ross.Preload;
            SelectedTransitionType = _ross.DefaultEffect;

            _ports = new List<PortInfo>(_ross.Sources.Select(p => new PortInfo(p.Id, p.Name)));
            Ports = CollectionViewSource.GetDefaultView(_ports);

            RefreshGpiSources();

            IsModified = false;
        }

        public override void Save()
        {
            base.Save();
            _ross.IpAddress = IpAddress;
            _ross.Preload = Preload;
            _ross.DefaultEffect = SelectedTransitionType;
            Engine.VideoSwitch = _ross;
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && _ipAddress?.Length > 0)
                return true;
            return false;
        }

        public UiCommand CommandRefreshSources { get; }
        public UiCommand CommandAddPort { get; }
        public UiCommand CommandDeletePort { get; }
        public ICollectionView Ports { get; private set; }
        public List<PortInfo> GpiSources { get => _gpiSources; private set => SetFieldNoModify(ref _gpiSources, value); }
        public VideoSwitcherTransitionStyle SelectedTransitionType { get => _selectedTransitionType; set => SetField(ref _selectedTransitionType, value); }
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }
        public PortInfo SelectedGpiSource { get => _selectedGpiSource; set => SetField(ref _selectedGpiSource, value); }
        public List<VideoSwitcherTransitionStyle> TransitionTypes { get; } = new List<VideoSwitcherTransitionStyle>()
        {
            VideoSwitcherTransitionStyle.Mix,
            VideoSwitcherTransitionStyle.Cut,
            VideoSwitcherTransitionStyle.VFade,
            VideoSwitcherTransitionStyle.UFade,
            VideoSwitcherTransitionStyle.FadeAndTake,
            VideoSwitcherTransitionStyle.TakeAndFade,
            VideoSwitcherTransitionStyle.WipeLeft,
            VideoSwitcherTransitionStyle.WipeTop,
            VideoSwitcherTransitionStyle.WipeReverseLeft,
            VideoSwitcherTransitionStyle.WipeReverseTop
        };

        public bool Preload { get => _preload; set => SetField(ref _preload, value); }
        public IVideoSwitchPort SelectedSource
        {
            get => _ross.SelectedSource;
            set
            {
                if (_ross?.SelectedSource == value)
                    return;
                _ross?.SetSource(value.Id);
            }
        }

    }
}
