using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.Interfaces;

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
            return ((media.TcStart + media.Duration).ToSMPTEFrames(frameRate) -1).SMPTEFramesToTimeSpan(frameRate);
        }

    }
}
