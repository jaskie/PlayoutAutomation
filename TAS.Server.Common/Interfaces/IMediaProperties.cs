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
        ulong FileSize { get; set; }
        string Folder { get; set; }
        string FullPath { get; }
        DateTime LastUpdated { get; }
        TMediaCategory MediaCategory { get; set; }
        TParental Parental { get; set; }
        Guid MediaGuid { get; }
        string MediaName { get; set; }
        TMediaStatus MediaStatus { get; set; }
        TMediaType MediaType { get; set; }
        TimeSpan TCPlay { get; set; }
        TimeSpan TCStart { get; set; }
        bool Verified { get; set; }
        TVideoFormat VideoFormat { get; set; }
        VideoFormatDescription VideoFormatDescription { get; }
        bool HasExtraLines { get; }
    }
}
