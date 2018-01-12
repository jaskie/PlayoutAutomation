using System;
using Newtonsoft.Json;
using TAS.Remoting.Server;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class MediaSegment : DtoBase, IMediaSegment
    {
        private string _segmentName;
        private TimeSpan _tcIn;
        private TimeSpan _tcOut;

        public MediaSegment(IMediaSegments owner)
        {
            Owner = owner;
        }

        public IMediaSegments Owner { get; }

        public ulong Id { get; set; }
        
        [JsonProperty]
        public string SegmentName
        {
            get { return _segmentName; }
            set { SetField(ref _segmentName, value); }
        }
        [JsonProperty]
        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set { SetField(ref _tcIn, value); }
        }

        [JsonProperty]
        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set { SetField(ref _tcOut, value); }
        }

        public void Save()
        {
            Id = EngineController.Database.DbSaveMediaSegment(this);
        }

        public void Delete()
        {
            if (Owner.Remove(this))
                EngineController.Database.DbDeleteMediaSegment(this);

        }

    }
}
