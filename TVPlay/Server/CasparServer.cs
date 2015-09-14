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

namespace TAS.Server
{
    public class CasparServer : PlayoutServer
    {
        private Svt.Caspar.CasparDevice _casparDevice;

        public override void Initialize()
        {
            Debug.WriteLine(this, "CasparServer initialize");
            lock (this)
            {
                if (!_isInitialized)
                {
                    base.Initialize();
                    _casparDevice = new Svt.Caspar.CasparDevice();
                    _casparDevice.ConnectionStatusChanged += _casparDevice_ConnectionStatusChanged;
                    _casparDevice.UpdatedChannels += _casparDevice_UpdatedChannels;
                    _casparDevice.UpdatedTemplates += _onUpdatedTemplates;
                    _connect();
                    foreach (CasparServerChannel channel in Channels)
                        channel.OwnerServer = this;
                }
            }
        }

        protected override void _connect()
        {
            string[] address = ServerAddress.Split(':');
            if (address.Length == 1)
                address[1] = "5250";
            if (address.Length == 2)
            {
                if (_casparDevice != null && !_casparDevice.IsConnected)
                    _casparDevice.Connect(address[0], UInt16.Parse(address[1]), true);
            }
            else throw new Exception(string.Format("Invalid server address: {0}", ServerAddress));
        }

        protected override void _disconnect()
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
                NotifyPropertyChanged("IsConnected");
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
            var files = AnimationDirectory.Files;
            var templates = _casparDevice.Templates.All.ToList();
            foreach (Svt.Caspar.TemplateInfo template in templates)
            {
                ServerMedia media = (ServerMedia)files.FirstOrDefault(f => f is ServerMedia
                    && f.FileName == template.Name
                    && f.Folder == template.Folder);
                if (media == null)
                {
                    media = new ServerMedia()
                        {
                            MediaType = TMediaType.AnimationFlash,
                            MediaName = template.Name,
                            Folder = template.Folder,
                            FileName = template.Name,
                            FileSize = (UInt64)template.Size,
                            MediaStatus = TMediaStatus.Available,
                            LastUpdated = DateTimeExtensions.FromFileTime(template.LastUpdated.ToUniversalTime(), DateTimeKind.Utc),
                            MediaGuid = Guid.NewGuid(),
                            Directory = AnimationDirectory,

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
                    AnimationDirectory.MediaRemove(media);
            }
        }

        protected override void DoDispose()
        {
            _disconnect();
            _casparDevice.ConnectionStatusChanged -= _casparDevice_ConnectionStatusChanged;
            _casparDevice.UpdatedChannels -= _casparDevice_UpdatedChannels;
            //_casparDevice.Updateemplates -= _onUpdatedTemplates;
            base.DoDispose();
        }
    }
  
}
