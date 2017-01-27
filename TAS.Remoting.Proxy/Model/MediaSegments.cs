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
        private List<MediaSegment> _segments { get { return Get<List<MediaSegment>>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<IMediaSegment> Segments { get { return _segments; } }

        #region Event handling 
        private event EventHandler<MediaSegmentEventArgs> _segmentAdded;
        public event EventHandler<MediaSegmentEventArgs> SegmentAdded
        {
            add
            {
                EventAdd(_segmentAdded);
                _segmentAdded += value;
            }
            remove
            {
                _segmentAdded -= value;
                EventRemove(_segmentAdded);
            }
        }


        private event EventHandler<MediaSegmentEventArgs> _segmentRemoved;
        public event EventHandler<MediaSegmentEventArgs> SegmentRemoved
        {
            add

            {
                EventAdd(_segmentRemoved);
                _segmentRemoved += value;
            }
            remove
            {
                _segmentRemoved -= value;
                EventRemove(_segmentRemoved);
            }
        }

        protected override void OnEventNotification(WebSocketMessage e)
        {
            switch (e.MemberName)
            {
                case nameof(IMediaSegments.SegmentAdded):
                    var eAdded = ConvertEventArgs<MediaSegmentEventArgs>(e);
                    _segments.Add(eAdded.Segment as MediaSegment);
                    _segmentAdded?.Invoke(this, eAdded);
                    break;
                case nameof(IMediaSegments.SegmentRemoved):
                    var eRemoved = ConvertEventArgs<MediaSegmentEventArgs>(e);
                    _segments.Remove(eRemoved.Segment as MediaSegment);
                    _segmentRemoved?.Invoke(this, ConvertEventArgs<MediaSegmentEventArgs>(e));
                    break;
            }
        }

        #endregion //Event handling

        public IMediaSegment Add(TimeSpan tcIn, TimeSpan tcOut, string segmentName)
        {
            return Query<IMediaSegment>(parameters: new object[] { tcIn, tcOut, segmentName });
        }

        public bool Remove(IMediaSegment segment)
        {
            return Query<bool>(parameters: segment);
        }

    }
}
