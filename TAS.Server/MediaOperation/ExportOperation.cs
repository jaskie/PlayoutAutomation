using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using NLog;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;
using TAS.Server.XDCAM;
using jNet.RPC;

namespace TAS.Server.MediaOperation
{
    public class ExportOperation : FileOperationBase, IExportOperation
    {
        private const string D10PadFilter = "pad=720:608:0:32";
        private const string D10PalImx50 = "-vsync cfr -r 25 -pix_fmt yuv422p -vcodec mpeg2video -minrate 50000k -maxrate 50000k -b:v 50000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 3 -bufsize 2000000 -rc_init_occupancy 2000000 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx5p";
        private const string D10PalImx40 = "-vsync cfr -r 25 -pix_fmt yuv422p -vcodec mpeg2video -minrate 40000k -maxrate 40000k -b:v 40000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 3 -bufsize 1600000 -rc_init_occupancy 1600000 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx4p";
        private const string D10PalImx30 = "-vsync cfr -r 25 -pix_fmt yuv422p -vcodec mpeg2video -minrate 30000k -maxrate 30000k -b:v 30000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 8 -bufsize 1200000 -rc_init_occupancy 1200000 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx3p";
        private const string D10PalDv25 = "-vsync cfr -r 25 -pix_fmt yuv420p -vcodec dvvideo -minrate 25000k -maxrate 25000k -b:v 25000k -top 0 -bufsize 1000000 -rc_init_occupancy 1000000";
        private const string Pcm24Le4Ch = "-acodec pcm_s24le -ar 48000 -ac 2 -d10_channelcount 4";
        private const string Pcm16Le4Ch = "-acodec pcm_s16le -ar 48000 -ac 2 -d10_channelcount 4";
        private const string Pcm16Le8Ch = "-acodec pcm_s16le -ar 48000 -ac 2 -d10_channelcount 8";

        private ulong _progressFileSize;
        private readonly List<MediaExportDescription> _sources = new List<MediaExportDescription>();
        private IMediaProperties _destMediaProperties;

        internal ExportOperation()
        {
            TryCount = 1;
        }

        [DtoMember]
        public IEnumerable<MediaExportDescription> Sources
        {
            get => _sources;
            set
            {
                _sources.Clear();
                _sources.AddRange(value);
            }
        }

        [DtoMember]
        public IMediaProperties DestProperties { get => _destMediaProperties; set => SetField(ref _destMediaProperties, value); }

        [DtoMember]
        public IMediaDirectory DestDirectory { get; set; }

        internal MediaBase Dest { get; set; }

        public TimeSpan StartTC { get; set; }

        public TimeSpan Duration { get; set; }

        public double AudioVolume { get; set; }

        public TmXFVideoExportFormat MXFVideoExportFormat { get; set; }

        public TmXFAudioExportFormat MXFAudioExportFormat { get; set; }

        protected override void OnOperationStatusChanged()
        {
            
        }

        protected override async Task<bool> InternalExecute()
        {
            StartTime = DateTime.UtcNow;
            IsIndeterminate = true;
            try
            {
                return await DoExecute();
            }
            catch (Exception e)
            {
                AddOutputMessage(LogLevel.Error, $"Error: {e.Message}");
                throw;
            }
        }

        private async Task<bool> DoExecute()
        {
            bool result;
            var helper = new FFMpegHelper(this, TimeSpan.FromTicks(_sources.Sum(e => e.Duration.Ticks)));
            if (!(DestDirectory is IngestDirectory destDirectory))
                throw new InvalidOperationException("Can only export to IngestDirectory");
            if (destDirectory.AccessType == TDirectoryAccessType.FTP)
            {
                using (var localDestMedia = (TempMedia) TempDirectory.Current.CreateMedia(Sources.First().Media))
                {
                    result = await EncodeToLocalFile(helper, destDirectory, localDestMedia.FullPath);
                    if (result)
                    {
                        _progressFileSize = (ulong) (new FileInfo(localDestMedia.FullPath)).Length;
                        Dest = _createDestMedia();
                        Dest.PropertyChanged += DestMedia_PropertyChanged;
                        try
                        {

                            AddOutputMessage(LogLevel.Trace, $"Transfering file to device as {Dest.FileName}");
                            result = await localDestMedia.CopyMediaTo(Dest, CancellationTokenSource.Token);
                            if (result)
                                Dest.MediaStatus = TMediaStatus.Available;
                        }

                        finally
                        {
                            Dest.PropertyChanged -= DestMedia_PropertyChanged;
                        }
                    }
                }
            }
            else
            {
                Dest = _createDestMedia();
                result = await EncodeToLocalFile(helper, destDirectory, Dest.FullPath);
                if (result)
                    Dest.MediaStatus = TMediaStatus.Copied;
            }
            if (!result)
            {
                return false;
            }
            Dest.Verify(true);
            return true;
        }

        private IngestMedia _createDestMedia()
        {
            if (!(DestDirectory is IngestDirectory directory))
                throw new ApplicationException($"{nameof(DestDirectory)} must be {nameof(IngestDirectory)}");
            IngestMedia result;
            if (directory.Kind == TIngestDirectoryKind.XDCAM)
            {
                var existingFiles = directory.GetAllFiles().Where(f =>
                    f.FileName.StartsWith("C", true, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                var maxFile = existingFiles.Length == 0
                    ? 1
                    : existingFiles.Max(m => int.Parse(m.FileName.Substring(1, 4))) + 1;
                result = new XdcamMedia
                {
                    MediaName = $"C{maxFile:D4}",
                    MediaType = TMediaType.Movie,
                    MediaGuid = Guid.NewGuid(),
                    LastUpdated = DateTime.UtcNow,
                    FileName = $"C{maxFile:D4}.MXF",
                    Folder = "Clip",
                    MediaStatus = TMediaStatus.Copying
                };
            }
            else
                result = new IngestMedia
                {
                    FileName = FileUtils.GetUniqueFileName(directory.Folder,
                        $"{FileUtils.SanitizeFileName(DestProperties.MediaName)}.{directory.ExportContainerFormat}"),
                    MediaName = DestProperties.MediaName,
                    Duration = DestProperties.Duration,
                    VideoFormat = DestProperties.VideoFormat,
                    LastUpdated = DateTime.UtcNow,
                    MediaGuid = Guid.NewGuid(),
                    MediaStatus = TMediaStatus.Copying
                };
            directory.AddMedia(result);
            return result;
        }

        private void DestMedia_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IMedia.FileSize))
                return;
            var fs = _progressFileSize;
            if (fs > 0 && sender is MediaBase media)
                Progress = (int)((media.FileSize * 100ul) / fs);
        }

        private async Task<bool> EncodeToLocalFile(FFMpegHelper helper, IngestDirectory directory, string outFile)
        {
            var localExportDescriptions = _sources.ToArray();
            var tempMediae = new List<TempMedia>();
            try
            {
                for (var i = 0; i < localExportDescriptions.Length; i++)
                {
                    var s = localExportDescriptions[i];
                    if (((MediaBase)s.Media).Directory is IIngestDirectory ingestDirectory &&
                        ingestDirectory.AccessType == TDirectoryAccessType.FTP)
                    {
                        var localSourceMedia = (TempMedia) TempDirectory.Current.CreateMedia(null);
                        tempMediae.Add(localSourceMedia);
                        AddOutputMessage(LogLevel.Trace, $"Copying to local file {localSourceMedia.FullPath}");
                        localSourceMedia.PropertyChanged += (source, ea) =>
                        {
                            if (ea.PropertyName != nameof(IMedia.FileSize))
                                return;
                            var fs = s.Media.FileSize;
                            if (fs > 0 && source is MediaBase media)
                                Progress = (int)(media.FileSize * 100ul / fs);
                        };
                        if (!await ((MediaBase) s.Media).CopyMediaTo(localSourceMedia, CancellationTokenSource.Token))
                            throw new ApplicationException("File not copied");
                        AddOutputMessage(LogLevel.Trace, "Verifing local file");
                        localSourceMedia.Verify(true);
                        localExportDescriptions[i] = new MediaExportDescription(localSourceMedia, s.Logos, TimeSpan.Zero, s.Duration, s.AudioVolume);
                    }
                }
                return await EncodeFromLocalFiles(helper, directory, outFile, localExportDescriptions);
            }
            finally
            {
                tempMediae.ForEach(m => m.Dispose());
            }
        }

        private async Task<bool> EncodeFromLocalFiles(FFMpegHelper helper, IngestDirectory directory, string outFile, ICollection<MediaExportDescription> exportMedia)
        {            
            var files = new StringBuilder();
            var index = 0;
            var complexFilterElements = new List<string>();
            var overlayOutputs = new StringBuilder();
            var startTimecode = exportMedia.First().StartTC;
            var isXdcamDirectory = directory.Kind == TIngestDirectoryKind.XDCAM;
            var outputFormatDesc = VideoFormatDescription.Descriptions[isXdcamDirectory || directory.ExportContainerFormat == TMovieContainerFormat.mxf ? TVideoFormat.PAL : directory.ExportVideoFormat];
            var scaleFilter = $"scale={outputFormatDesc.ImageSize.Width}:{outputFormatDesc.ImageSize.Height}:interl=-1";
            foreach (var e in exportMedia)
            {
                if (!(e.Media is MediaBase media))
                    continue;
                files.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, " -ss {0} -t {1} -i \"{2}\"", (e.StartTC - media.TcStart).TotalSeconds, e.Duration.TotalSeconds, media.FullPath);
                var videoOutputName = $"[v{index}]";
                var itemVideoFilters = new List<string>();
                if (media.HasExtraLines)
                    itemVideoFilters.Add("crop=720:576:0:32");
                var mediaFormatDescription = media.FormatDescription();
                itemVideoFilters.Add(mediaFormatDescription.IsWideScreen ? "setdar=dar=16/9" : "setdar=dar=4/3");
                if (!outputFormatDesc.Interlaced && mediaFormatDescription.Interlaced)
                {
                    itemVideoFilters.Add("yadif");
                    itemVideoFilters.Add($"fps={outputFormatDesc.FrameRate.Num}/{outputFormatDesc.FrameRate.Den}");
                }
                itemVideoFilters.Add(scaleFilter);
                complexFilterElements.Add($"[{index}]{string.Join(",", itemVideoFilters)}{videoOutputName}");
                var audioIndex = index;
                complexFilterElements.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0}]volume={1:F3}dB[a{0}]", audioIndex, e.AudioVolume));
                index++;
                var logos = e.Logos.ToArray();
                foreach (var iMedia in logos)
                {
                    if (!(iMedia is MediaBase logo))
                        continue;
                    files.Append($" -i \"{logo.FullPath}\"");
                    var newOutputName = $"[v{index}]";
                    complexFilterElements.Add($"{videoOutputName}[{index}]overlay{newOutputName}");
                    videoOutputName = newOutputName;
                    index++;
                }
                overlayOutputs.AppendFormat("{0}[a{1}]", videoOutputName, audioIndex);
            }
            if (isXdcamDirectory || directory.ExportContainerFormat == TMovieContainerFormat.mxf)
                if (MXFVideoExportFormat == TmXFVideoExportFormat.DV25)
                    complexFilterElements.Add($"{string.Join(string.Empty, overlayOutputs)}concat=n={exportMedia.Count}:v=1:a=1[v][p]");            
                else
                    complexFilterElements.Add($"{string.Join(string.Empty, overlayOutputs)}concat=n={exportMedia.Count}:v=1:a=1[vr][p], [vr]{D10PadFilter}[v]");
            else
                complexFilterElements.Add($"{string.Join(string.Empty, overlayOutputs)}concat=n={exportMedia.Count}:v=1:a=1[v][p]");
            complexFilterElements.Add("[p]apad=pad_len=1024[a]");
            var complexFilter = complexFilterElements.Count > 0 ? $" -filter_complex \"{string.Join(", ", complexFilterElements)}\"" : string.Empty;
            var command = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}{1} -map \"[v]\" -map \"[a]\" {2} -timecode {3}{4} -shortest -f {5} -y \"{6}\"",
                //0
                files,
                //1
                complexFilter,
                //2
                isXdcamDirectory || directory.ExportContainerFormat == TMovieContainerFormat.mxf ? 
                    string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1}", 
                              MXFVideoExportFormat == TmXFVideoExportFormat.DV25 ? D10PalDv25
                            : MXFVideoExportFormat == TmXFVideoExportFormat.IMX30 ? D10PalImx30
                            : MXFVideoExportFormat == TmXFVideoExportFormat.IMX40 ? D10PalImx40
                            : D10PalImx50
                        ,
                        MXFAudioExportFormat == TmXFAudioExportFormat.Channels4Bits24 ? Pcm24Le4Ch 
                            : MXFAudioExportFormat == TmXFAudioExportFormat.Channels4Bits16 ? Pcm16Le4Ch
                            : Pcm16Le8Ch)
                    :
                    directory.ExportParams,
                //3
                startTimecode.ToSmpteTimecodeString(VideoFormatDescription.Descriptions[DestProperties.VideoFormat].FrameRate),
                //4
                isXdcamDirectory|| directory.ExportContainerFormat == TMovieContainerFormat.mxf ? $" -metadata creation_time=\"{DateTime.UtcNow:o}\"" : string.Empty,
                //5
                (isXdcamDirectory || directory.ExportContainerFormat == TMovieContainerFormat.mxf) && MXFVideoExportFormat != TmXFVideoExportFormat.DV25 ? "mxf_d10" : directory.ExportContainerFormat.ToString(),
                outFile);
            if (await helper.RunProcess(command))
                return true;
            AddOutputMessage(LogLevel.Error, $"FFmpeg Encode(): Failed for {outFile}");
            return false;
        }

        public override string ToString()
        {
            var dest = Dest ?? DestProperties;
            return $"Export {string.Join(",", Sources)} -> {DestDirectory}:{MediaToString(dest)}";
        }
    }
}
