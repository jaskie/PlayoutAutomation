//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using jNet.RPC.Server;
using TAS.Common;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;

namespace TAS.Server
{

    public delegate void CommandNotifier(DateTime when, string command, Event sender);

    public class CasparServer : DtoBase, IPlayoutServer, IPlayoutServerProperties
    {
        private bool _isConnected;
        private int _isInitialized;
        private bool _needUpdateChannels;
        private Svt.Caspar.CasparDevice _casparDevice;

        #region IPersistent

        [XmlIgnore]
        [JsonProperty]
        public ulong Id { get; set; }

        public IDictionary<string, int> FieldLengths { get; } = EngineController.Current.Database.ServerFieldLengths;

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }
        #endregion
        
        public string ServerAddress { get; set; }

        public int OscPort { get; set; }

        public string MediaFolder { get; set; }

        public bool IsMediaFolderRecursive { get; set; }

        public string AnimationFolder { get; set; }

        public TServerType ServerType { get; set; }

        public TMovieContainerFormat MovieContainerFormat { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects), XmlIgnore]
        public IServerDirectory MediaDirectory { get; private set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects), XmlIgnore]
        public IAnimationDirectory AnimationDirectory { get; private set; }

        [XmlArray(nameof(Channels))]
        public List<CasparServerChannel> ChannelsSer { get; set; }

        [XmlIgnore]
        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemIsReference = true, ItemTypeNameHandling = TypeNameHandling.Objects)]
        public IEnumerable<IPlayoutServerChannel> Channels => ChannelsSer;

        [XmlArray(nameof(Recorders))]
        public List<CasparRecorder> RecordersSer { get; set; }

        [XmlIgnore]
        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemIsReference = true, ItemTypeNameHandling = TypeNameHandling.Objects)]
        public IEnumerable<IRecorder> Recorders => RecordersSer;

        [JsonProperty]
        [XmlIgnore]
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (!SetField(ref _isConnected, value))
                    return;
                RecordersSer.ForEach(r => r.IsServerConnected = value);
                ChannelsSer.ForEach(c => c.IsServerConnected = value);
            }
        }

        public void Initialize(MediaManager mediaManager, bool isPrimary)
        {
            Debug.WriteLine(this, "CasparServer initialize");
            if (Interlocked.Exchange(ref _isInitialized, 1) != default(int))
                return;
            MediaDirectory = new ServerDirectory(this, isPrimary) {Folder = MediaFolder};
            if (!string.IsNullOrWhiteSpace(AnimationFolder))
                AnimationDirectory = new AnimationDirectory(this, isPrimary) {Folder = AnimationFolder};
            _casparDevice = new Svt.Caspar.CasparDevice {IsRecordingSupported = ServerType == TServerType.CasparTVP};
            _casparDevice.ConnectionStatusChanged += CasparDevice_ConnectionStatusChanged;
            _casparDevice.UpdatedChannels += CasparDevice_UpdatedChannels;
            _casparDevice.UpdatedRecorders += CasparDevice_UpdatedRecorders;
            Connect();
        }

        public override string ToString()
        {
            return $"{GetType().Name} {ServerAddress}";
        }

        // private methods
        private void CasparDevice_UpdatedRecorders(object sender, EventArgs e)
        {
            var deviceRecorders = _casparDevice.Recorders.ToList();
            foreach (var devRec in deviceRecorders)
                RecordersSer.FirstOrDefault(r => r.Id == devRec.Id)?.SetRecorder(devRec);
        }
        
        private void Connect()
        {
            var address = ServerAddress.Split(':');
            var host = address.Length > 0 ? address[0] : "localhost";
            if (!(address.Length > 1 && int.TryParse(address[1], out var port)))
                port = 5250;
            if (_casparDevice != null && !_casparDevice.IsConnected)
                _casparDevice.Connect(host, port, OscPort, true);
            else throw new Exception($"Invalid server address: {ServerAddress}");
        }

        private void Disconnect()
        {
            if (_casparDevice?.IsConnected == true)
                _casparDevice.Disconnect();
        }

        private void CasparDevice_UpdatedChannels(object sender, EventArgs e)
        {
            if (_needUpdateChannels)
                UpdateChannels(_casparDevice.Channels);
        }

        private void UpdateChannels(Svt.Caspar.Channel[] channels)
        {
            if (channels != null && channels.Length > 0)
            {
                _needUpdateChannels = false;
                foreach (var c in ChannelsSer)
                    c.AssignCasparChannel(Array.Find(channels, csc => csc.Id == c.Id));
            }
        }

        private void CasparDevice_ConnectionStatusChanged(object sender, Svt.Network.ConnectionEventArgs e)
        {
            if (e.Connected)
            {
                _casparDevice.RefreshTemplates();
                _needUpdateChannels = true;
            }
            IsConnected = e.Connected;
            Debug.WriteLine(e.Connected, "Caspar connected");
        }
        
        protected override void DoDispose()
        {
            Disconnect();
            if (_casparDevice != null)
            {
                _casparDevice.ConnectionStatusChanged -= CasparDevice_ConnectionStatusChanged;
                _casparDevice.UpdatedChannels -= CasparDevice_UpdatedChannels;
                _casparDevice.UpdatedRecorders -= CasparDevice_UpdatedRecorders;
                _casparDevice.Dispose();
            }
            MediaDirectory?.Dispose();
            AnimationDirectory?.Dispose();
        }
    }


  
}
