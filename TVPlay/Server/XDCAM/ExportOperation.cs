using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Server;
using TAS.Common;
using System.IO;

namespace TAS.Server.XDCAM
{
    public class ExportOperation: FFMpegOperation
    {
        const string D10_IMX50 = "-vsync passthrough -pix_fmt yuv422p -vcodec mpeg2video -minrate 50000k -maxrate 50000k -b:v 50000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 3 -bufsize 2000000 -rc_init_occupancy 2000000 -rc_buf_aggressivity 0.25 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx5p -vf \"scale=720:576, pad=720:608:0:32\" -f mxf_d10";
        const string D10_IMX40 = "-vsync passthrough -pix_fmt yuv422p -vcodec mpeg2video -minrate 40000k -maxrate 40000k -b:v 40000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 3 -bufsize 1600000 -rc_init_occupancy 1600000 -rc_buf_aggressivity 0.25 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx4p -vf \"scale=720:576, pad=720:608:0:32\" -f mxf_d10";
        const string D10_IMX30 = "-vsync passthrough -pix_fmt yuv422p -vcodec mpeg2video -minrate 30000k -maxrate 30000k -b:v 30000k -intra -top 1 -flags +ildct+low_delay -dc 10 -ps 1 -qmin 1 -qmax 8 -bufsize 1200000 -rc_init_occupancy 1200000 -rc_buf_aggressivity 0.25 -intra_vlc 1 -non_linear_quant 1 -color_primaries 5 -color_trc 1 -colorspace 5 -rc_max_vbv_use 1 -tag:v mx3p -vf \"scale=720:576, pad=720:608:0:32\" -f mxf_d10";
        const string PCM24LE = "-acodec pcm_s24le -ar 48000 -ac 2 -d10_channelcount 4";
        const string PCM16LE = "-acodec pcm_s16le -ar 48000 -ac 2 -d10_channelcount 4";
        
        UInt64 _progressFileSize;

        public ExportOperation()
        {
            Kind = TFileOperationKind.Export;
            TryCount = 1;
        }

        public TimeSpan StartTC { get; set; }
        
        public TimeSpan Duration { get; set; }

        public decimal AudioVolume { get; set; }

        public IngestDirectory DestDirectory { get; set; }

        internal override bool Do()
        {
            if (Kind == TFileOperationKind.Export && SourceMedia.Directory.AccessType == TDirectoryAccessType.Direct)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                this.IsIndeterminate = true;                
                try
                {

                    bool success = false;
                    success = _do(SourceMedia);
                    if (!success)
                        TryCount--;
                    return success;
                }
                catch (Exception e)
                {
                    _addOutputMessage(e.Message);
                    TryCount--;
                    return false;
                }
            }
            return false;
        }

        private bool _do(Media inputMedia)
        {
            _progressDuration = SourceMedia.Duration;
            _addOutputMessage("Refreshing XDCAM content");
            DestDirectory.Refresh();
            bool result = false;
            var existingFiles = DestDirectory.Files.Where(f => f.FileName.StartsWith("C", true, System.Globalization.CultureInfo.InvariantCulture));
            int maxFile = existingFiles.Count() == 0 ? 1 : existingFiles.Max(m => int.Parse(m.FileName.Substring(1, 4))) + 1;
            DestMedia = new IngestMedia() {MediaName = string.Format("C{0:D4}", maxFile), FileName = string.Format("C{0:D4}.MXF", maxFile), Folder = "Clip", MediaStatus = TMediaStatus.Copying, Directory = DestDirectory};
            if (DestDirectory.AccessType == TDirectoryAccessType.FTP)
            {
                using (TempMedia localDestMedia = FileManager.TempDirectory.Get(inputMedia, ".MXF"))
                {
                    DestMedia.PropertyChanged += DestMedia_PropertyChanged;
                    try
                    {
                        result = _encode(inputMedia.FullPath, localDestMedia.FullPath);
                        if (result)
                        {
                            _progressFileSize = (UInt64)(new FileInfo(localDestMedia.FullPath)).Length;
                            _addOutputMessage(string.Format("Transfering file to device as {0}", DestMedia.FileName));
                            result = localDestMedia.CopyMediaTo(DestMedia, ref _aborted);
                        }
                    }
                    finally
                    {
                        DestMedia.PropertyChanged -= DestMedia_PropertyChanged;
                    }
                }
            }
            else
                result = _encode(inputMedia.FullPath, DestMedia.FullPath);
            if (result)
                DestMedia.MediaStatus = result ? TMediaStatus.Available : TMediaStatus.CopyError;
            if (result) OperationStatus = FileOperationStatus.Finished;
            return result;
        }

        void DestMedia_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileSize")
            {
                ulong fs = _progressFileSize;
                if (fs > 0 && sender is Media)
                    Progress = (int)(((sender as Media).FileSize * 100ul) / fs);
            }
        }


        bool _encode(string inputFile, string outFile)
        {
            Debug.WriteLine(this, "Export encode started");
            _addOutputMessage(string.Format("Encode started to file {0}", outFile));
            string command = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "-i \"{0}\" {1} {2} -ss {3} -filter:a \"volume={4:F3}dB\" -timecode {5} -y \"{6}\"",
                inputFile,
                DestDirectory.XDCAMVideoExportFormat == TxDCAMVideoExportFormat.IMX30 ? D10_IMX30
                    : DestDirectory.XDCAMVideoExportFormat == TxDCAMVideoExportFormat.IMX40 ? D10_IMX40
                    : D10_IMX50,
                DestDirectory.XDCAMAudioExportFormat == TxDCAMAudioExportFormat.Channels4Bits24 ? PCM24LE : PCM16LE,
                StartTC - SourceMedia.TCStart,
                AudioVolume,
                StartTC.ToSMPTETimecodeString(),
                outFile);
            if (RunProcess(command))
            {
                Debug.WriteLine(this, "Export encode succeed");
                _addOutputMessage("Encode finished successfully");
                return true;
            }
            Debug.WriteLine("FFmpeg _encode(): Failed for {0}", inputFile);
            return false;
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", SourceMedia.MediaName, DestDirectory.DirectoryName);
        }
    }
}
