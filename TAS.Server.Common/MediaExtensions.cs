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
            return VideoFormatDescription.Descriptions[media.VideoFormat];
        }
        public static RationalNumber FrameRate(this IMedia media)
        {
            return VideoFormatDescription.Descriptions[media.VideoFormat].FrameRate;
        }
    }
}
