using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml.Serialization;
using System.ComponentModel;
using TAS.Common;

namespace TAS.Server
{

    public delegate void CommandNotifier(DateTime When, string Command, Event sender);
    public delegate void VolumeChangeNotifier(PlayoutServerChannel channel, VideoLayer layer, decimal newvalue);

    public abstract class PlayoutServer : IDisposable, INotifyPropertyChanged
    {
        [XmlIgnore]
        public UInt64 idServer { get; internal set; }
        public string ServerAddress { get; set; }
        public string MediaFolder { get; set; }
        [XmlIgnore]
        public ServerDirectory MediaDirectory;
        [XmlIgnore]
        public AnimationDirectory AnimationDirectory;
        [XmlIgnore]
        private List<PlayoutServerChannel> _channels;
        public List<PlayoutServerChannel> Channels
        {
            get { return _channels; }
            set
            {
                foreach (PlayoutServerChannel c in value)
                    c.OwnerServer = this;
                _channels = value;
            }
        }
        protected abstract void _connect();
        protected abstract void _disconnect();
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

        public PlayoutServer()
        {
            MediaDirectory = new ServerDirectory(this);
            AnimationDirectory = new AnimationDirectory(this);
        }


        protected bool _isInitialized;
        public virtual void Initialize()
        {
            Debug.WriteLine(this, "Initializing");
            lock (this)
            {
                if (!_isInitialized)
                {
                    MediaDirectory.Folder = MediaFolder;
                    MediaDirectory.Initialize();
                    AnimationDirectory.Initialize();
                    _isInitialized = true;
                }
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
        protected virtual void DoDispose()
        {
            MediaDirectory.Dispose();
        }

        public virtual void Dispose()
        {
            if (!_disposed)
                DoDispose();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.GetType().Name, ServerAddress);
        }

    }

}
