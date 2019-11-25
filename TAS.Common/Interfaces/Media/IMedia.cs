using System;
using System.ComponentModel;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces.Media
{
    public interface IMedia: IMediaProperties, INotifyPropertyChanged
    {
        IMediaDirectory Directory { get; }
        bool FileExists();
        bool Delete();
        bool IsVerified { get; }
        void Verify(bool updateFormatAndDurations);
        void GetLoudness();
        bool RenameFileTo(string newFileName);
    }

    public interface IMediaProperties
    {
        TAudioChannelMapping AudioChannelMapping { get; set; }
        double AudioLevelIntegrated { get; set; }
        double AudioLevelPeak { get; set; }
        double AudioVolume { get; set; }
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
        Guid MediaGuid { get; set; }
    }
}
