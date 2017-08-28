using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public static class MediaExtensions
    {
        public static VideoFormatDescription FormatDescription(this IMedia media)
        {
            TVideoFormat format = media.VideoFormat;
            if (!Enum.IsDefined(typeof(TVideoFormat), format))
                format = TVideoFormat.Other;
            return VideoFormatDescription.Descriptions[format];
        }

        public static RationalNumber FrameRate(this IMedia media)
        {
            TVideoFormat format = media.VideoFormat;
            if (!Enum.IsDefined(typeof(TVideoFormat), format))
                format = TVideoFormat.Other;
            return VideoFormatDescription.Descriptions[format].FrameRate;
        }

        public static TimeSpan TcLastFrame(this IMedia media)
        {
            var frameRate = FrameRate(media);
            return ((media.TcStart + media.Duration).ToSMPTEFrames(frameRate) - 1).SMPTEFramesToTimeSpan(frameRate);
        }

        public static string MakeFileName(string idAux, string mediaName, string fileExtension)
        {
            var filenameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(idAux))
                filenameParts.Add(idAux);
            if (!string.IsNullOrWhiteSpace(mediaName))
                filenameParts.Add(mediaName);
            return (FileUtils.SanitizeFileName(string.Join(" ", filenameParts)) +
                   fileExtension).Trim();
        }
    }
}
