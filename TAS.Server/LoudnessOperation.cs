using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TAS.Common;
using TAS.Server;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    class LoudnessOperation : FFMpegOperation, ILoudnessOperation
    {

        private static readonly string lLufsPattern = @"    I:\s*-?\d*\.?\d* LUFS";
        private static readonly string lPeakPattern = @"    Peak:\s*-?\d*\.?\d* dBFS";
        private static readonly string LufsPattern = @"-?\d+\.\d";
        private static readonly string lProgressPattern = @" t: \d*\.?\d*";
        private static readonly string ProgressPattern = @"\d+\.?\d*";
        private static readonly Regex _regexlLufs = new Regex(lLufsPattern, RegexOptions.None);
        private static readonly Regex _regexlPeak = new Regex(lPeakPattern, RegexOptions.None);
        private static readonly Regex _regexLoudnesslProgress = new Regex(lProgressPattern, RegexOptions.None);
        private static readonly Regex _regexLoudnessProgress = new Regex(ProgressPattern, RegexOptions.None);

        private decimal _loudness = 0;
        private decimal _samplePeak = decimal.MinValue;
        private bool _loudnessMeasured = false;
        private bool _samplePeakMeasured = false;

        public LoudnessOperation()
        {
            Kind = TFileOperationKind.Loudness;
        }

        public event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured; // will not save to Media object if not null
        [JsonProperty]
        public TimeSpan MeasureStart { get; set; }
        [JsonProperty]
        public TimeSpan MeasureDuration { get; set; }

        public override bool Do()
        {
            if (Kind == TFileOperationKind.Loudness)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                try
                {

                    bool success = false;
                    Media sourceMedia = SourceMedia as Media;
                    if (sourceMedia == null)
                        throw new ArgumentException("LoudnessOperation: SourceMedia is not of type Media");
                    if (sourceMedia.Directory is IngestDirectory && ((IngestDirectory)sourceMedia.Directory).AccessType != TDirectoryAccessType.Direct)
                        using (TempMedia _localSourceMedia = (TempMedia)Owner.TempDirectory.CreateMedia(sourceMedia))
                        {
                            if (sourceMedia.CopyMediaTo(_localSourceMedia, ref _aborted))
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
                        success = _do(sourceMedia);
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

        private bool _do(IMedia inputMedia)
        {
            Debug.WriteLine(this, "Loudness operation started");
            string Params = string.Format("-nostats -i \"{0}\" -ss {1} -t {2} -filter_complex ebur128=peak=sample -f null -", inputMedia.FullPath, MeasureStart, MeasureDuration == TimeSpan.Zero ? inputMedia.DurationPlay: MeasureDuration);

            if (RunProcess(Params))
            {
                Debug.WriteLine(this, "Loudness operation succeed");
                OperationStatus = FileOperationStatus.Finished;
                return true;
            }
            Debug.WriteLine("FFmpeg rewraper Do(): Failed for {0}. Command line was {1}", (object)SourceMedia, Params);
            return false;
        }

        protected override void ProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the process command output. 
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                Match lineMatch = _regexLoudnesslProgress.Match(outLine.Data);
                if (lineMatch.Success)
                {
                    Match valueMatch = _regexLoudnessProgress.Match(lineMatch.Value);
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
                    Match luFSLineMatch = _regexlLufs.Match(outLine.Data);
                    if (luFSLineMatch.Success)
                    {
                        Regex _regexLufs = new Regex(LufsPattern, RegexOptions.None);
                        Match valueMatch = _regexLufs.Match(luFSLineMatch.Value);
                        if (valueMatch.Success)
                            _loudnessMeasured = (decimal.TryParse(valueMatch.Value.Trim(), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out _loudness));
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
                            else
                                _loudnessMeasured = false;
                        }
                    }
                    if (_samplePeakMeasured && _loudnessMeasured)
                    {
                        var refLoudness = this.Owner.VolumeReferenceLoudness;
                        decimal volume = -Math.Max(_loudness - refLoudness, _samplePeak); // prevents automatic amplification over 0dBFS
                        var h = AudioVolumeMeasured;
                        if (h == null)
                        {
                            SourceMedia.AudioLevelIntegrated = _loudness;
                            SourceMedia.AudioLevelPeak = _samplePeak;
                            SourceMedia.AudioVolume = volume;
                            if (SourceMedia is PersistentMedia)
                                (SourceMedia as PersistentMedia).Save();
                        }
                        else
                            h(this, new AudioVolumeEventArgs(volume));
                    }
                    AddOutputMessage(outLine.Data);
                }
            }
        }
    }

}
