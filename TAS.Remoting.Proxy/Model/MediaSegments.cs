using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class MediaSegments : ProxyObjectBase, IMediaSegments
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IMediaSegments.Segments))]
        private List<MediaSegment> _segments;

        [JsonProperty(nameof(IMediaSegments.Count))]
        private int _count;

        [JsonProperty(nameof(IMediaSegments.MediaGuid))]
        private Guid _mediaGuid;

        public IEnumerable<IMediaSegment> Segments => _segments;

        #pragma warning restore

        public int Count => _count;

        public Guid MediaGuid => _mediaGuid;

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

        protected override void OnEventNotification(SocketMessage message)
        {
            switch (message.MemberName)
            {
                case nameof(IMediaSegments.SegmentAdded):
                    var eAdded = Deserialize<MediaSegmentEventArgs>(message);
                    _segments.Add(eAdded.Segment as MediaSegment);
                    _segmentAdded?.Invoke(this, eAdded);
                    break;
                case nameof(IMediaSegments.SegmentRemoved):
                    var eRemoved = Deserialize<MediaSegmentEventArgs>(message);
                    _segments.Remove(eRemoved.Segment as MediaSegment);
                    _segmentRemoved?.Invoke(this, eRemoved);
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
