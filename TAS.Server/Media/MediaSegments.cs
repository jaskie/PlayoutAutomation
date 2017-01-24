using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class MediaSegments : Remoting.Server.DtoBase, Interfaces.IMediaSegments
    {
        private readonly Guid _mediaGuid;
        private readonly ConcurrentDictionary<Guid, IMediaSegment> _segments;
        public MediaSegments(Guid mediaGuid)
        {
            _mediaGuid = mediaGuid;
            _segments = new ConcurrentDictionary<Guid, IMediaSegment>();
        }

        [JsonProperty]
        public Guid MediaGuid { get { return _mediaGuid; } }

        [JsonProperty]
        public IEnumerable<IMediaSegment> Segments { get { return _segments.Values.ToList(); } }

        public IMediaSegment Add(TimeSpan tcIn, TimeSpan tcOut, string segmentName)
        {
            var result = new MediaSegment(this) { TcIn = tcIn, TcOut = tcOut, SegmentName = segmentName };
            if (_segments.TryAdd(result.DtoGuid, result))
            {
                SegmentAdded?.Invoke(this, new MediaSegmentEventArgs(result));
                NotifyPropertyChanged(nameof(Count));
            }
            return result;
        }

        public event EventHandler<MediaSegmentEventArgs> SegmentAdded;
        public event EventHandler<MediaSegmentEventArgs> SegmentRemoved;

        public void Add(IMediaSegment segment)
        {
        }

        public bool Remove(IMediaSegment segment)
        {
            bool result = false;
            IMediaSegment removed;
            if (_segments.TryRemove(((MediaSegment)segment).DtoGuid, out removed))
            {
                result = true;
                SegmentRemoved?.Invoke(this, new MediaSegmentEventArgs(removed));
                NotifyPropertyChanged(nameof(Count));
            }
            return result;
        }

        [JsonProperty]
        public int Count { get { return _segments.Count; } }

    }
}
