using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Client.Config.ViewModels.ArchiveDirectories;
using TAS.Client.Config.ViewModels.Plugins;
using TAS.Common;

namespace TAS.Client.Config.ViewModels.Engines
{
    public class EngineViewModel : ModifyableViewModelBase
    {        
        private TAspectRatioControl _aspectRatioControl;
        private string _engineName;
        private int _timeCorrection;
        private TVideoFormat _videoFormat;
        private bool _enableCGElementsForNewEvents;
        private TCrawlEnableBehavior _crawlEnableBehavior;
        private int _cgStartDelay;
        private ulong _instance;
        private Engine _engine;
        private PluginsViewModel _pluginsViewModel;

        public EngineViewModel(Engine engine)
        {
            _engine = engine;
            _pluginsViewModel = new PluginsViewModel(_engine);
            _pluginsViewModel.PluginChanged += PluginsViewModel_PluginChanged;
            CommandManageArchiveDirectories = new Common.UiCommand(_manageArchiveDirectories);
            PluginManagerCommand = new UiCommand(OpenPluginManager, CanOpenPluginManager);
            Init();
        }

        private void PluginsViewModel_PluginChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

        public void Init()
        {
            Channels = new List<object> { Common.Properties.Resources._none_ };
            foreach (var s in _engine.Servers)
                foreach (var c in s.Channels)
                    Channels.Add(c);
            _channelPRI = Channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((CasparServerChannel)c).Id == _engine.ServerChannelPRI
                                                        && ((CasparServer)((CasparServerChannel)c).Owner).Id == _engine.IdServerPRI);
            if (_channelPRI == null) _channelPRI = Channels.First();
            _channelSEC = Channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((CasparServerChannel)c).Id == _engine.ServerChannelSEC
                                                        && ((CasparServer)((CasparServerChannel)c).Owner).Id == _engine.IdServerSEC);
            if (_channelSEC == null) _channelSEC = Channels.First();
            _channelPRV = Channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((CasparServerChannel)c).Id == _engine.ServerChannelPRV
                                                        && ((CasparServer)((CasparServerChannel)c).Owner).Id == _engine.IdServerPRV);

            ArchiveDirectories = new List<object> { Common.Properties.Resources._none_ };
            ArchiveDirectories.AddRange(_engine.ArchiveDirectories.Directories);
            _archiveDirectory = _engine.IdArchive == 0 ? ArchiveDirectories.First() : ArchiveDirectories.FirstOrDefault(d => (d as ArchiveDirectory)?.IdArchive == _engine.IdArchive);
            if (_channelPRV == null) _channelPRV = Channels.First();
            if (_engine.Remote != null)
            {
                _remoteHostEnabled = true;
                _remoteHostListenPort = _engine.Remote.ListenPort;
            }
            
            CGStartDelay = _engine.CGStartDelay;
            CrawlEnableBehavior = _engine.CrawlEnableBehavior;
            StudioMode = _engine.StudioMode;
            EnableCGElementsForNewEvents = _engine.EnableCGElementsForNewEvents;
            Instance = _engine.Instance;
            VideoFormat = _engine.VideoFormat;
            TimeCorrection = _engine.TimeCorrection;
            AspectRatioControl = _engine.AspectRatioControl;
            EngineName = _engine.EngineName;
            IsModified = false;
            _engine.IsModified = false;
        }

        private bool CanOpenPluginManager(object obj)
        {
            return _pluginsViewModel.HasPlugins;
        }

        private void OpenPluginManager(object obj)
        {                                   
            WindowManager.Current.ShowDialog(_pluginsViewModel);                        
        }

        private void _manageArchiveDirectories(object obj)
        {
            using (var vm = new ArchiveDirectoriesViewmodel(_engine.ArchiveDirectories))
            {
                if (WindowManager.Current.ShowDialog(vm, "Archive") != true)
                    return;
                ArchiveDirectories = new List<object> { Common.Properties.Resources._none_ };
                ArchiveDirectories.AddRange(_engine.ArchiveDirectories.Directories);
                NotifyPropertyChanged(nameof(ArchiveDirectories));
                ArchiveDirectory = vm.SelectedDirectory;
            }
        }

        protected override void OnDispose() { }

        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public Array AspectRatioControls { get; } = Enum.GetValues(typeof(TAspectRatioControl));

        public Array CrawlEnableBehaviors { get; } = Enum.GetValues(typeof(TCrawlEnableBehavior));

        public Engine Engine => _engine;
        
        public string EngineName
        {
            get => _engineName;
            set => SetField(ref _engineName, value);
        }
        public TAspectRatioControl AspectRatioControl
        {
            get => _aspectRatioControl;
            set => SetField(ref _aspectRatioControl, value);
        }
        public int TimeCorrection
        {
            get => _timeCorrection;
            set => SetField(ref _timeCorrection, value);
        }

        public TVideoFormat VideoFormat
        {
            get => _videoFormat;
            set => SetField(ref _videoFormat, value);
        }
        public ulong Instance
        {
            get => _instance;
            set => SetField(ref _instance, value);
        }

        public bool EnableCGElementsForNewEvents
        {
            get => _enableCGElementsForNewEvents;
            set => SetField(ref _enableCGElementsForNewEvents, value);
        }

        public bool StudioMode { get => _studioMode; set => SetField(ref _studioMode, value); }

        public TCrawlEnableBehavior CrawlEnableBehavior
        {
            get => _crawlEnableBehavior;
            set => SetField(ref _crawlEnableBehavior, value);
        }

        public int CGStartDelay
        {
            get => _cgStartDelay;
            set => SetField(ref _cgStartDelay, value);
        }
        public List<object> Channels { get; private set; }

        private object _channelPRI;
        public object ChannelPRI
        {
            get => _channelPRI;
            set => SetField(ref _channelPRI, value);
        }

        private object _channelSEC;
        public object ChannelSEC
        {
            get => _channelSEC;
            set => SetField(ref _channelSEC, value);
        }

        private object _channelPRV;

        public object ChannelPRV
        {
            get => _channelPRV;
            set => SetField(ref _channelPRV, value);
        }
        public List<object> ArchiveDirectories { get; private set; }

        private object _archiveDirectory;
        public object ArchiveDirectory
        {
            get => _archiveDirectory;
            set => SetField(ref _archiveDirectory, value);
        }

        private bool _remoteHostEnabled;
        public bool RemoteHostEnabled
        {
            get => _remoteHostEnabled;
            set => SetField(ref _remoteHostEnabled, value);
        }

        private ushort _remoteHostListenPort;
        private bool _studioMode;

        public ushort RemoteHostListenPort
        {
            get => _remoteHostListenPort;
            set => SetField(ref _remoteHostListenPort, value);
        }

        public Common.UiCommand CommandManageArchiveDirectories { get; }
        public UiCommand PluginManagerCommand { get; }        

        public void Save()
        {
            _pluginsViewModel.Save();

            if (IsModified)
            {
                var playoutServerChannelPRI = _channelPRI as CasparServerChannel;
                _engine.IdServerPRI = ((CasparServer)playoutServerChannelPRI?.Owner)?.Id ?? 0;
                _engine.ServerChannelPRI = playoutServerChannelPRI?.Id ?? 0;
                var playoutServerChannelSEC = _channelSEC as CasparServerChannel;
                _engine.IdServerSEC = ((CasparServer)playoutServerChannelSEC?.Owner)?.Id ?? 0;
                _engine.ServerChannelSEC = playoutServerChannelSEC?.Id ?? 0;
                var playoutServerChannelPRV = _channelPRV as CasparServerChannel;
                _engine.IdServerPRV = ((CasparServer)playoutServerChannelPRV?.Owner)?.Id ?? 0;
                _engine.ServerChannelPRV = playoutServerChannelPRV?.Id ?? 0;
                _engine.Remote = _remoteHostEnabled ? new RemoteHost { ListenPort = RemoteHostListenPort } : null;
                _engine.IdArchive = (_archiveDirectory as ArchiveDirectory)?.IdArchive ?? 0;                
            }
            _engine.CGStartDelay = CGStartDelay;
            _engine.CrawlEnableBehavior = CrawlEnableBehavior;
            _engine.StudioMode = StudioMode;
            _engine.EnableCGElementsForNewEvents = EnableCGElementsForNewEvents;
            _engine.Instance = Instance;
            _engine.VideoFormat = VideoFormat;
            _engine.TimeCorrection = TimeCorrection;
            _engine.AspectRatioControl = AspectRatioControl;
            _engine.EngineName = EngineName;
            _engine.IsModified = true;            
        }               
    }
}
