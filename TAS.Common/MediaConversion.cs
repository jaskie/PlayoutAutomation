using System;
using System.Collections.Generic;

namespace TAS.Common
{
    public sealed class MediaConversion 
    {
        public readonly Enum Conversion;
        public readonly Enum OutputFormat;
        public readonly string FFMpegFilter;

        private MediaConversion(TAspectConversion type)
        {
            Conversion = type;
            switch (type)
            {
                case TAspectConversion.NoConversion:
                    break;
                case TAspectConversion.Force4_3:
                    FFMpegFilter = "setdar=dar=4/3";
                    break;
                case TAspectConversion.Force16_9:
                    FFMpegFilter = "setdar=dar=16/9";
                    break;
                case TAspectConversion.Letterbox:
                    FFMpegFilter = "scale=iw:ih*3/4:interl=-1, pad=0:ih*4/3:0:(oh-ih)/2:black, setdar=dar=4/3";
                    break;
                case TAspectConversion.PanScan:
                    FFMpegFilter = "crop=iw*3/4, scale=iw*4/3:ih:interl=-1, setdar=dar=4/3";
                    break;
                case TAspectConversion.PillarBox:
                    FFMpegFilter = "scale=iw*3/4:ih:interl=-1, pad=iw*4/3:0:(ow-iw)/2:0:black, setdar=dar=16/9";
                    break;
                case TAspectConversion.TiltScan:
                    FFMpegFilter = "crop=iw:ih*3/4, scale=iw:ih*4/3:interl=-1, setdar=dar=16/9";
                    break;
            }
        }

        private MediaConversion(TAudioChannelMappingConversion type)
        {
            Conversion = type;
            switch (type)
            {
                case TAudioChannelMappingConversion.Default:
                    OutputFormat = TAudioChannelMapping.Unknown;
                    break;
                case TAudioChannelMappingConversion.FirstTwoChannels:
                    OutputFormat = TAudioChannelMapping.Stereo;
                    FFMpegFilter = "pan=stereo|c0=c0|c1=c1";
                    break;
                case TAudioChannelMappingConversion.SecondTwoChannels:
                    OutputFormat = TAudioChannelMapping.Stereo;
                    FFMpegFilter = "pan=stereo|c0=c2|c1=c3";
                    break;
                case TAudioChannelMappingConversion.FirstChannelOnly:
                    OutputFormat = TAudioChannelMapping.Stereo;
                    FFMpegFilter = "pan=stereo|c0=c0|c1=c0";
                    break;
                case TAudioChannelMappingConversion.SecondChannelOnly:
                    OutputFormat = TAudioChannelMapping.Stereo;
                    FFMpegFilter = "pan=stereo|c0=c1|c1=c1";
                    break;
                case TAudioChannelMappingConversion.Combine1Plus2:
                    OutputFormat = TAudioChannelMapping.Stereo;
                    FFMpegFilter = "pan=stereo|c0=0.5*c0+0.5*c1|c1=0.5*c0+0.5*c1";
                    break;
                case TAudioChannelMappingConversion.Combine3Plus4:
                    OutputFormat = TAudioChannelMapping.Stereo;
                    FFMpegFilter = "pan=stereo|c0=0.5*c2+0.5*c3|c1=0.5*c2+0.5*c3";
                    break;
            }
        }

        private MediaConversion(TFieldOrder type)
        {
            Conversion = type;
            switch (type)
            {
                case TFieldOrder.Unknown:
                    break;
                case TFieldOrder.TFF:
                    OutputFormat = TFieldOrder.TFF;
                    FFMpegFilter = "setfield=tff";
                    break;
                case TFieldOrder.BFF:
                    OutputFormat = TFieldOrder.BFF;
                    FFMpegFilter = "setfield=bff";
                    break;
                case TFieldOrder.Progressive:
                    OutputFormat = TFieldOrder.Progressive;
                    FFMpegFilter = "setfield=prog";
                    break;
            }
        }

        public MediaConversion (double volume)
        {
            FFMpegFilter = string.Format(System.Globalization.CultureInfo.InvariantCulture, "volume={0:F3}dB", volume);
        }

        public static Dictionary<TAspectConversion, MediaConversion> AspectConversions = new Dictionary<TAspectConversion, MediaConversion>()
        {
            {TAspectConversion.NoConversion, new MediaConversion(TAspectConversion.NoConversion)},
            {TAspectConversion.Force4_3, new MediaConversion(TAspectConversion.Force4_3)},
            {TAspectConversion.Force16_9, new MediaConversion(TAspectConversion.Force16_9)},
            {TAspectConversion.PillarBox, new MediaConversion(TAspectConversion.PillarBox)},
            {TAspectConversion.TiltScan, new MediaConversion(TAspectConversion.TiltScan)},
            {TAspectConversion.Letterbox, new MediaConversion(TAspectConversion.Letterbox)},
            {TAspectConversion.PanScan, new MediaConversion(TAspectConversion.PanScan)}
        };

        public static Dictionary<TAudioChannelMappingConversion, MediaConversion> AudioChannelMapingConversions = new Dictionary<TAudioChannelMappingConversion, MediaConversion>()
        {
            {TAudioChannelMappingConversion.Default, new MediaConversion(TAudioChannelMappingConversion.Default)},
            {TAudioChannelMappingConversion.FirstTwoChannels, new MediaConversion(TAudioChannelMappingConversion.FirstTwoChannels)},
            {TAudioChannelMappingConversion.SecondTwoChannels, new MediaConversion(TAudioChannelMappingConversion.SecondTwoChannels)},
            {TAudioChannelMappingConversion.FirstChannelOnly, new MediaConversion(TAudioChannelMappingConversion.FirstChannelOnly)},
            {TAudioChannelMappingConversion.SecondChannelOnly, new MediaConversion(TAudioChannelMappingConversion.SecondChannelOnly)},
            {TAudioChannelMappingConversion.Combine1Plus2, new MediaConversion(TAudioChannelMappingConversion.Combine1Plus2)},
            {TAudioChannelMappingConversion.Combine3Plus4, new MediaConversion(TAudioChannelMappingConversion.Combine3Plus4)}
        };

        public static Dictionary<TFieldOrder, MediaConversion> SourceFieldOrderEnforceConversions = new Dictionary<TFieldOrder, MediaConversion>() 
        { 
            {TFieldOrder.Unknown, new MediaConversion(TFieldOrder.Unknown)},
            {TFieldOrder.TFF, new MediaConversion(TFieldOrder.TFF)},
            {TFieldOrder.BFF, new MediaConversion(TFieldOrder.BFF)},
            {TFieldOrder.Progressive, new MediaConversion(TFieldOrder.Progressive)}
        };

    }
}
