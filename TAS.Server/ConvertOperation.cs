using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Text.RegularExpressions;
using MediaInfoLib;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Globalization;
using System.Text;
using System.ComponentModel;
using TAS.FFMpegUtils;
using TAS.Common;
using resources = TAS.Client.Common.Properties.Resources;
using TAS.Server.Interfaces;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;
using TAS.Server.Common;

namespace TAS.Server
{
    public class ConvertOperation : FFMpegOperation, IConvertOperation
    {
        
        #region Properties

        public ConvertOperation()
        {
            Kind = TFileOperationKind.Convert;
            AspectConversion = TAspectConversion.NoConversion;
            SourceFieldOrderEnforceConversion = TFieldOrder.Unknown;
            AudioChannelMappingConversion = TAudioChannelMappingConversion.FirstTwoChannels;
        }

        #endregion // properties

        #region CheckFile
        private void CheckInputFile(Media mf)
        {
            MediaInfo mi = new MediaInfo();
            try
            {
                mi.Open(mf.FullPath);
                mi.Option("Complete");
                string miOutput = mi.Inform();

                Regex format_lxf = new Regex("Format\\s*:\\s*LXF");
                if (format_lxf.Match(miOutput).Success)
                {
                    string[] miOutputLines = miOutput.Split('\n');
                    Regex vitc = new Regex("ATC_VITC");
                    Regex re = new Regex("Time code of first frame\\s*:[\\s]\\d{2}:\\d{2}:\\d{2}:\\d{2}");
                    for (int i = 0; i < miOutputLines.Length; i++)
                    {
                        if (vitc.Match(miOutputLines[i]).Success && i >= 1)
                        {
                            Match m_tcs = re.Match(miOutputLines[i - 1]);
                            if (m_tcs.Success)
                            {
                                Regex reg_tc = new Regex("\\d{2}:\\d{2}:\\d{2}:\\d{2}");
                                Match m_tc = reg_tc.Match(m_tcs.Value);
                                if (m_tc.Success)
                                {
                                    DestMedia.TcStart = reg_tc.Match(m_tc.Value).Value.SMPTETimecodeToTimeSpan(mf.VideoFormatDescription.FrameRate);
                                    if (DestMedia.TcPlay == TimeSpan.Zero)
                                        DestMedia.TcPlay = DestMedia.TcStart;
                                    break;
                                }
                            }
                        }
                    }
                }

                Regex format_mxf = new Regex(@"Format\s*:\s*MXF");
                if (format_mxf.Match(miOutput).Success)
                {
                    string[] miOutputLines = miOutput.Split('\n');
                    Regex mxf_tc = new Regex(@"MXF TC");
                    Regex re = new Regex(@"Time code of first frame\s*:[\s]\d{2}:\d{2}:\d{2}:\d{2}");
                    for (int i = 0; i < miOutputLines.Length; i++)
                    {
                        Match mxf_match = mxf_tc.Match(miOutputLines[i]);
                        if (mxf_match.Success && i < miOutputLines.Length - 1)
                        {
                            Regex reg_tc = new Regex(@"\d{2}:\d{2}:\d{2}:\d{2}");
                            Match m_tc = re.Match(miOutputLines[i + 1]);
                            if (m_tc.Success)
                            {
                                DestMedia.TcStart = reg_tc.Match(m_tc.Value).Value.SMPTETimecodeToTimeSpan(mf.VideoFormatDescription.FrameRate);
                                if (DestMedia.TcPlay == TimeSpan.Zero)
                                    DestMedia.TcPlay = DestMedia.TcStart;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                mi.Close();
            }
        }


        #endregion // Checkfile

        #region IConvertOperation implementation

        private TAspectConversion _aspectConversion;
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        private decimal _audioVolume;
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        private TVideoFormat _outputFormat;

        [JsonProperty]
        public TAspectConversion AspectConversion { get { return _aspectConversion; } set { SetField(ref _aspectConversion, value, "AspectConversion"); } }
        [JsonProperty]
        public TAudioChannelMappingConversion AudioChannelMappingConversion { get { return _audioChannelMappingConversion; } set { SetField(ref _audioChannelMappingConversion, value, "AudioChannelMappingConversion"); } }
        [JsonProperty]
        public decimal AudioVolume { get { return _audioVolume; } set { SetField(ref _audioVolume, value, "AudioVolume"); } }
        [JsonProperty]
        public TFieldOrder SourceFieldOrderEnforceConversion { get { return _sourceFieldOrderEnforceConversion; } set { SetField(ref _sourceFieldOrderEnforceConversion, value, "SourceFieldOrderEnforceConversion"); } }
        [JsonProperty]
        public TVideoFormat OutputFormat { get { return _outputFormat; } set { SetField(ref _outputFormat, value, "OutputFormat"); } }
        [JsonProperty]
        public string IdAux
        {
            get
            {
                var media = DestMedia as IPersistentMedia;
                return (media == null) ? null : media.IdAux;
            }
            set
            {
                var media = DestMedia as IPersistentMedia;
                if (media != null)
                    media.IdAux = value;
                NotifyPropertyChanged("IdAux");
            }
        }
        [JsonProperty]
        public TimeSpan StartTC { get; set; }
        [JsonProperty]
        public TimeSpan Duration { get; set; }
        [JsonProperty]
        public bool Trim { get; set; }

        [JsonProperty]
        public bool LoudnessCheck { get; set; }


        #endregion // IConvertOperation implementation


        public override bool Do()
        {
            if (Kind == TFileOperationKind.Convert)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                IsIndeterminate = true;
                try
                {
                    Media sourceMedia = SourceMedia as IngestMedia;
                    if (sourceMedia == null)
                        throw new ArgumentException("ConvertOperation: SourceMedia is not of type IngestMedia");
                    bool success = false;
                    if (((IngestDirectory)sourceMedia.Directory).AccessType != TDirectoryAccessType.Direct)
                        using (TempMedia _localSourceMedia = Owner.TempDirectory.CreateMedia(sourceMedia))
                        {
                            _addOutputMessage(string.Format("Copying to local file {0}", _localSourceMedia.FullPath));
                            _localSourceMedia.PropertyChanged += _localSourceMedia_PropertyChanged;
                            if (sourceMedia.CopyMediaTo(_localSourceMedia, ref _aborted))
                            {
                                _addOutputMessage("Verifing local file");
                                _localSourceMedia.Verify();
                                try
                                {
                                    if (DestMedia.MediaType == TMediaType.Still)
                                        success = _convertStill(_localSourceMedia);
                                    else
                                        success = _convertMovie(_localSourceMedia, _localSourceMedia.StreamInfo);
                                }
                                finally
                                {
                                    _localSourceMedia.PropertyChanged -= _localSourceMedia_PropertyChanged;
                                }

                                if (!success)
                                    TryCount--;
                                return success;
                            }
                            return false;
                        }

                    else
                    {
                        if (sourceMedia is IngestMedia && sourceMedia.Verified)
                        {
                            if (DestMedia.MediaType == TMediaType.Still)
                                success = _convertStill(sourceMedia);
                            else
                                success = _convertMovie(sourceMedia, ((IngestMedia)sourceMedia).StreamInfo);
                            if (!success)
                                TryCount--;
                        }
                        else
                            _addOutputMessage("Waiting for media to verify");
                        return success;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    _addOutputMessage(e.Message);
                    TryCount--;
                    return false;
                }

            }
            else
                return base.Do();
        }

        void _localSourceMedia_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileSize")
            {
                ulong fs = SourceMedia.FileSize;
                if (fs > 0 && sender is Media)
                    Progress = (int)(((sender as Media).FileSize * 100ul) / fs);
            }
        }

        private bool _convertStill(Media _localSourceMedia)
        {
            Size destSize = DestMedia.VideoFormat == TVideoFormat.Other ? VideoFormatDescription.Descriptions[TVideoFormat.HD1080i5000].ImageSize : DestMedia.VideoFormatDescription.ImageSize;
            Image bmp = new Bitmap(destSize.Width, destSize.Height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(new Bitmap(_localSourceMedia.FullPath), 0, 0, destSize.Width, destSize.Height);
            ImageCodecInfo imageCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.FilenameExtension.Split(';').Select(se => se.Trim('*')).Contains(FileUtils.DefaultFileExtension(TMediaType.Still).ToUpperInvariant()));
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameter encoderParameter = new EncoderParameter(encoder, 90L);
            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = encoderParameter;
            bmp.Save(DestMedia.FullPath, imageCodecInfo, encoderParameters);
            DestMedia.MediaStatus = TMediaStatus.Copied;
            ((Media)DestMedia).Verify();
            OperationStatus = FileOperationStatus.Finished;
            return true;
        }

        #region Movie conversion
        private void _addConversion(MediaConversion conversion, List<string> filters)
        {
            if (conversion != null)
            {
                if (!string.IsNullOrWhiteSpace(conversion.FFMpegFilter))
                    filters.Add(conversion.FFMpegFilter);
            }
        }

        private string _encodeParameters(Media inputMedia, StreamInfo[] inputStreams)
        {
            List<string> filter_complex = new List<string>();
            StringBuilder ep = new StringBuilder();
            if (((IngestDirectory)SourceMedia.Directory).DoNotEncode)
            {
                ep.Append(" -c:v copy -c:a copy");
                if (AspectConversion == TAspectConversion.Force16_9)
                    ep.Append(" -aspect 16/9");
                else
                if (AspectConversion == TAspectConversion.Force4_3)
                    ep.Append(" -aspect 4/3");
            }
            else
            {
                #region Audio
                StreamInfo firstAudioStream = inputStreams.FirstOrDefault(s => s.StreamType == StreamType.AUDIO);
                if (firstAudioStream != null)
                {
                    MediaConversion audiChannelMappingConversion = MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion];
                    int inputTotalChannels = inputStreams.Where(s => s.StreamType == StreamType.AUDIO).Sum(s => s.ChannelCount);
                    int requiredOutputChannels;
                    switch ((TAudioChannelMappingConversion)audiChannelMappingConversion.OutputFormat)
                    {
                        case TAudioChannelMappingConversion.FirstTwoChannels:
                        case TAudioChannelMappingConversion.SecondTwoChannels:
                            requiredOutputChannels = 2;
                            break;
                        case TAudioChannelMappingConversion.FirstChannelOnly:
                        case TAudioChannelMappingConversion.SecondChannelOnly:
                        case TAudioChannelMappingConversion.Combine1plus2:
                        case TAudioChannelMappingConversion.Combine3plus4:
                            requiredOutputChannels = 1;
                            break;
                        default:
                            requiredOutputChannels = 0;
                            break;
                    }
                    if (requiredOutputChannels != firstAudioStream.ChannelCount)
                    {
                        int audio_stream_count = 0;
                        StringBuilder pf = new StringBuilder();
                        foreach (StreamInfo stream in inputStreams.Where(s => s.StreamType == StreamType.AUDIO))
                        {
                            pf.AppendFormat("[0:{0}]", stream.Index);
                            audio_stream_count += stream.ChannelCount;
                        }
                        filter_complex.Add(string.Format("{0}amerge=inputs={1}", pf.ToString(), audio_stream_count));
                    }
                    _addConversion(audiChannelMappingConversion, filter_complex);
                    if (AudioVolume != 0)
                        _addConversion(new MediaConversion(AudioVolume), filter_complex);
                    int lastFilterIndex = filter_complex.Count() - 1;
                    if (lastFilterIndex >= 0)
                    {
                        filter_complex[lastFilterIndex] = string.Format("{0}[a]", filter_complex[lastFilterIndex]);
                        ep.Append(" -map \"[a]\"");
                    }
                    ep.Append(" ").Append(((IngestDirectory)SourceMedia.Directory).EncodeParams).Append(" -ar 48000");
                }
                #endregion // audio
                #region Video
                VideoFormatDescription outputFormatDescription = VideoFormatDescription.Descriptions[OutputFormat];
                VideoFormatDescription inputFormatDescription = inputMedia.VideoFormatDescription;
                _addConversion(MediaConversion.SourceFieldOrderEnforceConversions[SourceFieldOrderEnforceConversion], filter_complex);
                if (inputMedia.HasExtraLines)
                {
                    filter_complex.Add("crop=720:576:0:32");
                    if (AspectConversion == TAspectConversion.NoConversion)
                    {
                        if (inputFormatDescription.IsWideScreen)
                            filter_complex.Add("setdar=dar=16/9");
                        else
                            filter_complex.Add("setdar=dar=4/3");
                    }
                }
                if (outputFormatDescription.ImageSize != inputFormatDescription.ImageSize)
                {
                    filter_complex.Add(string.Format("scale={0}:{1}", outputFormatDescription.ImageSize.Width, outputFormatDescription.ImageSize.Height));
                    if (AspectConversion == TAspectConversion.NoConversion)
                    {
                        if (inputFormatDescription.IsWideScreen)
                            filter_complex.Add("setdar=dar=16/9");
                        else
                            filter_complex.Add("setdar=dar=4/3");
                    }
                }
                if (AspectConversion != TAspectConversion.NoConversion)
                    _addConversion(MediaConversion.AspectConversions[AspectConversion], filter_complex);
                if (inputFormatDescription.FrameRate / outputFormatDescription.FrameRate == 2 && outputFormatDescription.Interlaced)
                    filter_complex.Add("tinterlace=interleave_top");
                filter_complex.Add(string.Format("fps=fps={0}", outputFormatDescription.FrameRate));
                if (outputFormatDescription.Interlaced)
                {
                    filter_complex.Add("fieldorder=tff");
                    ep.Append(" -flags +ildct+ilme");
                }
                else
                {
                    filter_complex.Add("w3fdif");
                }
                if (filter_complex.Any())
                    ep.AppendFormat(" -filter_complex \"{0}\"", string.Join(",", filter_complex));
                #endregion // Video
            }
            return ep.ToString();
        }

        private bool _is_trimmed()
        {
            return Trim && Duration > TimeSpan.Zero && !((IngestDirectory)SourceMedia.Directory).DoNotEncode;
        }

        private bool _convertMovie(Media media, StreamInfo[] streams)
        {
            _progressDuration = media.Duration;
            Debug.WriteLine(this, "Convert operation started");
            _addOutputMessage("Starting convert operation:");
            VideoFormatDescription formatDescription = VideoFormatDescription.Descriptions[OutputFormat];
            DestMedia.MediaStatus = TMediaStatus.Copying;
            CheckInputFile(media);
            string encodeParams = _encodeParameters(media, streams);
            string ingestRegion = _is_trimmed() ?
                string.Format(System.Globalization.CultureInfo.InvariantCulture, " -ss {0} -t {1}", StartTC - SourceMedia.TcStart, Duration) : string.Empty;
            string Params = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    " -i \"{1}\"{0} -vsync cfr{2} -timecode {3} -y \"{4}\"",
                    ingestRegion,
                    media.FullPath,
                    encodeParams,
                    StartTC.ToSMPTETimecodeString(formatDescription.FrameRate),
                    DestMedia.FullPath);
            if (DestMedia is ArchiveMedia && !Directory.Exists(Path.GetDirectoryName(DestMedia.FullPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(DestMedia.FullPath));
            DestMedia.AudioChannelMapping = (TAudioChannelMapping)MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion].OutputFormat;
            if (RunProcess(Params)  // FFmpeg 
                && DestMedia.FileExists())
            {
                DestMedia.MediaStatus = TMediaStatus.Copied;
                ((Media)DestMedia).Verify();
                if (Math.Abs(DestMedia.Duration.Ticks - (_is_trimmed() ? Duration.Ticks : media.Duration.Ticks)) > TimeSpan.TicksPerSecond / 2)
                {
                    DestMedia.MediaStatus = TMediaStatus.CopyError;
                    if (DestMedia is PersistentMedia)
                        (DestMedia as PersistentMedia).Save();
                    _addWarningMessage(string.Format(resources._encodeWarningDifferentDurations, media.Duration.ToSMPTETimecodeString(media.VideoFormatDescription.FrameRate), DestMedia.Duration.ToSMPTETimecodeString(DestMedia.VideoFormatDescription.FrameRate)));
                    Debug.WriteLine(this, "Convert operation succeed, but durations are diffrent");
                }
                else
                {
                    if ((SourceMedia.Directory is IngestDirectory) && ((IngestDirectory)SourceMedia.Directory).DeleteSource)
                        Owner.Queue(new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = SourceMedia });
                    _addOutputMessage("Convert operation finished successfully");
                    Debug.WriteLine(this, "Convert operation succeed");
                }
                OperationStatus = FileOperationStatus.Finished;
                if (LoudnessCheck)
                    Owner.Queue(new LoudnessOperation() { SourceMedia = this.DestMedia });
                return true;
            }
            Debug.WriteLine("FFmpeg rewraper Do(): Failed for {0}. Command line was {1}", (object)SourceMedia, Params);
            return false;
        }
        #endregion //Movie conversion

        protected override void ProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            base.ProcOutputHandler(sendingProcess, outLine);
            if (!string.IsNullOrEmpty(outLine.Data) 
                && outLine.Data.Contains("error")) 
                _addWarningMessage(string.Format(resources._encodeWarningFFmpeg, outLine.Data));
        }

    }

}
 