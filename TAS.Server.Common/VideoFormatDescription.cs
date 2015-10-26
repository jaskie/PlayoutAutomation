using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TAS.Common
{
    public sealed class VideoFormatDescription
    {
        public readonly TVideoFormat Format;
        public readonly Size ImageSize;
        public readonly RationalNumber FrameRate;
        public readonly bool Interlaced;
        public readonly RationalNumber SAR;
        private VideoFormatDescription(TVideoFormat format)
        {
            this.Format = format;
            switch (format)
            {
                case TVideoFormat.PAL_FHA:
                    Interlaced = true;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(64, 45);
                    break;
                case TVideoFormat.PAL:
                    Interlaced = true;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(16, 15);
                    break;
                case TVideoFormat.PAL_FHA_P:
                    Interlaced = false;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(64, 45);
                    break;
                case TVideoFormat.PAL_P:
                    Interlaced = false;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(16, 15);
                    break;
                case TVideoFormat.NTSC_FHA:
                    Interlaced = true;
                    ImageSize = new Size(640, 486);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(40, 33);
                    break;
                case TVideoFormat.NTSC:
                    Interlaced = true;
                    ImageSize = new Size(640, 486);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(10, 11);
                    break;
                case TVideoFormat.HD1080i5000:
                    Interlaced = true;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080i5994:
                    Interlaced = true;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(2997, 100);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080i6000:
                    Interlaced = true;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p2398:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(2398, 100);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p2400:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(24, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p2500:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p2997:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(2997, 100);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p3000:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p5000:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(50, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p5994:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(5994, 100);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD1080p6000:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(60, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD2160p2398:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(2398, 100);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD2160p2400:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(24, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD2160p2500:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD2160p2997:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(2997, 100);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD2160p3000:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD720p2500:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD720p5000:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(50, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD720p5994:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(5994, 100);
                    SAR = new RationalNumber(1, 1);
                    break;
                case TVideoFormat.HD720p6000:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(60, 1);
                    SAR = new RationalNumber(1, 1);
                    break;
            }
        }

        private VideoFormatDescription(Size imageSize, RationalNumber frameRate, RationalNumber sar, bool interlaced)
        {
            Format = TVideoFormat.Other;
            Interlaced = interlaced;
            ImageSize = imageSize;
            FrameRate = frameRate;
            SAR = sar;
        }


        public static Dictionary<TVideoFormat, VideoFormatDescription> Descriptions = new Dictionary<TVideoFormat, VideoFormatDescription>()
        {
            {TVideoFormat.PAL_FHA, new VideoFormatDescription(TVideoFormat.PAL_FHA)},
            {TVideoFormat.PAL, new VideoFormatDescription(TVideoFormat.PAL)},
            {TVideoFormat.PAL_FHA_P, new VideoFormatDescription(TVideoFormat.PAL_FHA_P)},
            {TVideoFormat.PAL_P, new VideoFormatDescription(TVideoFormat.PAL_P)},
            {TVideoFormat.NTSC_FHA, new VideoFormatDescription(TVideoFormat.NTSC_FHA)},
            {TVideoFormat.NTSC, new VideoFormatDescription(TVideoFormat.NTSC)},
            {TVideoFormat.HD1080i5000, new VideoFormatDescription(TVideoFormat.HD1080i5000)},
            {TVideoFormat.HD1080i5994, new VideoFormatDescription(TVideoFormat.HD1080i5994)},
            {TVideoFormat.HD1080i6000, new VideoFormatDescription(TVideoFormat.HD1080i6000)},
            {TVideoFormat.HD1080p2398, new VideoFormatDescription(TVideoFormat.HD1080p2398)},
            {TVideoFormat.HD1080p2400, new VideoFormatDescription(TVideoFormat.HD1080p2400)},
            {TVideoFormat.HD1080p2500, new VideoFormatDescription(TVideoFormat.HD1080p2500)},
            {TVideoFormat.HD1080p2997, new VideoFormatDescription(TVideoFormat.HD1080p2997)},
            {TVideoFormat.HD1080p3000, new VideoFormatDescription(TVideoFormat.HD1080p3000)},
            {TVideoFormat.HD1080p5000, new VideoFormatDescription(TVideoFormat.HD1080p5000)},
            {TVideoFormat.HD1080p5994, new VideoFormatDescription(TVideoFormat.HD1080p5994)},
            {TVideoFormat.HD1080p6000, new VideoFormatDescription(TVideoFormat.HD1080p6000)},
            {TVideoFormat.HD2160p2398, new VideoFormatDescription(TVideoFormat.HD2160p2398)},
            {TVideoFormat.HD2160p2400, new VideoFormatDescription(TVideoFormat.HD2160p2400)},
            {TVideoFormat.HD2160p2500, new VideoFormatDescription(TVideoFormat.HD2160p2500)},
            {TVideoFormat.HD2160p2997, new VideoFormatDescription(TVideoFormat.HD2160p2997)},
            {TVideoFormat.HD2160p3000, new VideoFormatDescription(TVideoFormat.HD2160p3000)},
            {TVideoFormat.HD720p2500, new VideoFormatDescription(TVideoFormat.HD720p2500)},
            {TVideoFormat.HD720p5000, new VideoFormatDescription(TVideoFormat.HD720p5000)},
            {TVideoFormat.HD720p5994, new VideoFormatDescription(TVideoFormat.HD720p5994)},
            {TVideoFormat.HD720p6000, new VideoFormatDescription(TVideoFormat.HD720p6000)},
            {TVideoFormat.Other, new VideoFormatDescription(TVideoFormat.Other)},
        };
        
        public static VideoFormatDescription Match(Size imageSize, RationalNumber frameRate, RationalNumber sar, bool interlaced)
        {
            var result = Descriptions.Values.FirstOrDefault((v) => v.ImageSize.Equals(imageSize) 
                                                                && (v.FrameRate.Equals(frameRate) || frameRate.Equals(RationalNumber.Zero))
                                                                && (v.SAR.Equals(sar) || sar.Equals(RationalNumber.Zero))
                                                                && v.Interlaced == interlaced);
            return result != null ? result : new VideoFormatDescription(imageSize, frameRate, sar, interlaced);
        }


        public TimeSpan FrameDuration { get { return FrameRate.IsInvalid ? TimeSpan.Zero : new TimeSpan(TimeSpan.TicksPerSecond * FrameRate.Den / FrameRate.Num); } }
        public long FrameTicks { get { return FrameRate.IsInvalid ? 0L : TimeSpan.TicksPerSecond * FrameRate.Den / FrameRate.Num; } }
    }
}
