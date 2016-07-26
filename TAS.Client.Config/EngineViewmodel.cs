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
        private bool _enableGPIForNewEvents;
        private ulong _instance;
        public EngineViewmodel(Model.Engine engine)
            : base(engine, new EngineView())
        {
            _channels = new List<object>() { TAS.Client.Common.Properties.Resources._none_ };
            Model.Servers.ForEach(s => s.Channels.ForEach(c => _channels.Add(c)));
            _channelPRI = _channels.FirstOrDefault(c => c is Model.CasparServerChannel 
                                                        && ((Model.CasparServerChannel)c).ChannelNumber == Model.ServerChannelPRI 
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerPRI);
            if (_channelPRI == null) _channelPRI = _channels.First();
            _channelSEC = _channels.FirstOrDefault(c => c is Model.CasparServerChannel
                                                        && ((Model.CasparServerChannel)c).ChannelNumber == Model.ServerChannelSEC
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerSEC);
            if (_channelSEC == null) _channelSEC = _channels.First();
            _channelPRV = _channels.FirstOrDefault(c => c is Model.CasparServerChannel
                                                        && ((Model.CasparServerChannel)c).ChannelNumber == Model.ServerChannelPRV
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerPRV);
            _archiveDirectories = new List<object>() { TAS.Client.Common.Properties.Resources._none_ };
            _archiveDirectories.AddRange(Model.ArchiveDirectories.Directories);
            _archiveDirectory = engine.IdArchive == 0 ? _archiveDirectories.First() : _archiveDirectories.FirstOrDefault(d => (d is Model.ArchiveDirectory) && ((Model.ArchiveDirectory)d).idArchive == engine.IdArchive);
            if (_channelPRV == null) _channelPRV = _channels.First();
            if (Model.Gpi != null)
            {
                _gpiEnabled = true;
                _gpiAddress = Model.Gpi.Address;
                _gpiGraphicsStartDelay = Model.Gpi.GraphicsStartDelay;
            }
            if (Model.Remote != null)
            {
                _remoteHostEnabled = true;
                _remoteHostEndpointAddress = Model.Remote.EndpointAddress;
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

        public string EngineName { get { return _engineName; } set { SetField(ref _engineName, value, nameof(EngineName)); } }
        public TAspectRatioControl AspectRatioControl { get { return _aspectRatioControl; } set { SetField(ref _aspectRatioControl, value, nameof(AspectRatioControl)); } }
        public int TimeCorrection { get { return _timeCorrection; } set { SetField(ref _timeCorrection, value, nameof(TimeCorrection)); } }
        public TVideoFormat VideoFormat { get { return _videoFormat; } set { SetField(ref _videoFormat, value, nameof(VideoFormat)); } }
        public ulong Instance { get { return _instance; } set { SetField(ref _instance, value, nameof(Instance)); } }
        public double VolumeReferenceLoudness { get { return _volumeReferenceLoudness; } set { SetField(ref _volumeReferenceLoudness, value, nameof(VolumeReferenceLoudness)); } }
        public bool EnableGPIForNewEvents { get { return _enableGPIForNewEvents; } set { SetField(ref _enableGPIForNewEvents, value, nameof(EnableGPIForNewEvents)); } }


        readonly List<object> _channels;
        public List<object> Channels { get { return _channels; } }
        private object _channelPRI;
        public object ChannelPRI { get { return _channelPRI; } set { SetField(ref _channelPRI, value, nameof(ChannelPRI)); } }
        private object _channelSEC;
        public object ChannelSEC { get { return _channelSEC; } set { SetField(ref _channelSEC, value, nameof(ChannelSEC)); } }
        private object _channelPRV;
        public object ChannelPRV { get { return _channelPRV; } set { SetField(ref _channelPRV, value, nameof(ChannelPRV)); } }
        private List<object> _archiveDirectories;
        public List<object> ArchiveDirectories { get { return _archiveDirectories; } }
        private object _archiveDirectory;
        public object ArchiveDirectory { get { return _archiveDirectory; } set { SetField(ref _archiveDirectory, value, nameof(ArchiveDirectory)); } }
        private bool _gpiEnabled;
        public bool GpiEnabled { get { return _gpiEnabled; } set { SetField(ref _gpiEnabled, value, nameof(GpiEnabled)); } }
        private string _gpiAddress;
        public string GpiAddress { get { return _gpiAddress; } set { SetField(ref _gpiAddress, value, nameof(GpiAddress)); } }
        private int _gpiGraphicsStartDelay;
        public int GpiGraphicsStartDelay { get { return _gpiGraphicsStartDelay; } set { SetField(ref _gpiGraphicsStartDelay, value, nameof(GpiGraphicsStartDelay)); } }
        private bool _remoteHostEnabled;
        public bool RemoteHostEnabled { get { return _remoteHostEnabled; } set { SetField(ref _remoteHostEnabled, value, nameof(RemoteHostEnabled)); } }
        private string _remoteHostEndpointAddress;
        public string RemoteHostEndpointAddress { get { return _remoteHostEndpointAddress; } set { SetField(ref _remoteHostEndpointAddress, value, nameof(RemoteHostEndpointAddress)); } }

        public UICommand CommandManageArchiveDirectories { get; private set; }


        public override void ModelUpdate(object destObject = null)
        {
            if (IsModified)
            {
                var playoutServerChannelPRI = _channelPRI as Model.CasparServerChannel;
                Model.IdServerPRI = playoutServerChannelPRI == null ? 0 : ((Model.CasparServer)playoutServerChannelPRI.Owner).Id;
                Model.ServerChannelPRI = playoutServerChannelPRI == null ? 0 : playoutServerChannelPRI.ChannelNumber;
                var playoutServerChannelSEC = _channelSEC as Model.CasparServerChannel;
                Model.IdServerSEC = playoutServerChannelSEC == null ? 0 : ((Model.CasparServer)playoutServerChannelSEC.Owner).Id;
                Model.ServerChannelSEC = playoutServerChannelSEC == null ? 0 : playoutServerChannelSEC.ChannelNumber;
                var playoutServerChannelPRV = _channelPRV as Model.CasparServerChannel;
                Model.IdServerPRV = playoutServerChannelPRV == null ? 0 : ((Model.CasparServer)playoutServerChannelPRV.Owner).Id;
                Model.ServerChannelPRV = playoutServerChannelPRV == null ? 0 : playoutServerChannelPRV.ChannelNumber;
                Model.Gpi = _gpiEnabled ? new Model.Gpi() { Address = this.GpiAddress, GraphicsStartDelay = this.GpiGraphicsStartDelay } : null;
                Model.Remote = _remoteHostEnabled ? new Model.RemoteHost() { EndpointAddress = RemoteHostEndpointAddress } : null;
                Model.IdArchive = _archiveDirectory is Model.ArchiveDirectory ? ((Model.ArchiveDirectory)_archiveDirectory).idArchive : 0;
                Model.IsModified = true;
            }
            base.ModelUpdate(destObject);
        }
    }
}
