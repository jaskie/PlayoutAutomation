using System;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IMedia: IMediaProperties, INotifyPropertyChanged
    {
        IMediaDirectory Directory { get; }
        bool FileExists();
        bool Delete();
        bool IsVerified { get; set; }
        void ReVerify();
        void Verify();
        void GetLoudness();
    }

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
        byte Parental { get; set; }
        string MediaName { get; set; }
        TMediaStatus MediaStatus { get; set; }
        TMediaType MediaType { get; set; }
        TimeSpan TcPlay { get; set; }
        TimeSpan TcStart { get; set; }
        TVideoFormat VideoFormat { get; set; }
        bool FieldOrderInverted { get; set; }
        Guid MediaGuid { get; }
    }
}
