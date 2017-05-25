using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using TAS.Server.Media;

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
            ProcessStartInfo oInfo = new ProcessStartInfo(FFmpegExe, parameters);
            oInfo.UseShellExecute = false;
            oInfo.CreateNoWindow = true;
            oInfo.RedirectStandardError = true;

            //try the process
            Debug.WriteLine(parameters, "Starting ffmpeg:");
            AddOutputMessage($"ffmpeg.exe {parameters}");
            try
            {
                using (Process procFFmpeg = Process.Start(oInfo))
                {
                    procFFmpeg.ErrorDataReceived += ProcOutputHandler;
                    procFFmpeg.BeginErrorReadLine();
                    bool finished = false;
                    while (!(IsAborted || finished))
                        finished = procFFmpeg.WaitForExit(1000);
                    if (IsAborted)
                    {
                        procFFmpeg.Kill();
                        Thread.Sleep(1000);
                        var destMedia = Dest as Media.MediaBase;
                        if (destMedia != null)
                            System.IO.File.Delete(destMedia.FullPath);
                        Debug.WriteLine(this, "Aborted");
                    }
                    return finished && (procFFmpeg.ExitCode == 0);
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
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                Match mProgressLine = RegexlProgress.Match(outLine.Data);
                if (mProgressLine.Success)
                {
                    Match mProgressVal = RegexProgress.Match(mProgressLine.Value);
                    if (mProgressVal.Success)
                    {
                        TimeSpan progressSeconds;
                        long duration = ProgressDuration.Ticks;
                        if (duration > 0
                            && TimeSpan.TryParse(mProgressVal.Value.Trim(), CultureInfo.InvariantCulture, out progressSeconds))
                            Progress = (int)((progressSeconds.Ticks * 100) / duration);
                    }
                }
                else
                    AddOutputMessage(outLine.Data);
            }
        }

    }
}
