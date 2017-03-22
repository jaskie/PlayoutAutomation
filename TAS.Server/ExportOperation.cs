using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.XDCAM;

namespace TAS.Server
{
    public class ExportOperation : FFMpegOperation
    {
        const string D10_PAD_FILTER = "pad=720:608:0:32";
        const string D10_PAL_IMX50 = "-vsync cfr -r 25 -pix_fmt yuv422p -vcodec mpeg2video -minrate 50000k -maxrate 50000k -b:v 50000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 3 -bufsize 2000000 -rc_init_occupancy 2000000 -rc_buf_aggressivity 0.25 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx5p";
        const string D10_PAL_IMX40 = "-vsync cfr -r 25 -pix_fmt yuv422p -vcodec mpeg2video -minrate 40000k -maxrate 40000k -b:v 40000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 3 -bufsize 1600000 -rc_init_occupancy 1600000 -rc_buf_aggressivity 0.25 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx4p";
        const string D10_PAL_IMX30 = "-vsync cfr -r 25 -pix_fmt yuv422p -vcodec mpeg2video -minrate 30000k -maxrate 30000k -b:v 30000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 8 -bufsize 1200000 -rc_init_occupancy 1200000 -rc_buf_aggressivity 0.25 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx3p";
        const string D10_PAL_DV25 = "-vsync cfr -r 25 -pix_fmt yuv420p -vcodec dvvideo -minrate 25000k -maxrate 25000k -b:v 25000k -top 0 -bufsize 1000000 -rc_init_occupancy 1000000";
        const string PCM24LE4CH = "-acodec pcm_s24le -ar 48000 -ac 2 -d10_channelcount 4";
        const string PCM16LE4CH = "-acodec pcm_s16le -ar 48000 -ac 2 -d10_channelcount 4";
        const string PCM16LE8CH = "-acodec pcm_s16le -ar 48000 -ac 2 -d10_channelcount 8";

        UInt64 _progressFileSize;

        private NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ExportOperation));

        public ExportOperation()
        {
            Kind = TFileOperationKind.Export;
            TryCount = 1;
        }

        private readonly List<ExportMedia> _exportMediaList = new List<ExportMedia>();
        public IEnumerable<ExportMedia> ExportMediaList
        {
            get { return _exportMediaList; }
            set
            {
                _exportMediaList.Clear();
                _exportMediaList.AddRange(value);
            }
        }

        public TimeSpan StartTC { get; set; }

        public TimeSpan Duration { get; set; }

        public decimal AudioVolume { get; set; }

        public string DestMediaName { get; set; }

        public TmXFVideoExportFormat MXFVideoExportFormat { get; set; }

        public TmXFAudioExportFormat MXFAudioExportFormat { get; set; }

        public override bool Do()
        {
            if (Kind == TFileOperationKind.Export)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                this.IsIndeterminate = true;                
                try
                {

                    bool success = false;
                    success = _do();
                    if (!success)
                        TryCount--;
                    else
                        AddOutputMessage("Operation completed successfully.");
                    return success;
                }
                catch (Exception e)
                {
                    AddOutputMessage($"Error: {e.Message}");
                    Logger.Error(e, "Do exception");
                    TryCount--;
                    return false;
                }
            }
            return false;
        }

        private bool _do()
        {
            bool result = false;
            _progressDuration = TimeSpan.FromTicks(_exportMediaList.Sum(e => e.Duration.Ticks));
            AddOutputMessage("Refreshing destination directory content");
            var destDirectory = DestDirectory as IngestDirectory;
            if (destDirectory == null)
                throw new InvalidOperationException("Can only export to IngestDirectory");
            destDirectory.Refresh();

            if (destDirectory.AccessType == TDirectoryAccessType.FTP)
            {
                using (TempMedia localDestMedia = (TempMedia)Owner.TempDirectory.CreateMedia(SourceMedia))
                {
                    DestMedia = _createDestMedia(destDirectory);
                    DestMedia.PropertyChanged += destMedia_PropertyChanged;
                    try
                    {
                        result = _encode(destDirectory, localDestMedia.FullPath);
                        if (result)
                        {
                            _progressFileSize = (UInt64)(new FileInfo(localDestMedia.FullPath)).Length;
                            AddOutputMessage($"Transfering file to device as {DestMedia.FileName}");
                            result = localDestMedia.CopyMediaTo((Media)DestMedia, ref _aborted);
                        }
                    }

                    finally
                    {
                        DestMedia.PropertyChanged -= destMedia_PropertyChanged;
                    }
                }
            }
            else
            {
                DestMedia = _createDestMedia(destDirectory);
                result = _encode(destDirectory, ((Media)DestMedia).FullPath);
            }
            if (result)
                DestMedia.MediaStatus = result ? TMediaStatus.Available : TMediaStatus.CopyError;
            if (result) OperationStatus = FileOperationStatus.Finished;
            return result;
        }

        IngestMedia _createDestMedia(IngestDirectory destDirectory)
        {
            if (destDirectory.IsXDCAM)
            {
                var existingFiles = DestDirectory.GetFiles().Where(f => f.FileName.StartsWith("C", true, System.Globalization.CultureInfo.InvariantCulture));
                int maxFile = existingFiles.Count() == 0 ? 1 : existingFiles.Max(m => int.Parse(m.FileName.Substring(1, 4))) + 1;
                return new XDCAMMedia(destDirectory) { MediaName = string.Format("C{0:D4}", maxFile), FileName = string.Format("C{0:D4}.MXF", maxFile), Folder = "Clip", MediaStatus = TMediaStatus.Copying };
            }
            else
                return new IngestMedia(destDirectory)
                {
                    MediaName = DestMediaName,
                    FileName = Common.FileUtils.GetUniqueFileName(DestDirectory.Folder, string.Format("{0}.{1}", DestMediaName, destDirectory.ExportContainerFormat)),
                    MediaStatus = TMediaStatus.Copying
                };
        }

        void destMedia_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMedia.FileSize))
            {
                ulong fs = _progressFileSize;
                if (fs > 0 && sender is Media)
                    Progress = (int)(((sender as Media).FileSize * 100ul) / fs);
            }
        }
        
        bool _encode(IngestDirectory directory, string outFile)
        {            
            Debug.WriteLine(this, "Export encode started");
            AddOutputMessage($"Encode started to file {outFile}");
            StringBuilder files = new StringBuilder();
            int index = 0;
            List<string> complexFilterElements = new List<string>();
            List<string> volumeFilterElements = new List<string>();
            StringBuilder overlayOutputs = new StringBuilder();
            List<ExportMedia> exportMedia = _exportMediaList.ToList();
            TimeSpan startTimecode = exportMedia.First().StartTC;
            VideoFormatDescription outputFormatDesc = VideoFormatDescription.Descriptions[directory.IsXDCAM || directory.ExportContainerFormat == TMovieContainerFormat.mxf ? TVideoFormat.PAL : directory.ExportVideoFormat];
            string scaleFilter = $"scale={outputFormatDesc.ImageSize.Width}:{outputFormatDesc.ImageSize.Height}:interl=-1";
            foreach (var e in exportMedia)
            {
                Media media = e.Media as Media;
                if (media != null)
                {
                    files.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, " -ss {0} -t {1} -i \"{2}\"", (e.StartTC - media.TcStart).TotalSeconds, e.Duration.TotalSeconds, media.FullPath);
                    string videoOutputName = $"[v{index}]";
                    List<string> itemVideoFilters = new List<string>();
                    if (((Media)media).HasExtraLines)
                        itemVideoFilters.Add("crop=720:576:0:32");
                    itemVideoFilters.Add(media.FormatDescription().IsWideScreen ? "setdar=dar=16/9" : "setdar=dar=4/3");
                    itemVideoFilters.Add(scaleFilter);
                    complexFilterElements.Add($"[{index}]{string.Join(",", itemVideoFilters)}{videoOutputName}");
                    int audioIndex = index;
                    complexFilterElements.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0}]volume={1:F3}dB[a{0}]", audioIndex, e.AudioVolume));
                    index++;
                    for (int i = 0; i < e.Logos.Length; i++)
                    {
                        Media logo = e.Logos[i] as Media;
                        if (logo != null)
                        {
                            files.Append($" -i \"{logo.FullPath}\"");
                            string newOutputName = $"[v{index}]";
                            complexFilterElements.Add($"{videoOutputName}[{index}]overlay{newOutputName}");
                            videoOutputName = newOutputName;
                            index++;
                        }
                    }
                    overlayOutputs.AppendFormat("{0}[a{1}]", videoOutputName, audioIndex);
                }
            }
            if (directory.IsXDCAM || directory.ExportContainerFormat == TMovieContainerFormat.mxf)
                if (MXFVideoExportFormat == TmXFVideoExportFormat.DV25)
                    complexFilterElements.Add(string.Format("{0}concat=n={1}:v=1:a=1[v][p]", string.Join(string.Empty, overlayOutputs), exportMedia.Count));            
                else
                    complexFilterElements.Add(string.Format("{0}concat=n={1}:v=1:a=1[vr][p], [vr]{2}[v]", string.Join(string.Empty, overlayOutputs), exportMedia.Count, D10_PAD_FILTER));
            else
                complexFilterElements.Add(string.Format("{0}concat=n={1}:v=1:a=1[v][p]", string.Join(string.Empty, overlayOutputs), exportMedia.Count));
            complexFilterElements.Add("[p]apad=pad_len=1024[a]");
            string complexFilter = complexFilterElements.Count > 0 ?
                string.Format(" -filter_complex \"{0}\"", string.Join(", ", complexFilterElements)) :
                string.Empty;
            string command = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}{1} -map \"[v]\" -map \"[a]\" {2} -timecode {3}{4} -shortest -f {5} -y \"{6}\"",
                //0
                files.ToString(),
                //1
                complexFilter,
                //2
                directory.IsXDCAM || directory.ExportContainerFormat == TMovieContainerFormat.mxf ? 
                    String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1}", 
                              MXFVideoExportFormat == TmXFVideoExportFormat.DV25 ? D10_PAL_DV25
                            : MXFVideoExportFormat == TmXFVideoExportFormat.IMX30 ? D10_PAL_IMX30
                            : MXFVideoExportFormat == TmXFVideoExportFormat.IMX40 ? D10_PAL_IMX40
                            : D10_PAL_IMX50
                        ,
                        MXFAudioExportFormat == TmXFAudioExportFormat.Channels4Bits24 ? PCM24LE4CH 
                            : MXFAudioExportFormat == TmXFAudioExportFormat.Channels4Bits16 ? PCM16LE4CH
                            : PCM16LE8CH)
                    :
                    directory.ExportParams,
                //3
                startTimecode.ToSMPTETimecodeString(VideoFormatDescription.Descriptions[DestMedia.VideoFormat].FrameRate),
                //4
                directory.IsXDCAM || directory.ExportContainerFormat == TMovieContainerFormat.mxf ? $" -metadata creation_time=\"{DateTime.UtcNow.ToString("o")}\"" : string.Empty,
                //5
                (directory.IsXDCAM || directory.ExportContainerFormat == TMovieContainerFormat.mxf) && MXFVideoExportFormat != TmXFVideoExportFormat.DV25 ? "mxf_d10" : directory.ExportContainerFormat.ToString(),
                outFile);
            if (RunProcess(command))
            {
                Debug.WriteLine(this, "Export encode succeed");
                AddOutputMessage("Encode finished successfully");
                return true;
            }
            Debug.WriteLine("FFmpeg _encode(): Failed for {0}", outFile);
            return false;
        }

        public override string Title
        {
            get
            {
                return string.Format("Export {0} -> {1}", string.Join(", ", _exportMediaList), DestDirectory.DirectoryName);
            }
        }
    }
}
