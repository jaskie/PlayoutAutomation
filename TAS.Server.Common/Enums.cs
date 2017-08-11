using System;
using System.ComponentModel;
using Infralution.Localization.Wpf;

namespace TAS.Server.Common
{
    [TypeConverter(typeof(TServerTypeEnumConverter))]
    public enum TServerType
    {
        Caspar = 0,
        CasparTVP = 1
    }

    class TServerTypeEnumConverter : ResourceEnumConverter
    {
        public TServerTypeEnumConverter()
            : base(typeof(TServerType), Properties.Enums.ResourceManager)
        { }
    }

    public enum TDirectoryAccessType { Direct, FTP };

    public enum TFileOperationKind { None, Copy, Move, Convert, Export, Delete, Loudness };

    [TypeConverter(typeof(FileOperationStatusEnumConverter))]
    public enum FileOperationStatus
    {
        Unknown,
        Waiting,
        InProgress,
        Finished,
        Failed,
        Aborted,
    };

    class FileOperationStatusEnumConverter : ResourceEnumConverter
    {
        public FileOperationStatusEnumConverter()
            : base(typeof(FileOperationStatus), Properties.Enums.ResourceManager)
        { }
    }

    [Flags]
    public enum AutoStartFlags : byte
    {
        None,
        Force,
        Daily
    }

    [TypeConverter(typeof(IngestStatusEnumConverter))]
    public enum TIngestStatus
    {
        Unknown,
        NotReady,
        InProgress,
        Ready
    }
    class IngestStatusEnumConverter : ResourceEnumConverter
    {
        public IngestStatusEnumConverter()
            : base(typeof(TIngestStatus), Properties.Enums.ResourceManager)
        { }
    }

    [Flags]
    public enum VideoLayer : sbyte
    {
        None = -1,
        Program = 0x10,
        CG1 = Program | 1,
        CG2 = Program | 2,
        CG3 = Program | 3,
        CG4 = Program | 4,
        CG5 = Program | 5,
        Animation = Program | 0xA,
        Preset = 0x2F,
        Preview = 0x30,
        PreviewCG1 = Preview | CG1,
        PreviewCG2 = Preview | CG2,
        PreviewCG3 = Preview | CG3,
        PreviewCG4 = Preview | CG4,
        PreviewCG5 = Preview | CG5,
        PreviewAnimation = Preview | Animation,
    }
    public enum TEngineOperation { Play, Pause, Stop, Clear, Load, Schedule }
    public enum TEngineState { NotInitialized, Idle, Running, Hold }
    public enum TemplateMethod : byte { Add, Play, Stop, Next, Remove, Clear, Update, Invoke }

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
            : base(typeof(TAspectRatioControl), Properties.Enums.ResourceManager)
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
            : base(typeof(TAspectConversion), Properties.Enums.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TmXFAudioExportFormatEnumConverter))]
    public enum TmXFAudioExportFormat : byte
    {
        Channels4Bits16,
        Channels8Bits16,
        Channels4Bits24,
    }

    class TmXFAudioExportFormatEnumConverter : ResourceEnumConverter
    {
        public TmXFAudioExportFormatEnumConverter()
            : base(typeof(TmXFAudioExportFormat), Properties.Enums.ResourceManager)
        { }
    }

    public enum TmXFVideoExportFormat : byte
    {
        IMX50,
        IMX40,
        IMX30,
        DV25
    }

    public enum TMovieContainerFormat
    {
        mov,
        mp4,
        mxf,
    }
    [TypeConverter(typeof(TIngestDirectoryKindEnumConverter))]
    public enum TIngestDirectoryKind
    {
        WatchFolder,
        BmdMediaExpressWatchFolder,
        XDCAM
    }

    class TIngestDirectoryKindEnumConverter : ResourceEnumConverter
    {
        public TIngestDirectoryKindEnumConverter()
            : base(typeof(TIngestDirectoryKind), Properties.Enums.ResourceManager)
        { }
    }

    public enum TArchivePolicyType { NoArchive, ArchivePlayedAndNotUsedWhenDeleteEvent };
    class TArchivePolicyTypeConversionEnumConverter : ResourceEnumConverter
    {
        public TArchivePolicyTypeConversionEnumConverter()
            : base(typeof(TArchivePolicyType), Properties.Enums.ResourceManager)
        { }
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
            : base(typeof(TAudioChannelMappingConversion), Properties.Enums.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TVideoFormatEnumConverter))]
    public enum TVideoFormat : byte
    {
        PAL_FHA = 0x0,
        PAL = 0x1,
        NTSC = 0x2,
        PAL_FHA_P = 0x3,
        PAL_P = 0x4,
        NTSC_FHA = 0x5,
        HD720p2500 = 0x8,
        HD720p5000 = 0x9,
        HD720p5994 = 0xA,
        HD720p6000 = 0xB,
        HD1080p2398 = 0x10,
        HD1080p2400 = 0x11,
        HD1080p2500 = 0x12,
        HD1080p2997 = 0x13,
        HD1080p3000 = 0x14,
        HD1080p5000 = 0x15,
        HD1080i5000 = 0x16,
        HD1080p5994 = 0x17,
        HD1080i5994 = 0x18,
        HD1080p6000 = 0x19,
        HD1080i6000 = 0x1A,
        HD2160p2398 = 0x20,
        HD2160p2400 = 0x21,
        HD2160p2500 = 0x22,
        HD2160p2997 = 0x23,
        HD2160p3000 = 0x24,
        HD2160p5000 = 0x25,
        HD2160p5994 = 0x26,
        HD2160p6000 = 0x27,
        Other = 0xFF
    }
    class TVideoFormatEnumConverter : ResourceEnumConverter
    {
        public TVideoFormatEnumConverter()
            : base(typeof(TVideoFormat), Properties.Enums.ResourceManager)
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
        Insert,
        Jingle
    };
    class TMediaCategoryEnumConverter : ResourceEnumConverter
    {
        public TMediaCategoryEnumConverter()
            : base(typeof(TMediaCategory), Properties.Enums.ResourceManager)
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
            : base(typeof(TAudioChannelMapping), Properties.Enums.ResourceManager)
        { }
    }
    [TypeConverter(typeof(TMediaTypeEnumConverter))]
    public enum TMediaType
    {
        Unknown,
        Movie,
        Still,
        Audio,
        Animation,
    };
    class TMediaTypeEnumConverter : ResourceEnumConverter
    {
        public TMediaTypeEnumConverter()
            : base(typeof(TMediaType), Properties.Enums.ResourceManager)
        { }
    }
    [TypeConverter(typeof(TMediaStatusEnumConverter))]
    public enum TMediaStatus : byte
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
            : base(typeof(TMediaStatus), Properties.Enums.ResourceManager)
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
            : base(typeof(TMediaErrorInfo), Properties.Enums.ResourceManager)
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
            : base(typeof(TFieldOrder), Properties.Enums.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TEventTypeEnumConverter))]
    public enum TEventType
    {
        Rundown = 0,
        Movie = 1,
        StillImage = 2,
        Live = 4,
        Container = 5,
        Animation = 6,
        CommandScript = 7,
    };
    class TEventTypeEnumConverter : ResourceEnumConverter
    {
        public TEventTypeEnumConverter()
            : base(typeof(TEventType), Properties.Enums.ResourceManager)
        { }
    }

    [TypeConverter(typeof(TStartTypeEnumConverter))]
    public enum TStartType
    {
        After,
        WithParent,
        Manual,
        OnFixedTime,
        None,
        WithParentFromEnd
    };
    class TStartTypeEnumConverter : ResourceEnumConverter
    {
        public TStartTypeEnumConverter()
            : base(typeof(TStartType), Properties.Enums.ResourceManager)
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
            : base(typeof(TPlayState), Properties.Enums.ResourceManager)
        { }
    }

    public enum TTransitionType
    {
        Cut = 0,
        Mix = 1,
        Push = 2,
        Slide = 3,
        Wipe = 4,
        Squeeze = 5,
    };

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
        [Color(0xFF98F098)]
        Green = 8,
        [Color(0xFFAFEEEE)]
        Turquoise = 9
    }
    class TMediaEmphasisEnumConverter : ResourceEnumConverter
    {
        public TMediaEmphasisEnumConverter()
            : base(typeof(TMediaEmphasis), Properties.Enums.ResourceManager)
        { }
    }

    public enum TEasing
    {
        Linear = 1,
        None,
        InQuad,
        OutQuad,
        InOutQuad,
        OutInQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        OutInCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        OutInQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        OutInQuint,
        InSine,
        OutSine,
        InOutSine,
        OutInSine,
        InExpo,
        OutExpo,
        InOutExpo,
        OutInExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        OutInCirc,
        InElastic,
        OutElastic,
        InOutElastic,
        OutInElastic,
        InBack,
        OutBack,
        InOutBack,
        OutInBack,
        OutBounce,
        InBounce,
        InOutBounce,
        OutInBounce
    }

    public enum TVideoCodec
    {
        copy,
        mpeg2video,
        libx264
    }

    public enum TAudioCodec
    {
        copy,
        aac,
        libmp3lame,
        ac3,
        mp2
    }

    public enum TCrawlEnableBehavior : byte
    {
        Never,
        ShowsOnly,
        AllButCommercials
    }

    public enum TDeckState
    {
        Unknown = 0,
        NotInVtrControlMode = 2,
        Playing = 4,
        Pecording = 8,
        Still = 0x10,
        ShuttleForward = 0x20,
        ShuttleReverse = 0x40,
        JogForward = 0x80,
        JogReverse = 0x100,
        Stopped = 0x200
    }

    public enum  TDeckControl
    {
        None,
        ExportPrepare,
        ExportComplete,
        Aborted,
        CapturePrepare,
        CaptureComplete
    }

    /// <summary>
    /// Scurity Object (user or role) kind
    /// </summary>
    public enum SceurityObjectType
    {
        User,
        Group,
    }

    class AuthenticationSourceEnumConverter : ResourceEnumConverter
    {
        public AuthenticationSourceEnumConverter()
            : base(typeof(AuthenticationSource), Properties.Enums.ResourceManager)
        { }
    }

    [TypeConverter(typeof(AuthenticationSourceEnumConverter))]
    public enum AuthenticationSource
    {
        Console, // user 
        WindowsCredentials,
        IpAddress
    }

}
