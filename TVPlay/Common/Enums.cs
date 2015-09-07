using Infralution.Localization.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Common
{

    public enum VideoLayer : sbyte
    {
        None = -1,
        Program = 0,
        CG1 = 1,
        CG2 = 2,
        CG3 = 3,
        Preset = 9,
        Preview = 10
    }
    public enum TEngineOperation { Start, Play, Pause, Stop, Clear, Load, Schedule }
    public enum TEngineState { NotInitialized, Idle, Running, Hold }

    [Flags]
    [TypeConverter(typeof(TAspectRatioControlEnumConverter))]
    public enum TAspectRatioControl
    {
        None,
        GPI,
        ImageResize,
        GPIandImageResize = GPI + ImageResize,
    }
    class TAspectRatioControlEnumConverter : ResourceEnumConverter
    {
        public TAspectRatioControlEnumConverter()
            : base(typeof(TAspectRatioControl), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TAspectConversionEnumConverter))]
    public enum TAspectConversion : byte
    {
        NoConversion,
        Force4_3,
        Force16_9,
        PillarBox,
        TiltScan,
        Letterbox,
        PanScan
    }
    class TAspectConversionEnumConverter : ResourceEnumConverter
    {
        public TAspectConversionEnumConverter()
            : base(typeof(TAspectConversion), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    public enum TxDCAMAudioExportFormat: byte
    {
        Channels4Bits16,
        Channels4Bits24,
    }

    class TxDCAMAudioExportFormatEnumConverter : ResourceEnumConverter
    {
        public TxDCAMAudioExportFormatEnumConverter()
            : base(typeof(TxDCAMAudioExportFormat), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }


    public enum TxDCAMVideoExportFormat: byte
    {
        IMX50,
        IMX40,
        IMX30
    }

    [TypeConverter(typeof(TAudioChannelMappingConversionEnumConverter))]
    public enum TAudioChannelMappingConversion : byte
    {
        Default,
        FirstTwoChannels,
        SecondTwoChannels,
        FirstChannelOnly, 
        SecondChannelOnly,
        Combine1plus2,
        Combine3plus4
    }
    class TAudioChannelMappingConversionEnumConverter : ResourceEnumConverter
    {
        public TAudioChannelMappingConversionEnumConverter()
            : base(typeof(TAudioChannelMappingConversion), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TVideoFormatEnumConverter))]
    public enum TVideoFormat : byte
    {
        PAL_FHA    = 0x0,
        PAL        = 0x1,
        PAL_FHA_P  = 0x3,
        PAL_P      = 0x4,
        NTSC_FHA   = 0x5,
        NTSC       = 0x6,
        HD720p2500 = 0x8,
        HD720p5000 = 0x9,
        HD720p5994 = 0xA,
        HD720p6000 = 0xB,
        HD1080p2398 = 0x10,
        HD1080p2400 = 0x11,
        HD1080p2500 = 0x12,
        HD1080p2997 = 0x13,
        HD1080p3000 = 0x14,
        HD1080p5000	= 0x15,
        HD1080i5000	= 0x16,
        HD1080p5994	= 0x17,
        HD1080i5994	= 0x18,
        HD1080p6000	= 0x19,
        HD1080i6000	= 0x1A,
        HD2160p2398	= 0x20,
        HD2160p2400	= 0x21,
        HD2160p2500	= 0x22,
        HD2160p2997	= 0x23,
        HD2160p3000 = 0x24,
        Other = 0xFF
    }
    class TVideoFormatEnumConverter : ResourceEnumConverter
    {
        public TVideoFormatEnumConverter()
            : base(typeof(TVideoFormat), TAS.Client.Properties.Resources.ResourceManager)
        { }
        protected override string GetValueText(System.Globalization.CultureInfo culture, object value)
        {
            string resourceName = GetResourceName(value);
            string result = _resourceManager.GetString(resourceName, culture);
            if (result == null)
                result = value.ToString();
            return result;
        }
    }

    [TypeConverter(typeof(TSMPTEFrameRateEnumConverter))]
    public enum TSMPTEFrameRate
    {
        SMPTERate24fps = 24,
        SMPTERate25fps = 25,
        SMPTERate30fps = 30,
        Unknown = 99
    }

    class TSMPTEFrameRateEnumConverter : ResourceEnumConverter
    {
        public TSMPTEFrameRateEnumConverter()
            : base(typeof(TVideoFormat), TAS.Client.Properties.Resources.ResourceManager)
        { }
        protected override string GetValueText(System.Globalization.CultureInfo culture, object value)
        {
            string resourceName = GetResourceName(value);
            string result = _resourceManager.GetString(resourceName, culture);
            if (result == null)
                result = value.ToString();
            return result;
        }
    }



    [TypeConverter(typeof(TMediaCategoryEnumConverter))]
    public enum TMediaCategory
    {
        Uncategorized,
        Show,
        Commercial,
        Promo,
        Sponsored,
        Fill,
    };
    class TMediaCategoryEnumConverter : ResourceEnumConverter
    {
        public TMediaCategoryEnumConverter()
            : base(typeof(TMediaCategory), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TAudioChannelMappingEnumConverter))]
    public enum TAudioChannelMapping : byte
    {
        Unknown = 0,          // will pass everything as is
        Mono = 1,         // 1.0          L
        Stereo = 2,           // 2.0           L R
        Dts = 3,              // 5.1           C L R Ls Rs LFE
        DolbyE = 4,           // 5.1+stereomix L R C LFE Ls Rs Lmix Rmix
        DolbyDigital = 5,     // 5.1           L C R Ls Rs LFE
        Smpte = 6,            // 5.1           L R C LFE Ls Rs
    }
    class TAudioChannelMappingEnumConverter : ResourceEnumConverter
    {
        public TAudioChannelMappingEnumConverter()
            : base(typeof(TAudioChannelMapping), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }
    [TypeConverter(typeof(TMediaTypeEnumConverter))]
    public enum TMediaType
    {
        Unknown,
        Movie,
        Still,
        Audio,
        AnimationFlash,
    };
    class TMediaTypeEnumConverter : ResourceEnumConverter
    {
        public TMediaTypeEnumConverter()
            : base(typeof(TMediaType), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }
    [TypeConverter(typeof(TMediaStatusEnumConverter))]
    public enum TMediaStatus
    {
        Unknown,
        Available,
        CopyPending,
        Copying,
        Copied,
        Deleted,
        CopyError,
        Required,
        ValidationError,
    };
    class TMediaStatusEnumConverter : ResourceEnumConverter
    {
        public TMediaStatusEnumConverter()
            : base(typeof(TMediaStatus), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TMediaErrorInfoEnumConverter))]
    public enum TMediaErrorInfo
    {
        NoError,
        Missing,
        TooShort,
    }
    class TMediaErrorInfoEnumConverter : ResourceEnumConverter
    {
        public TMediaErrorInfoEnumConverter()
            : base(typeof(TMediaStatus), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }


    [TypeConverter(typeof(TFieldOrderEnumConverter))]
    public enum TFieldOrder
    {
        Unknown,
        TFF,
        BFF,
        Progressive
    }
    class TFieldOrderEnumConverter : ResourceEnumConverter
    {
        public TFieldOrderEnumConverter()
            : base(typeof(TFieldOrder), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TEventTypeEnumConverter))]
    public enum TEventType
    {
        Rundown,
        Movie,
        StillImage,
        AnimationFlash,
        Live,
        Container,
    };
    class TEventTypeEnumConverter : ResourceEnumConverter
    {
        public TEventTypeEnumConverter()
            : base(typeof(TEventType), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TStartTypeEnumConverter))]
    public enum TStartType
    {
        After,
        With,
        Manual,
        OnFixedTime,
        None,
    };
    class TStartTypeEnumConverter : ResourceEnumConverter
    {
        public TStartTypeEnumConverter()
            : base(typeof(TStartType), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TPlayStateEnumConverter))]
    public enum TPlayState
    {
        Scheduled,
        Paused,
        Playing,
        Fading,
        Played,
        Aborted
    };
    class TPlayStateEnumConverter : ResourceEnumConverter
    {
        public TPlayStateEnumConverter()
            : base(typeof(TPlayState), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TLogoEnumConverter))]
    public enum TLogo
    {
        NoLogo,
        Normal,
        Live,
        Premiere,
        Replay,
    }
    class TLogoEnumConverter : ResourceEnumConverter
    {
        public TLogoEnumConverter()
            : base(typeof(TLogo), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }


    [TypeConverter(typeof(TCrawlEnumConverter))]
    public enum TCrawl
    {
        NoCrawl,
        Normal,
        Urgent,
        Sport,
    }
    class TCrawlEnumConverter : ResourceEnumConverter
    {
        public TCrawlEnumConverter()
            : base(typeof(TCrawl), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }


    public enum TTransitionType { Cut = 0, Mix = 1, Push = 2, Slide = 3, Wipe = 4 };
    
    [TypeConverter(typeof(TParentalEnumConverter))]
    public enum TParental
    {
        None,
        NoLimit,
        Limit07,
        Limit12,
        Limit16,
        Limit18,
    }
    class TParentalEnumConverter:ResourceEnumConverter
    {
        public TParentalEnumConverter()
            : base(typeof(TParental), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TMediaEmphasisEnumConverter))]
    public enum TMediaEmphasis : byte
    {
        [Color(0x0)]
        None = 0,
        [Color(0xFFC0C000)]
        Olive = 1,
        [Color(0xFFFFB6C1)]
        Pink = 2,
        [Color(0xFFFFE4C4)]
        Beige = 3,
        [Color(0xFF87CEFA)]
        SkyBlue = 4,
        [Color(0xFFFFFFC0)]
        Yellow = 5,
        [Color(0xFFEE82EE)]
        Violet = 6,
        [Color(0xFFFFA500)]
        Orange = 7,
    }
    class TMediaEmphasisEnumConverter : ResourceEnumConverter
    {
        public TMediaEmphasisEnumConverter()
            : base(typeof(TMediaEmphasis), TAS.Client.Properties.Resources.ResourceManager)
        { }
    }

}
