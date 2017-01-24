using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class MediaSegments : ProxyBase, IMediaSegments
    {
        public int Count { get { return Get<int>(); } set { SetLocalValue(value); } }
        
        public Guid MediaGuid { get { return Get<Guid>(); } set { SetLocalValue(value); } }

        [JsonProperty(nameof(IMediaSegments.Segments))]
        private List<MediaSegment> _segments { get { return Get<List<MediaSegment>>(); }  set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<IMediaSegment> Segments { get { return _segments; } }

        public event EventHandler<MediaSegmentEventArgs> SegmentAdded;
        public event EventHandler<MediaSegmentEventArgs> SegmentRemoved;

        public IMediaSegment Add(TimeSpan tcIn, TimeSpan tcOut, string segmentName)
        {
            return Query<IMediaSegment>(parameters: new object[] { tcIn, tcOut, segmentName });
        }

        public bool Remove(IMediaSegment segment)
        {
            return Query<bool>(parameters: segment);
        }

        protected override void OnEventNotification(WebSocketMessage e)
        {
            throw new NotImplementedException();
        }
    }
}
