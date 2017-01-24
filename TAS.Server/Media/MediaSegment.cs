using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TAS.Server.Interfaces;
using TAS.Remoting.Server;
using TAS.Server.Database;
using Newtonsoft.Json;

namespace TAS.Server
{
    public class MediaSegment : DtoBase, IMediaSegment, IMediaSegmentPersistent
    {
        private UInt64 _id;
        private readonly IMediaSegments _owner;

        public MediaSegment(IMediaSegments owner)
        {
            _owner = owner;
        }

        public IMediaSegments Owner { get { return _owner; } }

        public ulong Id
        {
            get { return _id; }
            set { _id = value; }
        }
        
        private string _segmentName;
        [JsonProperty]
        public string SegmentName
        {
            get { return _segmentName; }
            set { SetField(ref _segmentName, value, nameof(SegmentName)); }
        }

        private TimeSpan _tcIn;
        [JsonProperty]
        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set { SetField(ref _tcIn, value, nameof(TcIn)); }
        }

        private TimeSpan _tcOut;
        [JsonProperty]
        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set { SetField(ref _tcOut, value, nameof(TcOut)); }
        }

        public void Save()
        {
            _id = this.DbSave();
        }

        public void Delete()
        {
            if (_owner.Remove(this))
                this.DbDelete();

        }

    }
}
