using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TAS.Server.Interfaces;
using TAS.Common;

namespace TAS.Server
{
    public abstract class PlayoutServerChannel : IDisposable, INotifyPropertyChanged, IPlayoutServerChannelConfig
    {
        public PlayoutServer OwnerServer { get; set; }
        #region IPlayoutServerChannel
        public string ChannelName { get; set; }
        public int ChannelNumber {get; set;}
        [DefaultValue(1.0d)]
        public decimal MasterVolume { get; set; }
        public string LiveDevice { get; set; }
        #endregion // IPlayoutServerChannel
        protected bool? outputAspectNarrow;
        public Engine Engine { get; set; }

        internal abstract void Initialize();

        public abstract event VolumeChangeNotifier OnVolumeChanged;

        public abstract void ReStart(VideoLayer aVideoLayer);

        public abstract bool Load(Event aEvent);
        public abstract bool Load(IServerMedia media, VideoLayer videolayer, long seek, long duration);
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
        public abstract void SetAspect(VideoLayer layer, bool narrow);

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
            return string.Format("{0}->{1}", OwnerServer, ChannelNumber);
        }
    }
}
