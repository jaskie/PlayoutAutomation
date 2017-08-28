using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TAS.Common.Interfaces
{
    public interface IMediaSegments
    {
        Guid MediaGuid { get; }
        IEnumerable<IMediaSegment> Segments { get; }
        bool Remove(IMediaSegment segment);
        int Count { get; }
        IMediaSegment Add(TimeSpan tcIn, TimeSpan tcOut, string segmentName);
        event EventHandler<MediaSegmentEventArgs> SegmentAdded;
        event EventHandler<MediaSegmentEventArgs> SegmentRemoved;
    }

    [JsonObject(IsReference = false)]
    public class MediaSegmentEventArgs: EventArgs
    {
        public MediaSegmentEventArgs(IMediaSegment segment)
        {
            Segment = segment;
        }
        [JsonProperty(IsReference = true)]
        public IMediaSegment Segment { get; private set; }
    }
}
