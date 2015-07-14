using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TAS.Server;
using TAS.Common;

namespace TAS.Server
{
    public class MediaConversion 
    {
        private string _description;
        public string Description { get { return getDescription(); } set { _description = value; } }

        protected virtual string getDescription()
        {
            return _description;
        }
        public Enum OutputFormat { get; set; }
        public string FFMpegParameter { get; set; }
        public string FFMpegVideoFilter { get; set; }
        public string FFMpegAudioFilter { get { return getFFMpegAudioFilter(); } set { _fFMpegAudioFilter = value; } }

        protected string _fFMpegAudioFilter;
        protected virtual string getFFMpegAudioFilter()
        {
            return _fFMpegAudioFilter;
        }
        public static bool operator ==(MediaConversion mediaConversion1, MediaConversion mediaConversion2)
        {
            if ((object)mediaConversion1 == null || (object)mediaConversion2 == null)
                return false;
            return (mediaConversion1.Description == mediaConversion2.Description
                && mediaConversion1.FFMpegAudioFilter == mediaConversion2.FFMpegAudioFilter
                && mediaConversion1.FFMpegParameter == mediaConversion2.FFMpegParameter
                && mediaConversion1.FFMpegVideoFilter == mediaConversion2.FFMpegVideoFilter
                //&& mediaConversion1.OutputFormat == mediaConversion2.OutputFormat
                );
        }
        public static bool operator !=(MediaConversion mediaConversion1, MediaConversion mediaConversion2)
        {
            return !(mediaConversion1 == mediaConversion2);
        }

        public static bool Equals(MediaConversion mediaConversion1, MediaConversion mediaConversion2)
        {
            return mediaConversion1 == mediaConversion2;
        }

        public override bool Equals(object o)
        {
            if (o is MediaConversion)
            {
                MediaConversion conversion = (MediaConversion)o;
                return (this == conversion);
            }
            else
                return false;
        }

        public override string ToString()
        {
            return Description;
        }
    }

    public class MediaConversionAudioVolume : MediaConversion
    {
        public MediaConversionAudioVolume(double value)
        {
            AudioVolume = value;
        }
        public double AudioVolume {get; set;}
        protected override string getFFMpegAudioFilter()
        {
            if (AudioVolume == 0)
                return null;
            else
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "volume={0:F3}dB", AudioVolume);
        }
        protected override string getDescription()
        {
            if (AudioVolume == 0)
                return "Brak korekcji";
            else
                return string.Format("Korekcja o {0:F3} dB", AudioVolume);
        }
    }

    public sealed class AspectConversions
    {

        static List<MediaConversion> _all = new List<MediaConversion>()
            {
                NoConversion,
                PillarBox,
                TiltScan, 
                Letterbox, 
                PanScan
            };

        public static List<MediaConversion> All()
        {
            return _all;
        }
        public static MediaConversion NoConversion { get { return new MediaConversion() { Description = "Bez konwersji",         OutputFormat = TVideoFormat.PAL_FHA }; } }
        public static MediaConversion PillarBox { get { return new MediaConversion() { Description = "PillarBox (4:3->16:9)", OutputFormat = TVideoFormat.PAL_FHA, FFMpegVideoFilter = "scale=iw*3/4:ih:-1, pad=iw*4/3:0:(ow-iw)/2:0:black, setdar=dar=16/9" }; } }
        public static MediaConversion TiltScan { get { return new MediaConversion() { Description = "Tilt&Scan(4:3->16:9)", OutputFormat = TVideoFormat.PAL_FHA, FFMpegVideoFilter = "crop=iw:ih*3/4, scale=iw:ih*4/3:-1, setdar=dar=16/9" }; } }
        public static MediaConversion Letterbox { get { return new MediaConversion() { Description = "Letterbox (16:9->4:3)", OutputFormat = TVideoFormat.PAL, FFMpegVideoFilter = "scale=iw:ih*3/4:-1, pad=0:ih*4/3:0:(oh-ih)/2:black, setdar=dar=4/3" }; } }
        public static MediaConversion PanScan { get { return new MediaConversion() { Description = "Pan&Scan(16:9->4:3)", OutputFormat = TVideoFormat.PAL, FFMpegVideoFilter = "crop=iw*3/4, scale=iw*4/3:ih:-1, setdar=dar=4/3" }; } }
    }

    public sealed class AudioChannelMappingConversions
    {
        static List<MediaConversion> _all = new List<MediaConversion>()
            {
                Default,
                FirstTwoChannels,
                SecondTwoChannels,
                FirstChannelOnly,
                SecondChannelOnly,
                Combine1plus2,
                Combine3plus4
            };
        public static List<MediaConversion> All()
        {
            return _all;
        }
        public static MediaConversion Default           { get { return new MediaConversion() { Description = "Domyślnie",               OutputFormat = TAudioChannelMapping.Unknown }; } }
        public static MediaConversion FirstTwoChannels  { get { return new MediaConversion() { Description = "Ścieżki 1 i 2 -> Stereo", OutputFormat = TAudioChannelMapping.Stereo, FFMpegParameter = "ac 2", FFMpegAudioFilter = "pan=stereo|c0=c0|c1=c1" }; } }
        public static MediaConversion SecondTwoChannels { get { return new MediaConversion() { Description = "Ścieżki 3 i 4 -> Stereo", OutputFormat = TAudioChannelMapping.Stereo, FFMpegParameter = "ac 2", FFMpegAudioFilter = "pan=stereo|c0=c2|c1=c3" }; } }
        public static MediaConversion FirstChannelOnly  { get { return new MediaConversion() { Description = "Ścieżka 1 -> Mono",       OutputFormat = TAudioChannelMapping.Mono, FFMpegParameter = "ac 1", FFMpegAudioFilter = "pan=mono|c0=c0" }; } }
        public static MediaConversion SecondChannelOnly { get { return new MediaConversion() { Description = "Ścieżka 2 -> Mono",       OutputFormat = TAudioChannelMapping.Mono, FFMpegParameter = "ac 1", FFMpegAudioFilter = "pan=mono|c0=c1" }; } }
        public static MediaConversion Combine1plus2     { get { return new MediaConversion() { Description = "Ścieżki 1 i 2 -> Mono",   OutputFormat = TAudioChannelMapping.Mono, FFMpegParameter = "ac 1", FFMpegAudioFilter = "pan=mono|c0=0.5*c0+0.5*c1" }; } }
        public static MediaConversion Combine3plus4     { get { return new MediaConversion() { Description = "Ścieżki 3 i 4 -> Mono",   OutputFormat = TAudioChannelMapping.Mono, FFMpegParameter = "ac 1", FFMpegAudioFilter = "pan=mono|c0=0.5*c2+0.5*c3" }; } }
    }

    
    public sealed class SourceFieldOrderEnforceConversions
    {
        static List<MediaConversion> _all = new List<MediaConversion>()
            {
                Detect,
                ForceTopFieldFirst,
                ForceBottomFieldFirst,
            };
        public static List<MediaConversion> All()
        {
            return _all;
        }
        public static MediaConversion Detect                { get { return new MediaConversion() { Description = "Wykryj automatycznie",        OutputFormat = TFieldOrder.AutoDetect, }; } }
        public static MediaConversion ForceTopFieldFirst    { get { return new MediaConversion() { Description = "Wymuś górne pole pierwsze",   OutputFormat = TFieldOrder.TFF, FFMpegVideoFilter="setfield=tff" }; } }
        public static MediaConversion ForceBottomFieldFirst { get { return new MediaConversion() { Description = "Wymuś dolne pole pierwsze", OutputFormat = TFieldOrder.BFF, FFMpegParameter = "setfield=bff" }; } }
    }

}
