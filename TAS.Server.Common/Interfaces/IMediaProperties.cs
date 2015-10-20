using System;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaProperties
    {
        TAudioChannelMapping AudioChannelMapping { get; set; }
        decimal AudioLevelIntegrated { get; set; }
        decimal AudioLevelPeak { get; set; }
        decimal AudioVolume { get; set; }
        TimeSpan Duration { get; set; }
        TimeSpan DurationPlay { get; set; }
        string FileName { get; set; }
        ulong FileSize { get; }
        string Folder { get; }
        string FullPath { get; set; }
        DateTime LastUpdated { get; }
        TMediaCategory MediaCategory { get; set; }
        Guid MediaGuid { get; }
        string MediaName { get; set; }
        TMediaStatus MediaStatus { get; }
        TMediaType MediaType { get; set; }
        TimeSpan TCPlay { get; set; }
        TimeSpan TCStart { get; set; }
        bool Verified { get; }
        TVideoFormat VideoFormat { get; set; }
        VideoFormatDescription VideoFormatDescription { get; }
    }
}
