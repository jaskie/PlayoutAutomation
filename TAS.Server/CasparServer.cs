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

        public TServerType ServerType { get; set; }

        [XmlIgnore]
        public IServerDirectory MediaDirectory { get; private set; }
        [XmlIgnore]
        public IAnimationDirectory AnimationDirectory { get; private set; }

        [XmlArray(nameof(Channels))]
        public List<CasparServerChannel> _channels { get; set; }
        [XmlIgnore]
        public IEnumerable<IPlayoutServerChannel> Channels { get { return _channels; } }

        [XmlArray(nameof(Recorders))]
        public List<CasparRecorder> _recorders { get; set; }
        [XmlIgnore]
        public IEnumerable<IRecorder> Recorders { get { return _recorders; } }

        private Svt.Caspar.CasparDevice _casparDevice;

        protected bool _isInitialized;
        public void Initialize(MediaManager mediaManager)
        {
            Debug.WriteLine(this, "CasparServer initialize");
            lock (this)
            {
                if (!_isInitialized)
                {
                    MediaDirectory = new Server.ServerDirectory(this, mediaManager) { Folder = MediaFolder };
                    if (!string.IsNullOrWhiteSpace(AnimationFolder))
                        AnimationDirectory = new Server.AnimationDirectory(this, mediaManager) { Folder = AnimationFolder };
                    _casparDevice = new Svt.Caspar.CasparDevice() { IsRecordingSupported = ServerType == TServerType.CasparTVP };
                    _casparDevice.ConnectionStatusChanged += _casparDevice_ConnectionStatusChanged;
                    _casparDevice.UpdatedChannels += _casparDevice_UpdatedChannels;
                    _casparDevice.UpdatedRecorders += _casparDevice_UpdatedRecorders;
                    _connect();
                    _channels.ForEach(c => c.ownerServer = this);
                    _recorders.ForEach(r => r.ownerServer = this);
                    _isInitialized = true;
                }
            }
        }

        private void _casparDevice_UpdatedRecorders(object sender, EventArgs e)
        {
            var device_recorders = _casparDevice.Recorders.ToList();
            foreach (var dev_rec in device_recorders)
                _recorders.FirstOrDefault(r => r.Id == dev_rec.Id)?.SetRecorder(dev_rec);
        }

        protected bool _isConnected;
        [JsonProperty]
        [XmlIgnore]
        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (SetField(ref _isConnected, value, nameof(IsConnected)))
                {
                    _recorders.ForEach(r => r.IsServerConnected = value);
                    _channels.ForEach(c => c.IsServerConnected = value);
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
                _casparDevice.Connect(host, port, 6250, true);
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
            if (channels != null && channels.Count > 0)
            {
                _needUpdateChannels = false;
                foreach (CasparServerChannel c in Channels)
                {
                    c.CasparChannel = channels.Find(csc => csc.ID == c.Id);
                    c.Initialize();
                }
            }
        }
        private void _casparDevice_ConnectionStatusChanged(object sender, Svt.Network.ConnectionEventArgs e)
        {
            if (e.Connected)
            {
                _casparDevice.RefreshTemplates();
                if (_casparDevice.Channels.Count > 0)
                    _updateChannels(_casparDevice.Channels);
                else
                    _needUpdateChannels = true;
            }
            IsConnected = e.Connected;
            Debug.WriteLine(e.Connected, "Caspar connected");
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
            _casparDevice.UpdatedRecorders -= _casparDevice_UpdatedRecorders;
            _casparDevice.Dispose();
            MediaDirectory.Dispose();
            AnimationDirectory.Dispose();
        }
    }


  
}
