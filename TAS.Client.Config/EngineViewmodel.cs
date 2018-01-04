using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Config.Model;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Config
{
    public class EngineViewmodel : Common.EditViewmodelBase<Model.Engine>, IEngineProperties
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
            : base(engine)
        {
            _channels = new List<object>() { Common.Properties.Resources._none_ };
            Model.Servers.ForEach(s => s.Channels.ForEach(c => _channels.Add(c)));
            _channelPRI = _channels.FirstOrDefault(c => c is CasparServerChannel 
                                                        && ((Model.CasparServerChannel)c).Id == Model.ServerChannelPRI 
                                                        && ((Model.CasparServer)((Model.CasparServerChannel)c).Owner).Id == Model.IdServerPRI);
            if (_channelPRI == null) _channelPRI = _channels.First();
            _channelSEC = _channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((Model.CasparServerChannel)c).Id == Model.ServerChannelSEC
                                                        && ((Model.CasparServer)((Model.CasparServerChannel)c).Owner).Id == Model.IdServerSEC);
            if (_channelSEC == null) _channelSEC = _channels.First();
            _channelPRV = _channels.FirstOrDefault(c => c is CasparServerChannel
                                                        && ((Model.CasparServerChannel)c).Id == Model.ServerChannelPRV
                                                        && ((Model.CasparServer)((Model.CasparServerChannel)c).Owner).Id == Model.IdServerPRV);

            _archiveDirectories = new List<object> { Common.Properties.Resources._none_ };
            _archiveDirectories.AddRange(Model.ArchiveDirectories.Directories);
            _archiveDirectory = engine.IdArchive == 0 ? _archiveDirectories.First() : _archiveDirectories.FirstOrDefault(d =>  (d as ArchiveDirectory)?.idArchive == engine.IdArchive);
            if (_channelPRV == null) _channelPRV = _channels.First();
            if (Model.Remote != null)
            {
                _remoteHostEnabled = true;
                _remoteHostListenPort = Model.Remote.ListenPort;
            }
            CommandManageArchiveDirectories = new Common.UICommand { ExecuteDelegate = _manageArchiveDirectories };
        }

        private void _manageArchiveDirectories(object obj)
        {
            using (var dialog = new ArchiveDirectoriesViewmodel(Model.ArchiveDirectories))
            {
                dialog.Load();
                if (dialog.ShowDialog() != true)
                    return;
                _archiveDirectories = new List<object> {Common.Properties.Resources._none_};
                _archiveDirectories.AddRange(Model.ArchiveDirectories.Directories);
                NotifyPropertyChanged(nameof(ArchiveDirectories));
                ArchiveDirectory = dialog.SelectedDirectory;
            }
        }

        protected override void OnDispose() { }

        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public Array AspectRatioControls { get; } = Enum.GetValues(typeof(TAspectRatioControl));

        public Array CrawlEnableBehaviors { get; } = Enum.GetValues(typeof(TCrawlEnableBehavior));

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
        public List<object> Channels => _channels;
        private object _channelPRI;
        public object ChannelPRI { get { return _channelPRI; } set { SetField(ref _channelPRI, value); } }
        private object _channelSEC;
        public object ChannelSEC { get { return _channelSEC; } set { SetField(ref _channelSEC, value); } }
        private object _channelPRV;
        public object ChannelPRV { get { return _channelPRV; } set { SetField(ref _channelPRV, value); } }

        private List<object> _archiveDirectories;
        public List<object> ArchiveDirectories => _archiveDirectories;
        private object _archiveDirectory;
        public object ArchiveDirectory { get { return _archiveDirectory; } set { SetField(ref _archiveDirectory, value); } }
        private bool _remoteHostEnabled;
        public bool RemoteHostEnabled { get { return _remoteHostEnabled; } set { SetField(ref _remoteHostEnabled, value); } }
        private ushort _remoteHostListenPort;
        public ushort RemoteHostListenPort { get { return _remoteHostListenPort; } set { SetField(ref _remoteHostListenPort, value); } }

        public Common.UICommand CommandManageArchiveDirectories { get; }


        public override void Update(object destObject = null)
        {
            if (IsModified)
            {
                var playoutServerChannelPRI = _channelPRI as Model.CasparServerChannel;
                Model.IdServerPRI = ((Model.CasparServer) playoutServerChannelPRI?.Owner)?.Id ?? 0;
                Model.ServerChannelPRI = playoutServerChannelPRI?.Id ?? 0;
                var playoutServerChannelSEC = _channelSEC as Model.CasparServerChannel;
                Model.IdServerSEC = ((Model.CasparServer) playoutServerChannelSEC?.Owner)?.Id ?? 0;
                Model.ServerChannelSEC = playoutServerChannelSEC?.Id ?? 0;
                var playoutServerChannelPRV = _channelPRV as Model.CasparServerChannel;
                Model.IdServerPRV = ((Model.CasparServer) playoutServerChannelPRV?.Owner)?.Id ?? 0;
                Model.ServerChannelPRV = playoutServerChannelPRV?.Id ?? 0;
                Model.Remote = _remoteHostEnabled ? new Model.RemoteHost { ListenPort = RemoteHostListenPort } : null;
                Model.IdArchive = ((Model.ArchiveDirectory)_archiveDirectory)?.idArchive ?? 0;
                Model.IsModified = true;
            }
            base.Update(destObject);
        }
    }
}
