//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Configuration;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;
using System.ComponentModel;
using TAS.Server.Common;
using TAS.Remoting.Server;
using Newtonsoft.Json;

namespace TAS.Server
{

    public delegate void CommandNotifier(DateTime When, string Command, Event sender);
    public delegate void VolumeChangeNotifier(IPlayoutServerChannel channel, VideoLayer layer, decimal newvalue);

    public class CasparServer : DtoBase, IPlayoutServer, IDisposable 
    {
        [XmlIgnore]
        [JsonProperty]
        public UInt64 Id { get; set; }
        [JsonProperty]
        public string ServerAddress { get; set; }
        [JsonProperty]
        public string MediaFolder { get; set; }
        [JsonProperty]
        public string AnimationFolder { get; set; }
        [XmlIgnore]
        public IServerDirectory MediaDirectory { get; private set; }
        [XmlIgnore]
        public IAnimationDirectory AnimationDirectory { get; private set; }
        protected List<IPlayoutServerChannel> _channels;
        [XmlIgnore]
        public MediaManager MediaManager;

        [XmlIgnore]
        public List<IPlayoutServerChannel> Channels
        {
            get { return _serChannels.ConvertAll(new Converter<CasparServerChannel, IPlayoutServerChannel>(c => c)); }
        }
        private Svt.Caspar.CasparDevice _casparDevice;

        protected bool _isInitialized;
        public void Initialize()
        {
            Debug.WriteLine(this, "CasparServer initialize");
            lock (this)
            {
                if (!_isInitialized)
                {
                    MediaDirectory = new Server.ServerDirectory(this, MediaManager) { Folder = MediaFolder };
                    if (!string.IsNullOrWhiteSpace(AnimationFolder))
                        AnimationDirectory = new Server.AnimationDirectory(this, MediaManager) { Folder = AnimationFolder };
                    _casparDevice = new Svt.Caspar.CasparDevice();
                    _casparDevice.ConnectionStatusChanged += _casparDevice_ConnectionStatusChanged;
                    _casparDevice.UpdatedChannels += _casparDevice_UpdatedChannels;
                    _connect();
                    _isInitialized = true;
                }
            }
        }

        List<CasparServerChannel> _serChannels;

        [XmlArray("Channels")]
        public List<CasparServerChannel> serChannels
        {
            get { return _serChannels; }
            set { _serChannels = value; }
        }

        protected bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected != value)
                {
                    if (value)
                        _connect();
                    else
                        _disconnect();
                    NotifyPropertyChanged(nameof(IsConnected));
                }
            }
        }


        protected void _connect()
        {
            string[] address = ServerAddress.Split(':');
            string host = address.Length > 0 ? address[0] : "localhost";
            int port;
            if (!(address.Length > 1 && int.TryParse(address[1], out port)))
                port = 5250;
            if (_casparDevice != null && !_casparDevice.IsConnected)
                _casparDevice.Connect(host, port, true);
            else throw new Exception($"Invalid server address: {ServerAddress}");
        }

        protected void _disconnect()
        {
            if (_casparDevice != null && _casparDevice.IsConnected)
                _casparDevice.Disconnect();
            _casparDevice.Disconnect();
        }

        private bool _needUpdateChannels;
        private void _casparDevice_UpdatedChannels(object sender, EventArgs e)
        {
            if (_needUpdateChannels)
                _updateChannels(_casparDevice.Channels);
        }

        private void _updateChannels(List<Svt.Caspar.Channel> channels)
        {
            if (channels != null && channels.Count>0)
            {
                _needUpdateChannels = false;
                foreach (CasparServerChannel C in Channels)
                {
                    C.CasparChannel = channels.Find(csc => csc.ID == C.ChannelNumber);
                    C.Initialize();
                }
            }
        }
        private void _casparDevice_ConnectionStatusChanged(object sender, Svt.Network.ConnectionEventArgs e)
        {
            _isConnected = e.Connected;
            if (e.Connected)
            {
                _casparDevice.RefreshTemplates();
                if (_casparDevice.Channels.Count > 0)
                    _updateChannels(_casparDevice.Channels);
                else
                    _needUpdateChannels = true;
            }
            Debug.WriteLine(e.Connected, "Caspar connected");
            NotifyPropertyChanged(nameof(IsConnected));
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.GetType().Name, ServerAddress);
        }

        protected override void DoDispose()
        {
            _disconnect();
            _casparDevice.ConnectionStatusChanged -= _casparDevice_ConnectionStatusChanged;
            _casparDevice.UpdatedChannels -= _casparDevice_UpdatedChannels;
            MediaDirectory.Dispose();
            AnimationDirectory.Dispose();
        }
    }
  
}
