using MediaInfoLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TAS.Server;

namespace TAS.Server
{
    class LoudnessOperation : FileOperation
    {

        private static readonly string _ffExe = "ffmpeg.exe";
        private static readonly string lLufsPattern = @"    I:\s*-?\d*\.?\d* LUFS";
        private static readonly string lPeakPattern = @"    Peak:\s*-?\d*\.?\d* dBFS";
        private static readonly string LufsPattern = @"-?\d+\.\d";
        private static readonly string lProgressPattern = @" t: \d*\.?\d*";
        private static readonly string ProgressPattern = @"\d+\.?\d*";
        private static readonly Regex _regexlLufs = new Regex(lLufsPattern, RegexOptions.None);
        private static readonly Regex _regexlPeak = new Regex(lPeakPattern, RegexOptions.None);
        private static readonly Regex _regexlProgress = new Regex(lProgressPattern, RegexOptions.None);
        private static readonly Regex _regexProgress = new Regex(ProgressPattern, RegexOptions.None);

        private decimal _loudness = 0;
        private decimal _samplePeak = decimal.MinValue;
        private bool _loudnessMeasured = false;
        private bool _samplePeakMeasured = false;

        public LoudnessOperation()
        {
            Kind = TFileOperationKind.Loudness;
        }
        

        private bool RunProcess(string parameters)
        {
            ProcessStartInfo oInfo = new ProcessStartInfo(LoudnessOperation._ffExe, parameters);
            oInfo.UseShellExecute = false;
            oInfo.CreateNoWindow = true;
            oInfo.RedirectStandardError = true;
            Debug.WriteLine(parameters, "Starting ffmpeg with parameters");
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
                        Debug.WriteLine(this, "Aborted");
                    }
                    return finished && (_procFFmpeg.ExitCode == 0);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message, "Error running FFmpeg process");
                return false;
            }

        }

        internal override bool Do()
        {
            if (Kind == TFileOperationKind.Loudness)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                try
                {

                    bool success = false;
                    if (SourceMedia.Directory.AccessType != TDirectoryAccessType.Direct)
                        using (TempMedia _localSourceMedia = FileManager.TempDirectory.Get(SourceMedia))
                        {
                            if (SourceMedia.CopyMediaTo(_localSourceMedia, ref _aborted))
                            {
                                success = _do(_localSourceMedia);
                                if (!success)
                                    TryCount--;
                                return success;
                            }
                            else
                                return false;
                        }

                    else
                    {
                        success = _do(SourceMedia);
                        if (!success)
                            TryCount--;
                        return success;
                    }
                }
                catch
                {
                    TryCount--;
                    return false;
                }
            }
            else
                return base.Do();
        }

        private bool _do(Media inputMedia)
        {
            Debug.WriteLine(this, "Loudness operation started");
            string Params = string.Format("-nostats -i \"{0}\" -ss {1} -t {2} -filter_complex ebur128=peak=sample -f null -", inputMedia.FullPath, inputMedia.TCPlay-inputMedia.TCStart, inputMedia.DurationPlay);

            if (RunProcess(Params))
            {
                Debug.WriteLine(this, "Loudness operation succeed");
                OperationStatus = FileOperationStatus.Finished;
                return true;
            }
            Debug.WriteLine("FFmpeg rewraper Do(): Failed for {0}. Command line was {1}", (object)SourceMedia, Params);
            return false;
        }

        private void ProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the process command output. 
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                Match lineMatch = _regexlProgress.Match(outLine.Data);
                if (lineMatch.Success)
                {
                    Match valueMatch = _regexProgress.Match(lineMatch.Value);
                    if (valueMatch.Success)
                    {
                        double totalSeconds = SourceMedia.Duration.TotalSeconds;
                        double currentPos;
                        if (totalSeconds != 0
                            && double.TryParse(valueMatch.Value.Trim(), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out currentPos))
                            Progress = (int)((currentPos * 100) / totalSeconds);
                    }
                }
                else
                {
                    if (!_loudnessMeasured)
                    {
                        Match luFSLineMatch = _regexlLufs.Match(outLine.Data);
                        if (luFSLineMatch.Success)
                        {
                            Regex _regexLufs = new Regex(LufsPattern, RegexOptions.None);
                            Match valueMatch = _regexLufs.Match(luFSLineMatch.Value);
                            if (valueMatch.Success)
                                _loudnessMeasured = (decimal.TryParse(valueMatch.Value.Trim(), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out _loudness));
                        }
                    }
                    if (!_samplePeakMeasured)
                    {
                        Match truePeakLineMatch = _regexlPeak.Match(outLine.Data);
                        if (truePeakLineMatch.Success)
                        {
                            Regex _regexLufs = new Regex(LufsPattern, RegexOptions.None);
                            Match valueMatch = _regexLufs.Match(truePeakLineMatch.Value);
                            if (valueMatch.Success)
                                _samplePeakMeasured = (decimal.TryParse(valueMatch.Value.Trim(), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out _samplePeak));
                        }
                    }
                    if (_samplePeakMeasured && _loudnessMeasured)
                    {
                        var refLoudness = System.Windows.Application.Current.Properties["VolumeReferenceLoudness"];
                        SourceMedia.AudioLevelIntegrated = _loudness;
                        SourceMedia.AudioLevelPeak = _samplePeak;
                        SourceMedia.AudioVolume = -Math.Max(_loudness - ((refLoudness is decimal) ? (decimal)refLoudness : -23.0m), _samplePeak); // prevents automatic amplification over 0dBFS
                        if (SourceMedia is PersistentMedia)
                            (SourceMedia as PersistentMedia).Save();
                    }
                }
            }
        }
    }
}
