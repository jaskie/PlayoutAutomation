using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
        private readonly Ross _ross;
        private bool _isConnecting;

        public RossConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _ross = engine.VideoSwitch as Ross ?? new Ross();
            _ross.PropertyChanged += Ross_PropertyChanged;
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
            Ports.Remove(port);
        }

        private void AddOutputPort(object obj)
        {
            var lastItem = Ports.LastOrDefault();
            Ports.Add(new PortInfo((short)(lastItem == null ? 0 : lastItem.Id + 1), string.Empty));
            IsModified = true;
        }

        protected override bool CanConnect() => IpAddress?.Length > 0 && !_isConnecting && !IsConnected;

        protected async override void Connect()
        {
            _isConnecting = true;
            _ross.IpAddress = IpAddress;
            InvalidateRequerySuggested();
            await Task.Run(() => _ross.Connect(CancellationToken.None));
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
            Ports.Clear();
            foreach (var source in _ross.Sources.Select(p => new PortInfo(p.Id, p.Name)))
                Ports.Add(source);
            _selectedGpiSource = Ports.FirstOrDefault(p => p.Id == _ross.GpiPort?.Id);
            NotifyPropertyChanged(nameof(SelectedGpiSource));
            IsModified = false;
        }

        public override void Save()
        {
            base.Save();
            _ross.IpAddress = IpAddress;
            _ross.Preload = Preload;
            _ross.DefaultEffect = SelectedTransitionType;
            _ross.GpiPort = SelectedGpiSource;
            _ross.Sources = Ports.Cast<IVideoSwitchPort>().ToList();
            Engine.VideoSwitch = _ross;
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
        public ObservableCollection<PortInfo> Ports { get; } = new ObservableCollection<PortInfo>();
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

        public PortInfo SelectedSource
        {
            get => Ports.FirstOrDefault(p => p.Id == _ross.SelectedSource?.Id);
            set
            {
                if (_ross?.SelectedSource?.Id == value.Id)
                    return;
                _ross?.SetSource(value.Id);
            }
        }

    }
}
