using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Media;

namespace TAS.Server
{
    public class LoudnessOperation : FFMpegOperation, ILoudnessOperation
    {

        private static readonly string lLufsPattern = @"    I:\s*-?\d*\.?\d* LUFS";
        private static readonly string lPeakPattern = @"    Peak:\s*-?\d*\.?\d* dBFS";
        private static readonly string LufsPattern = @"-?\d+\.\d";
        private static readonly string lProgressPattern = @" t: \d*\.?\d*";
        private static readonly string ProgressPattern = @"\d+\.?\d*";
        private static readonly Regex RegexlLufs = new Regex(lLufsPattern, RegexOptions.None);
        private static readonly Regex RegexlPeak = new Regex(lPeakPattern, RegexOptions.None);
        private static readonly Regex RegexLoudnesslProgress = new Regex(lProgressPattern, RegexOptions.None);
        private static readonly Regex RegexLoudnessProgress = new Regex(ProgressPattern, RegexOptions.None);

        private double _loudness;
        private double _samplePeak = double.MinValue;
        private bool _loudnessMeasured;
        private bool _samplePeakMeasured;

        public LoudnessOperation(FileManager ownerFileManager) : base(ownerFileManager)
        {
            Kind = TFileOperationKind.Loudness;
        }

        public event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured; // will not save to Media object if not null

        [JsonProperty]
        public TimeSpan MeasureStart { get; set; }

        [JsonProperty]
        public TimeSpan MeasureDuration { get; set; }

        internal override bool Execute()
        {
            if (Kind != TFileOperationKind.Loudness)
                return base.Execute();
            StartTime = DateTime.UtcNow;
            OperationStatus = FileOperationStatus.InProgress;
            try
            {
                bool success;
                if (!(Source is MediaBase source))
                    throw new ArgumentException("LoudnessOperation: Source is not of type MediaBase");
                if (source.Directory is IngestDirectory directory &&
                    directory.AccessType != TDirectoryAccessType.Direct)
                    using (var localSourceMedia = (TempMedia) OwnerFileManager.TempDirectory.CreateMedia(source))
                    {
                        if (!source.CopyMediaTo(localSourceMedia, ref Aborted))
                            return false;
                        success = InternalExecute(localSourceMedia);
                        if (!success)
                            TryCount--;
                        return success;
                    }

                success = InternalExecute(source);
                if (!success)
                    TryCount--;
                return success;
            }
            catch
            {
                TryCount--;
                return false;
            }
        }

        private bool InternalExecute(MediaBase inputMedia)
        {
            Debug.WriteLine(this, "Loudness operation started");
            string Params = $"-nostats -i \"{inputMedia.FullPath}\" -ss {MeasureStart} -t {(MeasureDuration == TimeSpan.Zero ? inputMedia.DurationPlay : MeasureDuration)} -filter_complex ebur128=peak=sample -f null -";

            if (RunProcess(Params))
            {
                Debug.WriteLine(this, "Loudness operation succeed");
                OperationStatus = FileOperationStatus.Finished;
                return true;
            }
            Debug.WriteLine("FFmpeg rewraper Execute(): Failed for {0}. Command line was {1}", Source, Params);
            return false;
        }

        protected override void ProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the process command output. 
            if (string.IsNullOrEmpty(outLine.Data))
                return;
            var lineMatch = RegexLoudnesslProgress.Match(outLine.Data);
            if (lineMatch.Success)
            {
                var valueMatch = RegexLoudnessProgress.Match(lineMatch.Value);
                if (!valueMatch.Success)
                    return;
                var totalSeconds = Source.Duration.TotalSeconds;
                if (Math.Abs(totalSeconds) > double.Epsilon
                    && double.TryParse(valueMatch.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var currentPos))
                    Progress = (int)((currentPos * 100) / totalSeconds);
            }
            else
            {
                var luFsLineMatch = RegexlLufs.Match(outLine.Data);
                if (luFsLineMatch.Success)
                {
                    var regexLufs = new Regex(LufsPattern, RegexOptions.None);
                    var valueMatch = regexLufs.Match(luFsLineMatch.Value);
                    if (valueMatch.Success)
                        _loudnessMeasured = (double.TryParse(valueMatch.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _loudness));
                }
                if (!_samplePeakMeasured)
                {
                    var truePeakLineMatch = RegexlPeak.Match(outLine.Data);
                    if (truePeakLineMatch.Success)
                    {
                        var regexLufs = new Regex(LufsPattern, RegexOptions.None);
                        var valueMatch = regexLufs.Match(truePeakLineMatch.Value);
                        if (valueMatch.Success)
                            _samplePeakMeasured = double.TryParse(valueMatch.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _samplePeak);
                        else
                            _loudnessMeasured = false;
                    }
                }
                if (_samplePeakMeasured && _loudnessMeasured)
                {
                    var volume = -Math.Max(_loudness - OwnerFileManager.ReferenceLoudness, _samplePeak); // prevents automatic amplification over 0dBFS
                    var h = AudioVolumeMeasured;
                    if (h == null)
                    {
                        Source.AudioLevelIntegrated = _loudness;
                        Source.AudioLevelPeak = _samplePeak;
                        Source.AudioVolume = volume;
                        (Source as PersistentMedia)?.Save();
                    }
                    else
                        h(this, new AudioVolumeEventArgs(volume));
                }
                AddOutputMessage(outLine.Data);
            }
        }
    }

}
