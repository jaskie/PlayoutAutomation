using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.ComponentModel;
using TAS.FFMpegUtils;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Dependencies;
using TAS.Server.Media;

namespace TAS.Server
{
    public class IngestOperation : FFMpegOperation, IIngestOperation
    {

        private TAspectConversion _aspectConversion;
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        private double _audioVolume;
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        private TimeSpan _startTc;
        private TimeSpan _duration;
        private bool _trim;

        internal IngestOperation(FileManager ownerFileManager) : base(ownerFileManager)
        {
            Kind = TFileOperationKind.Ingest;
            _aspectConversion = TAspectConversion.NoConversion;
            _sourceFieldOrderEnforceConversion = TFieldOrder.Unknown;
            _audioChannelMappingConversion = TAudioChannelMappingConversion.FirstTwoChannels;
        }

        [JsonProperty]
        public TAspectConversion AspectConversion
        {
            get => _aspectConversion;
            set => SetField(ref _aspectConversion, value);
        }

        [JsonProperty]
        public TAudioChannelMappingConversion AudioChannelMappingConversion
        {
            get => _audioChannelMappingConversion;
            set => SetField(ref _audioChannelMappingConversion, value);
        }

        [JsonProperty]
        public double AudioVolume
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        [JsonProperty]
        public TFieldOrder SourceFieldOrderEnforceConversion
        {
            get => _sourceFieldOrderEnforceConversion;
            set => SetField(ref _sourceFieldOrderEnforceConversion, value);
        }

        [JsonProperty]
        public TimeSpan StartTC
        {
            get => _startTc;
            set => SetField(ref _startTc, value);
        }

        [JsonProperty]
        public TimeSpan Duration
        {
            get => _duration;
            set => SetField(ref _duration, value);
        }

        [JsonProperty]
        public bool Trim
        {
            get => _trim;
            set => SetField(ref _trim, value);
        }

        [JsonProperty]
        public bool LoudnessCheck { get; set; }

        internal override bool Execute()
        {
            if (Kind == TFileOperationKind.Ingest)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                IsIndeterminate = true;
                try
                {
                    MediaBase sourceMedia = Source as IngestMedia;
                    if (sourceMedia == null)
                        throw new ArgumentException("IngestOperation: Source is not of type IngestMedia");
                    bool success = false;
                    if (((IngestDirectory) sourceMedia.Directory).AccessType != TDirectoryAccessType.Direct)
                        using (TempMedia localSourceMedia =
                            (TempMedia) OwnerFileManager.TempDirectory.CreateMedia(sourceMedia))
                        {
                            try
                            {
                                AddOutputMessage($"Copying to local file {localSourceMedia.FullPath}");
                                localSourceMedia.PropertyChanged += LocalSourceMedia_PropertyChanged;
                                if (sourceMedia.CopyMediaTo(localSourceMedia, ref Aborted))
                                {
                                    AddOutputMessage("Verifing local file");
                                    localSourceMedia.Verify();
                                    if (DestProperties.MediaType == TMediaType.Still)
                                        success = ConvertStill(localSourceMedia);
                                    else
                                        success = ConvertMovie(localSourceMedia, localSourceMedia.StreamInfo);
                                    if (!success)
                                        TryCount--;
                                    return success;
                                }
                                return false;
                            }
                            finally
                            {
                                localSourceMedia.PropertyChanged -= LocalSourceMedia_PropertyChanged;
                            }
                        }
                    else
                    {
                        if (sourceMedia.IsVerified)
                        {
                            if (DestProperties.MediaType == TMediaType.Still)
                                success = ConvertStill(sourceMedia);
                            else
                                success = ConvertMovie(sourceMedia, ((IngestMedia) sourceMedia).StreamInfo);
                            if (!success)
                                TryCount--;
                        }
                        else
                            AddOutputMessage("Waiting for media to verify");
                        return success;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    AddOutputMessage(e.Message);
                    TryCount--;
                    return false;
                }

            }
            else
                return base.Execute();
        }

        private void LocalSourceMedia_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMedia.FileSize))
            {
                ulong fs = Source.FileSize;
                if (fs > 0 && sender is MediaBase)
                    Progress = (int)(((sender as MediaBase).FileSize * 100ul) / fs);
            }
        }

        private bool ConvertStill(MediaBase localSourceMedia)
        {
            CreateDestMediaIfNotExists();
            if (Dest == null || string.IsNullOrEmpty(localSourceMedia?.FileName))
                return false;
            Dest.MediaType = TMediaType.Still;
            Size destSize = Dest.VideoFormat == TVideoFormat.Other ? VideoFormatDescription.Descriptions[TVideoFormat.HD1080i5000].ImageSize : Dest.FormatDescription().ImageSize;
            Image bmp = new Bitmap(destSize.Width, destSize.Height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            if (Path.GetExtension(localSourceMedia.FileName).ToLowerInvariant() == ".tga")
            {
                var tgaImage = new TargaImage(localSourceMedia.FullPath);
                graphics.DrawImage(tgaImage.Image, 0, 0, destSize.Width, destSize.Height);
            }
            else
                graphics.DrawImage(new Bitmap(localSourceMedia.FullPath), 0, 0, destSize.Width, destSize.Height);
            ImageCodecInfo imageCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.FilenameExtension.Split(';').Select(se => se.Trim('*')).Contains(FileUtils.DefaultFileExtension(TMediaType.Still).ToUpperInvariant()));
            if (imageCodecInfo == null)
                return false;
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameter encoderParameter = new EncoderParameter(encoder, 90L);
            EncoderParameters encoderParameters = new EncoderParameters(1) {Param = {[0] = encoderParameter}};
            bmp.Save(Dest.FullPath, imageCodecInfo, encoderParameters);
            Dest.MediaStatus = TMediaStatus.Copied;
            Dest.Verify();
            OperationStatus = FileOperationStatus.Finished;
            return true;
        }

        #region Movie conversion
        private void AddConversion(MediaConversion conversion, List<string> filters)
        {
            if (!string.IsNullOrWhiteSpace(conversion?.FFMpegFilter))
                filters.Add(conversion.FFMpegFilter);
        }

        private string GetEncodeParameters(MediaBase inputMedia, StreamInfo[] inputStreams)
        {
            var videoFilters = new List<string>();
            var ep = new StringBuilder();
            if (!(Source.Directory is IngestDirectory sourceDir))
                return string.Empty;
            #region Video
            ep.AppendFormat(" -c:v {0}", sourceDir.VideoCodec);
            if (sourceDir.VideoCodec == TVideoCodec.copy)
            {
                if (AspectConversion == TAspectConversion.Force16_9)
                    ep.Append(" -aspect 16/9");
                else
                if (AspectConversion == TAspectConversion.Force4_3)
                    ep.Append(" -aspect 4/3");
            }
            else
            {
                ep.AppendFormat(" -b:v {0}k", (int)(inputMedia.FormatDescription().ImageSize.Height * 13 * sourceDir.VideoBitrateRatio));
                VideoFormatDescription outputFormatDescription = Dest.FormatDescription();
                VideoFormatDescription inputFormatDescription = inputMedia.FormatDescription();
                AddConversion(MediaConversion.SourceFieldOrderEnforceConversions[SourceFieldOrderEnforceConversion], videoFilters);
                if (inputMedia.HasExtraLines)
                {
                    videoFilters.Add("crop=720:576:0:32");
                    if (AspectConversion == TAspectConversion.NoConversion)
                    {
                        if (inputFormatDescription.IsWideScreen)
                            videoFilters.Add("setdar=dar=16/9");
                        else
                            videoFilters.Add("setdar=dar=4/3");
                    }
                }
                if (AspectConversion == TAspectConversion.NoConversion)
                {
                    if (inputFormatDescription.IsWideScreen)
                        videoFilters.Add("setdar=dar=16/9");
                    else
                        videoFilters.Add("setdar=dar=4/3");
                }
                if (AspectConversion != TAspectConversion.NoConversion)
                    AddConversion(MediaConversion.AspectConversions[AspectConversion], videoFilters);
                if (inputFormatDescription.FrameRate / outputFormatDescription.FrameRate == 2 && outputFormatDescription.Interlaced)
                    videoFilters.Add("tinterlace=interleave_top");
                videoFilters.Add($"fps=fps={outputFormatDescription.FrameRate}");
                if (outputFormatDescription.Interlaced)
                {
                    videoFilters.Add("fieldorder=tff");
                    ep.Append(" -flags +ildct+ilme");
                }
                else
                {
                    videoFilters.Add("w3fdif");
                }
                var additionalEncodeParams = ((IngestDirectory)Source.Directory).EncodeParams;
                if (!string.IsNullOrWhiteSpace(additionalEncodeParams))
                    ep.Append(" ").Append(additionalEncodeParams.Trim());
            }
            int lastFilterIndex = videoFilters.Count - 1;
            if (lastFilterIndex >= 0)
            {
                videoFilters[lastFilterIndex] = $"{videoFilters[lastFilterIndex]}[v]";
                ep.Append(" -map \"[v]\"");
            } else
            {
                var videoStream = inputStreams.FirstOrDefault(s => s.StreamType == StreamType.VIDEO);
                if (videoStream != null)
                    ep.AppendFormat(" -map 0:{0}", videoStream.Index);
            }
            #endregion // Video

            #region Audio
            List<string> audioFilters = new List<string>();
            StreamInfo[] audioStreams = inputStreams.Where(s => s.StreamType == StreamType.AUDIO).ToArray();
            if (audioStreams.Length > 0)
            {
                ep.AppendFormat(" -c:a {0}", sourceDir.AudioCodec);
                if (sourceDir.AudioCodec != TAudioCodec.copy)
                {
                    ep.AppendFormat(" -b:a {0}k", (int)(2 * 128 * sourceDir.AudioBitrateRatio));
                    MediaConversion audiChannelMappingConversion = MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion];
//                    int inputTotalChannels = audioStreams.Sum(s => s.ChannelCount);
                    int requiredOutputChannels;
                    switch ((TAudioChannelMappingConversion)audiChannelMappingConversion.OutputFormat)
                    {
                        case TAudioChannelMappingConversion.FirstTwoChannels:
                        case TAudioChannelMappingConversion.SecondChannelOnly:
                        case TAudioChannelMappingConversion.Combine1plus2:
                            requiredOutputChannels = 2;
                            break;
                        case TAudioChannelMappingConversion.SecondTwoChannels:
                        case TAudioChannelMappingConversion.Combine3plus4:
                            requiredOutputChannels = 4;
                            break;
                        case TAudioChannelMappingConversion.FirstChannelOnly:
                            requiredOutputChannels = 1;
                            break;
                        default:
                            requiredOutputChannels = 0;
                            break;
                    }
                    if (audioStreams.Length > 1 && requiredOutputChannels > audioStreams[0].ChannelCount)
                    {
                        //int audio_stream_count = 0;
                        StringBuilder pf = new StringBuilder();
                        foreach (StreamInfo stream in audioStreams)
                        {
                            pf.AppendFormat("[0:{0}]", stream.Index);
                            //audio_stream_count += stream.ChannelCount;
                        }
                        audioFilters.Add($"{pf}amerge=inputs={audioStreams.Length}");
                    }
                    AddConversion(audiChannelMappingConversion, audioFilters);
                    ep.Append(" -ac 2");
                    if (Math.Abs(AudioVolume) > double.Epsilon)
                        AddConversion(new MediaConversion(AudioVolume), audioFilters);
                    ep.Append(" -ar 48000");
                }
            }
            lastFilterIndex = audioFilters.Count - 1;
            if (lastFilterIndex >= 0)
            {
                audioFilters[lastFilterIndex] = $"{audioFilters[lastFilterIndex]}[a]";
                ep.Append(" -map \"[a]\"");
            }
            else
            {
                var audioStream = inputStreams.FirstOrDefault(s => s.StreamType == StreamType.AUDIO);
                if (audioStream != null)
                    ep.AppendFormat(" -map 0:{0}", audioStream.Index);
            }
            #endregion // audio
            var filters = videoFilters.Concat(audioFilters).ToArray();
            if (filters.Length > 0)
                ep.AppendFormat(" -filter_complex \"{0}\"", string.Join(",", filters));
            return ep.ToString();
        }

        private bool IsTrimmed()
        {
            return Trim && Duration > TimeSpan.Zero && ((IngestDirectory)Source.Directory).VideoCodec != TVideoCodec.copy ;
        }

        private bool ConvertMovie(MediaBase localSourceMedia, StreamInfo[] streams)
        {
            if (!localSourceMedia.FileExists() || streams == null)
            {
                Debug.WriteLine(this, "Cannot start conversion: file not readed");
                AddOutputMessage("Cannot start conversion: file not readed");
                return false;
            }
            CreateDestMediaIfNotExists();
            var destMedia = Dest;
            if (destMedia == null)
                return false;
            ProgressDuration = localSourceMedia.Duration;
            Debug.WriteLine(this, "Convert operation started");
            AddOutputMessage("Starting convert operation:");
            destMedia.MediaStatus = TMediaStatus.Copying;
            string encodeParams = GetEncodeParameters(localSourceMedia, streams);
            string ingestRegion = IsTrimmed() ?
                string.Format(CultureInfo.InvariantCulture, " -ss {0} -t {1}", StartTC - Source.TcStart, Duration) : string.Empty;
            string Params = string.Format(CultureInfo.InvariantCulture,
                " -i \"{1}\"{0} -vsync cfr{2} -timecode {3} -y \"{4}\"",
                ingestRegion,
                localSourceMedia.FullPath,
                encodeParams,
                StartTC.ToSMPTETimecodeString(destMedia.FrameRate()),
                destMedia.FullPath);
            if (Dest is ArchiveMedia)
                FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(destMedia.FullPath));
            Dest.AudioChannelMapping = (TAudioChannelMapping)MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion].OutputFormat;
            if (RunProcess(Params)  // FFmpeg 
                && destMedia.FileExists())
            {
                destMedia.MediaStatus = TMediaStatus.Copied;
                destMedia.Verify();
                destMedia.TcPlay = destMedia.TcStart;
                destMedia.DurationPlay = destMedia.Duration;
                if (Math.Abs(destMedia.Duration.Ticks - (IsTrimmed() ? Duration.Ticks : localSourceMedia.Duration.Ticks)) > TimeSpan.TicksPerSecond / 2)
                {
                    destMedia.MediaStatus = TMediaStatus.CopyError;
                    (destMedia as PersistentMedia)?.Save();
                    AddWarningMessage($"Durations are different: {localSourceMedia.Duration.ToSMPTETimecodeString(localSourceMedia.FrameRate())} vs {destMedia.Duration.ToSMPTETimecodeString(destMedia.FrameRate())}");
                    Debug.WriteLine(this, "Convert operation succeed, but durations are diffrent");
                }
                else
                {
                    if ((Source.Directory is IngestDirectory directory) && directory.DeleteSource)
                        ThreadPool.QueueUserWorkItem( o =>
                        {
                            Thread.Sleep(2000);
                            OwnerFileManager.Queue(new FileOperation(OwnerFileManager) { Kind = TFileOperationKind.Delete, Source = Source });
                        });
                    AddOutputMessage("Convert operation finished successfully");
                    Debug.WriteLine(this, "Convert operation succeed");
                }
                OperationStatus = FileOperationStatus.Finished;
                if (LoudnessCheck)
                {
                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        Thread.Sleep(2000);
                        OwnerFileManager.Queue( new LoudnessOperation(OwnerFileManager) { Source = destMedia });
                    });
                }
                return true;
            }
            Debug.WriteLine("FFmpeg rewraper Execute(): Failed for {0}. Command line was {1}", (object)Source, Params);
            return false;
        }
        #endregion //Movie conversion

        protected override void ProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            base.ProcOutputHandler(sendingProcess, outLine);
            if (!string.IsNullOrEmpty(outLine.Data) 
                && outLine.Data.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0) 
                AddWarningMessage($"FFmpeg error: {outLine.Data}");
        }

    }

}
 