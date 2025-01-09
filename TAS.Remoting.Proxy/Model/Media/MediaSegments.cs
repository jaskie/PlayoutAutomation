using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model.Media
{
    public class MediaSegments : ProxyObjectBase, IMediaSegments
    {
        #pragma warning disable CS0649

        [DtoMember(nameof(IMediaSegments.Segments))]
        private List<IMediaSegment> _segments;

        [DtoMember(nameof(IMediaSegments.Count))]
        private int _count;

        [DtoMember(nameof(IMediaSegments.MediaGuid))]
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

        protected override void OnEventNotification(string eventName, EventArgs eventArgs)
        {
            switch (eventName)
            {
                case nameof(IMediaSegments.SegmentAdded):
                    var eAdded = (MediaSegmentEventArgs)eventArgs;
                    _segments.Add(eAdded.Segment as MediaSegment);
                    _segmentAdded?.Invoke(this, eAdded);
                    return;
                case nameof(IMediaSegments.SegmentRemoved):
                    var eRemoved = (MediaSegmentEventArgs)eventArgs;
                    _segments.Remove(eRemoved.Segment as MediaSegment);
                    _segmentRemoved?.Invoke(this, eRemoved);
                    return;
            }
            base.OnEventNotification(eventName, eventArgs);
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
