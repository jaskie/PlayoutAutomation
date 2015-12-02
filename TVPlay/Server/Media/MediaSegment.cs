using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TAS.Data;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class MediaSegment : Remoting.DtoBase, IMediaSegment
    {
        internal UInt64 idMediaSegment;
        protected readonly Guid _mediaGuid;
        public MediaSegment(Guid mediaGuid)
        {
            _mediaGuid = mediaGuid;
        }

        private string _segmentName;
        public string SegmentName
        {
            get { return _segmentName; }
            set { SetField(ref _segmentName, value, "SegmentName"); }
        }

        private TimeSpan _tcIn;
        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set { SetField(ref _tcIn, value, "TcIn"); }
        }

        private TimeSpan _tcOut;
        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set { SetField(ref _tcOut, value, "TcOut"); }
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
