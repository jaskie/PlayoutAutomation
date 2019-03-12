using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TAS.Server.MediaOperation
{
    public class FFMpegHelper
    {
        private const string FFmpegExe = "ffmpeg.exe";
        private const string LProgressPattern = "time=" + @"\d\d:\d\d:\d\d\.?\d*";
        private const string ProgressPattern = @"\d\d:\d\d:\d\d\.?\d*";
        private readonly FileOperationBase _fileOperation;
        private readonly TimeSpan _progressDuration;

        public FFMpegHelper(FileOperationBase fileOperation, TimeSpan progressDuration)
        {
            _fileOperation = fileOperation;
            _progressDuration = progressDuration;
        }

        protected readonly Regex RegexlProgress = new Regex(LProgressPattern, RegexOptions.None);
        protected readonly Regex RegexProgress = new Regex(ProgressPattern, RegexOptions.None);
       
        public async Task<bool> RunProcess(string parameters)
        {
            //create a process info
            var oInfo = new ProcessStartInfo(FFmpegExe, parameters)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };

            //try the process
            Debug.WriteLine(parameters, "Starting ffmpeg:");
            _fileOperation.AddOutputMessage($"ffmpeg.exe {parameters}");
            return await Task.Run(() =>
            {
                try
                {
                    using (var procFFmpeg = Process.Start(oInfo))
                    {
                        if (procFFmpeg == null)
                            return false;
                        procFFmpeg.PriorityClass = ProcessPriorityClass.BelowNormal;
                        procFFmpeg.ErrorDataReceived += ProcOutputHandler;
                        procFFmpeg.BeginErrorReadLine();
                        var finished = false;
                        while (!(_fileOperation.IsAborted || finished))
                            finished = procFFmpeg.WaitForExit(1000);
                        if (!_fileOperation.IsAborted)
                            return finished && procFFmpeg.ExitCode == 0;
                        procFFmpeg.Kill();
                        Thread.Sleep(1000);
                        Debug.WriteLine(this, "Aborted");
                        return finished && procFFmpeg.ExitCode == 0;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message, "Error running FFmpeg process");
                    _fileOperation.AddOutputMessage(e.ToString());
                    return false;
                }
            });
        }

        public event DataReceivedEventHandler DataReceived;

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
                var duration = _progressDuration.Ticks;
                if (duration > 0
                    && TimeSpan.TryParse(mProgressVal.Value.Trim(), CultureInfo.InvariantCulture,
                        out var progressSeconds))
                    _fileOperation.Progress = (int) ((progressSeconds.Ticks * 100) / duration);
            }
            else
            {
                _fileOperation.AddOutputMessage(outLine.Data);
                if (!string.IsNullOrEmpty(outLine.Data)
                    && outLine.Data.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                    _fileOperation.AddWarningMessage($"FFmpeg error: {outLine.Data}");
                DataReceived?.Invoke(this, outLine);
            }
        }

        
    }
}
