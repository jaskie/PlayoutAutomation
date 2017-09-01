using System;
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

        #pragma warning restore

        public string SegmentName { get { return _segmentName; } set { Set(value); } }

        public TimeSpan TcIn { get { return _tcIn; } set { Set(value); } }

        public TimeSpan TcOut { get { return _tcOut; } set { Set(value); } }
        
        public void Delete()
        {
            Invoke();
        }

        public IMediaSegments Owner { get; }
        public ulong Id { get; set; }

        public void Save()
        {
            Invoke();
        }

        protected override void OnEventNotification(WebSocketMessage message) { }

    }
}
