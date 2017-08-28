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
    class LoudnessOperation : FFMpegOperation, ILoudnessOperation
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

        private decimal _loudness;
        private decimal _samplePeak = decimal.MinValue;
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
            if (Kind == TFileOperationKind.Loudness)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                try
                {

                    bool success;
                    if (Source == null)
                        throw new ArgumentException("LoudnessOperation: Source is not of type Media");
                    if (Source.Directory is IngestDirectory && ((IngestDirectory)Source.Directory).AccessType != TDirectoryAccessType.Direct)
                        using (TempMedia localSourceMedia = (TempMedia)OwnerFileManager.TempDirectory.CreateMedia(Source))
                        {
                            if (SourceMedia.CopyMediaTo(localSourceMedia, ref Aborted))
                            {
                                success = InternalExecute(localSourceMedia);
                                if (!success)
                                    TryCount--;
                                return success;
                            }
                            return false;
                        }

                    success = InternalExecute(SourceMedia);
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
            return base.Execute();
        }

        private bool InternalExecute(MediaBase inputMedia)
        {
            Debug.WriteLine(this, "Loudness operation started");
            string Params = string.Format("-nostats -i \"{0}\" -ss {1} -t {2} -filter_complex ebur128=peak=sample -f null -", inputMedia.FullPath, MeasureStart, MeasureDuration == TimeSpan.Zero ? inputMedia.DurationPlay: MeasureDuration);

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
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                Match lineMatch = RegexLoudnesslProgress.Match(outLine.Data);
                if (lineMatch.Success)
                {
                    Match valueMatch = RegexLoudnessProgress.Match(lineMatch.Value);
                    if (valueMatch.Success)
                    {
                        double totalSeconds = Source.Duration.TotalSeconds;
                        double currentPos;
                        if (totalSeconds != 0
                            && double.TryParse(valueMatch.Value.Trim(), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out currentPos))
                            Progress = (int)((currentPos * 100) / totalSeconds);
                    }
                }
                else
                {
                    Match luFsLineMatch = RegexlLufs.Match(outLine.Data);
                    if (luFsLineMatch.Success)
                    {
                        Regex regexLufs = new Regex(LufsPattern, RegexOptions.None);
                        Match valueMatch = regexLufs.Match(luFsLineMatch.Value);
                        if (valueMatch.Success)
                            _loudnessMeasured = (decimal.TryParse(valueMatch.Value.Trim(), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out _loudness));
                    }
                    if (!_samplePeakMeasured)
                    {
                        Match truePeakLineMatch = RegexlPeak.Match(outLine.Data);
                        if (truePeakLineMatch.Success)
                        {
                            Regex regexLufs = new Regex(LufsPattern, RegexOptions.None);
                            Match valueMatch = regexLufs.Match(truePeakLineMatch.Value);
                            if (valueMatch.Success)
                                _samplePeakMeasured = (decimal.TryParse(valueMatch.Value.Trim(), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out _samplePeak));
                            else
                                _loudnessMeasured = false;
                        }
                    }
                    if (_samplePeakMeasured && _loudnessMeasured)
                    {
                        decimal volume = -Math.Max(_loudness - OwnerFileManager.ReferenceLoudness, _samplePeak); // prevents automatic amplification over 0dBFS
                        var h = AudioVolumeMeasured;
                        if (h == null)
                        {
                            Source.AudioLevelIntegrated = _loudness;
                            Source.AudioLevelPeak = _samplePeak;
                            Source.AudioVolume = volume;
                            if (Source is PersistentMedia)
                                (Source as PersistentMedia).Save();
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
