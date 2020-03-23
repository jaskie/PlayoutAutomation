using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class MediaSegment : ServerObjectBase, IMediaSegment
    {
        private string _segmentName;
        private TimeSpan _tcIn;
        private TimeSpan _tcOut;

        public MediaSegment(IMediaSegments owner)
        {
            Owner = owner;
            FieldLengths = EngineController.Current.Database.MediaSegmentFieldLengths;
        }

        public IMediaSegments Owner { get; }

        public ulong Id { get; set; }
        
        [DtoMember]
        public string SegmentName
        {
            get => _segmentName;
            set => SetField(ref _segmentName, value);
        }
        [DtoMember]
        public TimeSpan TcIn
        {
            get => _tcIn;
            set => SetField(ref _tcIn, value);
        }

        [DtoMember]
        public TimeSpan TcOut
        {
            get => _tcOut;
            set => SetField(ref _tcOut, value);
        }


        public IDictionary<string, int> FieldLengths { get; }


        public void Save()
        {
            Id = EngineController.Current.Database.SaveMediaSegment(this);
        }

        public void Delete()
        {
            if (Owner.Remove(this))
                EngineController.Current.Database.DeleteMediaSegment(this);

        }

    }
}
