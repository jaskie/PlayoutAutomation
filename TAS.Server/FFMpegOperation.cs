using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace TAS.Server
{
    public abstract class FFMpegOperation: FileOperation
    {
        const string FFmpegExe = "ffmpeg.exe";
        const string LProgressPattern = "time=" + @"\d\d:\d\d:\d\d\.?\d*";
        const string ProgressPattern = @"\d\d:\d\d:\d\d\.?\d*";

        internal FFMpegOperation(FileManager ownerFileManager): base(ownerFileManager) { }

        protected readonly Regex RegexlProgress = new Regex(LProgressPattern, RegexOptions.None);
        protected readonly Regex RegexProgress = new Regex(ProgressPattern, RegexOptions.None);
        protected TimeSpan ProgressDuration;
       
        protected bool RunProcess(string parameters)
        {
            //create a process info
            var oInfo = new ProcessStartInfo(FFmpegExe, parameters)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            //try the process
            Debug.WriteLine(parameters, "Starting ffmpeg:");
            AddOutputMessage($"ffmpeg.exe {parameters}");
            try
            {
                using (var procFFmpeg = Process.Start(oInfo))
                {
                    if (procFFmpeg == null)
                        return false;
                    procFFmpeg.ErrorDataReceived += ProcOutputHandler;
                    procFFmpeg.BeginErrorReadLine();
                    var finished = false;
                    while (!(IsAborted || finished))
                        finished = procFFmpeg.WaitForExit(1000);
                    if (!IsAborted)
                        return finished && procFFmpeg.ExitCode == 0;
                    procFFmpeg.Kill();
                    Thread.Sleep(1000);
                    var destMedia = Dest;
                    if (destMedia != null)
                        System.IO.File.Delete(destMedia.FullPath);
                    Debug.WriteLine(this, "Aborted");
                    return finished && procFFmpeg.ExitCode == 0;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message, "Error running FFmpeg process");
                AddOutputMessage(e.ToString());
                return false;
            }
        }

        protected virtual void ProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrEmpty(outLine.Data))
                return;
            var mProgressLine = RegexlProgress.Match(outLine.Data);
            if (mProgressLine.Success)
            {
                var mProgressVal = RegexProgress.Match(mProgressLine.Value);
                if (!mProgressVal.Success)
                    return;
                var duration = ProgressDuration.Ticks;
                if (duration > 0
                    && TimeSpan.TryParse(mProgressVal.Value.Trim(), CultureInfo.InvariantCulture, out var progressSeconds))
                    Progress = (int)((progressSeconds.Ticks * 100) / duration);
            }
            else
                AddOutputMessage(outLine.Data);
        }

    }
}
