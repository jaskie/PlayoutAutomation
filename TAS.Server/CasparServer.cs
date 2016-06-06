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
                    MediaDirectory = new Server.ServerDirectory(this, MediaManager);
                    AnimationDirectory = new Server.AnimationDirectory(this, MediaManager);
                    MediaDirectory.Folder = MediaFolder;
                    AnimationDirectory.Folder = MediaFolder;
                    _casparDevice = new Svt.Caspar.CasparDevice();
                    _casparDevice.ConnectionStatusChanged += _casparDevice_ConnectionStatusChanged;
                    _casparDevice.UpdatedChannels += _casparDevice_UpdatedChannels;
                    _casparDevice.UpdatedTemplates += _onUpdatedTemplates;
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
                    NotifyPropertyChanged("IsConnected");
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
            else throw new Exception(string.Format("Invalid server address: {0}", ServerAddress));
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
            NotifyPropertyChanged("IsConnected");
        }

        private void _onUpdatedTemplates(object o, EventArgs e)
        {
            var files = AnimationDirectory.GetFiles();
            var templates = _casparDevice.Templates.All.ToList();
            foreach (Svt.Caspar.TemplateInfo template in templates)
            {
                ServerMedia media = (ServerMedia)files.FirstOrDefault(f => f is ServerMedia
                    && f.FileName == template.Name
                    && f.Folder == template.Folder);
                if (media == null)
                {
                    media = new ServerMedia(AnimationDirectory as AnimationDirectory, Guid.Empty, ulong.MinValue, MediaManager.ArchiveDirectory)
                        {
                            MediaType = TMediaType.Animation,
                            MediaName = template.Name,
                            FullPath = Path.Combine(AnimationDirectory.Folder, template.Folder, template.Name),
                            FileSize = (UInt64)template.Size,
                            MediaStatus = TMediaStatus.Available,
                            LastUpdated = DateTimeExtensions.FromFileTime(template.LastUpdated.ToUniversalTime(), DateTimeKind.Utc),
                        };
                    media.Save();
                }
                else // media != null
                {
                    if (media.FileSize != (UInt64)template.Size
                        || media.LastUpdated != DateTimeExtensions.FromFileTime(template.LastUpdated.ToUniversalTime(), DateTimeKind.Utc))
                    {
                        media.FileSize = (UInt64)template.Size;
                        media.LastUpdated = DateTimeExtensions.FromFileTime(template.LastUpdated.ToUniversalTime(), DateTimeKind.Utc);
                        media.Save();
                    }
                }
            }
            foreach (Media media in files)
            {
                Svt.Caspar.TemplateInfo i = templates.FirstOrDefault(t => media.FileName == t.Name && media.Folder == t.Folder);
                if (i == null)
                    ((AnimationDirectory)AnimationDirectory).MediaRemove(media);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _disposed = false;
        public virtual void Dispose()
        {
            if (!_disposed)
                DoDispose();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.GetType().Name, ServerAddress);
        }

        protected void DoDispose()
        {
            _disconnect();
            _casparDevice.ConnectionStatusChanged -= _casparDevice_ConnectionStatusChanged;
            _casparDevice.UpdatedChannels -= _casparDevice_UpdatedChannels;
            MediaDirectory.Dispose();
        }
    }
  
}
