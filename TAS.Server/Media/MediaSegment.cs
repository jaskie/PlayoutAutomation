using System;
using System.Collections.Generic;
using jNet.RPC.Server;
using Newtonsoft.Json;
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
            FieldLengths = EngineController.Database.MediaSegmentFieldLengths;
        }

        public IMediaSegments Owner { get; }

        public ulong Id { get; set; }
        
        [JsonProperty]
        public string SegmentName
        {
            get => _segmentName;
            set => SetField(ref _segmentName, value);
        }
        [JsonProperty]
        public TimeSpan TcIn
        {
            get => _tcIn;
            set => SetField(ref _tcIn, value);
        }

        [JsonProperty]
        public TimeSpan TcOut
        {
            get => _tcOut;
            set => SetField(ref _tcOut, value);
        }


        public IDictionary<string, int> FieldLengths { get; }


        public void Save()
        {
            Id = EngineController.Database.SaveMediaSegment(this);
        }

        public void Delete()
        {
            if (Owner.Remove(this))
                EngineController.Database.DeleteMediaSegment(this);

        }

    }
}
