using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Text;
using System.ComponentModel;
using TAS.FFMpegUtils;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Dependencies;
using TAS.Server.Media;
using LogLevel = NLog.LogLevel;

namespace TAS.Server.MediaOperation
{
    public class IngestOperation : FileOperationBase, IIngestOperation
    {
        private readonly object _destMediaLock = new object();

        private TAspectConversion _aspectConversion;
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        private double _audioVolume;
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        private TimeSpan _startTc;
        private TimeSpan _duration;
        private bool _trim;
        private IMedia _source;
        private IMediaProperties _destProperties;
        private IMediaDirectory _destDirectory;

        internal IngestOperation()
        {
            _aspectConversion = TAspectConversion.NoConversion;
            _sourceFieldOrderEnforceConversion = TFieldOrder.Unknown;
            _audioChannelMappingConversion = TAudioChannelMappingConversion.FirstTwoChannels;
        }

        [JsonProperty]
        public IMedia Source { get => _source; set => SetField(ref _source, value); }

        [JsonProperty]
        public IMediaProperties DestProperties { get => _destProperties; set => SetField(ref _destProperties, value); }

        [JsonProperty]
        public IMediaDirectory DestDirectory { get => _destDirectory; set => SetField(ref _destDirectory, value); }

        internal MediaBase Dest { get; set; }

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

        protected override void OnOperationStatusChanged()
        {
        }

        protected override async Task<bool> InternalExecute()
        {
            StartTime = DateTime.UtcNow;
            IsIndeterminate = true;
            try
            {
                if (!(Source is IngestMedia sourceMedia))
                    throw new ArgumentException("IngestOperation: Source is not of type IngestMedia");
                sourceMedia.NotifyIngestStatusUpdated(DestDirectory as ServerDirectory, TIngestStatus.InProgress);
                if (((IngestDirectory)sourceMedia.Directory).AccessType != TDirectoryAccessType.Direct)
                    using (var localSourceMedia = (TempMedia)TempDirectory.Current.CreateMedia(sourceMedia))
                    {
                        try
                        {
                            AddOutputMessage(LogLevel.Trace, $"Copying to local file {localSourceMedia.FullPath}");
                            localSourceMedia.PropertyChanged += LocalSourceMedia_PropertyChanged;
                            if (!await sourceMedia.CopyMediaTo(localSourceMedia, CancellationTokenSource.Token))
                                return false;
                            AddOutputMessage(LogLevel.Trace, "Verifing local file");
                            localSourceMedia.Verify(true);
                            var result = DestProperties.MediaType == TMediaType.Still
                                ? ConvertStill(localSourceMedia)
                                : await ConvertMovie(localSourceMedia, localSourceMedia.StreamInfo);
                            sourceMedia.NotifyIngestStatusUpdated(DestDirectory as ServerDirectory, result ? TIngestStatus.Ready : TIngestStatus.NotReady);
                            return result;
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
                        var result = DestProperties.MediaType == TMediaType.Still
                            ? ConvertStill(sourceMedia)
                            : await ConvertMovie(sourceMedia, sourceMedia.StreamInfo);
                        sourceMedia.NotifyIngestStatusUpdated(DestDirectory as ServerDirectory, result ? TIngestStatus.Ready : TIngestStatus.NotReady);
                        return result;
                    }
                    else
                        AddOutputMessage(LogLevel.Trace, "Waiting for media to verify");
                    sourceMedia.NotifyIngestStatusUpdated(DestDirectory as ServerDirectory, TIngestStatus.Unknown);
                    return false;
                }
            }
            catch (Exception e)
            {
                AddOutputMessage(LogLevel.Error, e.Message);
                throw;
            }
        }

        protected virtual void CreateDestMediaIfNotExists()
        {
            lock (_destMediaLock)
            {
                if (Dest != null)
                    return;
                if (!(DestDirectory is MediaDirectoryBase mediaDirectory))
                    throw new ApplicationException($"Cannot create destination media on {DestDirectory}");
                Dest = (MediaBase)mediaDirectory.CreateMedia(DestProperties ?? Source);
            }
        }

        private void LocalSourceMedia_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IMedia.FileSize))
                return;
            var fs = Source.FileSize;
            if (fs > 0 && sender is MediaBase media)
                Progress = (int)(media.FileSize * 100ul / fs);
        }

        private bool ConvertStill(MediaBase localSourceMedia)
        {
            CreateDestMediaIfNotExists();
            if (Dest == null || string.IsNullOrEmpty(localSourceMedia?.FileName))
                return false;
            Dest.MediaType = TMediaType.Still;
            var destSize = Dest.VideoFormat == TVideoFormat.Other ? VideoFormatDescription.Descriptions[TVideoFormat.HD1080i5000].ImageSize : Dest.FormatDescription().ImageSize;
            var bmp = new Bitmap(destSize.Width, destSize.Height, PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(bmp);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            if (Path.GetExtension(localSourceMedia.FileName).ToLowerInvariant() == ".tga")
            {
                var tgaImage = new TargaImage(localSourceMedia.FullPath);
                graphics.DrawImage(tgaImage.Image, 0, 0, destSize.Width, destSize.Height);
            }
            else
                graphics.DrawImage(new Bitmap(localSourceMedia.FullPath), 0, 0, destSize.Width, destSize.Height);
            var imageCodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.FilenameExtension.Split(';').Select(se => se.Trim('*')).Contains(FileUtils.DefaultFileExtension(TMediaType.Still).ToUpperInvariant()));
            if (imageCodecInfo == null)
            {
                Dest.Delete();
                return false;
            }
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
            var encoderParameter = new EncoderParameter(encoder, 90L);
            var encoderParameters = new EncoderParameters(1) { Param = { [0] = encoderParameter } };
            bmp.Save(Dest.FullPath, imageCodecInfo, encoderParameters);
            Dest.MediaStatus = TMediaStatus.Copied;
            Dest.Verify(false);
            return true;
        }

        #region Movie conversion
        private void AddConversion(MediaConversion conversion, List<string> filters)
        {
            if (!string.IsNullOrWhiteSpace(conversion?.FFMpegFilter))
                filters.Add(conversion.FFMpegFilter);
        }

        private string GetEncodeParameters(IngestDirectory sourceDir, MediaBase inputMedia, StreamInfo[] inputStreams)
        {
            var videoFilters = new List<string>();
            var ep = new StringBuilder();
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
                var outputFormatDescription = Dest.FormatDescription();
                var inputFormatDescription = inputMedia.FormatDescription();
                AddConversion(MediaConversion.SourceFieldOrderEnforceConversions[SourceFieldOrderEnforceConversion], videoFilters);
                if (inputMedia.HasExtraLines)
                    videoFilters.Add("crop=720:576:0:32");
                if (AspectConversion == TAspectConversion.NoConversion)
                    videoFilters.Add(inputFormatDescription.IsWideScreen ? "setdar=dar=16/9" : "setdar=dar=4/3");
                else
                    AddConversion(MediaConversion.AspectConversions[AspectConversion], videoFilters);
                if (inputFormatDescription.FrameRate / outputFormatDescription.FrameRate == 2 && outputFormatDescription.Interlaced)
                    videoFilters.Add("tinterlace=interleave_top");
                videoFilters.Add($"fps=fps={outputFormatDescription.FrameRate}");
                if (inputFormatDescription.Interlaced)
                {
                    if (outputFormatDescription.Interlaced)
                    {
                        videoFilters.Add("fieldorder=tff");
                        ep.Append(" -flags +ildct+ilme");
                    }
                    else
                        videoFilters.Add("w3fdif");
                }
                var additionalEncodeParams = sourceDir.EncodeParams;
                if (!string.IsNullOrWhiteSpace(additionalEncodeParams))
                    ep.Append(" ").Append(additionalEncodeParams.Trim());
            }
            var lastFilterIndex = videoFilters.Count - 1;
            if (lastFilterIndex >= 0)
            {
                videoFilters[lastFilterIndex] = $"{videoFilters[lastFilterIndex]}[v]";
                ep.Append(" -map \"[v]\"");
            }
            else
            {
                var videoStream = inputStreams.FirstOrDefault(s => s.StreamType == StreamType.VIDEO);
                if (videoStream != null)
                    ep.AppendFormat(" -map 0:{0}", videoStream.Index);
            }

            #endregion // Video

            #region Audio
            var audioFilters = new List<string>();
            var audioStreams = inputStreams.Where(s => s.StreamType == StreamType.AUDIO).ToArray();
            if (audioStreams.Length > 0)
            {
                ep.AppendFormat(" -c:a {0}", sourceDir.AudioCodec);
                if (sourceDir.AudioCodec != TAudioCodec.copy)
                {
                    ep.AppendFormat(" -b:a {0}k", (int)(2 * 128 * sourceDir.AudioBitrateRatio));
                    var audiChannelMappingConversion = MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion];
                    //                    int inputTotalChannels = audioStreams.Sum(s => s.ChannelCount);
                    int requiredOutputChannels;
                    switch ((TAudioChannelMappingConversion)audiChannelMappingConversion.OutputFormat)
                    {
                        case TAudioChannelMappingConversion.FirstTwoChannels:
                        case TAudioChannelMappingConversion.SecondChannelOnly:
                        case TAudioChannelMappingConversion.Combine1Plus2:
                            requiredOutputChannels = 2;
                            break;
                        case TAudioChannelMappingConversion.SecondTwoChannels:
                        case TAudioChannelMappingConversion.Combine3Plus4:
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
                        var pf = new StringBuilder();
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
            if (!(Source is MediaBase mediaBase && mediaBase.Directory is IngestDirectory ingestDirectory))
                throw new ApplicationException("Media not belongs to IngestDirectory");
            return Trim && Duration > TimeSpan.Zero && ingestDirectory.VideoCodec != TVideoCodec.copy;
        }

        private async Task<bool> ConvertMovie(MediaBase localSourceMedia, StreamInfo[] streams)
        {
            if (!localSourceMedia.FileExists() || streams == null)
            {
                AddOutputMessage(LogLevel.Error, "Cannot start ingest: file not readed");
                return false;
            }
            CreateDestMediaIfNotExists();
            var destMedia = Dest;
            if (destMedia == null)
                return false;
            var helper = new FFMpegHelper(this, localSourceMedia.Duration);
            destMedia.MediaStatus = TMediaStatus.Copying;
            if (!(Source is MediaBase mediaBase && mediaBase.Directory is IngestDirectory ingestDirectory))
                throw new ApplicationException("Media not belongs to IngestDirectory");
            var encodeParams = GetEncodeParameters(ingestDirectory, localSourceMedia, streams);
            var ingestRegion = IsTrimmed() ? string.Format(CultureInfo.InvariantCulture, " -ss {0} -t {1}", StartTC - Source.TcStart, Duration) : string.Empty;
            var Params = string.Format(CultureInfo.InvariantCulture,
                " -i \"{1}\"{0} -vsync cfr{2} -timecode {3} -y \"{4}\"",
                ingestRegion,
                localSourceMedia.FullPath,
                encodeParams,
                StartTC.ToSmpteTimecodeString(destMedia.FrameRate()),
                destMedia.FullPath);
            if (Dest is ArchiveMedia)
                FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(destMedia.FullPath));
            Dest.AudioChannelMapping = (TAudioChannelMapping)MediaConversion.AudioChannelMapingConversions[AudioChannelMappingConversion].OutputFormat;
            if (await helper.RunProcess(Params)  // FFmpeg 
                && destMedia.FileExists())
            {
                destMedia.MediaStatus = TMediaStatus.Copied;
                destMedia.Verify(true);
                destMedia.TcPlay = destMedia.TcStart;
                destMedia.DurationPlay = destMedia.Duration;
                ((MediaDirectoryBase)DestDirectory).RefreshVolumeInfo();
                if (Math.Abs(destMedia.Duration.Ticks - (IsTrimmed() ? Duration.Ticks : localSourceMedia.Duration.Ticks)) > TimeSpan.TicksPerSecond / 2)
                {
                    destMedia.MediaStatus = TMediaStatus.CopyError;
                    (destMedia as PersistentMedia)?.Save();
                    AddWarningMessage($"Durations are different: {localSourceMedia.Duration.ToSmpteTimecodeString(localSourceMedia.FrameRate())} vs {destMedia.Duration.ToSmpteTimecodeString(destMedia.FrameRate())}");
                }
                else
                {
                    if (ingestDirectory.DeleteSource)
                        ThreadPool.QueueUserWorkItem(o =>
                       {
                           Thread.Sleep(2000);
                           FileManager.Current.Queue(new DeleteOperation { Source = Source });
                       });
                }
                if (LoudnessCheck)
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        Thread.Sleep(2000);
                        FileManager.Current.Queue(new LoudnessOperation { Source = destMedia });
                    });
                }
                return true;
            }
            destMedia.Delete();
            return false;
        }
        #endregion //Movie conversion

        public override string ToString()
        {
            var dest = Dest ?? DestProperties;
            return $"Ingest {Source} -> {DestDirectory}:{MediaToString(dest)}";
        }
    }

}
 