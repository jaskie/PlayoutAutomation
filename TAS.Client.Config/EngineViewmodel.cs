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
        private ulong _instance;
        public EngineViewmodel(Model.Engine engine)
            : base(engine, new EngineView())
        {
            _channels = new List<object>() { TAS.Client.Common.Properties.Resources._none_ };
            Model.Servers.ForEach(s => s.Channels.ForEach(c => _channels.Add(c)));
            _channelPGM = _channels.FirstOrDefault(c => c is Model.CasparServerChannel 
                                                        && ((Model.CasparServerChannel)c).ChannelNumber == Model.ServerChannelPGM 
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerPGM);
            if (_channelPGM == null) _channelPGM = _channels.First();
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
                NotifyPropertyChanged("ArchiveDirectories");
                ArchiveDirectory = dialog.SelectedDirectory;
            }
        }

        protected override void OnDispose() { }

        readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        readonly Array _aspectRatioControls = Enum.GetValues(typeof(TAspectRatioControl));
        public Array AspectRatioControls { get { return _aspectRatioControls; } }

        public string EngineName { get { return _engineName; } set { SetField(ref _engineName, value, "EngineName"); } }
        public TAspectRatioControl AspectRatioControl { get { return _aspectRatioControl; } set { SetField(ref _aspectRatioControl, value, "AspectRatioControl"); } }
        public int TimeCorrection { get { return _timeCorrection; } set { SetField(ref _timeCorrection, value, "TimeCorrection"); } }
        public TVideoFormat VideoFormat { get { return _videoFormat; } set { SetField(ref _videoFormat, value, "VideoFormat"); } }
        public ulong Instance { get { return _instance; } set { SetField(ref _instance, value, "Instance"); } }
        public double VolumeReferenceLoudness { get { return _volumeReferenceLoudness; } set { SetField(ref _volumeReferenceLoudness, value, "VolumeReferenceLoudness"); } }

        readonly List<object> _channels;
        public List<object> Channels { get { return _channels; } }
        private object _channelPGM;
        public object ChannelPGM { get { return _channelPGM; } set { SetField(ref _channelPGM, value, "ChannelPGM"); } }
        private object _channelPRV;
        public object ChannelPRV { get { return _channelPRV; } set { SetField(ref _channelPRV, value, "ChannelPRV"); } }
        private List<object> _archiveDirectories;
        public List<object> ArchiveDirectories { get { return _archiveDirectories; } }
        private object _archiveDirectory;
        public object ArchiveDirectory { get { return _archiveDirectory; } set { SetField(ref _archiveDirectory, value, "ArchiveDirectory"); } }
        private bool _gpiEnabled;
        public bool GpiEnabled { get { return _gpiEnabled; } set { SetField(ref _gpiEnabled, value, "GpiEnabled"); } }
        private string _gpiAddress;
        public string GpiAddress { get { return _gpiAddress; } set { SetField(ref _gpiAddress, value, "GpiAddress"); } }
        private int _gpiGraphicsStartDelay;
        public int GpiGraphicsStartDelay { get { return _gpiGraphicsStartDelay; } set { SetField(ref _gpiGraphicsStartDelay, value, "GpiGraphicsStartDelay"); } }
        private bool _remoteHostEnabled;
        public bool RemoteHostEnabled { get { return _remoteHostEnabled; } set { SetField(ref _remoteHostEnabled, value, "RemoteHostEnabled"); } }
        private string _remoteHostEndpointAddress;
        public string RemoteHostEndpointAddress { get { return _remoteHostEndpointAddress; } set { SetField(ref _remoteHostEndpointAddress, value, "RemoteHostEndpointAddress"); } }

        public UICommand CommandManageArchiveDirectories { get; private set; }


        public override void Save(object destObject = null)
        {
            if (Modified)
            {
                var playoutServerChannelPGM = _channelPGM as Model.CasparServerChannel;
                Model.IdServerPGM = playoutServerChannelPGM == null ? 0 : ((Model.CasparServer)playoutServerChannelPGM.Owner).Id;
                Model.ServerChannelPGM = playoutServerChannelPGM == null ? 0 : playoutServerChannelPGM.ChannelNumber;
                var playoutServerChannelPRV = _channelPRV as Model.CasparServerChannel;
                Model.IdServerPRV = playoutServerChannelPRV == null ? 0 : ((Model.CasparServer)playoutServerChannelPRV.Owner).Id;
                Model.ServerChannelPRV = playoutServerChannelPRV == null ? 0 : playoutServerChannelPRV.ChannelNumber;
                Model.Gpi = _gpiEnabled ? new Model.Gpi() { Address = this.GpiAddress, GraphicsStartDelay = this.GpiGraphicsStartDelay } : null;
                Model.Remote = _remoteHostEnabled ? new Model.RemoteHost() { EndpointAddress = RemoteHostEndpointAddress } : null;
                Model.IdArchive = _archiveDirectory is Model.ArchiveDirectory ? ((Model.ArchiveDirectory)_archiveDirectory).idArchive : 0;
                Model.Modified = true;
            }
            base.Save(destObject);
        }
    }
}
