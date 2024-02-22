using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IMediaSegments
    {
        Guid MediaGuid { get; }
        IEnumerable<IMediaSegment> Segments { get; }
        int Count { get; }
        bool Remove(IMediaSegment segment);
        IMediaSegment Add(TimeSpan tcIn, TimeSpan tcOut, string segmentName);
        event EventHandler<MediaSegmentEventArgs> SegmentAdded;
        event EventHandler<MediaSegmentEventArgs> SegmentRemoved;
    }

    public class MediaSegmentEventArgs: EventArgs
    {
        public MediaSegmentEventArgs(IMediaSegment segment)
        {
            Segment = segment;
        }

        public IMediaSegment Segment { get; private set; }
    }
}
