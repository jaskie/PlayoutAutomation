﻿using System;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class MediaProxy : IMediaProperties
    {
        public TAudioChannelMapping AudioChannelMapping { get; set; }
        public double AudioLevelIntegrated { get; set; }
        public double AudioLevelPeak { get; set; }
        public double AudioVolume { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan DurationPlay { get; set; }
        public string FileName { get; set; }
        public ulong FileSize { get; set; }
        public string Folder { get; set; }
        public DateTime LastUpdated { get; set; }
        public TMediaCategory MediaCategory { get; set; }
        public byte Parental { get; set; }
        public string MediaName { get; set; }
        public TMediaStatus MediaStatus { get; set; }
        public TMediaType MediaType { get; set; }
        public TimeSpan TcPlay { get; set; }
        public TimeSpan TcStart { get; set; }
        public TVideoFormat VideoFormat { get; set; }
        public bool FieldOrderInverted { get; set; }
        public Guid MediaGuid { get; set; }
        public bool HasTransparency { get; set; }

        public static MediaProxy FromMedia(IMediaProperties media)
        {
            return new MediaProxy
            {
                AudioChannelMapping = media.AudioChannelMapping,
                AudioLevelIntegrated = media.AudioLevelIntegrated,
                AudioLevelPeak = media.AudioLevelPeak,
                AudioVolume = media.AudioVolume,
                Duration = media.Duration,
                DurationPlay = media.DurationPlay,
                FileName = media.FileName,
                FileSize = media.FileSize,
                Folder = media.Folder,
                LastUpdated = media.LastUpdated,
                MediaCategory = media.MediaCategory,
                MediaName = media.MediaName,
                MediaStatus = media.MediaStatus,
                MediaType = media.MediaType,
                Parental = media.Parental,
                TcPlay = media.TcPlay,
                TcStart = media.TcStart,
                VideoFormat = media.VideoFormat,
                FieldOrderInverted = media.FieldOrderInverted,
                MediaGuid = media.MediaGuid,
            };
        }
    }

    public class PersistentMediaProxy: MediaProxy, IPersistentMediaProperties
    {
        public TMediaEmphasis MediaEmphasis { get; set; }
        public DateTime? KillDate { get; set; }
        public ulong IdProgramme { get; set; }
        public ulong IdPersistentMedia { get; set; }
        public bool IsProtected { get; set; }
        public string IdAux { get; set; }

        public static PersistentMediaProxy FromMedia(IPersistentMediaProperties media)
        {
            return new PersistentMediaProxy
            {
                AudioChannelMapping = media.AudioChannelMapping,
                AudioLevelIntegrated = media.AudioLevelIntegrated,
                AudioLevelPeak = media.AudioLevelPeak,
                AudioVolume = media.AudioVolume,
                Duration = media.Duration,
                DurationPlay = media.DurationPlay,
                FileName = media.FileName,
                FileSize = media.FileSize,
                Folder = media.Folder,
                LastUpdated = media.LastUpdated,
                MediaCategory = media.MediaCategory,
                MediaName = media.MediaName,
                MediaStatus = media.MediaStatus,
                MediaType = media.MediaType,
                Parental = media.Parental,
                TcPlay = media.TcPlay,
                TcStart = media.TcStart,
                VideoFormat = media.VideoFormat,
                FieldOrderInverted = media.FieldOrderInverted,
                MediaGuid = media.MediaGuid,
                IdAux = media.IdAux,
                IdProgramme = media.IdProgramme,
                KillDate = media.KillDate,
                MediaEmphasis = media.MediaEmphasis,
            };
        }
    }
}
