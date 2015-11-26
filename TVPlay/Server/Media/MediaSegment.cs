using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TAS.Data;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class MediaSegment : IMediaSegment
    {
        internal UInt64 idMediaSegment;
        protected readonly Guid _mediaGuid;
        private readonly Guid _dtoGuid = Guid.NewGuid();
        public MediaSegment(Guid mediaGuid)
        {
            _mediaGuid = mediaGuid;
        }

        public Guid DtoGuid { get { return _dtoGuid; } }

        private string _segmentName;
        public string SegmentName
        {
            get { return _segmentName; }
            set { SetField(ref _segmentName, value, "SegmentName"); }
        }

        private TimeSpan _tCIn;
        public TimeSpan TCIn
        {
            get { return _tCIn; }
            set { SetField(ref _tCIn, value, "TCIn"); }
        }

        private TimeSpan _tCOut;
        public TimeSpan TCOut
        {
            get { return _tCOut; }
            set { SetField(ref _tCOut, value, "TCOut"); }
        }

        public Guid MediaGuid
        {
            get { return _mediaGuid; }
        }


        public void Save()
        {
            this.DbSave();
        }

        public void Delete()
        {
            this.DbDelete();
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            lock (this)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
            }
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
