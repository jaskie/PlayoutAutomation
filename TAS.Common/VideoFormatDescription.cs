using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TAS.Common
{
    public sealed class VideoFormatDescription
    {
        #region Constructors
        private VideoFormatDescription(TVideoFormat format)
        {
            Format = format;
            switch (format)
            {
                case TVideoFormat.Other:
                case TVideoFormat.Unknown:
                    ImageSize = new Size(0, 0);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(0, 1);
                    break;
                case TVideoFormat.PAL_FHA:
                    Interlaced = true;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(64, 45);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.PAL:
                    Interlaced = true;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(16, 15);
                    IsWideScreen = false;
                    break;
                case TVideoFormat.PAL_FHA_P:
                    Interlaced = false;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(64, 45);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.PAL_P:
                    Interlaced = false;
                    ImageSize = new Size(720, 576);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(16, 15);
                    IsWideScreen = false;
                    break;
                case TVideoFormat.NTSC_FHA:
                    Interlaced = true;
                    ImageSize = new Size(720, 480);
                    FrameRate = new RationalNumber(30000, 1001);
                    SAR = new RationalNumber(32, 27);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.NTSC:
                    Interlaced = true;
                    ImageSize = new Size(720, 480);
                    FrameRate = new RationalNumber(30000, 1001);
                    SAR = new RationalNumber(8, 9);
                    IsWideScreen = false;
                    break;
                case TVideoFormat.HD1080i5000:
                    Interlaced = true;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080i5994:
                    Interlaced = true;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(30000, 1001);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080i6000:
                    Interlaced = true;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p2398:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(2398, 100);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p2400:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(24, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p2500:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p2997:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(30000, 1001);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p3000:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p5000:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(50, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p5994:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(5994, 100);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD1080p6000:
                    Interlaced = false;
                    ImageSize = new Size(1920, 1080);
                    FrameRate = new RationalNumber(60, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p2398:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(2398, 100);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p2400:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(24, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p2500:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p2997:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(30000, 1001);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p3000:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(30, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p5000:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(50, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p5994:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(5994, 100);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD2160p6000:
                    Interlaced = false;
                    ImageSize = new Size(3840, 2160);
                    FrameRate = new RationalNumber(60, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD720p2500:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(25, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD720p5000:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(50, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD720p5994:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(5994, 100);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
                    break;
                case TVideoFormat.HD720p6000:
                    Interlaced = false;
                    ImageSize = new Size(1440, 720);
                    FrameRate = new RationalNumber(60, 1);
                    SAR = new RationalNumber(1, 1);
                    IsWideScreen = true;
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
            IsWideScreen = true;
        }

        private VideoFormatDescription() { }
        #endregion
        
        public TVideoFormat Format { get; }
        
        public Size ImageSize { get; }

        public RationalNumber FrameRate { get; }

        public bool Interlaced { get; }

        public RationalNumber SAR { get; }

        public bool IsWideScreen { get; }

        public static Dictionary<TVideoFormat, VideoFormatDescription> Descriptions = new Dictionary<TVideoFormat, VideoFormatDescription>
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
            {TVideoFormat.HD2160p5000, new VideoFormatDescription(TVideoFormat.HD2160p5000)},
            {TVideoFormat.HD2160p5994, new VideoFormatDescription(TVideoFormat.HD2160p5994)},
            {TVideoFormat.HD2160p6000, new VideoFormatDescription(TVideoFormat.HD2160p6000)},
            {TVideoFormat.HD720p2500, new VideoFormatDescription(TVideoFormat.HD720p2500)},
            {TVideoFormat.HD720p5000, new VideoFormatDescription(TVideoFormat.HD720p5000)},
            {TVideoFormat.HD720p5994, new VideoFormatDescription(TVideoFormat.HD720p5994)},
            {TVideoFormat.HD720p6000, new VideoFormatDescription(TVideoFormat.HD720p6000)},
            {TVideoFormat.Other, new VideoFormatDescription(TVideoFormat.Other)},
            {TVideoFormat.Unknown, new VideoFormatDescription(TVideoFormat.Unknown)},
        };
        
        public static VideoFormatDescription Match(Size imageSize, RationalNumber frameRate, RationalNumber sar, bool interlaced)
        {
            var result = Descriptions.Values.FirstOrDefault(v => v.ImageSize.Equals(imageSize) 
                                                                && (v.FrameRate.Equals(frameRate) || frameRate.Equals(RationalNumber.Zero))
                                                                && (v.SAR.Equals(sar) || sar.Equals(RationalNumber.Zero))
                                                                && v.Interlaced == interlaced);
            if (result != null)
                return result;
            if (imageSize.Height == Descriptions[TVideoFormat.PAL].ImageSize.Height && 
                frameRate == Descriptions[TVideoFormat.PAL].FrameRate &&
                imageSize.Width == 768)
                return interlaced ? Descriptions[TVideoFormat.PAL] : Descriptions[TVideoFormat.PAL_P];
            if (imageSize.Height == Descriptions[TVideoFormat.PAL].ImageSize.Height && imageSize.Width == Descriptions[TVideoFormat.PAL].ImageSize.Width &&
                frameRate == Descriptions[TVideoFormat.PAL].FrameRate &&
                sar.Equals(new RationalNumber(59, 54)))
                return interlaced ? Descriptions[TVideoFormat.PAL] : Descriptions[TVideoFormat.PAL_P];
            if (imageSize.Height == Descriptions[TVideoFormat.PAL_FHA].ImageSize.Height &&
                frameRate == Descriptions[TVideoFormat.PAL_FHA].FrameRate &&
                (imageSize.Width == Descriptions[TVideoFormat.PAL].ImageSize.Width || imageSize.Width == 1024 || imageSize.Width == 1050))
                return interlaced ? Descriptions[TVideoFormat.PAL_FHA] : Descriptions[TVideoFormat.PAL_FHA_P];
            return new VideoFormatDescription(imageSize, frameRate, sar, interlaced); 
        }

        public TimeSpan FrameDuration => FrameRate.IsZero ? TimeSpan.Zero : new TimeSpan(TimeSpan.TicksPerSecond * FrameRate.Den / FrameRate.Num);
        public long FrameTicks => FrameRate.IsZero ? 0L : TimeSpan.TicksPerSecond * FrameRate.Den / FrameRate.Num;

        public override string ToString() => Format.ToString();
    }
}
