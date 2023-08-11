﻿//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using jNet.RPC.Server;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;
using System.ComponentModel;
using TAS.Database.Common;
using jNet.RPC;

namespace TAS.Server
{

    [DtoType(typeof(IPlayoutServer))]

    public class CasparServer : ServerObjectBase, IPlayoutServer, IPlayoutServerProperties
    {
        private bool _isConnected;
        private int _isInitialized;
        private bool _needUpdateChannels;
        private Svt.Caspar.CasparDevice _casparDevice;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #region IPersistent

        [XmlIgnore]
        [DtoMember]
        public ulong Id { get; set; }

        public IDictionary<string, int> FieldLengths { get; } = DatabaseProvider.Database.ServerFieldLengths;

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }
        #endregion
        
        [Hibernate]
        public string ServerAddress { get; set; }

        [Hibernate]
        public int OscPort { get; set; }

        [Hibernate]
        public string MediaFolder { get; set; }

        [Hibernate]
        public bool IsMediaFolderRecursive { get; set; }

        [Hibernate]
        public string AnimationFolder { get; set; }

        [Hibernate]
        public TServerType ServerType { get; set; }

        [Hibernate]
        public TMovieContainerFormat MovieContainerFormat { get; set; }

        [DtoMember, XmlIgnore]
        public IServerDirectory MediaDirectory { get; private set; }

        [DtoMember, XmlIgnore]
        public IAnimationDirectory AnimationDirectory { get; private set; }

        [XmlArray(nameof(Channels)), Hibernate(nameof(Channels))]
        public List<CasparServerChannel> ChannelsSer { get; set; }

        [XmlIgnore]
        [DtoMember]
        public IEnumerable<IPlayoutServerChannel> Channels => ChannelsSer;

        [XmlArray(nameof(Recorders)), Hibernate(nameof(Recorders))]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<CasparRecorder> RecordersSer { get; set; }

        [XmlIgnore]
        [DtoMember]
        public IEnumerable<IRecorder> Recorders => RecordersSer;

        [DtoMember]
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
                if (value)
                    Logger.Info("Server {0} connected", ServerAddress);
                else
                    Logger.Error("Server {0} disconnected", ServerAddress);
            }
        }

        public void Initialize(MediaManager mediaManager)
        {
            Debug.WriteLine(this, "CasparServer initialize");
            if (Interlocked.Exchange(ref _isInitialized, 1) != default(int))
                return;
            MediaDirectory = new ServerDirectory(this) {Folder = MediaFolder};
            if (!string.IsNullOrWhiteSpace(AnimationFolder))
                AnimationDirectory = new AnimationDirectory(this) {Folder = AnimationFolder};
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
