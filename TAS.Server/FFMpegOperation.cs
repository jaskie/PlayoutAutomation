using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace TAS.Server
{
    public abstract class FFMpegOperation: FileOperation
    {
        const string _ffExe = "ffmpeg.exe";
        const string _lProgressPattern = "time=" + @"\d\d:\d\d:\d\d\.?\d*";
        const string _progressPattern = @"\d\d:\d\d:\d\d\.?\d*";
        protected readonly Regex _regexlProgress = new Regex(_lProgressPattern, RegexOptions.None);
        protected readonly Regex _regexProgress = new Regex(_progressPattern, RegexOptions.None);
        protected TimeSpan _progressDuration;
        
        protected bool RunProcess(string parameters)
        {
            //create a process info
            ProcessStartInfo oInfo = new ProcessStartInfo(_ffExe, parameters);
            oInfo.UseShellExecute = false;
            oInfo.CreateNoWindow = true;
            oInfo.RedirectStandardError = true;

            //try the process
            Debug.WriteLine(parameters, "Starting ffmpeg:");
            AddOutputMessage($"ffmpeg.exe {parameters}");
            try
            {
                using (Process _procFFmpeg = Process.Start(oInfo))
                {
                    _procFFmpeg.ErrorDataReceived += ProcOutputHandler;
                    _procFFmpeg.BeginErrorReadLine();
                    bool finished = false;
                    while (!(Aborted || finished))
                        finished = _procFFmpeg.WaitForExit(1000);
                    if (Aborted)
                    {
                        _procFFmpeg.Kill();
                        Thread.Sleep(1000);
                        Media destMedia = DestMedia as Media;
                        if (destMedia != null)
                            System.IO.File.Delete(destMedia.FullPath);
                        Debug.WriteLine(this, "Aborted");
                    }
                    return finished && (_procFFmpeg.ExitCode == 0);
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
                Match mProgressLine = _regexlProgress.Match(outLine.Data);
                if (mProgressLine.Success)
                {
                    Match mProgressVal = _regexProgress.Match(mProgressLine.Value);
                    if (mProgressVal.Success)
                    {
                        TimeSpan progressSeconds;
                        long duration = _progressDuration.Ticks;
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
