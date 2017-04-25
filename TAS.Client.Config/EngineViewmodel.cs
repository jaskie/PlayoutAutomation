using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Config
{
    public class EngineViewmodel : EditViewmodelBase<Model.Engine>
    {
        private TAspectRatioControl _aspectRatioControl;
        private string _engineName;
        private int _timeCorrection;
        private TVideoFormat _videoFormat;
        private double _volumeReferenceLoudness;
        private bool _enableCGElementsForNewEvents;
        private TCrawlEnableBehavior _crawlEnableBehavior;
        private int _cgStartDelay;
        private ulong _instance;
        public EngineViewmodel(Model.Engine engine)
            : base(engine, new EngineView())
        {
            _channels = new List<object>() { TAS.Client.Common.Properties.Resources._none_ };
            Model.Servers.ForEach(s => s.Channels.ForEach(c => _channels.Add(c)));
            _channelPRI = _channels.FirstOrDefault(c => c is Model.CasparServerChannel 
                                                        && ((Model.CasparServerChannel)c).Id == Model.ServerChannelPRI 
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerPRI);
            if (_channelPRI == null) _channelPRI = _channels.First();
            _channelSEC = _channels.FirstOrDefault(c => c is Model.CasparServerChannel
                                                        && ((Model.CasparServerChannel)c).Id == Model.ServerChannelSEC
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerSEC);
            if (_channelSEC == null) _channelSEC = _channels.First();
            _channelPRV = _channels.FirstOrDefault(c => c is Model.CasparServerChannel
                                                        && ((Model.CasparServerChannel)c).Id == Model.ServerChannelPRV
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerPRV);

            _archiveDirectories = new List<object>() { TAS.Client.Common.Properties.Resources._none_ };
            _archiveDirectories.AddRange(Model.ArchiveDirectories.Directories);
            _archiveDirectory = engine.IdArchive == 0 ? _archiveDirectories.First() : _archiveDirectories.FirstOrDefault(d => (d is Model.ArchiveDirectory) && ((Model.ArchiveDirectory)d).idArchive == engine.IdArchive);
            if (_channelPRV == null) _channelPRV = _channels.First();
            if (Model.Remote != null)
            {
                _remoteHostEnabled = true;
                _remoteHostListenPort = Model.Remote.ListenPort;
            }
            CommandManageArchiveDirectories = new UICommand() { ExecuteDelegate = _manageArchiveDirectories };
        }

        private void _manageArchiveDirectories(object obj)
        {
            var dialog = new ArchiveDirectoriesViewmodel(Model.ArchiveDirectories);
            if (dialog.ShowDialog() == true)
            {
                _archiveDirectories = new List<object>() { TAS.Client.Common.Properties.Resources._none_ };
                _archiveDirectories.AddRange(Model.ArchiveDirectories.Directories);
                NotifyPropertyChanged(nameof(ArchiveDirectories));
                ArchiveDirectory = dialog.SelectedDirectory;
            }
        }

        protected override void OnDispose() { }

        static readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        static readonly Array _aspectRatioControls = Enum.GetValues(typeof(TAspectRatioControl));
        public Array AspectRatioControls { get { return _aspectRatioControls; } }

        static readonly Array _crawlEnableBehaviors = Enum.GetValues(typeof(TCrawlEnableBehavior));
        public Array CrawlEnableBehaviors { get { return _crawlEnableBehaviors; } }

        public string EngineName { get { return _engineName; } set { SetField(ref _engineName, value); } }
        public TAspectRatioControl AspectRatioControl { get { return _aspectRatioControl; } set { SetField(ref _aspectRatioControl, value); } }
        public int TimeCorrection { get { return _timeCorrection; } set { SetField(ref _timeCorrection, value); } }
        public TVideoFormat VideoFormat { get { return _videoFormat; } set { SetField(ref _videoFormat, value); } }
        public ulong Instance { get { return _instance; } set { SetField(ref _instance, value); } }
        public double VolumeReferenceLoudness { get { return _volumeReferenceLoudness; } set { SetField(ref _volumeReferenceLoudness, value); } }
        public bool EnableCGElementsForNewEvents { get { return _enableCGElementsForNewEvents; } set { SetField(ref _enableCGElementsForNewEvents, value); } }
        public TCrawlEnableBehavior CrawlEnableBehavior { get { return _crawlEnableBehavior; } set { SetField(ref _crawlEnableBehavior, value); } }
        public int CGStartDelay { get { return _cgStartDelay; } set { SetField(ref _cgStartDelay, value); } }

        readonly List<object> _channels;
        public List<object> Channels { get { return _channels; } }
        private object _channelPRI;
        public object ChannelPRI { get { return _channelPRI; } set { SetField(ref _channelPRI, value); } }
        private object _channelSEC;
        public object ChannelSEC { get { return _channelSEC; } set { SetField(ref _channelSEC, value); } }
        private object _channelPRV;
        public object ChannelPRV { get { return _channelPRV; } set { SetField(ref _channelPRV, value); } }

        private List<object> _archiveDirectories;
        public List<object> ArchiveDirectories { get { return _archiveDirectories; } }
        private object _archiveDirectory;
        public object ArchiveDirectory { get { return _archiveDirectory; } set { SetField(ref _archiveDirectory, value); } }
        private bool _remoteHostEnabled;
        public bool RemoteHostEnabled { get { return _remoteHostEnabled; } set { SetField(ref _remoteHostEnabled, value); } }
        private ushort _remoteHostListenPort;
        public ushort RemoteHostListenPort { get { return _remoteHostListenPort; } set { SetField(ref _remoteHostListenPort, value); } }

        public UICommand CommandManageArchiveDirectories { get; private set; }


        public override void ModelUpdate(object destObject = null)
        {
            if (IsModified)
            {
                var playoutServerChannelPRI = _channelPRI as Model.CasparServerChannel;
                Model.IdServerPRI = playoutServerChannelPRI == null ? 0 : ((Model.CasparServer)playoutServerChannelPRI.Owner).Id;
                Model.ServerChannelPRI = playoutServerChannelPRI == null ? 0 : playoutServerChannelPRI.Id;
                var playoutServerChannelSEC = _channelSEC as Model.CasparServerChannel;
                Model.IdServerSEC = playoutServerChannelSEC == null ? 0 : ((Model.CasparServer)playoutServerChannelSEC.Owner).Id;
                Model.ServerChannelSEC = playoutServerChannelSEC == null ? 0 : playoutServerChannelSEC.Id;
                var playoutServerChannelPRV = _channelPRV as Model.CasparServerChannel;
                Model.IdServerPRV = playoutServerChannelPRV == null ? 0 : ((Model.CasparServer)playoutServerChannelPRV.Owner).Id;
                Model.ServerChannelPRV = playoutServerChannelPRV == null ? 0 : playoutServerChannelPRV.Id;
                Model.Remote = _remoteHostEnabled ? new Model.RemoteHost() { ListenPort = RemoteHostListenPort } : null;
                Model.IdArchive = _archiveDirectory is Model.ArchiveDirectory ? ((Model.ArchiveDirectory)_archiveDirectory).idArchive : 0;
                Model.IsModified = true;
            }
            base.ModelUpdate(destObject);
        }
    }
}
