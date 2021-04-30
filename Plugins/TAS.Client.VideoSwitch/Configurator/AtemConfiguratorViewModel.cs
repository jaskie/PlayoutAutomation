using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    internal class AtemConfiguratorViewModel : ConfiguratorViewModelBase
    {
        private string _ipAddress;
        private bool _preload;
        private RouterBase _gpiRouter;
        private VideoSwitcherTransitionStyle? _selectedTransitionType;
        private PortInfo _selectedGpiSource;
        private List<PortInfo> _gpiSources;

        public AtemConfiguratorViewModel(VideoSwitcher videoSwitcher) : base(videoSwitcher)
        {
            CommandRefreshSources = new UiCommand(RefreshGpiSources, CanRefreshGpiSources);
        }

        private void TestRouter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IVideoSwitch.SelectedSource))
                NotifyPropertyChanged(nameof(SelectedTestSource));
            else if (e.PropertyName == nameof(IVideoSwitch.Sources))
                NotifyPropertyChanged(nameof(TestSources));
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
            if (_gpiRouter != null)
                _gpiRouter.Dispose();


            _gpiSources = new List<PortInfo>()
            {
                new PortInfo(-1, "None")
            };

            var testThread = new Thread(new ThreadStart(() =>
            {
                _gpiRouter = new VideoSwitcher(CommunicatorType.Atem) { IpAddress = _ipAddress };
                if (!_gpiRouter.Connect())
                    return;              

                foreach (var port in _gpiRouter.Sources)
                    _gpiSources.Add(new PortInfo(port.PortId, port.PortName));

                GpiSources = CollectionViewSource.GetDefaultView(_gpiSources);
                _gpiRouter.Dispose();
                NotifyPropertyChanged(nameof(GpiSources));
            }));
            testThread.SetApartmentState(ApartmentState.MTA);
            testThread.Name = "Gpi Router Thread";
            testThread.IsBackground = true;
            testThread.Start();

            _selectedGpiSource = _gpiSources.FirstOrDefault(p => p.Id == ((VideoSwitcher)Router)?.GpiPort?.Id);
            NotifyPropertyChanged(nameof(SelectedGpiSource));
        }

        protected override bool CanConnect(object obj)
        {
            if (TestRouter == null && IpAddress?.Length > 0)
                return true;
            
            return false;
        }

        protected override void Connect(object obj)
        {
            if (_gpiRouter != null)
                _gpiRouter.Dispose();

            TestRouter = new VideoSwitcher(CommunicatorType.Atem)
            {
                IpAddress = IpAddress,                
                DefaultEffect = _selectedTransitionType ?? VideoSwitcherTransitionStyle.Cut
            };            

            TestRouter.PropertyChanged += TestRouter_PropertyChanged;
            TestRouter.Connect();            
        }

        protected override void Disconnect(object obj)
        {
            TestRouter.PropertyChanged -= TestRouter_PropertyChanged;
            base.Disconnect(obj);
        }

        protected override void Init()
        {
            _gpiSources = new List<PortInfo>()
            {
                new PortInfo(-1, "None")
            };
            GpiSources = CollectionViewSource.GetDefaultView(_gpiSources);

            IpAddress = null;
            SelectedTransitionType = null;
            SelectedGpiSource = _gpiSources.FirstOrDefault();
            Preload = false;

            if (Router == null)
            {
                IsModified = false;
                return;
            }

            Preload = Router.Preload;
            IpAddress = Router.IpAddress;
            SelectedTransitionType = TransitionTypes.FirstOrDefault(r => r == ((VideoSwitcher)Router).DefaultEffect);

            if (((VideoSwitcher)Router).GpiPort != null)
                _gpiSources.Add(((VideoSwitcher)Router).GpiPort);
            GpiSources.Refresh();
            SelectedGpiSource = _gpiSources.FirstOrDefault(p => ((VideoSwitcher)Router)?.GpiPort?.Id == p.Id) ?? _gpiSources.First();            

            IsModified = false;
        }

        protected override void OnDispose()
        {
            
        }

        public override void Save()
        {
            Router = new VideoSwitcher
            {
                Type = CommunicatorType.Atem,
                IsEnabled = IsEnabled,
                DefaultEffect = _selectedTransitionType ?? VideoSwitcherTransitionStyle.Cut,
                IpAddress = _ipAddress,
                GpiPort = _selectedGpiSource?.Id != -1 ? _selectedGpiSource : null,
                Preload = _preload
            };
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && _ipAddress?.Length > 0 && _selectedTransitionType != null)
                return true;
            return false;
        }

        public UiCommand CommandRefreshSources { get; }
        public ICollectionView GpiSources { get; private set; }
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
        public IVideoSwitchPort SelectedTestSource
        {
            get => TestRouter?.SelectedSource;
            set
            {
                if (TestRouter?.Sources == value)
                    return;

                if (value == null)
                    return;

                TestRouter?.SetSource(value.PortId);
            }
        }

        
    }
}
