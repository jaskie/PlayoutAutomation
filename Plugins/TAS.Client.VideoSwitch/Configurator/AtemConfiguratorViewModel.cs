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
    public class AtemConfiguratorViewModel : ConfiguratorViewModelBase
    {
        private string _ipAddress;
        private bool _preload;
        private VideoSwitcherTransitionStyle? _selectedTransitionType;
        private PortInfo _selectedGpiSource;
        private List<PortInfo> _sources;
        private readonly Atem _atem;
        
        public AtemConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _atem = engine.VideoSwitch as Atem ?? new Atem();
            _atem.PropertyChanged += Atem_PropertyChanged;
            CommandRefreshSources = new UiCommand(RefreshGpiSources, CanRefreshGpiSources);
            Load();
        }

        public override string PluginName => "BMD Atem switcher";

        public override IPlugin Model => _atem;

        private void Atem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IVideoSwitch.SelectedSource))
                NotifyPropertyChanged(nameof(SelectedSource));
            else if (e.PropertyName == nameof(IVideoSwitch.Sources))
                NotifyPropertyChanged(nameof(Sources));
            else if (e.PropertyName == nameof(IVideoSwitch.IsConnected))
                NotifyPropertyChanged(nameof(IsConnected));
        }

        private bool CanRefreshGpiSources(object obj)
        {
            if (_ipAddress?.Length > 0)
                return true;
            return false;
        }

        private void RefreshGpiSources(object obj)
        {

            _sources = new List<PortInfo>()
            {
                new PortInfo(-1, "None")
            };

            //var testThread = new Thread(new ThreadStart(() =>
            //{
            //    _gpiRouter = new VideoSwitcher(CommunicatorType.Atem) { IpAddress = _ipAddress };
            //    if (!_gpiRouter.Connect())
            //        return;              

            //    foreach (var port in _gpiRouter.Sources)
            //        _gpiSources.Add(new PortInfo(port.PortId, port.PortName));

            //    GpiSources = CollectionViewSource.GetDefaultView(_gpiSources);
            //    _gpiRouter.Dispose();
            //    NotifyPropertyChanged(nameof(GpiSources));
            //}));
            //testThread.SetApartmentState(ApartmentState.MTA);
            //testThread.Name = "Gpi Router Thread";
            //testThread.IsBackground = true;
            //testThread.Start();

            //_selectedGpiSource = _gpiSources.FirstOrDefault(p => p.Id == ((VideoSwitcher)Router)?.GpiPort?.Id);
            //NotifyPropertyChanged(nameof(SelectedGpiSource));
        }

        protected override bool CanConnect(object obj)
        {
            if (IpAddress?.Length > 0)
                return true;
            
            return false;
        }

        protected override void Connect(object obj)
        {
            _atem.IpAddress = IpAddress;
            _atem.DefaultEffect = _selectedTransitionType ?? VideoSwitcherTransitionStyle.Cut;
            _atem.Connect();            
        }

        protected override void Disconnect(object obj)
        {
            _atem.Disconnect();
            base.Disconnect(obj);
        }

        public override void Load()
        {
            _sources = new List<PortInfo>()
            {
                new PortInfo(-1, "None")
            };
            Sources = CollectionViewSource.GetDefaultView(_sources);


            Preload = _atem.Preload;
            IpAddress = _atem.IpAddress;
            SelectedTransitionType = TransitionTypes.FirstOrDefault(r => r == _atem.DefaultEffect);

            Sources.Refresh();
            SelectedGpiSource = _sources.FirstOrDefault(p => _atem.GpiPort?.Id == p.Id) ?? _sources.First();            

            IsModified = false;
        }

        protected override void OnDispose()
        {
            
        }

        public override void Save()
        {
            _atem.IsEnabled = IsEnabled;
            _atem.DefaultEffect = _selectedTransitionType ?? VideoSwitcherTransitionStyle.Cut;
            _atem.IpAddress = _ipAddress;
            _atem.GpiPort = _selectedGpiSource?.Id != -1 ? _selectedGpiSource : null;
            _atem.Preload = _preload;
            Engine.VideoSwitch = _atem;
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && _ipAddress?.Length > 0 && _selectedTransitionType != null)
                return true;
            return false;
        }

        public UiCommand CommandRefreshSources { get; }
        public ICollectionView Sources { get; private set; }
        public VideoSwitcherTransitionStyle? SelectedTransitionType { get => _selectedTransitionType; set => SetField(ref _selectedTransitionType, value); }
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }
        public PortInfo SelectedGpiSource { get => _selectedGpiSource; set => SetField(ref _selectedGpiSource, value); }
        public List<VideoSwitcherTransitionStyle> TransitionTypes { get; set; } = new List<VideoSwitcherTransitionStyle>()
        {
                VideoSwitcherTransitionStyle.Mix,
                VideoSwitcherTransitionStyle.Wipe,
                VideoSwitcherTransitionStyle.Dip
        };
        
        
        public bool Preload { get => _preload; set => SetField(ref _preload, value); }
        public IVideoSwitchPort SelectedSource
        {
            get => _atem?.SelectedSource;
            set
            {
                if (_atem?.SelectedSource == value)
                    return;
                _atem?.SetSource(value.PortId);
            }
        }

        
    }
}
