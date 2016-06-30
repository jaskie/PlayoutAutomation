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
        DateTime LastUpdated { get; set; }
        TMediaCategory MediaCategory { get; set; }
        TParental Parental { get; set; }
        string MediaName { get; set; }
        TMediaStatus MediaStatus { get; set; }
        TMediaType MediaType { get; set; }
        TimeSpan TcPlay { get; set; }
        TimeSpan TcStart { get; set; }
        TVideoFormat VideoFormat { get; set; }
        bool FieldOrderInverted { get; set; }
    }
}
