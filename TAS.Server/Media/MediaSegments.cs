using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class MediaSegments : ServerObjectBase, IMediaSegments
    {
        private readonly Dictionary<Guid, IMediaSegment> _segments = new Dictionary<Guid, IMediaSegment>();

        public MediaSegments(Guid mediaGuid)
        {
            MediaGuid = mediaGuid;
        }

        [DtoMember]
        public Guid MediaGuid { get; }

        [DtoMember]
        public IEnumerable<IMediaSegment> Segments
        {
            get
            {
                lock (((IDictionary) _segments).SyncRoot)
                    return _segments.Values.ToList();
            }
        }

        [DtoMember]
        public int Count
        {
            get
            {
                lock (((IDictionary) _segments).SyncRoot)
                    return _segments.Count;
            }
        }


        public IMediaSegment Add(TimeSpan tcIn, TimeSpan tcOut, string segmentName)
        {
            var result = new MediaSegment(this) {TcIn = tcIn, TcOut = tcOut, SegmentName = segmentName};
            lock (((IDictionary) _segments).SyncRoot)
                _segments[result.DtoGuid] = result;
            SegmentAdded?.Invoke(this, new MediaSegmentEventArgs(result));
            NotifyPropertyChanged(nameof(Count));
            return result;
        }

        public bool Remove(IMediaSegment segment)
        {
            bool result;
            lock (((IDictionary) _segments).SyncRoot)
                result = _segments.Remove(((MediaSegment) segment).DtoGuid);
            if (result)
            {
                SegmentRemoved?.Invoke(this, new MediaSegmentEventArgs(segment));
                NotifyPropertyChanged(nameof(Count));
            }
            return result;
        }

        public event EventHandler<MediaSegmentEventArgs> SegmentAdded;
        public event EventHandler<MediaSegmentEventArgs> SegmentRemoved;
    }
}
