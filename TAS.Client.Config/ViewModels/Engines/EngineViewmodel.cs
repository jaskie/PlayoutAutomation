using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Client.Config.ViewModels.ArchiveDirectories;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Engines
{
    public class EngineViewmodel : Common.EditViewModelBase<Engine>, IEngineProperties
    {        
        private TAspectRatioControl _aspectRatioControl;
        private string _engineName;
        private int _timeCorrection;
        private TVideoFormat _videoFormat;
        private bool _enableCGElementsForNewEvents;
        private TCrawlEnableBehavior _crawlEnableBehavior;
        private int _cgStartDelay;
        private ulong _instance;
        public EngineViewmodel(Engine engine) : base(engine)
        {            
            Channels = new List<object> { Common.Properties.Resources._none_ };
            foreach(var s in Model.Servers)
                foreach (var c in s.Channels)
                    Channels.Add(c);
            _channelPRI = Channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((CasparServerChannel)c).Id == Model.ServerChannelPRI
                                                        && ((CasparServer)((CasparServerChannel)c).Owner).Id == Model.IdServerPRI);
            if (_channelPRI == null) _channelPRI = Channels.First();
            _channelSEC = Channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((CasparServerChannel)c).Id == Model.ServerChannelSEC
                                                        && ((CasparServer)((CasparServerChannel)c).Owner).Id == Model.IdServerSEC);
            if (_channelSEC == null) _channelSEC = Channels.First();
            _channelPRV = Channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((CasparServerChannel)c).Id == Model.ServerChannelPRV
                                                        && ((CasparServer)((CasparServerChannel)c).Owner).Id == Model.IdServerPRV);

            ArchiveDirectories = new List<object> { Common.Properties.Resources._none_ };
            ArchiveDirectories.AddRange(Model.ArchiveDirectories.Directories);
            _archiveDirectory = engine.IdArchive == 0 ? ArchiveDirectories.First() : ArchiveDirectories.FirstOrDefault(d => (d as ArchiveDirectory)?.IdArchive == engine.IdArchive);
            if (_channelPRV == null) _channelPRV = Channels.First();
            if (Model.Remote != null)
            {
                _remoteHostEnabled = true;
                _remoteHostListenPort = Model.Remote.ListenPort;
            }
            CommandManageArchiveDirectories = new Common.UiCommand(_manageArchiveDirectories);
        }

        private void _manageArchiveDirectories(object obj)
        {
            using (var vm = new ArchiveDirectoriesViewmodel(Model.ArchiveDirectories))
            {
                if (UiServices.WindowManager.ShowDialog(vm, "Archive") != true)
                    return;
                ArchiveDirectories = new List<object> { Common.Properties.Resources._none_ };
                ArchiveDirectories.AddRange(Model.ArchiveDirectories.Directories);
                NotifyPropertyChanged(nameof(ArchiveDirectories));
                ArchiveDirectory = vm.SelectedDirectory;
            }
        }

        protected override void OnDispose() { }

        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public Array AspectRatioControls { get; } = Enum.GetValues(typeof(TAspectRatioControl));

        public Array CrawlEnableBehaviors { get; } = Enum.GetValues(typeof(TCrawlEnableBehavior));

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
        public List<object> Channels { get; }

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

        protected override void Update(object destObject = null)
        {
            if (IsModified)
            {
                var playoutServerChannelPRI = _channelPRI as CasparServerChannel;
                Model.IdServerPRI = ((CasparServer)playoutServerChannelPRI?.Owner)?.Id ?? 0;
                Model.ServerChannelPRI = playoutServerChannelPRI?.Id ?? 0;
                var playoutServerChannelSEC = _channelSEC as CasparServerChannel;
                Model.IdServerSEC = ((CasparServer)playoutServerChannelSEC?.Owner)?.Id ?? 0;
                Model.ServerChannelSEC = playoutServerChannelSEC?.Id ?? 0;
                var playoutServerChannelPRV = _channelPRV as CasparServerChannel;
                Model.IdServerPRV = ((CasparServer)playoutServerChannelPRV?.Owner)?.Id ?? 0;
                Model.ServerChannelPRV = playoutServerChannelPRV?.Id ?? 0;
                Model.Remote = _remoteHostEnabled ? new RemoteHost { ListenPort = RemoteHostListenPort } : null;
                Model.IdArchive = (_archiveDirectory as ArchiveDirectory)?.IdArchive ?? 0;
                Model.IsModified = true;
            }
            base.Update(destObject);
        }

        public void Save()
        {
            Update(Model);
        }
    }
}
