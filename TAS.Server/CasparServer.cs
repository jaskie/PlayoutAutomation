//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Remoting.Server;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Server.Media;

namespace TAS.Server
{

    public delegate void CommandNotifier(DateTime when, string command, Event sender);

    public class CasparServer : DtoBase, IPlayoutServer
    {
        private bool _isConnected;
        private bool _isInitialized;
        private bool _needUpdateChannels;
        private Svt.Caspar.CasparDevice _casparDevice;

        #region IPersistent

        [XmlIgnore]
        [JsonProperty]
        public ulong Id { get; set; }

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

        [JsonProperty]
        public string MediaFolder { get; set; }

        [JsonProperty]
        public string AnimationFolder { get; set; }

        public TServerType ServerType { get; set; }

        [XmlIgnore]
        public IServerDirectory MediaDirectory { get; private set; }

        [XmlIgnore]
        public IAnimationDirectory AnimationDirectory { get; private set; }

        [XmlArray(nameof(Channels))]
        public List<CasparServerChannel> ChannelsSer { get; set; }
        [XmlIgnore]
        public IEnumerable<IPlayoutServerChannel> Channels => ChannelsSer;

        [XmlArray(nameof(Recorders))]
        public List<CasparRecorder> RecordersSer { get; set; }
        [XmlIgnore]
        public IEnumerable<IRecorder> Recorders => RecordersSer;

        [JsonProperty]
        [XmlIgnore]
        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (SetField(ref _isConnected, value))
                {
                    RecordersSer.ForEach(r => r.IsServerConnected = value);
                    ChannelsSer.ForEach(c => c.IsServerConnected = value);
                }
            }
        }

        public void Initialize(MediaManager mediaManager)
        {
            Debug.WriteLine(this, "CasparServer initialize");
            lock (this)
            {
                if (!_isInitialized)
                {
                    MediaDirectory = new ServerDirectory(this, mediaManager) { Folder = MediaFolder };
                    if (!string.IsNullOrWhiteSpace(AnimationFolder))
                        AnimationDirectory = new AnimationDirectory(this, mediaManager) { Folder = AnimationFolder };
                    _casparDevice = new Svt.Caspar.CasparDevice() { IsRecordingSupported = ServerType == TServerType.CasparTVP };
                    _casparDevice.ConnectionStatusChanged += CasparDevice_ConnectionStatusChanged;
                    _casparDevice.UpdatedChannels += CasparDevice_UpdatedChannels;
                    _casparDevice.UpdatedRecorders += CasparDevice_UpdatedRecorders;
                    Connect();
                    _isInitialized = true;
                }
            }
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
            string[] address = ServerAddress.Split(':');
            string host = address.Length > 0 ? address[0] : "localhost";
            int port;
            if (!(address.Length > 1 && int.TryParse(address[1], out port)))
                port = 5250;
            if (_casparDevice != null && !_casparDevice.IsConnected)
                _casparDevice.Connect(host, port, OscPort, true);
            else throw new Exception($"Invalid server address: {ServerAddress}");
        }

        private void Disconnect()
        {
            if (_casparDevice != null && _casparDevice.IsConnected)
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
                    c.SetChannel(Array.Find(channels, csc => csc.ID == c.Id));
            }
        }

        private void CasparDevice_ConnectionStatusChanged(object sender, Svt.Network.ConnectionEventArgs e)
        {
            if (e.Connected)
            {
                _casparDevice.RefreshTemplates();
                if (_casparDevice.Channels.Length > 0)
                    UpdateChannels(_casparDevice.Channels);
                else
                    _needUpdateChannels = true;
            }
            IsConnected = e.Connected;
            Debug.WriteLine(e.Connected, "Caspar connected");
        }
        
        protected override void DoDispose()
        {
            Disconnect();
            _casparDevice.ConnectionStatusChanged -= CasparDevice_ConnectionStatusChanged;
            _casparDevice.UpdatedChannels -= CasparDevice_UpdatedChannels;
            _casparDevice.UpdatedRecorders -= CasparDevice_UpdatedRecorders;
            _casparDevice.Dispose();
            MediaDirectory.Dispose();
            AnimationDirectory.Dispose();
        }
    }


  
}
