using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace TAS.Server
{
    public abstract class PlayoutServerChannel : INotifyPropertyChanged, IDisposable
    {
        internal PlayoutServer OwnerServer;
        public readonly string ChannelName;
        public int ChannelNumber;
        public decimal MasterVolume = 1;
        protected bool outputAspectNarrow;
        internal Engine Engine { get; set; }

        internal abstract void Initialize();

        public abstract event VolumeChangeNotifier OnVolumeChanged;

        public abstract void ReStart(VideoLayer aVideoLayer);

        public abstract bool Load(Event aEvent);
        public abstract bool Load(ServerMedia media, VideoLayer videolayer, long seek, long duration);
        public abstract bool Load(System.Drawing.Color color, VideoLayer layer);
        public abstract bool Seek(VideoLayer videolayer, long position);

        public abstract bool LoadNext(Event aEvent);

        public abstract bool Play(Event aEvent);
        public abstract bool Play(VideoLayer videolayer);

        public abstract bool Stop(Event aEvent);
        public abstract bool Stop(VideoLayer videolayer);

        public abstract bool Pause(Event aEvent);
        public abstract bool Pause(VideoLayer videolayer);

        public abstract void Clear(VideoLayer aVideoLayer);

        public abstract void SetVolume(VideoLayer videolayer, decimal volume);

        protected abstract TVideoFormat _getFormat();
        public TVideoFormat Format
        {
            get { return _getFormat(); }
        }
        public abstract void SetAspect(bool narrow);

        public abstract void Clear();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _disposed = false;

        public void Dispose()
        {
            lock (this)
                if (!_disposed)
                    DoDispose();
        }

        protected virtual void DoDispose()
        {

        }

        public override string ToString()
        {
            return string.Format("Server {0} channel {1}", OwnerServer.idServer, ChannelNumber);
        }
    }
}
