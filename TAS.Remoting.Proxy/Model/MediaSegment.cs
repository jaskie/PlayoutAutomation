using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Remoting.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class MediaSegment : ProxyBase, IMediaSegment
    {
#pragma warning disable CS0649

        [JsonProperty(nameof(IMediaSegment.SegmentName))]
        private string _segmentName;

        [JsonProperty(nameof(IMediaSegment.TcIn))]
        private TimeSpan _tcIn;

        [JsonProperty(nameof(IMediaSegment.TcOut))]
        private TimeSpan _tcOut;

        [JsonProperty(nameof(IMediaSegment.FieldLengths))]
        private IDictionary<string, int> _fieldLengths;

#pragma warning restore

        public string SegmentName { get => _segmentName; set => Set(value); }

        public TimeSpan TcIn { get => _tcIn; set => Set(value); }

        public TimeSpan TcOut { get => _tcOut; set => Set(value); }

        public void Delete()
        {
            Invoke();
        }
        public IDictionary<string, int> FieldLengths { get => _fieldLengths; set => Set(value); }

        public IMediaSegments Owner { get; }

        public ulong Id { get; set; }

        public void Save()
        {
            Invoke();
        }

        protected override void OnEventNotification(WebSocketMessage message) { }

    }
}
