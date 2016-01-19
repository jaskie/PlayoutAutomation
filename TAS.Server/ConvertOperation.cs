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

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ConvertOperation : FFMpegOperation, IConvertOperation
    {
        
        #region Properties

        StreamInfo[] inputFileStreams;

        public ConvertOperation()
        {
            Kind = TFileOperationKind.Convert;
            AspectConversion = TAspectConversion.NoConversion;
            SourceFieldOrderEnforceConversion = TFieldOrder.Unknown;
            AudioChannelMappingConversion = TAudioChannelMappingConversion.FirstTwoChannels;
        }

        #endregion // properties

        #region CheckFile
        private void CheckInputFile(IMedia mf)
        {
            using (FFMpegWrapper ff = new FFMpegWrapper(mf.FullPath))
            {
                inputFileStreams = ff.GetStreamInfo();
            }

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
                    if (sourceMedia.Directory.AccessType != TDirectoryAccessType.Direct)
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
                                    success = _do(_localSourceMedia);
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
                        success = _do(sourceMedia);
                        if (!success)
                            TryCount--;
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

        private void _addConversion(MediaConversion conversion, StringBuilder parameters, List<string> videoFilters, List<string> audioFilters)
        {
            if (conversion != null)
            {
                if (!string.IsNullOrWhiteSpace(conversion.FFMpegParameter))
                    parameters.AppendFormat(" -{0}", conversion.FFMpegParameter);
                if (!string.IsNullOrWhiteSpace(conversion.FFMpegAudioFilter))
                    audioFilters.Add(conversion.FFMpegAudioFilter);
                if (!string.IsNullOrWhiteSpace(conversion.FFMpegVideoFilter))
                    videoFilters.Add(conversion.FFMpegVideoFilter);
            }
        }

        private string _encodeParameters(IMedia inputMedia)
        {
            List<string> vf = new List<string>();
            List<string> af = new List<string>();
            StringBuilder ep;
            if (((IngestDirectory)SourceMedia.Directory).DoNotEncode)
            {
                ep = new StringBuilder("-c:v copy -c:a copy");
                if (AspectConversion == TAspectConversion.Force16_9)
                    ep.Append(" -aspect 16/9");
                else
                if (AspectConversion == TAspectConversion.Force4_3)
                    ep.Append(" -aspect 4/3");
            }
            else
            {
                ep = new StringBuilder(((IngestDirectory)SourceMedia.Directory).EncodeParams).Append(" -ar 48000");
                if (inputMedia.HasExtraLines)
                {
                    vf.Add("crop=720:576:0:32");
                    vf.Add("setdar=dar=16/9");
                }
                if (inputFileStreams.Count(s => s.StreamType == StreamType.AUDIO) > 8)
                    af.Add("aformat=channel_layouts=0xFFFF");
                _addConversion(MediaConversion.AspectConversions[AspectConversion], ep, vf, af);
                _addConversion(MediaConversion.SourceFieldOrderEnforceConversions[SourceFieldOrderEnforceConversion], ep, vf, af);
                MediaConversion audiChannelMappingConversion = MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion];
                if (inputFileStreams.Count(s => s.StreamType == StreamType.AUDIO) >= 2 && !audiChannelMappingConversion.OutputFormat.Equals(TAudioChannelMapping.Unknown))
                {
                    foreach (StreamInfo stream in inputFileStreams.Where(s => s.StreamType == StreamType.AUDIO))
                        for (int i = 0; i < stream.ChannelCount; i++)
                            ep.AppendFormat(" -map_channel 0.{0}.{1}", stream.Index, i);
                }
                _addConversion(audiChannelMappingConversion, ep, vf, af);
                if (AudioVolume != 0)
                    _addConversion(new MediaConversion(AudioVolume), ep, vf, af);
                VideoFormatDescription outputFormatDescription = VideoFormatDescription.Descriptions[OutputFormat];
                VideoFormatDescription inputFormatDescription = SourceMedia.VideoFormatDescription;
                if (outputFormatDescription.ImageSize != inputFormatDescription.ImageSize)
                    vf.Add(string.Format("scale={0}:{1}", outputFormatDescription.ImageSize.Width, outputFormatDescription.ImageSize.Height));
                vf.Add(string.Format("fps=fps={0}", outputFormatDescription.FrameRate));
                if (outputFormatDescription.Interlaced)
                {
                    vf.Add("fieldorder=tff");
                    ep.Append(" -flags +ildct+ilme");
                }
                else
                {
                    vf.Add("w3fdif");
                }
                if (vf.Any())
                    ep.AppendFormat(" -filter:v \"{0}\"", string.Join(",", vf));
                if (af.Any())
                    ep.AppendFormat(" -filter:a \"{0}\"", string.Join(",", af));
            }
            return ep.ToString();
        }

        private bool _do(Media inputMedia)
        {
            _progressDuration = inputMedia.Duration;
            Debug.WriteLine(this, "Convert operation started");
            _addOutputMessage("Starting convert operation:");
            VideoFormatDescription formatDescription = VideoFormatDescription.Descriptions[OutputFormat];
            DestMedia.MediaStatus = TMediaStatus.Copying;
            CheckInputFile(inputMedia);
            string encodeParams = _encodeParameters(inputMedia);
            string Params = string.Format("-i \"{0}\" -vsync cfr {1} -timecode {2} -y \"{3}\"",
                    inputMedia.FullPath,
                    encodeParams,
                    DestMedia.TcStart.ToSMPTETimecodeString(formatDescription.FrameRate),
                    DestMedia.FullPath);

            if (DestMedia is ArchiveMedia && !Directory.Exists(Path.GetDirectoryName(DestMedia.FullPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(DestMedia.FullPath));
            DestMedia.AudioChannelMapping = (TAudioChannelMapping)MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion].OutputFormat;
            if (RunProcess(Params)  // FFmpeg 
                && DestMedia.FileExists())
            {
                DestMedia.MediaStatus = TMediaStatus.Copied;
                ((Media)DestMedia).Verify();
                if (Math.Abs(DestMedia.Duration.Ticks - inputMedia.Duration.Ticks) > TimeSpan.TicksPerSecond / 2)
                {
                    DestMedia.MediaStatus = TMediaStatus.CopyError;
                    if (DestMedia is PersistentMedia)
                        (DestMedia as PersistentMedia).Save();
                    _addWarningMessage(string.Format(resources._encodeWarningDifferentDurations, inputMedia.Duration.ToSMPTETimecodeString(inputMedia.VideoFormatDescription.FrameRate), DestMedia.Duration.ToSMPTETimecodeString(DestMedia.VideoFormatDescription.FrameRate)));
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
                return true;
            }
            Debug.WriteLine("FFmpeg rewraper Do(): Failed for {0}. Command line was {1}", (object)SourceMedia, Params);
            return false;
        }

        protected override void ProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            base.ProcOutputHandler(sendingProcess, outLine);
            if (!string.IsNullOrEmpty(outLine.Data) 
                && outLine.Data.Contains("error")) 
                _addWarningMessage(string.Format(resources._encodeWarningFFmpeg, outLine.Data));
        }

    }

}
 